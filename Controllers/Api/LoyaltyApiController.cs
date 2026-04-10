using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoStudioMVC.Services;
using PhoStudioMVC.Data;
using System.Security.Claims;

namespace PhoStudioMVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LoyaltyApiController : ControllerBase
    {
        private readonly LoyaltyService _loyaltyService;
        private readonly ApplicationDbContext _context;

        public LoyaltyApiController(LoyaltyService loyaltyService, ApplicationDbContext context)
        {
            _loyaltyService = loyaltyService;
            _context = context;
        }

        /// <summary>
        /// Lấy thông tin loyalty của khách
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetLoyaltyProfile()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var loyalty = await _context.CustomerLoyalties.FindAsync(clientId);

            if (loyalty == null)
                return NotFound(new { message = "Chưa có thông tin loyalty" });

            return Ok(loyalty);
        }

        /// <summary>
        /// Tạo mã giới thiệu
        /// </summary>
        [HttpPost("generate-referral")]
        public async Task<IActionResult> GenerateReferralCode()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var code = await _loyaltyService.GenerateReferralCode(clientId);

            return Ok(new { code });
        }

        /// <summary>
        /// Áp dụng mã giới thiệu
        /// </summary>
        [HttpPost("apply-referral")]
        public async Task<IActionResult> ApplyReferralCode([FromBody] ApplyReferralRequest request)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var success = await _loyaltyService.ApplyReferralCode(clientId, request.ReferralCode);

            if (!success)
                return BadRequest(new { message = "Mã giới thiệu không hợp lệ" });

            return Ok(new { message = "Áp dụng mã giới thiệu thành công" });
        }

        /// <summary>
        /// Lấy lịch sử loyalty transactions
        /// </summary>
        [HttpGet("transactions")]
        public IActionResult GetTransactions()
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var transactions = _context.LoyaltyTransactions
                .Where(t => t.ClientId == clientId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(50)
                .ToList();

            return Ok(transactions);
        }

        public class ApplyReferralRequest
        {
            public string ReferralCode { get; set; }
        }
    }
}
