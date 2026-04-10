using System;
using System.ComponentModel.DataAnnotations;

namespace PhoStudioMVC.Models.Entities
{
    public class FavoriteSelectionAudit
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Username { get; set; }

        [Required]
        public string BookingId { get; set; } = string.Empty;

        public int FavoriteCount { get; set; }

        [MaxLength(2000)]
        public string? Note { get; set; }

        [MaxLength(100)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

