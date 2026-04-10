using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoStudioMVC.Services;
using PhoStudioMVC.Data;
using PhoStudioMVC.Models.Entities;
using System.Security.Claims;

namespace PhoStudioMVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RefundApiController : ControllerBase
    {
        private readonly RefundService _refundService;
        private readonly ApplicationDbContext _context;

        public RefundApiController(RefundService refundService, ApplicationDbContext context)
        {
            _refundService = refundService;
            _context = context;
        }

        /// <summary>
        /// Kiểm tra booking có thể hoàn tiền không
        /// </summary>
        [HttpGet("check/{bookingId}")]
        public async Task<IActionResult> CheckRefundEligibility(string bookingId)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _context.Bookings.FindAsync(bookingId);

            if (booking == null || booking.ClientId != clientId)
                return NotFound(new { message = "Booking không tồn tại" });

            var (canRefund, message, refundAmount) = await _refundService.CanRefundBooking(bookingId);

            return Ok(new
            {
                canRefund,
                message,
                refundAmount,
                bookingDate = booking.BookingDate,
                depositAmount = booking.DepositAmount
            });
        }

        /// <summary>
        /// Tạo yêu cầu hoàn tiền
        /// </summary>
        [HttpPost("request")]
        public async Task<IActionResult> PostRefundRequest([FromBody] CreateRefundRequest request)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                var refundRequest = await _refundService.CreateRefundRequest(
                    request.BookingId,
                    clientId,
                    request.Reason,
                    userId,
                    request.BankAccount,
                    request.BankName
                );

                return Ok(new
                {
                    message = "Yêu cầu hoàn tiền được gửi thành công",
                    refundId = refundRequest.Id,
                    status = refundRequest.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy yêu cầu hoàn tiền của khách
        /// </summary>
        [HttpGet("history")]
        public IActionResult GetRefundHistory()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var refunds = _context.RefundRequests
                .Where(r => r.ClientId == clientId)
                .OrderByDescending(r => r.RequestedAt)
                .ToList();

            return Ok(refunds);
        }

        /// <summary>
        /// Lấy chi tiết yêu cầu hoàn tiền
        /// </summary>
        [HttpGet("{refundId}")]
        public async Task<IActionResult> GetRefundDetail(int refundId)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var refund = await _context.RefundRequests.FindAsync(refundId);

            if (refund == null || refund.ClientId != clientId)
                return NotFound();

            return Ok(refund);
        }

        public class CreateRefundRequest
        {
            public string BookingId { get; set; }
            public string Reason { get; set; }
            public string BankAccount { get; set; }
            public string BankName { get; set; }
        }
    }
}
