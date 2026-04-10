using PhoStudioMVC.Models.Entities;
using System.Collections.Generic;

namespace PhoStudioMVC.Models.ViewModels
{
    public class AdminRevenueViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public decimal GrowthRate { get; set; }
        
        public decimal OnlineRevenue { get; set; }
        public decimal CashRevenue { get; set; }

        public string MonthlyRevenueJson { get; set; } = "[]";

        // Lists to populate the dashboard tables
        public List<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public List<ApplicationUser> RecentClients { get; set; } = new List<ApplicationUser>();
        public List<ApplicationUser> StaffMembers { get; set; } = new List<ApplicationUser>();
        public List<ServicePackage> ServicePackages { get; set; } = new List<ServicePackage>();
        public List<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
