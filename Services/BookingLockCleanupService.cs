using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using PhoStudioMVC.Data;
using PhoStudioMVC.Models.Enums;
using PhoStudioMVC.Utils;

namespace PhoStudioMVC.Services
{
    /// <summary>
    /// Background service dọn dẹp các Booking bị khóa quá 15 phút (TimeSlotLocked hết hạn).
    /// </summary>
    public class BookingLockCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingLockCleanupService> _logger;

        public BookingLockCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<BookingLockCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Chạy định kỳ mỗi 1 phút
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var now = TimeHelper.VnNow;

                    var expiredBookings = await db.Bookings
                        .Where(b =>
                            b.Status == BookingStatus.TimeSlotLocked &&
                            b.LockExpirationTime.HasValue &&
                            b.LockExpirationTime.Value < now)
                        .ToListAsync(stoppingToken);

                    if (expiredBookings.Count > 0)
                    {
                        foreach (var booking in expiredBookings)
                        {
                            booking.Status = BookingStatus.Cancelled;
                            booking.LockExpirationTime = null;
                            booking.CancelledAt = now;
                            booking.CancelReason = "Hết thời gian giữ chỗ 15 phút";
                        }

                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("BookingLockCleanupService: Đã hủy {Count} booking hết hạn lock.", expiredBookings.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "BookingLockCleanupService: Lỗi khi dọn dẹp booking hết hạn.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}

