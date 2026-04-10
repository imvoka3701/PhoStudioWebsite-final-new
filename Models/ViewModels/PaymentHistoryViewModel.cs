using PhoStudioMVC.Models.Enums;

namespace PhoStudioMVC.Models.ViewModels;

public class PaymentHistoryViewModel
{
    // Filter params
    public int? FilterMonth { get; set; }
    public int? FilterYear { get; set; }
    public PaymentType? FilterType { get; set; }
    public string? FilterClientName { get; set; }

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 20;

    // Data
    public List<TransactionRowViewModel> Transactions { get; set; } = new();

    // Summary
    public decimal TotalOnline { get; set; }
    public decimal TotalCash { get; set; }
    public decimal GrandTotal => TotalOnline + TotalCash;
}

public class TransactionRowViewModel
{
    public int Id { get; set; }
    public string BookingId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentType PaymentType { get; set; }
    public PaymentStatus Status { get; set; }
    public string? GatewayTxId { get; set; }
    public DateTime? CompletedAt { get; set; }
}
