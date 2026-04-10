using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using PhoStudioMVC.Data;
using PhoStudioMVC.Models.Entities;
using PhoStudioMVC.Models.Enums;

namespace PhoStudioMVC.Controllers
{
    [Route("Account/[action]")]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToRoleDefaultPage(User);
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, bool rememberMe = false)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập email và mật khẩu.";
                return View();
            }

            var hash = HashPassword(password);
            // Try email first, then username as fallback
            var user = _context.Users.FirstOrDefault(u => 
                (u.Email == username || u.Username == username) && 
                u.PasswordHash == hash);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Email hoặc mật khẩu không đúng";
                return View();
            }

            // ── CHECK IsActive ─────────────────────────────────
            if (!user.IsActive)
            {
                TempData["ErrorMessage"] = "Tài khoản đã bị khóa. Liên hệ quản trị viên.";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("FullName", user.FullName ?? user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTime.UtcNow.AddDays(30) : (DateTime?)null
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Use the just-issued principal (User isn't updated until next request)
            return RedirectToRoleDefaultPage(new ClaimsPrincipal(claimsIdentity));
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string username, string fullName, string phone, string email, string password, string confirmPassword)
        {
            // Validate Confirm Password
            if (password != confirmPassword)
            {
                TempData["RegisterError"] = "Mật khẩu xác nhận không khớp!";
                return View("Login");
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fullName))
            {
                TempData["RegisterError"] = "Vui lòng điền đầy đủ các trường bắt buộc.";
                return View("Login");
            }

            if (_context.Users.Any(u => u.Username == username))
            {
                TempData["RegisterError"] = "Tên đăng nhập đã tồn tại!";
                return View("Login");
            }

            if (!string.IsNullOrWhiteSpace(email) && _context.Users.Any(u => u.Email == email))
            {
                TempData["RegisterError"] = "Email đã được sử dụng!";
                return View("Login");
            }

            var user = new ApplicationUser
            {
                Username = username,
                FullName = fullName,
                Phone = phone,
                Email = email,
                PasswordHash = HashPassword(password),
                Role = UserRole.Customer,
                IsActive = true
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Tạo tài khoản thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToRoleDefaultPage(ClaimsPrincipal user)
        {
            if (user.IsInRole("Admin")) return RedirectToAction("Dashboard", "Admin");
            if (user.IsInRole("Photographer")) return RedirectToAction("Schedule", "Photographer");
            if (user.IsInRole("Client") || user.IsInRole("Customer")) return RedirectToAction("Index", "Home");
            return RedirectToAction("Index", "Home");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
