using Microsoft.EntityFrameworkCore;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;

namespace WhatsApp.Shared.Services
{
    public class SaaSSubscriptionService : ISaaSSubscriptionService
    {
        private readonly SaaSDbContext _context;

        public SaaSSubscriptionService(SaaSDbContext context)
        {
            _context = context;
        }

        // Subscription Plans
        public async Task<IEnumerable<SubscriptionPlan>> GetAllActivePlansAsync()
        {
            return await _context.SubscriptionPlans
                .Where(sp => sp.IsActive)
                .OrderBy(sp => sp.Price)
                .ToListAsync();
        }

        public async Task<SubscriptionPlan?> GetPlanByIdAsync(int planId)
        {
            return await _context.SubscriptionPlans
                .FirstOrDefaultAsync(sp => sp.Id == planId && sp.IsActive);
        }

        public async Task<SubscriptionPlan> CreatePlanAsync(SubscriptionPlan plan)
        {
            plan.CreatedAt = DateTime.UtcNow;
            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();
            return plan;
        }

        public async Task<SubscriptionPlan> UpdatePlanAsync(SubscriptionPlan plan)
        {
            plan.UpdatedAt = DateTime.UtcNow;
            _context.SubscriptionPlans.Update(plan);
            await _context.SaveChangesAsync();
            return plan;
        }

        public async Task<bool> DeletePlanAsync(int planId)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(planId);
            if (plan == null) return false;

            plan.IsActive = false;
            plan.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        // User Subscriptions
        public async Task<UserSubscription?> GetActiveUserSubscriptionAsync(string userId)
        {
            return await _context.UserSubscriptions
                .Include(us => us.SubscriptionPlan)
                .Where(us => us.UserId == userId && us.IsActive && us.EndDate > DateTime.UtcNow)
                .OrderByDescending(us => us.StartDate)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<UserSubscription>> GetUserSubscriptionsAsync(string userId)
        {
            return await _context.UserSubscriptions
                .Include(us => us.SubscriptionPlan)
                .Where(us => us.UserId == userId)
                .OrderByDescending(us => us.StartDate)
                .ToListAsync();
        }

        public async Task<UserSubscription> CreateUserSubscriptionAsync(string userId, int planId, decimal amountPaid, string paymentMethod = "Demo")
        {
            var plan = await GetPlanByIdAsync(planId);
            if (plan == null)
                throw new ArgumentException("Invalid subscription plan");

            // Cancel existing active subscription
            var existingSubscription = await GetActiveUserSubscriptionAsync(userId);
            if (existingSubscription != null)
            {
                existingSubscription.IsActive = false;
                existingSubscription.CancelledAt = DateTime.UtcNow;
            }

            var subscription = new UserSubscription
            {
                UserId = userId,
                SubscriptionPlanId = planId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1), // Default to 1 month
                AmountPaid = amountPaid,
                PaymentMethod = paymentMethod,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            // Load the plan for return
            subscription.SubscriptionPlan = plan;
            return subscription;
        }

        public async Task<bool> CancelSubscriptionAsync(int subscriptionId, string userId)
        {
            var subscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(us => us.Id == subscriptionId && us.UserId == userId && us.IsActive);

            if (subscription == null) return false;

            subscription.IsActive = false;
            subscription.CancelledAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RenewSubscriptionAsync(int subscriptionId)
        {
            var subscription = await _context.UserSubscriptions
                .Include(us => us.SubscriptionPlan)
                .FirstOrDefaultAsync(us => us.Id == subscriptionId);

            if (subscription == null) return false;

            subscription.EndDate = subscription.EndDate.AddMonths(1);
            await _context.SaveChangesAsync();
            return true;
        }

        // Usage & Quota Management
        public async Task<int> GetRemainingQuotaAsync(string userId)
        {
            var subscription = await GetActiveUserSubscriptionAsync(userId);
            if (subscription == null) return 0;

            var today = DateTime.UtcNow.Date;
            var usedToday = await _context.MessageHistory
                .Where(mh => mh.UserId == userId && mh.SentAt.Date == today && mh.Status == "Sent")
                .CountAsync();

            return Math.Max(0, subscription.SubscriptionPlan.MaxMessagesPerDay - usedToday);
        }

        public async Task<bool> CanSendMessagesAsync(string userId, int messageCount)
        {
            var remainingQuota = await GetRemainingQuotaAsync(userId);
            return remainingQuota >= messageCount;
        }

        public async Task UpdateUsageAsync(string userId, int messagesSent)
        {
            var today = DateTime.UtcNow.Date;
            var stats = await _context.UsageStatistics
                .FirstOrDefaultAsync(us => us.UserId == userId && us.Date == today);

            if (stats == null)
            {
                stats = new UsageStatistic
                {
                    UserId = userId,
                    Date = today,
                    MessagesSent = messagesSent
                };
                _context.UsageStatistics.Add(stats);
            }
            else
            {
                stats.MessagesSent += messagesSent;
            }

            await _context.SaveChangesAsync();
        }

        public async Task ResetDailyQuotasAsync()
        {
            // Reset API key daily usage
            var apiKeys = await _context.ApiKeys
                .Where(ak => ak.IsActive && ak.LastUsageResetAt.Date < DateTime.UtcNow.Date)
                .ToListAsync();

            foreach (var apiKey in apiKeys)
            {
                apiKey.DailyQuotaUsed = 0;
                apiKey.LastUsageResetAt = DateTime.UtcNow.Date;
            }

            await _context.SaveChangesAsync();
        }

        // Analytics
        public async Task<UsageStatistic> GetDailyUsageAsync(string userId, DateTime date)
        {
            return await _context.UsageStatistics
                .FirstOrDefaultAsync(us => us.UserId == userId && us.Date.Date == date.Date)
                ?? new UsageStatistic { UserId = userId, Date = date.Date };
        }

        public async Task<IEnumerable<UsageStatistic>> GetUsageHistoryAsync(string userId, DateTime fromDate, DateTime toDate)
        {
            return await _context.UsageStatistics
                .Where(us => us.UserId == userId && us.Date >= fromDate.Date && us.Date <= toDate.Date)
                .OrderByDescending(us => us.Date)
                .ToListAsync();
        }

        public async Task UpdateUsageStatisticsAsync(string userId, int messagesSent, int delivered, int failed, decimal cost)
        {
            var today = DateTime.UtcNow.Date;
            var stats = await _context.UsageStatistics
                .FirstOrDefaultAsync(us => us.UserId == userId && us.Date == today);

            if (stats == null)
            {
                stats = new UsageStatistic
                {
                    UserId = userId,
                    Date = today
                };
                _context.UsageStatistics.Add(stats);
            }

            stats.MessagesSent += messagesSent;
            stats.MessagesDelivered += delivered;
            stats.MessagesFailed += failed;
            stats.TotalCost += cost;

            await _context.SaveChangesAsync();
        }
    }
}