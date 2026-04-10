using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PhoStudioMVC.Models.Enums;

namespace PhoStudioMVC.Models.Entities
{
    public enum RefundStatus
    {
        Requested = 1,
        Approved = 2,
        Processing = 3,
        Completed = 4,
        Rejected = 5,
        Cancelled = 6
    }

    public class RefundRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string BookingId { get; set; } = string.Empty;
        [ForeignKey(nameof(BookingId))]
        public Booking? Booking { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;
        [ForeignKey(nameof(ClientId))]
        public ApplicationUser? Client { get; set; }

        [Required]
        public RefundStatus Status { get; set; } = RefundStatus.Requested;

        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundAmount { get; set; }

        [Required, MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? AdminNotes { get; set; }

        // Lý do t? ch?i (n?u rejected)
        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        // Thông tin hoàn ti?n
        public string? RefundMethode { get; set; } // BankTransfer, VnPay, etc
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }

        public string? TransactionId { get; set; } // ID giao d?ch hoàn ti?n

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? RejectedAt { get; set; }

        [Required]
        public string ApprovedBy { get; set; } = string.Empty; // Admin ID
    }

    public class RefundPolicy
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string PolicyName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required, Range(0, 100)]
        public int DaysBeforeBookingForFullRefund { get; set; } = 7; // Hoàn 100% trong vòng 7 ngày

        [Required, Range(0, 100)]
        public decimal RefundPercentageAfterDeadline { get; set; } = 50; // Hoàn 50% sau h?n

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
