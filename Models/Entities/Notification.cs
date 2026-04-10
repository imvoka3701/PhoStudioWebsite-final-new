using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoStudioMVC.Models.Entities
{
    public enum NotificationType
    {
        BookingConfirmed = 1,
        BookingReminder = 2,
        PaymentReceived = 3,
        AlbumReady = 4,
        ReviewRequest = 5,
        CouponAvailable = 6,
        LoyaltyPoints = 7,
        PromoAlert = 8,
        SystemNotice = 9
    }

    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;
        [ForeignKey(nameof(ClientId))]
        public ApplicationUser? Client { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Content { get; set; }

        public string? BookingId { get; set; } // Liên k?t booking (n?u có)
        public string? ActionUrl { get; set; } // Link ?? hành ??ng

        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        public bool EmailSent { get; set; } = false;
        public DateTime? EmailSentAt { get; set; }

        public bool SmsSent { get; set; } = false;
        public DateTime? SmsSentAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class NotificationTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        [Required, MaxLength(200)]
        public string EmailSubject { get; set; } = string.Empty;

        [Required]
        public string EmailBody { get; set; } = string.Empty; // HTML template

        public string? SmsTemplate { get; set; } // SMS content

        public bool IsActive { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
