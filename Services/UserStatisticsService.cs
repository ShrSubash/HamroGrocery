using HamroGrocery.Data;
using HamroGrocery.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HamroGrocery.Services
{
    public class UserStatisticsService : IUserStatisticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserStatisticsService> _logger;

        public UserStatisticsService(ApplicationDbContext context, ILogger<UserStatisticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task UpdateUserStatisticsAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return;

                // CRITICAL: Mark as modified so EF saves changes
                _context.Entry(user).State = EntityState.Modified;

                // FIXED: Now counts Delivered orders (not Completed!)
                var deliveredOrders = await _context.Orders
                    .Where(o => o.CustomerId == userId && o.Status == OrderStatus.Delivered)
                    .ToListAsync();

                user.TotalPurchases = deliveredOrders.Count;
                user.TotalSpent = deliveredOrders.Sum(o => o.TotalAmount);

                var loyaltySettings = await _context.LoyaltySettings.FirstOrDefaultAsync();
                if (loyaltySettings?.IsLoyaltyProgramEnabled == true)
                {
                    user.LoyaltyPoints = (int)(user.TotalSpent * loyaltySettings.PointsPerDollarSpent);
                }
                else
                {
                    user.LoyaltyPoints = 0;
                }

                user.MembershipTier = CalculateMembershipTier(user.TotalPurchases, loyaltySettings);
                user.ReviewCount = await _context.Reviews.CountAsync(r => r.UserId == userId && !r.IsDeleted);
                user.ComplaintCount = await _context.Complaints.CountAsync(c => c.UserId == userId && !c.IsDeleted);
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} stats updated → {user.TotalPurchases} orders, AUD {user.TotalSpent}, {user.LoyaltyPoints} pts, Tier: {user.MembershipTier}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to update stats for user {userId}");
            }
        }
        public async Task RecalculateAllUserStatisticsAsync()
        {
            try
            {
                var users = await _context.Users.ToListAsync();

                foreach (var user in users)
                {
                    await UpdateUserStatisticsAsync(user.Id);
                }

                _logger.LogInformation($"Recalculated statistics for {users.Count} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating all user statistics");
            }
        }

        private MembershipTier CalculateMembershipTier(int totalPurchases, LoyaltySettings? settings)
        {
            if (settings == null) return MembershipTier.Bronze;

            if (totalPurchases >= settings.DiamondRequirement) return MembershipTier.Diamond;
            if (totalPurchases >= settings.PlatinumRequirement) return MembershipTier.Platinum;
            if (totalPurchases >= settings.GoldRequirement) return MembershipTier.Gold;
            if (totalPurchases >= settings.SilverRequirement) return MembershipTier.Silver;
            return MembershipTier.Bronze;
        }
    }
}