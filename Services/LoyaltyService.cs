using Microsoft.EntityFrameworkCore;
using PhoStudioMVC.Data;
using PhoStudioMVC.Models.Entities;
using PhoStudioMVC.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PhoStudioMVC.Services
{
    public class LoyaltyService
    {
        private readonly ApplicationDbContext _context;
        private static DateTime VnNow => TimeHelper.VnNow;

        private const long POINTS_PER_1MILLION = 100; // 1M ? = 100 ?i?m

        public LoyaltyService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Thêm ?i?m loyalty khi booking thanh toán
        /// </summary>
        public async Task AddLoyaltyPoints(string clientId, string bookingId, decimal amount, string reason)
        {
            var loyalty = await _context.CustomerLoyalties
                .FirstOrDefaultAsync(l => l.ClientId == clientId);

            if (loyalty == null)
            {
                loyalty = new CustomerLoyalty
                {
                    ClientId = clientId,
                    CreatedAt = VnNow
                };
                _context.CustomerLoyalties.Add(loyalty);
                await _context.SaveChangesAsync();
            }

            long points = (long)(amount / 1_000_000 * POINTS_PER_1MILLION);

            loyalty.TotalPoints += points;
            loyalty.AvailablePoints += points;
            loyalty.TotalSpent += amount;
            loyalty.LastPurchaseAt = VnNow;

            // C?p nh?t membership tier
            UpdateMembershipTier(loyalty);

            var transaction = new LoyaltyTransaction
            {
                ClientId = clientId,
                Points = points,
                Reason = reason,
                BookingId = bookingId
            };

            _context.LoyaltyTransactions.Add(transaction);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Tr? ?i?m khi khách s? d?ng ?i?m
        /// </summary>
        public async Task<bool> UsePoints(string clientId, long pointsToUse, string reason)
        {
            var loyalty = await _context.CustomerLoyalties
                .FirstOrDefaultAsync(l => l.ClientId == clientId);

            if (loyalty == null || loyalty.AvailablePoints < pointsToUse)
                return false;

            loyalty.AvailablePoints -= pointsToUse;

            var transaction = new LoyaltyTransaction
            {
                ClientId = clientId,
                Points = -pointsToUse,
                Reason = reason
            };

            _context.LoyaltyTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// T?o mã gi?i thi?u cho khách
        /// </summary>
        public async Task<string> GenerateReferralCode(string clientId)
        {
            var existing = await _context.ReferralCodes
                .FirstOrDefaultAsync(r => r.ClientId == clientId && r.IsActive);

            if (existing != null)
                return existing.Code;

            string code = GenerateUniqueCode();
            var referralCode = new ReferralCode
            {
                Code = code,
                ClientId = clientId,
                IsActive = true
            };

            _context.ReferralCodes.Add(referralCode);
            await _context.SaveChangesAsync();

            return code;
        }

        /// <summary>
        /// Áp d?ng mã gi?i thi?u
        /// </summary>
        public async Task<bool> ApplyReferralCode(string newClientId, string referralCode)
        {
            var code = await _context.ReferralCodes
                .FirstOrDefaultAsync(r => r.Code == referralCode && r.IsActive);

            if (code == null)
                return false;

            // Ki?m tra t?ng l?n dùng
            if (code.MaxUsageCount > 0 && code.UsageCount >= code.MaxUsageCount)
                return false;

            // C?ng ?i?m cho ng??i ???c gi?i thi?u
            await AddLoyaltyPoints(newClientId, "", 0, $"S? d?ng mã gi?i thi?u {referralCode}");

            // C?ng ?i?m cho ng??i gi?i thi?u
            await AddLoyaltyPoints(code.ClientId, "", 0, $"Khách dùng mã gi?i thi?u");

            code.UsageCount++;
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// C?p nh?t membership tier d?a trên t?ng chi tiêu
        /// </summary>
        private void UpdateMembershipTier(CustomerLoyalty loyalty)
        {
            if (loyalty.TotalSpent >= 100_000_000)
                loyalty.MembershipTier = "Platinum";
            else if (loyalty.TotalSpent >= 50_000_000)
                loyalty.MembershipTier = "Gold";
            else if (loyalty.TotalSpent >= 20_000_000)
                loyalty.MembershipTier = "Silver";
            else if (loyalty.TotalSpent >= 5_000_000)
                loyalty.MembershipTier = "Bronze";
            else
                loyalty.MembershipTier = "Regular";
        }

        private static string GenerateUniqueCode()
        {
            return "REF" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        }
    }
}
