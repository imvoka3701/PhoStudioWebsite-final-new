using Microsoft.EntityFrameworkCore;
using PhoStudioMVC.Data;
using PhoStudioMVC.Models.Entities;
using PhoStudioMVC.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PhoStudioMVC.Services
{
    public class RefundService
    {
        private readonly ApplicationDbContext _context;
        private static DateTime VnNow => TimeHelper.VnNow;

        public RefundService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Ki?m tra booking có th? hoàn ti?n không
        /// </summary>
        public async Task<(bool CanRefund, string Message, decimal RefundAmount)> CanRefundBooking(string bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Transactions)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return (false, "Booking không t?n t?i.", 0);

            // Ki?m tra tr?ng thái
            if (booking.Status != Models.Enums.BookingStatus.Deposited && 
                booking.Status != Models.Enums.BookingStatus.TimeSlotLocked)
            {
                return (false, "Ch? có th? hoàn ti?n cho booking ? tr?ng thái 'Deposited' ho?c 'TimeSlotLocked'.", 0);
            }

            var policy = await _context.RefundPolicies
                .Where(p => p.IsActive)
                .FirstOrDefaultAsync();

            if (policy == null)
                return (false, "Không tìm th?y chính sách hoàn ti?n.", 0);

            // Tính toán ngày h?n
            var deadlineForFullRefund = booking.BookingDate.AddDays(-policy.DaysBeforeBookingForFullRefund);
            decimal refundAmount = booking.DepositAmount;

            if (VnNow > deadlineForFullRefund)
            {
                // Quá h?n - hoàn m?t ph?n
                refundAmount = booking.DepositAmount * policy.RefundPercentageAfterDeadline / 100;
            }

            return (true, "Có th? hoàn ti?n", refundAmount);
        }

        /// <summary>
        /// T?o yêu c?u hoàn ti?n
        /// </summary>
        public async Task<RefundRequest> CreateRefundRequest(
            string bookingId,
            string clientId,
            string reason,
            string approvedBy,
            string? bankAccount = null,
            string? bankName = null)
        {
            var (canRefund, _, refundAmount) = await CanRefundBooking(bookingId);

            if (!canRefund)
                throw new InvalidOperationException("Booking không ?? ?i?u ki?n hoàn ti?n.");

            var refundRequest = new RefundRequest
            {
                BookingId = bookingId,
                ClientId = clientId,
                Reason = reason,
                RefundAmount = refundAmount,
                Status = RefundStatus.Requested,
                ApprovedBy = approvedBy,
                BankAccount = bankAccount,
                BankName = bankName,
                RequestedAt = VnNow
            };

            _context.RefundRequests.Add(refundRequest);
            await _context.SaveChangesAsync();

            return refundRequest;
        }

        /// <summary>
        /// Admin duy?t yêu c?u hoàn ti?n
        /// </summary>
        public async Task<bool> ApproveRefund(int refundId, string adminId)
        {
            var refund = await _context.RefundRequests.FindAsync(refundId);
            if (refund == null || refund.Status != RefundStatus.Requested)
                return false;

            refund.Status = RefundStatus.Approved;
            refund.ApprovedAt = VnNow;
            refund.ApprovedBy = adminId;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Admin t? ch?i yêu c?u
        /// </summary>
        public async Task<bool> RejectRefund(int refundId, string rejectionReason, string adminId)
        {
            var refund = await _context.RefundRequests.FindAsync(refundId);
            if (refund == null || refund.Status != RefundStatus.Requested)
                return false;

            refund.Status = RefundStatus.Rejected;
            refund.RejectedAt = VnNow;
            refund.RejectionReason = rejectionReason;
            refund.ApprovedBy = adminId;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// ?ánh d?u hoàn ti?n ?ã x? lý
        /// </summary>
        public async Task<bool> CompleteRefund(int refundId, string transactionId)
        {
            var refund = await _context.RefundRequests.FindAsync(refundId);
            if (refund == null || refund.Status != RefundStatus.Approved)
                return false;

            refund.Status = RefundStatus.Completed;
            refund.CompletedAt = VnNow;
            refund.TransactionId = transactionId;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
