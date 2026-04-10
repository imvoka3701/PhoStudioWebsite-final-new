using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoStudioMVC.Data;
using PhoStudioMVC.Models.Entities;
using PhoStudioMVC.Models.Enums;
using PhoStudioMVC.Services;
using PhoStudioMVC.Utils;
using System.Security.Claims;

namespace PhoStudioMVC.Controllers
{
    [Route("Booking/[action]")]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CouponService _couponService;
        private readonly LoyaltyService _loyaltyService;
        private readonly NotificationService _notificationService;
        private readonly RefundService _refundService;
        private static DateTime VnNow => TimeHelper.VnNow;

        public BookingController(
            ApplicationDbContext context,
            CouponService couponService,
            LoyaltyService loyaltyService,
            NotificationService notificationService,
            RefundService refundService)
        {
            _context = context;
            _couponService = couponService;
            _loyaltyService = loyaltyService;
            _notificationService = notificationService;
            _refundService = refundService;
        }

        [HttpGet("~/Booking")]
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.ServicePackages = _context.ServicePackages.Where(s => s.IsActive).ToList();
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Packages()
        {
            var packages = _context.ServicePackages
                .Where(s => s.IsActive)
                .ToList();

            // SQLite does not support ORDER BY on decimal columns.
            // Sort in-memory after materializing the query results.
            packages = packages
                .OrderBy(s => s.Price)
                .ToList();

            return View(packages);
        }

