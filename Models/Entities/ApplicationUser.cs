using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PhoStudioMVC.Models.Enums;

namespace PhoStudioMVC.Models.Entities
{
    public class ApplicationUser
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(15)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public UserRole Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<Booking> ClientBookings { get; set; } = new List<Booking>();
        public ICollection<Booking> PhotographerBookings { get; set; } = new List<Booking>();
    }
}
