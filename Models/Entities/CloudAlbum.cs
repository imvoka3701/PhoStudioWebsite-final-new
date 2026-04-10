using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoStudioMVC.Models.Entities
{
    public class CloudAlbum
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string BookingId { get; set; } = string.Empty;
        
        [ForeignKey(nameof(BookingId))]
        public Booking? Booking { get; set; }

        [Required]
        [MaxLength(1000)]
        public string AlbumUrl { get; set; } = string.Empty;

        [Required]
        [MaxLength(6)]
        [MinLength(6)]
        public string SecureOTP { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiryDate { get; set; }

        public bool IsVerifiedByClient { get; set; } = false;

        public DateTime? FirstVerifiedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
