using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoStudioMVC.Models.Entities
{
    public class CustomerLoyalty
    {
        [Key]
        public string ClientId { get; set; } = string.Empty;
        [ForeignKey(nameof(ClientId))]
        public ApplicationUser? Client { get; set; }

        public long TotalPoints { get; set; } = 0; // T?ng ?i?m
        public long AvailablePoints { get; set; } = 0; // ?i?m có th? dùng

        public int TotalBookings { get; set; } = 0; // T?ng booking

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSpent { get; set; } = 0; // T?ng chi tiêu

        public string? MembershipTier { get; set; } // Bronze, Silver, Gold, Platinum

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastPurchaseAt { get; set; }
    }

    public class LoyaltyTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;
        [ForeignKey(nameof(ClientId))]
        public ApplicationUser? Client { get; set; }

        [Required]
        public long Points { get; set; } // S? ?i?m (âm = tr?, d??ng = c?ng)

        [Required, MaxLength(200)]
        public string Reason { get; set; } = string.Empty; // Lý do: Booking, Referral, etc

        public string? BookingId { get; set; } // Liên k?t booking
        public string? ReferralCode { get; set; } // Mã gi?i thi?u

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ReferralCode
    {
        [Key]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string ClientId { get; set; } = string.Empty;
        [ForeignKey(nameof(ClientId))]
        public ApplicationUser? Client { get; set; }

        public long RewardPoints { get; set; } = 100; // ?i?m th??ng cho ng??i ???c gi?i thi?u

        public int UsageCount { get; set; } = 0;
        public int MaxUsageCount { get; set; } = 0; // 0 = unlimited

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
