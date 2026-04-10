using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PhoStudioMVC.Models.Enums;

namespace PhoStudioMVC.Models.Entities
{
    public class Booking
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ClientId { get; set; } = string.Empty;
        [ForeignKey(nameof(ClientId))]
        public ApplicationUser? Client { get; set; }

        public string? PhotographerId { get; set; }
        [ForeignKey(nameof(PhotographerId))]
        public ApplicationUser? Photographer { get; set; }

        [Required]
        public int ServicePackageId { get; set; }
        [ForeignKey(nameof(ServicePackageId))]
        public ServicePackage? ServicePackage { get; set; }

        [Required]
        public DateTime BookingDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CommissionAmount { get; set; }

        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.TimeSlotLocked;

        [Required]
        public BookingWorkflowStatus WorkflowStatus { get; set; } = BookingWorkflowStatus.PendingReview;

        public DateTime? LockExpirationTime { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        [MaxLength(2000)]
        public string? ConceptNote { get; set; }

        [MaxLength(2000)]
        public string? AdminNote { get; set; }

        public DateTime? CancelledAt { get; set; }

        [MaxLength(500)]
        public string? CancelReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // New fields for enhanced features
        public DateTime? ReminderSentAt { get; set; } // L?n cu?i g?i nh?c nh?
        public bool IsReviewed { get; set; } = false; // ?ã ???c review ch?a
        public DateTime? ReviewDeadline { get; set; } // H?n review

        // Navigation Properties
        public CloudAlbum? CloudAlbum { get; set; }
        public ICollection<AlbumAsset> AlbumAssets { get; set; } = new List<AlbumAsset>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<RetouchRequest> RetouchRequests { get; set; } = new List<RetouchRequest>();
        public ICollection<BookingCoupon> BookingCoupons { get; set; } = new List<BookingCoupon>();
        public BookingReview? Review { get; set; }
        public RefundRequest? RefundRequest { get; set; }
    }
}
