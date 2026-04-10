using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PhoStudioMVC.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PhoStudioMVC.Controllers.Api;

[Route("api/auth")]
[ApiController]
public class AuthApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public AuthApiController(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] ApiLoginRequest req)
    {
        var user = _db.Users.FirstOrDefault(u => u.Email == req.Email);
        if (user == null || !user.IsActive)
            return Unauthorized(new { error = "Email hoặc mật khẩu không đúng." });

        var hash = HashPassword(req.Password);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(user.PasswordHash),
                Encoding.UTF8.GetBytes(hash)))
        {
            return Unauthorized(new { error = "Email hoặc mật khẩu không đúng." });
        }

        var role = user.Role.ToString(); // Admin / Photographer / Client
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            fullName = user.FullName,
            role,
            userId = user.Id
        });
    }

    private static string HashPassword(string password)
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
}

public record ApiLoginRequest(string Email, string Password);

