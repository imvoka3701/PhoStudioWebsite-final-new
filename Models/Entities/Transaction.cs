using System;
using System.ComponentModel.DataAnnotations;
using PhoStudioMVC.Models.Enums;

namespace PhoStudioMVC.Models.Entities
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required]
        public string BookingId { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public PaymentType PaymentType { get; set; }

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        // Reference từ cổng thanh toán (null nếu tiền mặt)
        public string? GatewayTransactionId { get; set; }
        public string? GatewayResponse { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navigation
        public Booking Booking { get; set; } = null!;
    }
}

