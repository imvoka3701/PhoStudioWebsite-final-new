using System;
using System.ComponentModel.DataAnnotations;
using PhoStudioMVC.Models.Enums;

namespace PhoStudioMVC.Models.Entities
{
    public class RetouchRequest
    {
        public int Id { get; set; }

        [Required]
        public string BookingId { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Note { get; set; } = string.Empty;   // Khách ghi chú yêu cầu

        public string? PhotoReference { get; set; }        // Tên/số ảnh cần retouch

        // Tối đa 2 lần retouch/booking
        public int RequestNumber { get; set; }             // 1 hoặc 2

        public RetouchStatus Status { get; set; } = RetouchStatus.Pending;

        public DateTime CreatedAt { get; set; }

        // Navigation
        public Booking Booking { get; set; } = null!;
    }
}

