using Microsoft.EntityFrameworkCore;
using PhoStudioMVC.Data;
using PhoStudioMVC.Models.Entities;
using PhoStudioMVC.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PhoStudioMVC.Services
{
    public class CouponService
    {
        private readonly ApplicationDbContext _context;
        private static DateTime VnNow => TimeHelper.VnNow;

        public CouponService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Ki?m tra mã coupon có h?p l? không
        /// </summary>
        public async Task<(bool IsValid, string Message, decimal DiscountAmount)> ValidateCoupon(string code, string clientId, decimal orderAmount)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == code.ToUpper() && c.IsActive);

            if (coupon == null)
                return (false, "Mã coupon không t?n t?i.", 0);

            // Ki?m tra th?i h?n
            if (VnNow < coupon.ValidFrom || VnNow > coupon.ValidUntil)
                return (false, "Mã coupon ?ã h?t h?n.", 0);

            // Ki?m tra t?ng l?n dùng
            if (coupon.MaxUsageCount.HasValue && coupon.CurrentUsageCount >= coupon.MaxUsageCount)
                return (false, "Mã coupon ?ã h?t s? l?n s? d?ng.", 0);

            // Ki?m tra t?ng l?n dùng/khách
            if (coupon.MaxUsagePerUser.HasValue)
            {
                var userUsageCount = await _context.BookingCoupons
                    .Where(bc => bc.CouponId == coupon.Id && bc.Booking!.ClientId == clientId)
                    .CountAsync();

                if (userUsageCount >= coupon.MaxUsagePerUser)
                    return (false, "B?n ?ã dùng h?t l?n v?i mã coupon này.", 0);
            }

            // Ki?m tra giá t?i thi?u
            if (coupon.MinOrderAmount.HasValue && orderAmount < coupon.MinOrderAmount)
                return (false, $"??n hàng ph?i t? {coupon.MinOrderAmount:N0}? tr? lên.", 0);

            // Tính ti?n gi?m
            decimal discountAmount = (orderAmount * coupon.DiscountPercent) / 100;

            if (coupon.MaxDiscountAmount.HasValue && discountAmount > coupon.MaxDiscountAmount)
                discountAmount = coupon.MaxDiscountAmount.Value;

            return (true, "Coupon h?p l?", discountAmount);
        }

        /// <summary>
        /// Áp d?ng coupon vào booking
        /// </summary>
        public async Task<bool> ApplyCoupon(string bookingId, string couponCode, decimal discountAmount)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == couponCode.ToUpper());

            if (coupon == null)
                return false;

            var bookingCoupon = new BookingCoupon
            {
                BookingId = bookingId,
                CouponId = coupon.Id,
                DiscountAmount = discountAmount
            };

            _context.BookingCoupons.Add(bookingCoupon);
            coupon.CurrentUsageCount++;
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// L?y t?ng discount c?a m?t booking
        /// </summary>
        public async Task<decimal> GetTotalDiscount(string bookingId)
        {
            return await _context.BookingCoupons
                .Where(bc => bc.BookingId == bookingId)
                .SumAsync(bc => bc.DiscountAmount);
        }
    }
}
