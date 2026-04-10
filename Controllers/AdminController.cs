using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoStudioMVC.Data;
using PhoStudioMVC.Models.Entities;
using PhoStudioMVC.Models.Enums;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using PhoStudioMVC.Models.ViewModels;
using PhoStudioMVC.Utils;
using System.Dynamic;

namespace PhoStudioMVC.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/[action]")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private static DateTime VnNow => TimeHelper.VnNow;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("~/Admin")]
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var now = VnNow;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOf12Months = firstDayOfMonth.AddMonths(-11);

            // KPI Cards
            var totalThisMonth = await _context.Bookings
                .CountAsync(b => b.CreatedAt >= firstDayOfMonth && b.CreatedAt < firstDayOfMonth.AddMonths(1));

            var pending = await _context.Bookings
                .CountAsync(b => b.Status == BookingStatus.Deposited);

            var completed = await _context.Bookings
                .CountAsync(b => b.Status == BookingStatus.FullyPaid);

            var cancelled = await _context.Bookings
                .CountAsync(b => b.Status == BookingStatus.Cancelled && b.CancelledAt >= firstDayOfMonth);

            // Revenue this month
            var revenueOnline = await _context.Transactions
                .Where(t => t.PaymentType == PaymentType.OnlineDeposit
                         && t.Status == PaymentStatus.Success
                         && t.CompletedAt >= firstDayOfMonth)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var revenueCash = await _context.Transactions
                .Where(t => t.PaymentType == PaymentType.CashRemainder
                         && t.Status == PaymentStatus.Success
                         && t.CompletedAt >= firstDayOfMonth)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            // Monthly data 12 tháng
            var monthlyRaw = await _context.Transactions
                .Where(t => t.Status == PaymentStatus.Success && t.CompletedAt >= startOf12Months)
                .GroupBy(t => new { t.CompletedAt!.Value.Year, t.CompletedAt.Value.Month, t.PaymentType })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    g.Key.PaymentType,
                    Total = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            var months = Enumerable.Range(0, 12)
                .Select(i => startOf12Months.AddMonths(i))
                .ToList();

            var monthLabels = months.Select(m => $"{m.Month:D2}/{m.Year}").ToList();
            var monthlyOnline = months.Select(m => monthlyRaw
                .FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month && r.PaymentType == PaymentType.OnlineDeposit)?.Total ?? 0)
                .ToList();
            var monthlyCash = months.Select(m => monthlyRaw
                .FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month && r.PaymentType == PaymentType.CashRemainder)?.Total ?? 0)
                .ToList();

            // Hiệu suất thợ
            var photographerPerf = await _context.Bookings
                .Include(b => b.Photographer)
                .Where(b => b.PhotographerId != null && b.Status == BookingStatus.FullyPaid && b.CreatedAt >= firstDayOfMonth)
                .GroupBy(b => new { b.PhotographerId, b.Photographer!.FullName })
                .Select(g => new PhotographerPerformanceItem
                {
                    PhotographerId = g.Key.PhotographerId!,
                    PhotographerName = g.Key.FullName,
                    CompletedBookings = g.Count(),
                    TotalCommission = g.Sum(x => x.CommissionAmount ?? 0),
                    TotalRevenue = g.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.CompletedBookings)
                // SQLite provider can't ORDER BY decimal reliably; cast to double for ordering only.
                .ThenByDescending(x => (double)x.TotalRevenue)
                .ToListAsync();

            // SQLite provider cannot translate projections into custom types reliably.
            // Project to anonymous first, then map to TopServiceData in-memory.
            var topServicesRaw = await _context.Bookings
                .Include(b => b.ServicePackage)
                .Where(b => b.Status != BookingStatus.Cancelled && b.CreatedAt >= firstDayOfMonth)
                .GroupBy(b => new { b.ServicePackageId, Name = b.ServicePackage!.Name })
                .Select(g => new
                {
                    g.Key.Name,
                    BookingCount = g.Count(),
                    TotalRevenue = g.Sum(b => b.TotalAmount)
                })
                .OrderByDescending(x => x.BookingCount)
                .Take(5)
                .ToListAsync();

            var topServices = topServicesRaw
                .Select(x => new TopServiceData(x.Name, x.BookingCount, x.TotalRevenue))
                .ToList();

            var totalPhotographers = await _context.Users.CountAsync(u => u.Role == UserRole.Photographer);

            // Recently registered clients/customers for admin monitoring.
            var recentClients = await _context.Users
                .Where(u => u.Role == UserRole.Client || u.Role == UserRole.Customer)
                .OrderByDescending(u => u.CreatedAt)
                .AsNoTracking()
                .Take(6)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalBookingsThisMonth = totalThisMonth,
                PendingBookings = pending,
                CompletedBookings = completed,
                CancelledBookings = cancelled,
                RevenueOnline = revenueOnline,
                RevenueCash = revenueCash,
                MonthLabels = monthLabels,
                MonthlyOnlineRevenue = monthlyOnline,
                MonthlyCashRevenue = monthlyCash,
                TotalPhotographers = totalPhotographers,
                PhotographerPerformance = photographerPerf,
                TopServices = topServices,
                RecentClients = recentClients
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportRevenue(string format, int month, int year)
        {
            if (month < 1 || month > 12 || year < 2020 || year > 2099)
                return BadRequest("Tháng hoặc năm không hợp lệ.");

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var transactions = await _context.Transactions
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Client)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.ServicePackage)
                .Where(t => t.Status == PaymentStatus.Success
                         && t.CompletedAt >= startDate
                         && t.CompletedAt < endDate)
                .OrderBy(t => t.CompletedAt)
                .AsNoTracking()
                .ToListAsync();

            // Per master prompt: PDF export is not implemented (Excel only).
            return format?.ToLower() switch
            {
                "excel" => BuildExcelFile(transactions, month, year),
                _ => BadRequest("Chỉ hỗ trợ format=excel.")
            };
        }

        private FileResult BuildExcelFile(List<Transaction> transactions, int month, int year)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add($"Doanh thu {month:D2}-{year}");

            ws.Cell("A1").Value = $"BÁO CÁO DOANH THU THÁNG {month:D2}/{year}";
            ws.Range("A1:G1").Merge();
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 14;
            ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell("A2").Value = $"Xuất ngày: {VnNow:dd/MM/yyyy HH:mm}";
            ws.Range("A2:G2").Merge();
            ws.Cell("A2").Style.Font.Italic = true;

            var headers = new[] { "STT", "Mã lịch", "Khách hàng", "Gói dịch vụ",
                                  "Loại thanh toán", "Số tiền (VNĐ)", "Thời gian" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(4, i + 1).Value = headers[i];

            var headerRange = ws.Range(4, 1, 4, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            decimal onlineTotal = 0, cashTotal = 0;
            int row = 5;

            foreach (var (t, idx) in transactions.Select((t, i) => (t, i)))
            {
                var bookingCode = (t.BookingId ?? string.Empty).PadLeft(6, '0');
                ws.Cell(row, 1).Value = idx + 1;
                ws.Cell(row, 2).Value = $"#{bookingCode}";
                ws.Cell(row, 3).Value = t.Booking?.Client?.FullName ?? "";
                ws.Cell(row, 4).Value = t.Booking?.ServicePackage?.Name ?? "";
                ws.Cell(row, 5).Value = t.PaymentType == PaymentType.OnlineDeposit
                    ? "Online (VNPay/Cọc)" : "Tiền mặt (Phần còn lại)";
                ws.Cell(row, 6).Value = (double)t.Amount;
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 7).Value = t.CompletedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";

                if (idx % 2 == 0)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F8FF");

                if (t.PaymentType == PaymentType.OnlineDeposit) onlineTotal += t.Amount;
                else cashTotal += t.Amount;

                row++;
            }

            row++;
            ws.Cell(row, 4).Value = "Tổng Online:";
            ws.Cell(row, 4).Style.Font.Bold = true;
            ws.Cell(row, 5).Value = (double)onlineTotal;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 5).Style.Font.Bold = true;

            row++;
            ws.Cell(row, 4).Value = "Tổng Tiền mặt:";
            ws.Cell(row, 4).Style.Font.Bold = true;
            ws.Cell(row, 5).Value = (double)cashTotal;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 5).Style.Font.Bold = true;

            row++;
            ws.Cell(row, 4).Value = "TỔNG CỘNG:";
            ws.Cell(row, 4).Style.Font.Bold = true;
            ws.Cell(row, 4).Style.Font.FontSize = 12;
            ws.Cell(row, 5).Value = (double)(onlineTotal + cashTotal);
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 5).Style.Font.Bold = true;
            ws.Cell(row, 5).Style.Font.FontSize = 12;
            ws.Cell(row, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF3CD");

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"DoanhThu_{month:D2}_{year}.xlsx");
        }

        private FileResult BuildPdfFile(List<Transaction> transactions, int month, int year)
        {
            using var ms = new MemoryStream();
            var document = new Document(PageSize.A4.Rotate());
            PdfWriter.GetInstance(document, ms);
            document.Open();

            var fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
            BaseFont baseFont = System.IO.File.Exists(fontPath)
                ? BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED)
                : BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, false);

            var titleFont = new Font(baseFont, 16, Font.BOLD);
            var headerFont = new Font(baseFont, 10, Font.BOLD);
            var bodyFont = new Font(baseFont, 9);
            var totalFont = new Font(baseFont, 10, Font.BOLD);

            document.Add(new Paragraph($"BÁO CÁO DOANH THU THÁNG {month:D2}/{year}", titleFont) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph($"Xuất ngày: {VnNow:dd/MM/yyyy HH:mm}", bodyFont) { Alignment = Element.ALIGN_CENTER });
            document.Add(new Paragraph(" "));

            var table = new PdfPTable(7) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 4f, 8f, 18f, 18f, 14f, 12f, 14f });

            var headerBg = new BaseColor(30, 58, 95);
            string[] cols = { "STT", "Mã lịch", "Khách hàng", "Gói dịch vụ", "Loại TT", "Số tiền (VNĐ)", "Thời gian" };
            foreach (var col in cols)
            {
                var cell = new PdfPCell(new Phrase(col, headerFont))
                {
                    BackgroundColor = headerBg,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 6
                };
                cell.Phrase.Font.Color = new BaseColor(255, 255, 255);
                table.AddCell(cell);
            }

            decimal total = 0;
            var altBg = new BaseColor(245, 248, 255);
            foreach (var (t, idx) in transactions.Select((t, i) => (t, i)))
            {
                var bg = idx % 2 == 0 ? altBg : new BaseColor(255, 255, 255);
                void AddCell(string text, int align = Element.ALIGN_LEFT) =>
                    table.AddCell(new PdfPCell(new Phrase(text, bodyFont)) { BackgroundColor = bg, HorizontalAlignment = align, Padding = 5 });

                var bookingCode = (t.BookingId ?? string.Empty).PadLeft(6, '0');
                AddCell((idx + 1).ToString(), Element.ALIGN_CENTER);
                AddCell($"#{bookingCode}", Element.ALIGN_CENTER);
                AddCell(t.Booking?.Client?.FullName ?? "");
                AddCell(t.Booking?.ServicePackage?.Name ?? "");
                AddCell(t.PaymentType == PaymentType.OnlineDeposit ? "Online" : "Tiền mặt", Element.ALIGN_CENTER);
                AddCell($"{t.Amount:#,##0}", Element.ALIGN_RIGHT);
                AddCell(t.CompletedAt?.ToString("dd/MM/yyyy HH:mm") ?? "", Element.ALIGN_CENTER);
                total += t.Amount;
            }

            var totalCell = new PdfPCell(new Phrase("TỔNG CỘNG", totalFont))
            {
                Colspan = 5,
                HorizontalAlignment = Element.ALIGN_RIGHT,
                BackgroundColor = new BaseColor(255, 243, 205),
                Padding = 6
            };
            table.AddCell(totalCell);
            table.AddCell(new PdfPCell(new Phrase($"{total:#,##0}", totalFont)) { HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = new BaseColor(255, 243, 205), Padding = 6 });
            table.AddCell(new PdfPCell(new Phrase("")));

            document.Add(table);
            document.Close();

            return File(ms.ToArray(), "application/pdf", $"DoanhThu_{month:D2}_{year}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> Services()
        {
            var packages = await _context.ServicePackages
                .IgnoreQueryFilters()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View("Services/Index", packages);
        }

        [HttpGet]
        public IActionResult ServiceCreate()
        {
            return View("Services/Create");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ServiceCreate(string name, decimal price, int durationMinutes, string? description, string? thumbnailUrl)
        {
            if (string.IsNullOrWhiteSpace(name) || price < 10_000 || durationMinutes < 30 || durationMinutes > 480)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ. Kiểm tra lại tên, giá và thời lượng.";
                return View("Services/Create");
            }

            var service = new ServicePackage
            {
                Name = name.Trim(),
                Price = price,
                DurationMinutes = durationMinutes,
                Description = description,
                ThumbnailUrl = thumbnailUrl,
                IsActive = true,
                CreatedAt = VnNow
            };
            _context.ServicePackages.Add(service);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã thêm gói dịch vụ: {service.Name}";
            return RedirectToAction(nameof(Services));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ServiceEdit(int id)
        {
            var service = await _context.ServicePackages
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == id);
            if (service == null) return NotFound();
            return View("Services/Edit", service);
        }

        [HttpPost("{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ServiceEdit(int id, string name, decimal price, int durationMinutes, string? description, string? thumbnailUrl, bool isActive)
        {
            var service = await _context.ServicePackages
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == id);
            if (service == null) return NotFound();

            if (string.IsNullOrWhiteSpace(name) || price < 10_000 || durationMinutes < 30 || durationMinutes > 480)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ. Kiểm tra lại tên, giá và thời lượng.";
                return View("Services/Edit", service);
            }

            service.Name = name.Trim();
            service.Price = price;
            service.DurationMinutes = durationMinutes;
            service.Description = description;
            service.ThumbnailUrl = thumbnailUrl;
            service.IsActive = isActive;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật gói dịch vụ: {service.Name}";
            return RedirectToAction(nameof(Services));
        }

        [HttpPost("{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(int id)
        {
            var package = await _context.ServicePackages
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == id);
            if (package == null) return NotFound();

            var hasBookings = await _context.Bookings
                .AnyAsync(b => b.ServicePackageId == id);

            if (hasBookings)
            {
                package.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["Warning"] = "Đã ẩn gói dịch vụ (không xóa vì có lịch sử đặt lịch).";
            }
            else
            {
                _context.ServicePackages.Remove(package);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa gói dịch vụ thành công.";
            }

            return RedirectToAction(nameof(Services));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleServiceStatus(int serviceId)
        {
            var service = await _context.ServicePackages.FindAsync(serviceId);
            if (service == null) return NotFound();

            service.IsActive = !service.IsActive; // Toggle
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã {(service.IsActive ? "kích hoạt" : "vô hiệu hóa")} dịch vụ {service.Name}.";
            return RedirectToAction(nameof(Services));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreService(int id)
        {
            var package = await _context.ServicePackages
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (package == null) return NotFound();
            
            if (package.IsActive)
            {
                TempData["Warning"] = "Gói dịch vụ này đã đang hoạt động.";
                return RedirectToAction("Services");
            }

            package.IsActive = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã kích hoạt lại gói: {package.Name}";
            return RedirectToAction("Services");
        }

        [HttpGet]
        public async Task<IActionResult> Bookings(int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;

            var query = _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.ServicePackage)
                .Include(b => b.Photographer)
                .Include(b => b.Transactions)
                .OrderByDescending(b => b.BookingDate)
                .AsNoTracking();

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var bookings = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Photographers = await _context.Users
                .Where(u => u.Role == UserRole.Photographer)
                .OrderBy(u => u.FullName)
                .ToListAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPhotographer(string bookingId, string photographerId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return NotFound();

            if (string.IsNullOrEmpty(photographerId))
            {
                TempData["Error"] = "Vui lòng chọn Thợ chụp hợp lệ.";
                return RedirectToAction("Bookings");
            }

            booking.PhotographerId = photographerId;
            // 20% Commission auto-calculated
            booking.CommissionAmount = booking.TotalAmount * 0.2m; 
            booking.WorkflowStatus = BookingWorkflowStatus.Assigned;

            // Standardize status
            if (booking.Status == BookingStatus.TimeSlotLocked)
            {
                booking.Status = BookingStatus.Deposited;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã điều phối Thợ chụp thành công!";
            return RedirectToAction("Bookings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCashPayment(string id)
        {
            var booking = await _context.Bookings
                .Include(b => b.CloudAlbum)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.ShootCompleted)
            {
                TempData["Error"] = "Chỉ có thể xác nhận thu tiền mặt khi lịch ở trạng thái 'Đã chụp xong'.";
                return RedirectToAction("Bookings");
            }

            var remainingAmount = booking.TotalAmount - booking.DepositAmount;
            if (remainingAmount < 0) remainingAmount = 0;

            booking.Status = BookingStatus.FullyPaid;
            if (booking.CloudAlbum != null)
            {
                booking.WorkflowStatus = BookingWorkflowStatus.Closed;
            }

            var transaction = new Transaction
            {
                BookingId = booking.Id,
                PaymentType = PaymentType.CashRemainder,
                Status = PaymentStatus.Success,
                Amount = remainingAmount,
                CreatedAt = VnNow,
                CompletedAt = VnNow,
                Note = "Khách hàng thanh toán tiền mặt tại studio"
            };
            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xác nhận thanh toán đủ cho đơn #{booking.Id}.";
            return RedirectToAction("Bookings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminCancelBooking(string id, string cancelReason)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            if (booking.Status == BookingStatus.FullyPaid || booking.Status == BookingStatus.Cancelled)
            {
                TempData["Error"] = "Không thể hủy lịch đã hoàn thành hoặc đã bị hủy.";
                return RedirectToAction("Bookings");
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = VnNow;
            booking.CancelReason = cancelReason ?? "Admin hủy";

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã hủy lịch thành công.";
            return RedirectToAction("Bookings");
        }

        [HttpGet]
        public async Task<IActionResult> Clients()
        {
            var clients = await _context.Users
                .Where(u => u.Role == UserRole.Client || u.Role == UserRole.Customer)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(clients);
        }

        [HttpGet]
        public async Task<IActionResult> AuditAccounts()
        {
            var users = await _context.Users
                .Include(u => u.ClientBookings)
                    .ThenInclude(b => b.CloudAlbum)
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FullName)
                .AsNoTracking()
                .ToListAsync();

            var auditList = users.Select(u => new AccountAuditViewModel
            {
                UserId           = u.Id,
                Username         = u.Username,
                FullName         = u.FullName,
                Email            = u.Email,
                Phone            = u.Phone,
                Role             = u.Role,
                RegistrationDate = u.CreatedAt,
                IsActive         = u.IsActive,
                TotalBookings    = u.ClientBookings.Count,
                DeliveredAlbums  = u.ClientBookings.Count(b => b.CloudAlbum != null),
                PendingAlbums    = u.ClientBookings.Count(b => b.CloudAlbum == null &&
                                       b.Status != PhoStudioMVC.Models.Enums.BookingStatus.Cancelled),
                IsLoginViable    = !string.IsNullOrWhiteSpace(u.PasswordHash) && u.IsActive
            }).ToList();

            var summary = new AccountAuditSummary
            {
                TotalUsers           = auditList.Count,
                HealthyAccounts      = auditList.Count(u => u.IsLoginViable),
                LockedAccounts       = auditList.Count(u => !u.IsActive),
                TotalPendingAlbums   = auditList.Sum(u => u.PendingAlbums),
                TotalDeliveredAlbums = auditList.Sum(u => u.DeliveredAlbums),
                Users                = auditList
            };

            return View(summary);
        }

        [HttpGet]
        public async Task<IActionResult> Albums()
        {
            var albums = await _context.CloudAlbums
                .Include(a => a.Booking)
                .ThenInclude(b => b!.Client)
                .Include(a => a.Booking)
                .ThenInclude(b => b!.ServicePackage)
                .Include(a => a.Booking)
                .ThenInclude(b => b!.Photographer)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(albums);
        }

        [HttpGet]
        public async Task<IActionResult> SelectedPhotos()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.Photographer)
                .Include(b => b.ServicePackage)
                .Include(b => b.AlbumAssets)
                .Include(b => b.RetouchRequests)
                .Where(b => b.AlbumAssets.Any(a => a.IsFavorite))
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
                    PhotographerName = b.Photographer?.FullName,
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAlbumUrl(string albumId, string albumUrl)
        {
            if (string.IsNullOrWhiteSpace(albumId)) return BadRequest("albumId thiếu.");
            if (string.IsNullOrWhiteSpace(albumUrl))
            {
                TempData["Error"] = "AlbumUrl không được bỏ trống.";
                return RedirectToAction(nameof(Albums));
            }

            var album = await _context.CloudAlbums.FirstOrDefaultAsync(a => a.Id == albumId);
            if (album == null) return NotFound();

            album.AlbumUrl = albumUrl.Trim();
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật link album.";
            return RedirectToAction(nameof(Albums));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAlbumOtp(string albumId)
        {
            if (string.IsNullOrWhiteSpace(albumId)) return BadRequest("albumId thiếu.");
            var album = await _context.CloudAlbums.FirstOrDefaultAsync(a => a.Id == albumId);
            if (album == null) return NotFound();

            album.SecureOTP = new Random().Next(100000, 1000000).ToString();
            album.IsVerifiedByClient = false;
            album.FirstVerifiedAt = null;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã reset OTP. OTP mới: {album.SecureOTP}";
            return RedirectToAction(nameof(Albums));
        }

        [HttpPost("ResetAlbumOtpByBooking")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAlbumOtpByBooking(string bookingId)
        {
            if (string.IsNullOrWhiteSpace(bookingId)) return BadRequest("bookingId thiếu.");
            var album = await _context.CloudAlbums.FirstOrDefaultAsync(a => a.BookingId == bookingId);
            if (album == null) return NotFound();

            album.SecureOTP = new Random().Next(100000, 1000000).ToString();
            album.IsVerifiedByClient = false;
            album.FirstVerifiedAt = null;
            await _context.SaveChangesAsync();

            return Ok(new { bookingId, otp = album.SecureOTP });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendAlbumExpiry(string albumId, int days = 30)
        {
            if (string.IsNullOrWhiteSpace(albumId)) return BadRequest("albumId thiếu.");
            if (days < 1) days = 1;
            if (days > 365) days = 365;

            var album = await _context.CloudAlbums.FirstOrDefaultAsync(a => a.Id == albumId);
            if (album == null) return NotFound();

            album.ExpiryDate = album.ExpiryDate.AddDays(days);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã gia hạn album thêm {days} ngày.";
            return RedirectToAction(nameof(Albums));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeAlbum(string albumId)
        {
            if (string.IsNullOrWhiteSpace(albumId)) return BadRequest("albumId thiếu.");
            var album = await _context.CloudAlbums.FirstOrDefaultAsync(a => a.Id == albumId);
            if (album == null) return NotFound();

            // Soft revoke: expire immediately (keeps audit trail, blocks OTP usage).
            album.ExpiryDate = VnNow.AddSeconds(-1);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thu hồi quyền truy cập album (đã khóa theo hạn).";
            return RedirectToAction(nameof(Albums));
        }

        [HttpGet]
        public IActionResult Settings()
        {
            return View();
        }

        [HttpGet("HR")]
        public async Task<IActionResult> HR(int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;

            var query = _context.Users
                .Where(u => u.Role == UserRole.Photographer)
                .OrderBy(u => u.FullName)
                .AsNoTracking();

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var photographers = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View("HR/Index", photographers);
        }

        // Backward-compatible alias
        [HttpGet]
        public Task<IActionResult> HRIndex(int page = 1) => HR(page);

        [HttpGet("HR/Create")]
        public IActionResult HRCreate()
        {
            return View("HR/Create");
        }

        [HttpPost("HR/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HRCreate(string username, string fullName, string email, string phone)
        {
            const string defaultPassword = "Photographer@123";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Vui lòng điền đầy đủ Username, Họ tên và Email.";
                return View("HR/Create");
            }

            if (_context.Users.Any(u => u.Username == username))
            {
                TempData["Error"] = "Username đã tồn tại.";
                return View("HR/Create");
            }

            if (_context.Users.Any(u => u.Email == email))
            {
                TempData["Error"] = "Email đã được sử dụng.";
                return View("HR/Create");
            }

            var photographer = new ApplicationUser
            {
                Username = username,
                FullName = fullName,
                Email = email,
                Phone = phone,
                PasswordHash = HashPassword(defaultPassword),
                Role = UserRole.Photographer,
                IsActive = true,
                CreatedAt = VnNow
            };

            _context.Users.Add(photographer);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tạo tài khoản thợ chụp thành công.";
            TempData["NewPassword"] = defaultPassword;

            return RedirectToAction(nameof(HR));
        }

        [HttpPost("HR/Deactivate/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HRDeactivate(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role != UserRole.Photographer)
            {
                TempData["Error"] = "Thợ chụp không tồn tại.";
                return RedirectToAction(nameof(HR));
            }

            user.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã vô hiệu hóa tài khoản thợ chụp.";
            return RedirectToAction(nameof(HR));
        }

        [HttpPost("HR/Reactivate/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HRReactivate(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role != UserRole.Photographer)
            {
                TempData["Error"] = "Thợ chụp không tồn tại.";
                return RedirectToAction(nameof(HR));
            }

            user.IsActive = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã kích hoạt lại tài khoản thợ chụp.";
            return RedirectToAction(nameof(HR));
        }

        [HttpPost("HR/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HRDelete(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role != UserRole.Photographer)
            {
                TempData["Error"] = "Thợ chụp không tồn tại.";
                return RedirectToAction(nameof(HR));
            }

            var conflictBooking = await _context.Bookings
                .Where(b => b.PhotographerId == id && b.Status < BookingStatus.ShootCompleted)
                .FirstOrDefaultAsync();

            if (conflictBooking != null)
            {
                TempData["Error"] = "Không thể xóa: có booking chưa hoàn thành chụp.";
                return RedirectToAction(nameof(HR));
            }

            // Bỏ thợ chụp ra khỏi các booking hoặc xóa nếu cần
            var assignedBookings = await _context.Bookings
                .Where(b => b.PhotographerId == id)
                .ToListAsync();

            foreach (var booking in assignedBookings)
            {
                booking.PhotographerId = null;
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa thợ chụp thành công.";
            return RedirectToAction(nameof(HR));
        }

        [HttpGet]
        public async Task<IActionResult> PaymentHistory(
            int? filterMonth, int? filterYear, PaymentType? filterType,
            string? filterClientName, int page = 1)
        {
            var query = _context.Transactions
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Client)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.ServicePackage)
                .Where(t => t.Status == PaymentStatus.Success)
                .AsNoTracking()
                .AsQueryable();

            // Áp dụng filter tháng/năm
            if (filterMonth.HasValue && filterYear.HasValue)
            {
                var start = new DateTime(filterYear.Value, filterMonth.Value, 1);
                var end = start.AddMonths(1);
                query = query.Where(t => t.CompletedAt >= start && t.CompletedAt < end);
            }

            // Áp dụng filter loại thanh toán
            if (filterType.HasValue)
                query = query.Where(t => t.PaymentType == filterType.Value);

            // Áp dụng filter tên khách hàng
            if (!string.IsNullOrWhiteSpace(filterClientName))
                query = query.Where(t => t.Booking != null
                                      && t.Booking.Client != null
                                      && t.Booking.Client.FullName.Contains(filterClientName));

            // Tính tổng (trước khi phân trang)
            var totals = await query
                .GroupBy(t => t.PaymentType)
                .Select(g => new { Type = g.Key, Sum = g.Sum(t => t.Amount) })
                .ToListAsync();

            var totalOnline = totals.FirstOrDefault(x => x.Type == PaymentType.OnlineDeposit)?.Sum ?? 0;
            var totalCash = totals.FirstOrDefault(x => x.Type == PaymentType.CashRemainder)?.Sum ?? 0;

            // Phân trang
            const int pageSize = 20;
            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var rows = await query
                .OrderByDescending(t => t.CompletedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransactionRowViewModel
                {
                    Id = t.Id,
                    BookingId = t.BookingId,
                    ClientName = t.Booking != null && t.Booking.Client != null
                        ? t.Booking.Client.FullName
                        : "N/A",
                    ServiceName = t.Booking != null && t.Booking.ServicePackage != null
                        ? t.Booking.ServicePackage.Name
                        : "N/A",
                    Amount = t.Amount,
                    PaymentType = t.PaymentType,
                    Status = t.Status,
                    GatewayTxId = t.GatewayTransactionId,
                    CompletedAt = t.CompletedAt
                })
                .ToListAsync();

            var vm = new PaymentHistoryViewModel
            {
                FilterMonth = filterMonth,
                FilterYear = filterYear,
                FilterType = filterType,
                FilterClientName = filterClientName,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                Transactions = rows,
                TotalOnline = totalOnline,
                TotalCash = totalCash
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> RetouchRequests(RetouchStatus? filterStatus)
        {
            var query = _context.RetouchRequests
                .Include(r => r.Booking).ThenInclude(b => b.Client)
                .Include(r => r.Booking).ThenInclude(b => b.Photographer)
                .Include(r => r.Booking).ThenInclude(b => b.ServicePackage)
                .AsNoTracking()
                .AsQueryable();

            if (filterStatus.HasValue)
                query = query.Where(r => r.Status == filterStatus.Value);

            var list = await query
                .OrderBy(r => r.Status)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.FilterStatus = filterStatus;
            return View(list);
        }

        [HttpPost("Admin/UpdateRetouchStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRetouchStatus(int retouchId, RetouchStatus newStatus)
        {
            var request = await _context.RetouchRequests.FindAsync(retouchId);
            if (request == null) return NotFound();

            bool validTransition = (request.Status, newStatus) switch
            {
                (RetouchStatus.Pending, RetouchStatus.InProgress) => true,
                (RetouchStatus.InProgress, RetouchStatus.Completed) => true,
                _ => false
            };

            if (!validTransition)
            {
                TempData["Error"] = "Chuyển trạng thái không hợp lệ.";
                return RedirectToAction(nameof(RetouchRequests));
            }

            request.Status = newStatus;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật yêu cầu retouch #{retouchId}.";
            return RedirectToAction(nameof(RetouchRequests));
        }

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hashBytes = sha256.ComputeHash(bytes);
                var builder = new System.Text.StringBuilder();
                foreach (var b in hashBytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
