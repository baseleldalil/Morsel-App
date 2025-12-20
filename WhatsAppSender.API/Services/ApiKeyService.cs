using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;

namespace WhatsAppSender.API.Services
{
    public interface IApiKeyService
    {
        Task<ApiKey?> ValidateApiKeyAsync(string apiKey);
        Task<ApiKey> CreateApiKeyAsync(string userId, int subscriptionPlanId, string name);
        Task<bool> CheckQuotaAsync(int apiKeyId);
        Task UpdateUsageAsync(int apiKeyId, int messageCount);
        Task<ApiUsageStats> GetUsageStatsAsync(string apiKey);
        Task<List<ApiKey>> GetUserApiKeysAsync(string userId);
        Task<bool> RevokeApiKeyAsync(int apiKeyId, string userId);
    }

    public class ApiKeyService : IApiKeyService
    {
        private readonly SaaSDbContext _context;
        private readonly ILogger<ApiKeyService> _logger;
        private readonly IMemoryCache _cache;
        private readonly Models.ApiKeySettings _settings;

        public ApiKeyService(
            SaaSDbContext context,
            ILogger<ApiKeyService> logger,
            IMemoryCache cache,
            IOptions<Models.ApiKeySettings> settings)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _settings = settings.Value;
        }

        public async Task<ApiKey?> ValidateApiKeyAsync(string apiKey)
        {
            // Try to get from cache first
            var cacheKey = $"apikey_{apiKey}";
            if (_cache.TryGetValue(cacheKey, out ApiKey? cachedKey))
            {
                // Still need to check expiration and daily reset
                if (cachedKey != null &&
                    (!cachedKey.ExpiresAt.HasValue || cachedKey.ExpiresAt >= DateTime.UtcNow))
                {
                    // Check if daily usage needs reset
                    if (cachedKey.LastUsageResetAt < DateTime.UtcNow.Date)
                    {
                        cachedKey.DailyQuotaUsed = 0;
                        cachedKey.LastUsageResetAt = DateTime.UtcNow.Date;
                        // Update database asynchronously
                        _ = Task.Run(async () =>
                        {
                            var dbKey = await _context.ApiKeys.FindAsync(cachedKey.Id);
                            if (dbKey != null)
                            {
                                dbKey.DailyQuotaUsed = 0;
                                dbKey.LastUsageResetAt = DateTime.UtcNow.Date;
                                await _context.SaveChangesAsync();
                            }
                        });
                    }
                    return cachedKey;
                }
            }

            // Cache miss or invalid - fetch from database
            var key = await _context.ApiKeys
                .AsNoTracking()
                .Include(k => k.SubscriptionPlan)
                .Include(k => k.User)
                .FirstOrDefaultAsync(k => k.Key == apiKey && k.IsActive);

            if (key == null)
            {
                var maskedKey = apiKey?.Length > 8 ? apiKey.Substring(0, 8) + "..." : apiKey ?? "null";
                _logger.LogWarning("Invalid API key attempted: {ApiKey}", maskedKey);
                return null;
            }

            // Check if key is expired
            if (key.ExpiresAt.HasValue && key.ExpiresAt < DateTime.UtcNow)
            {
                var maskedKey = apiKey?.Length > 8 ? apiKey.Substring(0, 8) + "..." : apiKey ?? "null";
                _logger.LogWarning("Expired API key attempted: {ApiKey}", maskedKey);
                return null;
            }

            // Reset daily usage if it's a new day
            if (key.LastUsageResetAt < DateTime.UtcNow.Date)
            {
                key.DailyQuotaUsed = 0;
                key.LastUsageResetAt = DateTime.UtcNow.Date;
                // Update in database
                var trackedKey = await _context.ApiKeys.FindAsync(key.Id);
                if (trackedKey != null)
                {
                    trackedKey.DailyQuotaUsed = 0;
                    trackedKey.LastUsageResetAt = DateTime.UtcNow.Date;
                    await _context.SaveChangesAsync();
                }
            }

            // Cache the result
            _cache.Set(cacheKey, key, TimeSpan.FromMinutes(_settings.CacheExpirationMinutes));

            return key;
        }

