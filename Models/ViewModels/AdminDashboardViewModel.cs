using System.Collections.Generic;
using PhoStudioMVC.Models.Entities;

namespace PhoStudioMVC.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalBookingsThisMonth { get; set; }
        public int PendingBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }

        public decimal RevenueOnline { get; set; }
        public decimal RevenueCash { get; set; }
        public decimal TotalRevenue => RevenueOnline + RevenueCash;

        public List<string> MonthLabels { get; set; } = new();
        public List<decimal> MonthlyOnlineRevenue { get; set; } = new();
        public List<decimal> MonthlyCashRevenue { get; set; } = new();

        public int TotalPhotographers { get; set; }
        public List<PhotographerPerformanceItem> PhotographerPerformance { get; set; } = new();

        public List<TopServiceData> TopServices { get; set; } = new();

        // Recently registered clients (Customer/Client) for admin monitoring.
        public List<ApplicationUser> RecentClients { get; set; } = new();
    }

    public record TopServiceData(string ServiceName, int BookingCount, decimal Revenue);

    public class PhotographerPerformanceItem
    {
        public string PhotographerId { get; set; } = string.Empty;
        public string PhotographerName { get; set; } = string.Empty;
        public int CompletedBookings { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}

