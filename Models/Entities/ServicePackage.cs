using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhoStudioMVC.Models.Entities
{
    public class ServicePackage
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required, Range(0, 999_999_999)]
        public decimal Price { get; set; }

        [Required]
        public int DurationMinutes { get; set; }

        public string? ThumbnailUrl { get; set; }

        public string? ImageUrl { get; set; }

        // FR02: chỉ được ẩn (soft delete)
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}

