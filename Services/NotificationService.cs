using Microsoft.EntityFrameworkCore;
using PhoStudioMVC.Data;
using PhoStudioMVC.Models.Entities;
using PhoStudioMVC.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PhoStudioMVC.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private static DateTime VnNow => TimeHelper.VnNow;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// T?o th�ng b�o trong h? th?ng
        /// </summary>
        public async Task<Notification> CreateNotification(
            string clientId,
            NotificationType type,
            string title,
            string? content = null,
            string? bookingId = null,
            string? actionUrl = null)
        {
            var notification = new Notification
            {
                ClientId = clientId,
                Type = type,
                Title = title,
                Content = content,
                BookingId = bookingId,
                ActionUrl = actionUrl,
                CreatedAt = VnNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return notification;
        }

        /// <summary>
        /// G?i th�ng b�o booking x�c nh?n
        /// </summary>
        public async Task SendBookingConfirmation(string clientId, Booking booking)
        {
            await CreateNotification(
                clientId,
                NotificationType.BookingConfirmed,
                "??t l?ch th�nh c�ng",
                $"L?ch ch?p ng�y {booking.BookingDate:dd/MM/yyyy} l�c {booking.StartTime:hh\\:mm} ?� ???c x�c nh?n.",
                booking.Id,
                $"/Booking/BookingDetail/{booking.Id}"
            );
        }

        /// <summary>
        /// G?i th�ng b�o nh?c nh? tr??c ng�y ch?p
        /// </summary>
        public async Task SendBookingReminder(Booking booking)
        {
            // Ch? g?i nh?c nh? 1 l?n, 1 ng�y tr??c ch?p
            if (booking.ReminderSentAt.HasValue)
                return;

            var reminderDate = booking.BookingDate.AddDays(-1);
            if (VnNow.Date != reminderDate.Date)
                return;

            await CreateNotification(
                booking.ClientId,
                NotificationType.BookingReminder,
                "Nhắc nhở lịch chụp",
                $"L?ch ch?p c?a b?n v�o ng�y mai l�c {booking.StartTime:hh\\:mm}.",
                booking.Id,
                $"/Booking/BookingDetail/{booking.Id}"
            );

            booking.ReminderSentAt = VnNow;
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// G?i th�ng b�o album s?n s�ng
        /// </summary>
        public async Task SendAlbumReadyNotification(CloudAlbum album)
        {
            var booking = await _context.Bookings.FindAsync(album.BookingId);
            if (booking == null)
                return;

            await CreateNotification(
                booking.ClientId,
                NotificationType.AlbumReady,
                "Album ch?p s?n s�ng",
                "Album ch?p c?a b?n ?� s?n s�ng ?? xem. Vui l�ng nh?p m� OTP ?? xem.",
                album.BookingId,
                $"/Booking/ViewAlbum/{album.BookingId}"
            );
        }

        /// <summary>
        /// G?i y�u c?u review
        /// </summary>
        public async Task SendReviewRequest(Booking booking)
        {
            // Ch? g?i 1 l?n
            if (booking.IsReviewed || booking.ReviewDeadline.HasValue)
                return;

            booking.ReviewDeadline = VnNow.AddDays(7);

            await CreateNotification(
                booking.ClientId,
                NotificationType.ReviewRequest,
                "Vui l�ng ?�nh gi� d?ch v?",
                "H�y chia s? ?�nh gi� c?a b?n v? d?ch v? ch?p h�nh.",
                booking.Id,
                $"/Booking/BookingDetail/{booking.Id}"
            );

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// G?i th�ng b�o coupon/promo m?i
        /// </summary>
        public async Task SendPromoAlert(string clientId, string title, string content, string? couponCode = null)
        {
            await CreateNotification(
                clientId,
                NotificationType.CouponAvailable,
                title,
                content
            );
        }

        /// <summary>
        /// L?y th�ng b�o ch?a ??c
        /// </summary>
        public async Task<int> GetUnreadCount(string clientId)
        {
            return await _context.Notifications
                .Where(n => n.ClientId == clientId && !n.IsRead)
                .CountAsync();
        }

        /// <summary>
        /// ?�nh d?u ?� ??c
        /// </summary>
        public async Task MarkAsRead(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = VnNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
