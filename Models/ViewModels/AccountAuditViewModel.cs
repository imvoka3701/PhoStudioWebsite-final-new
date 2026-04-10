using PhoStudioMVC.Models.Enums;

namespace PhoStudioMVC.Models.ViewModels
{
    public class AccountAuditViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public UserRole Role { get; set; }
        public DateTime RegistrationDate { get; set; }
        public bool IsActive { get; set; }

        // Booking & album stats
        public int TotalBookings { get; set; }
        public int DeliveredAlbums { get; set; }
        public int PendingAlbums { get; set; }

        // Login viability: password hash present + account active
        public bool IsLoginViable { get; set; }
        public string LoginStatus => IsLoginViable
            ? (IsActive ? "Có thể đăng nhập" : "Bị khóa")
            : "Mật khẩu trống";
    }

    public class AccountAuditSummary
    {
        public int TotalUsers { get; set; }
        public int HealthyAccounts { get; set; }
        public int LockedAccounts { get; set; }
        public int TotalPendingAlbums { get; set; }
        public int TotalDeliveredAlbums { get; set; }
        public List<AccountAuditViewModel> Users { get; set; } = new();
    }
}
