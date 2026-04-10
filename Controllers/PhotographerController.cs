using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoStudioMVC.Data;
using PhoStudioMVC.Models.Entities;
using PhoStudioMVC.Models.Enums;
using PhoStudioMVC.Models.ViewModels;
using PhoStudioMVC.Utils;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IO;
using System.Security.Cryptography;

namespace PhoStudioMVC.Controllers
{
    [Authorize(Roles = "Photographer")]
    [Route("Photographer/[action]")]
    public class PhotographerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private static DateTime VnNow => TimeHelper.VnNow;

        public PhotographerController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Schedule()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.ServicePackage)
                .Include(b => b.CloudAlbum)
                .Include(b => b.RetouchRequests)
                .Where(b => b.PhotographerId == CurrentUserId &&
                            b.Status >= BookingStatus.Deposited && 
                            b.Status != BookingStatus.Cancelled)
                .ToListAsync();

            // SQLite provider doesn't support translating TimeSpan ordering into ORDER BY.
            // Sort in-memory after materializing the query results.
            bookings = bookings
                .OrderBy(b => b.BookingDate)
                .ThenBy(b => b.StartTime)
                .ToList();

            return View("Schedule", bookings);
        }

        // Old route compatibility — redirects to canonical /Photographer/Schedule
        [HttpGet("~/Photographer")]
        [HttpGet]
        public IActionResult Index() => RedirectToAction(nameof(Schedule));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkShootCompleted(string bookingId)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.PhotographerId == CurrentUserId);
            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Deposited)
            {
                TempData["Error"] = "Chỉ có thể đánh dấu hoàn thành khi trạng thái là 'Đã đặt cọc'.";
                return RedirectToAction(nameof(Schedule));
            }

            // Server-side time check: BookingDate + StartTime <= VnNow
            var shootDateTime = booking.BookingDate.Date + booking.StartTime;
            if (shootDateTime > VnNow)
            {
                TempData["Error"] = "Chưa đến giờ chụp.";
                return RedirectToAction(nameof(Schedule));
            }

            booking.Status = BookingStatus.ShootCompleted;
            booking.WorkflowStatus = BookingWorkflowStatus.ShootCompleted;
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Đã đánh dấu đã chụp xong.";
            return RedirectToAction(nameof(Schedule));
        }

        // GET /Photographer/UploadAlbum/{bookingId}
        [HttpGet("{bookingId}")]
        public async Task<IActionResult> UploadAlbum(string bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.ServicePackage)
                .Include(b => b.CloudAlbum)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.PhotographerId == CurrentUserId);
            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.ShootCompleted && booking.Status != BookingStatus.FullyPaid)
            {
                TempData["Error"] = "Chỉ được trả ảnh khi lịch đã chụp xong hoặc đã thanh toán đủ.";
                return RedirectToAction(nameof(Schedule));
            }

            if (booking.CloudAlbum != null)
            {
                TempData["Error"] = "Booking này đã có album được bàn giao trước đó.";
                return RedirectToAction(nameof(Schedule));
            }

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAlbum(string bookingId, string? albumUrl, List<IFormFile>? files)
        {
            const int maxFiles = 50;
            const long maxFileBytes = 12 * 1024 * 1024; // 12MB per file
            var allowedExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            var booking = await _context.Bookings
                .Include(b => b.CloudAlbum)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.PhotographerId == CurrentUserId);
            if (booking == null)
            {
                TempData["Error"] = "Booking không tồn tại.";
                return RedirectToAction(nameof(Schedule));
            }

            if (booking.Status != BookingStatus.ShootCompleted && booking.Status != BookingStatus.FullyPaid)
            {
                TempData["Error"] = "Chỉ được trả ảnh khi lịch đã chụp xong hoặc đã thanh toán đủ.";
                return RedirectToAction(nameof(Schedule));
            }

            if (booking.CloudAlbum != null)
            {
                TempData["Error"] = "Booking này đã có album được bàn giao trước đó.";
                return RedirectToAction(nameof(Schedule));
            }

            var hasFiles = files != null && files.Any(f => f != null && f.Length > 0);
            if (!hasFiles && string.IsNullOrWhiteSpace(albumUrl))
            {
                TempData["Error"] = "Vui lòng nhập Album URL hoặc upload ảnh nội bộ.";
                return RedirectToAction(nameof(UploadAlbum), new { bookingId });
            }

            if (hasFiles)
            {
                var validFiles = files!.Where(f => f != null && f.Length > 0).ToList();
                if (validFiles.Count > maxFiles)
                {
                    TempData["Error"] = $"Tối đa {maxFiles} ảnh mỗi lần upload.";
                    return RedirectToAction(nameof(UploadAlbum), new { bookingId });
                }

                foreach (var f in validFiles)
                {
                    if (f.Length > maxFileBytes)
                    {
                        TempData["Error"] = $"File '{f.FileName}' vượt quá 12MB.";
                        return RedirectToAction(nameof(UploadAlbum), new { bookingId });
                    }
                    var ext = Path.GetExtension(f.FileName);
                    if (string.IsNullOrWhiteSpace(ext) || !allowedExt.Contains(ext))
                    {
                        TempData["Error"] = $"File '{f.FileName}' không đúng định dạng ảnh hỗ trợ (.jpg, .jpeg, .png, .webp).";
                        return RedirectToAction(nameof(UploadAlbum), new { bookingId });
                    }
                }
            }

            var secureOTP = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var finalAlbumUrl = albumUrl ?? string.Empty;

            if (hasFiles)
            {
                var webRoot = _env.WebRootPath;
                if (string.IsNullOrWhiteSpace(webRoot))
                {
                    TempData["Error"] = "Server chưa cấu hình WebRoot để lưu ảnh nội bộ.";
                    return RedirectToAction(nameof(UploadAlbum), new { bookingId });
                }

                var folderRel = $"/uploads/albums/{bookingId}";
                var folderAbs = Path.Combine(webRoot, "uploads", "albums", bookingId);
                Directory.CreateDirectory(folderAbs);

                foreach (var f in files!.Where(x => x != null && x.Length > 0))
                {
                    var safeName = Path.GetFileName(f.FileName);
                    var ext = Path.GetExtension(safeName);
                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    var absPath = Path.Combine(folderAbs, fileName);
                    await using (var stream = System.IO.File.Create(absPath))
                    {
                        await f.CopyToAsync(stream);
                    }

                    var asset = new AlbumAsset
                    {
                        BookingId = bookingId,
                        FileName = safeName,
                        RelativePath = $"{folderRel}/{fileName}",
                        ContentType = f.ContentType,
                        SizeBytes = f.Length,
                        CreatedAt = VnNow
                    };
                    _context.AlbumAssets.Add(asset);
                }

                // Internal album page (after OTP verified) will read AlbumAssets
                finalAlbumUrl = $"/Client/AlbumContent?bookingId={bookingId}";
            }

            var cloudAlbum = new CloudAlbum
            {
                BookingId = bookingId,
                AlbumUrl = finalAlbumUrl,
                SecureOTP = secureOTP,
                ExpiryDate = VnNow.AddDays(30)
            };

            await _context.CloudAlbums.AddAsync(cloudAlbum);
            booking.WorkflowStatus = BookingWorkflowStatus.Delivered;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo album thành công. OTP: {secureOTP}";
            return RedirectToAction(nameof(Schedule));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeliverAlbum(string bookingId, string albumUrl)
        {
            var booking = await _context.Bookings
                .Include(b => b.CloudAlbum)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.PhotographerId == CurrentUserId);
            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.ShootCompleted && booking.Status != BookingStatus.FullyPaid)
            {
                TempData["Error"] = "Chỉ được trả ảnh khi lịch hẹn đã 'Hoàn thành'.";
                return RedirectToAction(nameof(Schedule));
            }

            if (string.IsNullOrWhiteSpace(albumUrl))
            {
                TempData["Error"] = "Đường dẫn Album Cloud không hợp lệ.";
                return RedirectToAction(nameof(Schedule));
            }

            // CloudAlbum is a 1-to-1 relationship.
            if (booking.CloudAlbum != null)
            {
                TempData["Error"] = "Booking này đã có album được bàn giao trước đó.";
                return RedirectToAction(nameof(Schedule));
            }

            // Generate cryptographically secure 6-digit OTP
            var secureOTP = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            var cloudAlbum = new CloudAlbum
            {
                BookingId = bookingId,
                AlbumUrl = albumUrl,
                SecureOTP = secureOTP,
                ExpiryDate = VnNow.AddDays(30)
            };

            await _context.CloudAlbums.AddAsync(cloudAlbum);
            booking.WorkflowStatus = BookingWorkflowStatus.Delivered;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo Album thành công! OTP: {secureOTP}";
            return RedirectToAction(nameof(Schedule));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRetouch(int requestId)
        {
            var request = await _context.RetouchRequests
                .Include(r => r.Booking)
                .FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null || request.Booking == null)
            {
                TempData["Error"] = "Yêu cầu retouch không tồn tại.";
                return RedirectToAction(nameof(MyRetouchRequests));
            }

            if (request.Booking.PhotographerId != CurrentUserId)
            {
                return Forbid();
            }

            if (request.Status == RetouchStatus.Completed)
            {
                TempData["Success"] = "Yêu cầu retouch đã được hoàn thành trước đó.";
                return RedirectToAction(nameof(MyRetouchRequests));
            }

            request.Status = RetouchStatus.Completed;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã hoàn thành retouch #{requestId}.";
            return RedirectToAction(nameof(MyRetouchRequests));
        }

        // GET /Photographer/MyRetouchRequests
        [HttpGet]
        public async Task<IActionResult> MyRetouchRequests()
        {
            var myRequests = await _context.RetouchRequests
                .Include(r => r.Booking).ThenInclude(b => b.Client)
                .Include(r => r.Booking).ThenInclude(b => b.ServicePackage)
                .Where(r => r.Booking.PhotographerId == CurrentUserId
                         && r.Status != RetouchStatus.Completed)
                .OrderBy(r => r.Status)
                .ThenByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return View(myRequests);
        }

        [HttpGet]
        public async Task<IActionResult> SelectedPhotos()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.ServicePackage)
                .Include(b => b.AlbumAssets)
                .Include(b => b.RetouchRequests)
                .Where(b => b.PhotographerId == CurrentUserId && b.AlbumAssets.Any(a => a.IsFavorite))
                .OrderByDescending(b => b.BookingDate)
                .AsNoTracking()
                .ToListAsync();

            var model = bookings.Select(b =>
            {
                var latestReq = b.RetouchRequests
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefault();
                return new SelectedPhotosBookingGroup
                {
                    BookingId = b.Id,
                    ClientName = b.Client?.FullName ?? "N/A",
                    ClientPhone = b.Client?.Phone,
                    ServiceName = b.ServicePackage?.Name ?? "N/A",
                    BookingDate = b.BookingDate,
                    PhotographerName = User.FindFirstValue("FullName") ?? User.Identity?.Name,
                    LatestRequestNote = latestReq?.Note,
                    LatestRequestAt = latestReq?.CreatedAt,
                    FavoriteCount = b.AlbumAssets.Count(a => a.IsFavorite),
                    Photos = b.AlbumAssets
                        .Where(a => a.IsFavorite)
                        .OrderByDescending(a => a.FavoritedAt ?? a.CreatedAt)
                        .Select(a => new SelectedPhotoItem
                        {
                            AssetId = a.Id,
                            FileName = a.FileName,
                            RelativePath = a.RelativePath,
                            FavoritedAt = a.FavoritedAt
                        })
                        .ToList()
                };
            }).ToList();

            return View(model);
        }
    }
}
