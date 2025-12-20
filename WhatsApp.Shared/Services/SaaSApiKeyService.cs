using Microsoft.EntityFrameworkCore;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;
using System.Security.Cryptography;
using System.Text;

namespace WhatsApp.Shared.Services
{
    public class SaaSApiKeyService : ISaaSApiKeyService
    {
        private readonly SaaSDbContext _context;

        public SaaSApiKeyService(SaaSDbContext context)
        {
            _context = context;
        }

        public async Task<ApiKey?> GetApiKeyAsync(string key)
        {
            return await _context.ApiKeys
                .Include(ak => ak.User)
                .Include(ak => ak.SubscriptionPlan)
                .FirstOrDefaultAsync(ak => ak.Key == key && ak.IsActive);
        }

        public async Task<IEnumerable<ApiKey>> GetUserApiKeysAsync(string userId)
        {
            return await _context.ApiKeys
                .Include(ak => ak.SubscriptionPlan)
                .Where(ak => ak.UserId == userId && ak.IsActive)
                .OrderByDescending(ak => ak.CreatedAt)
                .ToListAsync();
        }

        public async Task<ApiKey> CreateApiKeyAsync(string userId, int subscriptionPlanId, string name)
        {
            var user = await _context.Users.FindAsync(userId);
            var plan = await _context.SubscriptionPlans.FindAsync(subscriptionPlanId);

            if (user == null || plan == null)
                throw new ArgumentException("Invalid user or subscription plan");

            // Check if user has reached API key limit
            var existingKeysCount = await _context.ApiKeys
                .CountAsync(ak => ak.UserId == userId && ak.IsActive);

            if (existingKeysCount >= plan.MaxApiKeys)
                throw new InvalidOperationException($"Maximum API keys limit ({plan.MaxApiKeys}) reached for this plan");

            var apiKey = new ApiKey
            {
                UserId = userId,
                SubscriptionPlanId = subscriptionPlanId,
                Key = GenerateApiKey(),
                Name = name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow,
                LastUsageResetAt = DateTime.UtcNow.Date
            };

            _context.ApiKeys.Add(apiKey);
            await _context.SaveChangesAsync();

            // Load related entities
            apiKey.User = user;
            apiKey.SubscriptionPlan = plan;

            return apiKey;
        }

        public async Task<bool> RevokeApiKeyAsync(string key, string userId)
        {
            var apiKey = await _context.ApiKeys
                .FirstOrDefaultAsync(ak => ak.Key == key && ak.UserId == userId && ak.IsActive);

            if (apiKey == null) return false;

            apiKey.IsActive = false;
            apiKey.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ValidateApiKeyAsync(string key)
        {
            var apiKey = await GetApiKeyAsync(key);
            if (apiKey == null || !apiKey.IsActive) return false;

            // Check if user has active subscription
            var hasActiveSubscription = await _context.UserSubscriptions
                .AnyAsync(us => us.UserId == apiKey.UserId && us.IsActive && us.EndDate > DateTime.UtcNow);

            return hasActiveSubscription;
        }

        public async Task UpdateLastUsedAsync(string key)
        {
            var apiKey = await _context.ApiKeys.FirstOrDefaultAsync(ak => ak.Key == key);
            if (apiKey != null)
            {
                apiKey.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> CanMakeApiCallAsync(string key)
        {
            var apiKey = await GetApiKeyAsync(key);
            if (apiKey == null || !apiKey.IsActive) return false;

            // Reset daily quota if it's a new day
            if (apiKey.LastUsageResetAt.Date < DateTime.UtcNow.Date)
            {
                apiKey.DailyQuotaUsed = 0;
                apiKey.LastUsageResetAt = DateTime.UtcNow.Date;
                await _context.SaveChangesAsync();
            }

            return apiKey.DailyQuotaUsed < apiKey.SubscriptionPlan.MaxMessagesPerDay;
        }

        public async Task IncrementUsageAsync(string key, int messageCount)
        {
            var apiKey = await _context.ApiKeys.FirstOrDefaultAsync(ak => ak.Key == key);
            if (apiKey != null)
            {
                apiKey.DailyQuotaUsed += messageCount;
                apiKey.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private string GenerateApiKey()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            const int keyLength = 32;

            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[keyLength];
            rng.GetBytes(bytes);

            var result = new StringBuilder(keyLength);
            for (int i = 0; i < keyLength; i++)
            {
                result.Append(chars[bytes[i] % chars.Length]);
            }

            return $"wa_{result}";
        }
    }
}