using Microsoft.EntityFrameworkCore;
using PhoStudioMVC.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using PhoStudioMVC.Services;
using PhoStudioMVC.Models.Entities;
using PhoStudioMVC.Models.Enums;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.WebEncoders;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

builder.Services.Configure<WebEncoderOptions>(options => 
{
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqliteOptions => sqliteOptions.CommandTimeout(30)
    ));

builder.Services.AddHostedService<BookingLockCleanupService>();

// Health checks (NFR — Availability)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    })
    .AddJwtBearer("ApiBearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register new services
builder.Services.AddScoped<QRCodeService>();
builder.Services.AddScoped<CouponService>();
builder.Services.AddScoped<LoyaltyService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<RefundService>();

var app = builder.Build();

// ── Pre-flight: wake up the database before Kestrel accepts traffic ──────────
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

const int maxRetries = 5;
const int retryDelaySeconds = 3;

for (int attempt = 1; attempt <= maxRetries; attempt++)
{
    try
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        startupLogger.LogInformation("DB wake-up attempt {Attempt}/{Max}...", attempt, maxRetries);

        var canConnect = await db.Database.CanConnectAsync();
        if (!canConnect)
        {
            startupLogger.LogWarning("Cannot connect to database on attempt {Attempt}. Retrying in {Delay}s...", attempt, retryDelaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
            continue;
        }

        // Run migrations (idempotent — safe to call every startup)
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        startupLogger.LogInformation("Database is ready.");

        // ── Seed demo accounts & service packages ────────────────────────────
        var admin = db.Users.FirstOrDefault(u => u.Email == "admin@phostudio.vn");
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                Username = "admin",
                FullName = "System Admin",
                Email = "admin@phostudio.vn",
                Phone = "0900000001",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(admin);
        }
        admin.Username = "admin";
        admin.Role = UserRole.Admin;
        admin.IsActive = true;
        admin.PasswordHash = HashPassword("Admin@123");
        if (admin.CreatedAt == default) admin.CreatedAt = DateTime.UtcNow;

        var photographer = db.Users.FirstOrDefault(u => u.Username == "photographer01" || u.Email == "photo1@phostudio.vn");
        if (photographer == null)
        {
            photographer = new ApplicationUser();
            db.Users.Add(photographer);
        }
        photographer.Username = "photographer01";
        photographer.FullName = "Photographer Demo";
        photographer.Email = "photo1@phostudio.vn";
        photographer.Phone = "0900000002";
        photographer.PasswordHash = HashPassword("Photographer@123");
        photographer.Role = UserRole.Photographer;
        photographer.IsActive = true;
        if (photographer.CreatedAt == default) photographer.CreatedAt = DateTime.UtcNow;

        var customer = db.Users.FirstOrDefault(u => u.Username == "customer01" || u.Email == "customer1@phostudio.vn");
        if (customer == null)
        {
            customer = new ApplicationUser();
            db.Users.Add(customer);
        }
        customer.Username = "customer01";
        customer.FullName = "Customer Demo";
        customer.Email = "customer1@phostudio.vn";
        customer.Phone = "0900000003";
        customer.PasswordHash = HashPassword("Customer@123");
        customer.Role = UserRole.Customer;
        customer.IsActive = true;
        if (customer.CreatedAt == default) customer.CreatedAt = DateTime.UtcNow;

        var smk = db.ServicePackages.FirstOrDefault(s => s.Name == "Svc Smoke d057718f");
        if (smk != null) { smk.Name = "Gói Khói Nghệ Thuật"; smk.Price = 850_000m; }

        UpsertService(db, 1, "Gói Kỷ Yếu",   2_500_000m, 120, "Gói chụp kỷ yếu chuẩn studio");
        UpsertService(db, 2, "Gói Ảnh Cưới",  8_000_000m, 240, "Gói ảnh cưới cao cấp 240 phút");
        UpsertService(db, 3, "Gói Nàng Thơ",  3_500_000m,  90, "Gói chân dung nàng thơ 90 phút");

        await db.SaveChangesAsync();

        break; // success — exit retry loop
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "DB startup failed on attempt {Attempt}/{Max}.", attempt, maxRetries);
        if (attempt == maxRetries)
        {
            startupLogger.LogCritical("All {Max} DB startup attempts failed. Application will start without guaranteed DB state.", maxRetries);
            break;
        }
        await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PhoStudio API v1"));
}

// Only redirect to HTTPS in production — avoids warning in local dev without HTTPS cert
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

static string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    var builder = new StringBuilder();
    foreach (var b in bytes)
    {
        builder.Append(b.ToString("x2"));
    }

    return builder.ToString();
}

static void UpsertService(ApplicationDbContext db, int id, string name, decimal price, int durationMinutes, string description)
{
    var service = db.ServicePackages.IgnoreQueryFilters().FirstOrDefault(s => s.Id == id);
    if (service == null)
    {
        db.ServicePackages.Add(new ServicePackage
        {
            Id = id,
            Name = name,
            Price = price,
            DurationMinutes = durationMinutes,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        return;
    }

    service.Name = name;
    service.Price = price;
    service.DurationMinutes = durationMinutes;
    service.Description = description;
    service.IsActive = true;
    if (service.CreatedAt == default) service.CreatedAt = DateTime.UtcNow;
}

public partial class Program { }