        [Authorize]
        [HttpGet("{packageId}")]
        public IActionResult SelectTime(int packageId)
        {
            var selectedPackage = _context.ServicePackages
                .FirstOrDefault(s => s.IsActive && s.Id == packageId);
            if (selectedPackage == null)
            {
                TempData["Error"] = "Gói dịch vụ không tồn tại hoặc đã bị ẩn.";
                return RedirectToAction(nameof(Packages));
            }

            // SQLite does not support ORDER BY on decimal columns.
            var servicePackages = _context.ServicePackages
                .Where(s => s.IsActive)
                .ToList();

            ViewBag.ServicePackages = servicePackages
                .OrderBy(s => s.Price)
                .ToList();
            ViewBag.SelectedPackageId = packageId;
            ViewBag.SelectedPackageName = selectedPackage.Name;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CreateBooking(DateTime bookingDate, string StartTime, string FullName, string phone, string? email, int ServicePackageId, string? Notes)
        {
            // Set fallback for redirection
            var redirectAction = ServicePackageId > 0 ? "SelectTime" : "Packages";
            var redirectRouteValues = ServicePackageId > 0 ? (object)new { packageId = ServicePackageId } : null;

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                TempData["Error"] = $"Thông tin không hợp lệ: {firstError}. Vui lòng kiểm tra lại.";
                return RedirectToAction(redirectAction, redirectRouteValues);
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(FullName))
            {
                TempData["Error"] = "Vui lòng nhập họ và tên.";
                return RedirectToAction(redirectAction, redirectRouteValues);
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                TempData["Error"] = "Vui lòng nhập số điện thoại.";
                return RedirectToAction(redirectAction, redirectRouteValues);
            }

            if (string.IsNullOrWhiteSpace(StartTime))
            {
                TempData["Error"] = "Vui lòng chọn khung giờ chụp.";
                return RedirectToAction(redirectAction, redirectRouteValues);
            }

            if (bookingDate.Date < VnNow.Date)
            {
                TempData["Error"] = "Ngày chụp không được trong quá khứ.";
                return RedirectToAction(redirectAction, redirectRouteValues);
            }

            if (!TimeSpan.TryParse(StartTime, out TimeSpan startTimeParsed))
            {
                TempData["Error"] = "Giờ chụp không hợp lệ. Vui lòng thử lại.";
                return RedirectToAction(redirectAction, redirectRouteValues);
            }

            // Step 1.2: Execute the Race Condition Prevention query (Rule #3)
            bool isConflict = _context.Bookings.Any(b =>
                b.BookingDate == bookingDate.Date &&
                b.StartTime == startTimeParsed &&
                (b.Status == BookingStatus.Deposited
                 || b.Status == BookingStatus.ShootCompleted
                 || b.Status == BookingStatus.FullyPaid
                 || (b.Status == BookingStatus.TimeSlotLocked && b.LockExpirationTime > VnNow)));

            if (isConflict)
            {
                TempData["Error"] = "Khung giờ này vừa có người đặt. Vui lòng chọn giờ khác.";
                return RedirectToAction(redirectAction, redirectRouteValues);
            }

            // Step 1.3: Apply Rule #4 (Price Calc) and Rule #2 (15-min lock)
            var service = await _context.ServicePackages.FindAsync(ServicePackageId);
            if (service == null || !service.IsActive)
            {
                TempData["Error"] = "Gói dịch vụ không hợp lệ.";
                return RedirectToAction("Packages");
            }

            // Step 1.4: Map ClientId
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Combine customer info and notes using string.Join
            var notesList = new List<string>
            {
                $"Khách: {FullName}",
                $"SĐT: {phone}"
            };
            if (!string.IsNullOrWhiteSpace(email))
                notesList.Add($"Email: {email}");
            if (!string.IsNullOrWhiteSpace(Notes))
                notesList.Add($"Yêu cầu: {Notes}");

            string combinedNotes = string.Join("\n", notesList);

            var newBooking = new Booking
            {
                ClientId = userId!,
                ServicePackageId = ServicePackageId,
                BookingDate = bookingDate.Date,
                StartTime = startTimeParsed,
                EndTime = startTimeParsed.Add(TimeSpan.FromMinutes(service.DurationMinutes)),
                TotalAmount = service.Price,
                DepositAmount = service.Price * 0.3m,
                Status = BookingStatus.TimeSlotLocked,
                WorkflowStatus = BookingWorkflowStatus.PendingReview,
                LockExpirationTime = VnNow.AddMinutes(15),
                Notes = combinedNotes
            };

            // Step 1.5: Save to DB
            _context.Bookings.Add(newBooking);
            await _context.SaveChangesAsync();

            // Step 1.6: Redirect
            return RedirectToAction("Payment", new { bookingId = newBooking.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client,Customer")]
        public async Task<IActionResult> LockSlot(DateTime shootDate, string startTime, int packageId, string? conceptNote)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Check for conflicts after opening transaction
                bool conflict = await _context.Bookings.AnyAsync(b =>
                    b.BookingDate.Date == shootDate.Date &&
                    b.StartTime == TimeSpan.Parse(startTime) &&
                    b.Status != BookingStatus.Cancelled);

                if (conflict)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Khung giờ vừa được đặt" });
                }

                var service = await _context.ServicePackages.FindAsync(packageId);
                if (service == null || !service.IsActive)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Gói dịch vụ không hợp lệ" });
                }

                var startTimeParsed = TimeSpan.Parse(startTime);
                var totalAmount = service.Price;
                var depositAmount = Math.Ceiling(totalAmount * 0.30m);

                var newBooking = new Booking
                {
                    Id = Guid.NewGuid().ToString(),
                    ClientId = userId,
                    ServicePackageId = packageId,
                    BookingDate = shootDate,
                    StartTime = startTimeParsed,
                    EndTime = startTimeParsed.Add(TimeSpan.FromMinutes(service.DurationMinutes)),
                    TotalAmount = totalAmount,
                    DepositAmount = depositAmount,
                    Status = BookingStatus.TimeSlotLocked,
                    WorkflowStatus = BookingWorkflowStatus.PendingReview,
                    LockExpirationTime = VnNow.AddMinutes(15),
                    Notes = conceptNote ?? string.Empty,
                    CreatedAt = VnNow
                };

                _context.Bookings.Add(newBooking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new {
                    success = true,
                    bookingId = newBooking.Id,
                    expiresAt = newBooking.LockExpirationTime,
                    depositAmount = depositAmount,
                    totalAmount = totalAmount
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [Authorize]
        [HttpGet("{bookingId}")]
        public async Task<IActionResult> Payment(string bookingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.ServicePackage)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.ClientId == userId);

            if (booking == null) return NotFound();

            if (booking.Status == BookingStatus.TimeSlotLocked && booking.LockExpirationTime < VnNow)
            {
                booking.Status = BookingStatus.Cancelled;
                await _context.SaveChangesAsync();
                TempData["Error"] = "Thời gian giữ chỗ đã hết hạn. Vui lòng đặt lại lịch mới.";
                return RedirectToAction("Index");
            }

            if (booking.Status != BookingStatus.TimeSlotLocked)
            {
                return RedirectToAction("Dashboard", "Client");
            }

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ConfirmPayment(string bookingId, string couponCode = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.ServicePackage)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.ClientId == userId);

            if (booking == null) return NotFound();

            if (booking.Status == BookingStatus.TimeSlotLocked && booking.LockExpirationTime >= VnNow)
            {
                booking.Status = BookingStatus.Deposited;
                booking.LockExpirationTime = null; // Clear lock

                // Apply coupon if provided
                if (!string.IsNullOrEmpty(couponCode))
                {
                    var (isValid, message, discountAmount) = await _couponService.ValidateCoupon(
                        couponCode, userId, booking.DepositAmount);

                    if (isValid && discountAmount > 0)
                    {
                        await _couponService.ApplyCoupon(bookingId, couponCode, discountAmount);
                        booking.DepositAmount -= discountAmount;
                    }
                }

                var onlineTx = new Transaction
                {
                    BookingId = booking.Id,
                    Amount = booking.DepositAmount,
                    PaymentType = PaymentType.OnlineDeposit,
                    Status = PaymentStatus.Success,
                    CreatedAt = VnNow,
                    CompletedAt = VnNow,
                    Note = "Khách hàng thanh toán cọc online.",
                    GatewayTransactionId = "SIM" + Guid.NewGuid().ToString("N")[..8].ToUpper()
                };
                _context.Transactions.Add(onlineTx);

                // Add loyalty points
                await _loyaltyService.AddLoyaltyPoints(userId, bookingId, booking.TotalAmount, "Booking thanh toán cọc");

                // Update booking count
                var loyalty = await _context.CustomerLoyalties.FindAsync(userId);
                if (loyalty != null)
                {
                    loyalty.TotalBookings++;
                }

                // Send confirmation notification
                await _notificationService.SendBookingConfirmation(userId, booking);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Thanh toán thành công. Lịch hẹn của bạn đã được xác nhận!";
                return RedirectToAction("BookingConfirmation", new { id = booking.Id });
            }

            if (booking.Status == BookingStatus.TimeSlotLocked && booking.LockExpirationTime < VnNow)
            {
                booking.Status = BookingStatus.Cancelled;
                await _context.SaveChangesAsync();
                TempData["Error"] = "Đã quá thời gian thanh toán. Vui lòng đặt lại.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client,Customer")]
        public Task<IActionResult> SimulatePayment(string bookingId) => ConfirmPayment(bookingId);

