using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoStudioMVC.Models.Entities
{
    public class Coupon
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required, Range(0, 100)]
        public decimal DiscountPercent { get; set; } // Ph?n tr?m gi?m

        [Range(0, 999_999_999)]
        public decimal? MaxDiscountAmount { get; set; } // Gi?m t?i ?a

        [Range(0, 999_999_999)]
        public decimal? MinOrderAmount { get; set; } // ??n t?i thi?u

        public int? MaxUsageCount { get; set; } // T?ng l?n dùng t?i ?a
        public int CurrentUsageCount { get; set; } = 0;

        public int? MaxUsagePerUser { get; set; } // T?i ?a l?n/khách

        public DateTime ValidFrom { get; set; }
        public DateTime ValidUntil { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ICollection<BookingCoupon> BookingCoupons { get; set; } = new List<BookingCoupon>();
    }

    public class BookingCoupon
    {
        public int Id { get; set; }

        [Required]
        public string BookingId { get; set; } = string.Empty;
        public Booking? Booking { get; set; }

        [Required]
        public int CouponId { get; set; }
        public Coupon? Coupon { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    }
}