        public async Task<ApiKey> CreateApiKeyAsync(string userId, int subscriptionPlanId, string name)
        {
            var apiKey = new ApiKey
            {
                Key = GenerateApiKey(),
                UserId = userId,
                SubscriptionPlanId = subscriptionPlanId,
                Name = name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddYears(1), // Keys expire in 1 year
                LastUsageResetAt = DateTime.UtcNow.Date
            };

            _context.ApiKeys.Add(apiKey);
            await _context.SaveChangesAsync();

            _logger.LogInformation("API key created for user: {UserId}", userId);
            return apiKey;
        }

        public async Task<bool> CheckQuotaAsync(int apiKeyId)
        {
            var apiKey = await _context.ApiKeys
                .Include(k => k.SubscriptionPlan)
                .FirstOrDefaultAsync(k => k.Id == apiKeyId);

            if (apiKey?.SubscriptionPlan == null)
                return false;

            return apiKey.DailyQuotaUsed < apiKey.SubscriptionPlan.MaxMessagesPerDay;
        }

        public async Task UpdateUsageAsync(int apiKeyId, int messageCount)
        {
            var apiKey = await _context.ApiKeys.FindAsync(apiKeyId);
            if (apiKey != null)
            {
                apiKey.TotalMessagesUsed += messageCount;
                apiKey.DailyQuotaUsed += messageCount;
                apiKey.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usage updated for API key {ApiKeyId}: +{MessageCount} messages",
                    apiKeyId, messageCount);
            }
        }

        public async Task<ApiUsageStats> GetUsageStatsAsync(string apiKey)
        {
            var key = await _context.ApiKeys
                .Include(k => k.SubscriptionPlan)
                .FirstOrDefaultAsync(k => k.Key == apiKey);

            if (key?.SubscriptionPlan == null)
            {
                return new ApiUsageStats
                {
                    SubscriptionPlan = "Invalid"
                };
            }

            return new ApiUsageStats
            {
                TotalMessages = key.TotalMessagesUsed,
                TodayMessages = key.DailyQuotaUsed,
                RemainingQuota = key.SubscriptionPlan.MaxMessagesPerDay - key.DailyQuotaUsed,
                LastUsed = key.LastUsedAt.HasValue ? key.LastUsedAt.Value : key.CreatedAt,
                SubscriptionPlan = key.SubscriptionPlan.Name
            };
        }

        public async Task<List<ApiKey>> GetUserApiKeysAsync(string userId)
        {
            return await _context.ApiKeys
                .Include(k => k.SubscriptionPlan)
                .Where(k => k.UserId == userId)
                .OrderByDescending(k => k.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> RevokeApiKeyAsync(int apiKeyId, string userId)
        {
            var apiKey = await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.Id == apiKeyId && k.UserId == userId);

            if (apiKey == null)
                return false;

            apiKey.IsActive = false;
            await _context.SaveChangesAsync();

            // Invalidate cache
            _cache.Remove($"apikey_{apiKey.Key}");

            _logger.LogInformation("API key {ApiKeyId} revoked for user: {UserId}", apiKeyId, userId);
            return true;
        }

        private static string GenerateApiKey()
        {
            const string prefix = "wapp_";
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);

            var result = new char[32];
            for (int i = 0; i < bytes.Length; i++)
            {
                result[i] = chars[bytes[i] % chars.Length];
            }

            return prefix + new string(result);
        }
    }

    public class ApiUsageStats
    {
        public int TotalMessages { get; set; }
        public int TodayMessages { get; set; }
        public int RemainingQuota { get; set; }
        public DateTime LastUsed { get; set; }
        public string SubscriptionPlan { get; set; } = string.Empty;
    }
}