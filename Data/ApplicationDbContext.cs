using Microsoft.EntityFrameworkCore;
using PhoStudioMVC.Models.Entities;
using PhoStudioMVC.Models.Enums;

namespace PhoStudioMVC.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<ServicePackage> ServicePackages { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<CloudAlbum> CloudAlbums { get; set; }
        public DbSet<AlbumAsset> AlbumAssets { get; set; }
        public DbSet<FavoriteSelectionAudit> FavoriteSelectionAudits { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<RetouchRequest> RetouchRequests { get; set; }

        // New DbSets
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<BookingCoupon> BookingCoupons { get; set; }
        public DbSet<BookingReview> BookingReviews { get; set; }
        public DbSet<PhotographerRating> PhotographerRatings { get; set; }
        public DbSet<CustomerLoyalty> CustomerLoyalties { get; set; }
        public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }
        public DbSet<ReferralCode> ReferralCodes { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
        public DbSet<RefundRequest> RefundRequests { get; set; }
        public DbSet<RefundPolicy> RefundPolicies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Global query filter for soft-deleted packages
            // Use optional navigation on Booking side to avoid EF warning 10622
            modelBuilder.Entity<ServicePackage>()
                .HasQueryFilter(p => p.IsActive);

            // Decimal precision mapping
            modelBuilder.Entity<ServicePackage>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Booking>()
                .Property(b => b.DepositAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Booking>()
                .Property(b => b.CommissionAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasColumnType("decimal(18,2)");

            // Unique index is handled automatically by HasOne().WithOne()
            // modelBuilder.Entity<CloudAlbum>()
            //     .HasIndex(a => a.BookingId)
            //     .IsUnique();

            // Unique index: prevent double-booking same slot regardless of photographer assignment
            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.BookingDate, b.StartTime })
                .IsUnique()
                .HasFilter($"[{nameof(Booking.Status)}] <> {(int)BookingStatus.Cancelled}");

            // Unique index: prevent photographer assigned to 2 bookings at same slot
            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.BookingDate, b.StartTime, b.PhotographerId })
                .IsUnique()
                .HasFilter($"[{nameof(Booking.Status)}] <> {(int)BookingStatus.Cancelled} AND [{nameof(Booking.PhotographerId)}] IS NOT NULL");

            // Configure One-to-Many relationships for Bookings and Users (Client vs Photographer)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Client)
                .WithMany(u => u.ClientBookings)
                .HasForeignKey(b => b.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Photographer)
                .WithMany(u => u.PhotographerBookings)
                .HasForeignKey(b => b.PhotographerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Booking -> ServicePackage
            // IsOptional prevents EF warning 10622 caused by the global IsActive filter
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.ServicePackage)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.ServicePackageId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Booking -> CloudAlbum
            modelBuilder.Entity<CloudAlbum>()
                .HasOne(c => c.Booking)
                .WithOne(b => b.CloudAlbum)
                .HasForeignKey<CloudAlbum>(c => c.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AlbumAsset>()
                .HasOne(a => a.Booking)
                .WithMany(b => b.AlbumAssets)
                .HasForeignKey(a => a.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Booking)
                .WithMany(b => b.Transactions)
                .HasForeignKey(t => t.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RetouchRequest>()
                .HasOne(r => r.Booking)
                .WithMany(b => b.RetouchRequests)
                .HasForeignKey(r => r.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed Default Users
            modelBuilder.Entity<ApplicationUser>().HasData(
                new ApplicationUser
                {
                    Id = "admin_seed_1",
                    Username = "admin",
                    FullName = "System Administrator",
                    Email = "admin@phostudio.vn",
                    Phone = "0901123456",
                    PasswordHash = "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9", // Admin@123
                    Role = PhoStudioMVC.Models.Enums.UserRole.Admin,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1)
                },
                new ApplicationUser
                {
                    Id = "photo_seed_1",
                    Username = "photographer01",
                    FullName = "Photographer Demo",
                    Email = "photo1@phostudio.vn",
                    Phone = "0900000002",
                    PasswordHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020ca112706e90", // Photographer@123
                    Role = PhoStudioMVC.Models.Enums.UserRole.Photographer,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1)
                },
                new ApplicationUser
                {
                    Id = "customer_seed_1",
                    Username = "customer01",
                    FullName = "Customer Demo",
                    Email = "customer1@phostudio.vn",
                    Phone = "0900000003",
                    PasswordHash = "f2d81a021020614f08e82069fa9f7498c0d95b508f7ce19c6736736412e2c560", // Customer@123
                    Role = PhoStudioMVC.Models.Enums.UserRole.Customer,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1)
                }
            );

            // Seed Default Service Packages
            modelBuilder.Entity<ServicePackage>().HasData(
                new ServicePackage { Id = 1, Name = "Gói Chụp Ngoài Trời Cơ Bản", Price = 1500000m, Description = "Chụp hình kỷ yếu ngoại cảnh 1 buổi", IsActive = true, DurationMinutes = 120, CreatedAt = new DateTime(2024, 1, 1) },
                new ServicePackage { Id = 2, Name = "Gói Phóng Sự Cưới Premium", Price = 5000000m, Description = "Phóng sự cưới cao cấp nửa ngày", IsActive = true, DurationMinutes = 240, CreatedAt = new DateTime(2024, 1, 1) },
                new ServicePackage { Id = 3, Name = "Gói Chân Dung Studio", Price = 3000000m, Description = "Chụp chân dung nghệ thuật trong Studio", IsActive = true, DurationMinutes = 120, CreatedAt = new DateTime(2024, 1, 1) }
            );
        }
    }
}
