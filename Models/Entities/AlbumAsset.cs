using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoStudioMVC.Models.Entities
{
    public class AlbumAsset
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string BookingId { get; set; } = string.Empty;

        [ForeignKey(nameof(BookingId))]
        public Booking? Booking { get; set; }

        [Required]
        [MaxLength(300)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string RelativePath { get; set; } = string.Empty; // e.g. /uploads/albums/{bookingId}/file.jpg

        [MaxLength(100)]
        public string? ContentType { get; set; }

        public long SizeBytes { get; set; }

        public bool IsFavorite { get; set; } = false;

        public DateTime? FavoritedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

