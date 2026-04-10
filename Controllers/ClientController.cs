using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoStudioMVC.Data;
using PhoStudioMVC.Models.Entities;
using PhoStudioMVC.Models.Enums;
using PhoStudioMVC.Utils;
using System.Security.Claims;

namespace PhoStudioMVC.Controllers
{
    [Authorize(Roles = "Client,Customer")]
    [Route("Client/[action]")]
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<ClientController> _logger;
        private static DateTime VnNow => TimeHelper.VnNow;

        public ClientController(ApplicationDbContext context, IMemoryCache memoryCache, ILogger<ClientController> logger)
        {
            _context = context;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        [HttpGet("~/Client")]
        [HttpGet]
        public IActionResult Dashboard() => RedirectToAction(nameof(MyAlbums));

        [HttpGet]
        public async Task<IActionResult> MyAlbums()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bookings = await _context.Bookings
                .Include(b => b.ServicePackage)
                .Include(b => b.CloudAlbum)
                .Include(b => b.AlbumAssets)
                .Include(b => b.Photographer)
                .Where(b => b.ClientId == userId && b.Status != BookingStatus.Cancelled)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View("Dashboard", bookings);
        }

        [HttpGet("{bookingId}")]
        public async Task<IActionResult> ViewAlbum(string bookingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.ServicePackage)
                .Include(b => b.CloudAlbum)
                .Include(b => b.AlbumAssets)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.ClientId == userId);

            if (booking == null) return NotFound();

            var cloudAlbum = booking.CloudAlbum;
            
            if (cloudAlbum == null) 
            {
                TempData["Error"] = "Booking này chưa được trả ảnh.";
                return RedirectToAction(nameof(MyAlbums));
            }

            if (VnNow > cloudAlbum.ExpiryDate)
            {
                TempData["Error"] = "Đường dẫn Cloud Album đã bị khóa dựa theo chính sách 30 ngày bảo lưu của Studio.";
                return RedirectToAction(nameof(MyAlbums));
            }

            if (cloudAlbum.IsVerifiedByClient)
            {
                return RedirectToAction(nameof(AlbumContent), new { bookingId });
            }

            ViewBag.AlbumId = cloudAlbum.Id;
            return View(booking);
        }

        [HttpGet("{bookingId}")]
        public async Task<IActionResult> AlbumContent(string bookingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.ServicePackage)
                .Include(b => b.CloudAlbum)
                .Include(b => b.AlbumAssets)
                .Include(b => b.RetouchRequests)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.ClientId == userId);
            if (booking == null) return NotFound();

            var cloudAlbum = booking.CloudAlbum;
            if (cloudAlbum == null)
            {
                TempData["Error"] = "Booking này chưa được trả ảnh.";
                return RedirectToAction(nameof(MyAlbums));
            }
            if (VnNow > cloudAlbum.ExpiryDate)
            {
                TempData["Error"] = "Album đã hết hạn.";
                return RedirectToAction(nameof(MyAlbums));
            }
            if (!cloudAlbum.IsVerifiedByClient)
            {
                TempData["Error"] = "Vui lòng xác thực OTP trước khi xem album.";
                return RedirectToAction(nameof(ViewAlbum), new { bookingId });
            }

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int assetId, string bookingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            if (IsRateLimited($"toggle-favorite:{userId}", maxRequests: 30, window: TimeSpan.FromSeconds(10), out var retryIn))
            {
                TempData["Warning"] = $"Bạn thao tác quá nhanh. Vui lòng thử lại sau khoảng {retryIn} giây.";
                return RedirectToAction(nameof(AlbumContent), new { bookingId });
            }

            var asset = await _context.AlbumAssets
                .Include(a => a.Booking)
                .FirstOrDefaultAsync(a => a.Id == assetId && a.BookingId == bookingId);
            if (asset == null || asset.Booking == null || asset.Booking.ClientId != userId)
            {
                TempData["Error"] = "Không tìm thấy ảnh cần thao tác.";
                return RedirectToAction(nameof(MyAlbums));
            }

            var cloudAlbum = await _context.CloudAlbums
                .FirstOrDefaultAsync(a => a.BookingId == bookingId);
            if (cloudAlbum == null || VnNow > cloudAlbum.ExpiryDate || !cloudAlbum.IsVerifiedByClient)
            {
                TempData["Error"] = "Phiên truy cập album không hợp lệ. Vui lòng xác thực OTP lại.";
                return RedirectToAction(nameof(ViewAlbum), new { bookingId });
            }

            asset.IsFavorite = !asset.IsFavorite;
            asset.FavoritedAt = asset.IsFavorite ? VnNow : null;
            await _context.SaveChangesAsync();

            TempData["Success"] = asset.IsFavorite ? "Đã thêm ảnh vào danh sách yêu thích." : "Đã bỏ ảnh khỏi danh sách yêu thích.";
            return RedirectToAction(nameof(AlbumContent), new { bookingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFavoriteSelection(string bookingId, string? note)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            if (IsRateLimited($"submit-favorite:{userId}", maxRequests: 2, window: TimeSpan.FromSeconds(12), out var retryIn))
            {
                TempData["Warning"] = $"Bạn vừa gửi quá nhanh. Vui lòng đợi {retryIn} giây rồi thử lại.";
                return RedirectToAction(nameof(AlbumContent), new { bookingId });
            }

            var booking = await _context.Bookings
                .Include(b => b.AlbumAssets)
                .Include(b => b.RetouchRequests)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.ClientId == userId);

            if (booking == null)
            {
                TempData["Error"] = "Booking không tồn tại.";
                return RedirectToAction(nameof(MyAlbums));
            }

            var favorites = booking.AlbumAssets.Where(a => a.IsFavorite).ToList();
            if (!favorites.Any())
            {
                TempData["Error"] = "Bạn chưa chọn ảnh yêu thích nào để gửi studio.";
                return RedirectToAction(nameof(AlbumContent), new { bookingId });
            }

            var existingCount = booking.RetouchRequests.Count;
            if (existingCount >= 2)
            {
                TempData["Error"] = "Đã đạt giới hạn 2 lần yêu cầu retouch.";
                return RedirectToAction(nameof(AlbumContent), new { bookingId });
            }

            var photoRefs = string.Join(", ", favorites.Take(20).Select(f => f.FileName));
            var userNote = string.IsNullOrWhiteSpace(note) ? "Khách đã gửi danh sách ảnh yêu thích." : note.Trim();

            var request = new PhoStudioMVC.Models.Entities.RetouchRequest
            {
                BookingId = booking.Id,
                Note = $"{userNote} (Tổng ảnh đã chọn: {favorites.Count})",
                PhotoReference = photoRefs,
                RequestNumber = existingCount + 1,
                Status = RetouchStatus.Pending,
                CreatedAt = VnNow
            };

            _context.RetouchRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã gửi danh sách {favorites.Count} ảnh yêu thích cho studio.";
            return RedirectToAction(nameof(AlbumContent), new { bookingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOTP(string albumId, string otp)
        {
            var cloudAlbum = await _context.CloudAlbums.FindAsync(albumId);
            if (cloudAlbum == null || VnNow > cloudAlbum.ExpiryDate)
            {
                TempData["Error"] = "Album không tồn tại hoặc đã hết hạn.";
                return RedirectToAction(nameof(MyAlbums));
            }

            if (string.Compare(cloudAlbum.SecureOTP, otp, StringComparison.Ordinal) != 0)
            {
                TempData["Error"] = "Mã OTP không chính xác. Vui lòng kiểm tra lại.";
                return RedirectToAction("ViewAlbum", new { bookingId = cloudAlbum.BookingId });
            }

            if (!cloudAlbum.IsVerifiedByClient)
            {
                cloudAlbum.IsVerifiedByClient = true;
                cloudAlbum.FirstVerifiedAt ??= VnNow;
                await _context.SaveChangesAsync();
            }

            TempData["VerifiedAlbumUrl"] = Url.Action("AlbumContent", new { bookingId = cloudAlbum.BookingId });
            TempData["OtpSuccess"] = "Xác thực OTP thành công. Album đã được mở khóa.";
            return RedirectToAction("ViewAlbum", new { bookingId = cloudAlbum.BookingId });
        }

        private bool IsRateLimited(string key, int maxRequests, TimeSpan window, out int retryInSeconds)
        {
            retryInSeconds = 0;
            var state = _memoryCache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = window;
                return new RateLimitState { Count = 0, WindowStart = VnNow };
            })!;

            lock (state.Sync)
            {
                if (VnNow - state.WindowStart > window)
                {
                    state.WindowStart = VnNow;
                    state.Count = 0;
                }
                if (state.Count >= maxRequests)
                {
                    retryInSeconds = Math.Max(1, (int)Math.Ceiling((window - (VnNow - state.WindowStart)).TotalSeconds));
                    return true;
                }
                state.Count++;
                return false;
            }
        }

        private sealed class RateLimitState
        {
            public int Count { get; set; }
            public DateTime WindowStart { get; set; }
            public object Sync { get; } = new();
        }
    }
}