        [Authorize(Roles = "Client,Customer")]
        [HttpGet("{id}")]
        public async Task<IActionResult> BookingConfirmation(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.ServicePackage)
                .Include(b => b.Transactions)
                .FirstOrDefaultAsync(b => b.Id == id && b.ClientId == userId);
            if (booking == null) return NotFound();

            return View(booking);
        }

        [Authorize(Roles = "Client,Customer")]
        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bookings = await _context.Bookings
                .Include(b => b.ServicePackage)
                .Include(b => b.Photographer)
                .Where(b => b.ClientId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .Take(20)
                .ToListAsync();

            return View(bookings);
        }

        [Authorize(Roles = "Client,Customer")]
        [HttpGet("{id}")]
        public async Task<IActionResult> BookingDetail(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.ServicePackage)
                .Include(b => b.Photographer)
                .Include(b => b.Transactions)
                .Include(b => b.CloudAlbum)
                .Include(b => b.RetouchRequests)
                .FirstOrDefaultAsync(b => b.Id == id && b.ClientId == userId);
            if (booking == null) return NotFound();
            return View(booking);
        }

        [Authorize(Roles = "Client,Customer")]
        [HttpGet]
        public async Task<IActionResult> MyAlbums()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bookings = await _context.Bookings
                .Include(b => b.CloudAlbum)
                .Include(b => b.ServicePackage)
                .Where(b => b.ClientId == userId && b.Status != BookingStatus.Cancelled)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        [Authorize(Roles = "Client,Customer")]
        [HttpGet("{bookingId}")]
        public async Task<IActionResult> ViewAlbum(string bookingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.CloudAlbum)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.ClientId == userId);

            if (booking == null) return NotFound();

            var cloudAlbum = booking.CloudAlbum;
            if (cloudAlbum == null)
            {
                TempData["Error"] = "Booking này chưa có album nào được tạo.";
                return RedirectToAction("MyAlbums");
            }

            if (cloudAlbum.ExpiryDate <= VnNow)
            {
                return View("AlbumExpired", cloudAlbum);
            }

            if (cloudAlbum.IsVerifiedByClient)
            {
                return RedirectToAction("AlbumContent", new { albumId = cloudAlbum.Id });
            }

            return View("ViewAlbum", cloudAlbum);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client,Customer")]
        public async Task<IActionResult> VerifyOTP(string albumId, string otp)
        {
            var cloudAlbum = await _context.CloudAlbums.Include(a => a.Booking).FirstOrDefaultAsync(a => a.Id == albumId);
            if (cloudAlbum == null)
            {
                TempData["Error"] = "Album không tồn tại.";
                return RedirectToAction("MyAlbums");
            }

            if (cloudAlbum.ExpiryDate <= VnNow)
            {
                return View("AlbumExpired", cloudAlbum);
            }

            if (string.IsNullOrWhiteSpace(otp) || otp.Length != 6 || !otp.All(char.IsDigit))
            {
                TempData["Error"] = "Mã OTP phải là 6 chữ số.";
                return RedirectToAction("ViewAlbum", new { bookingId = cloudAlbum.BookingId });
            }

            if (cloudAlbum.SecureOTP != otp)
            {
                TempData["Error"] = "Mã OTP không chính xác. Vui lòng thử lại.";
                return RedirectToAction("ViewAlbum", new { bookingId = cloudAlbum.BookingId });
            }

            cloudAlbum.IsVerifiedByClient = true;
            cloudAlbum.FirstVerifiedAt = VnNow;
            await _context.SaveChangesAsync();

            return RedirectToAction("AlbumContent", new { albumId = cloudAlbum.Id });
        }

        [Authorize(Roles = "Client,Customer")]
        [HttpGet("{albumId}")]
        public async Task<IActionResult> AlbumContent(string albumId)
        {
            var cloudAlbum = await _context.CloudAlbums.Include(a => a.Booking).FirstOrDefaultAsync(a => a.Id == albumId);
            if (cloudAlbum == null) return NotFound();

            if (cloudAlbum.ExpiryDate <= VnNow)
            {
                return View("AlbumExpired", cloudAlbum);
            }

            if (!cloudAlbum.IsVerifiedByClient)
            {
                return RedirectToAction("ViewAlbum", new { bookingId = cloudAlbum.BookingId });
            }

            return View(cloudAlbum);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client,Customer")]
        public async Task<IActionResult> CancelBooking(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.ClientId == userId);
            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status != BookingStatus.Deposited)
            {
                TempData["Error"] = "Chỉ có thể hủy lịch khi đã đặt cọc.";
                return RedirectToAction("BookingDetail", new { id });
            }

            var shootDateTime = booking.BookingDate.Date + booking.StartTime;
            var hoursUntilShoot = (shootDateTime - VnNow).TotalHours;

            if (hoursUntilShoot < 48)
            {
                TempData["Error"] = $"Buổi chụp còn {Math.Floor(hoursUntilShoot)} giờ, không thể tự hủy. Liên hệ studio.";
                return RedirectToAction("BookingDetail", new { id });
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = VnNow;
            booking.CancelReason = "Khách hủy";

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã hủy lịch thành công.";
            return RedirectToAction("MyBookings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client,Customer")]
        public async Task<IActionResult> RequestRetouch(string id, string note)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings
                .Include(b => b.RetouchRequests)
                .FirstOrDefaultAsync(b => b.Id == id && b.ClientId == userId);

            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.ShootCompleted && booking.Status != BookingStatus.FullyPaid)
            {
                TempData["Error"] = "Chỉ có thể yêu cầu retouch sau khi chụp xong.";
                return RedirectToAction("BookingDetail", new { id });
            }

            var existingRequests = await _context.RetouchRequests
                .Where(r => r.BookingId == id)
                .ToListAsync();

            if (existingRequests.Count >= 2)
            {
                TempData["Error"] = "Bạn đã dùng hết 2 lần yêu cầu retouch.";
                return RedirectToAction("BookingDetail", new { id });
            }

            var requestNumber = existingRequests.Count + 1;
            var retouchRequest = new RetouchRequest
            {
                BookingId = id,
                Note = note?.Trim() ?? string.Empty,
                RequestNumber = requestNumber,
                Status = RetouchStatus.Pending,
                CreatedAt = VnNow
            };

            _context.RetouchRequests.Add(retouchRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã gửi yêu cầu retouch lần " + requestNumber;
            return RedirectToAction("BookingDetail", new { id });
        }

        // ===== COUPON PAGES =====
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Coupons()
        {
            var coupons = _context.Coupons
                .Where(c => c.IsActive && c.ValidFrom <= VnNow && c.ValidUntil >= VnNow)
                .OrderByDescending(c => c.DiscountPercent)
                .ToList();

            return View(coupons);
        }

        // ===== LOYALTY PAGES =====
        [Authorize(Roles = "Client,Customer")]
        [HttpGet]
        public async Task<IActionResult> Loyalty()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var loyalty = await _context.CustomerLoyalties.FindAsync(userId);

            if (loyalty == null)
            {
                loyalty = new CustomerLoyalty
                {
                    ClientId = userId,
                    CreatedAt = VnNow
                };
                _context.CustomerLoyalties.Add(loyalty);
                await _context.SaveChangesAsync();
            }

            return View(loyalty);
        }

        // ===== NOTIFICATIONS =====
        [Authorize(Roles = "Client,Customer")]
        [HttpGet]
        public async Task<IActionResult> NotificationsCenter()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _context.Notifications
                .Where(n => n.ClientId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View("Notifications", notifications);
        }
    }
}
