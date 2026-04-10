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
    public class CouponApiController : ControllerBase
    {
        private readonly CouponService _couponService;

        public CouponApiController(CouponService couponService)
        {
            _couponService = couponService;
        }

        /// <summary>
        /// Kiểm tra mã coupon có hợp lệ không
        /// </summary>
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponRequest request)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var (isValid, message, discountAmount) = await _couponService.ValidateCoupon(
                request.Code,
                clientId,
                request.OrderAmount);

            return Ok(new
            {
                isValid,
                message,
                discountAmount
            });
        }

        public class ValidateCouponRequest
        {
            public string Code { get; set; }
            public decimal OrderAmount { get; set; }
        }
    }
}
