using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoStudioMVC.Models.Entities
{
    public class BookingReview
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

        [Required, Range(1, 5)]
        public int Rating { get; set; } // 1-5 sao

        [MaxLength(1000)]
        public string? Comment { get; set; }

        [MaxLength(500)]
        public string? Photographer { get; set; } // Tên nhân viên ch?p

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public class PhotographerRating
    {
        [Key]
        public string PhotographerId { get; set; } = string.Empty;

        [Range(1, 5)]
        public double AverageRating { get; set; } = 0;

        public int TotalReviews { get; set; } = 0;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
