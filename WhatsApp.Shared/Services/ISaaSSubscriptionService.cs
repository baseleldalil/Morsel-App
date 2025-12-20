using WhatsApp.Shared.Models;

namespace WhatsApp.Shared.Services
{
    public interface ISaaSSubscriptionService
    {
        // Subscription Plans
        Task<IEnumerable<SubscriptionPlan>> GetAllActivePlansAsync();
        Task<SubscriptionPlan?> GetPlanByIdAsync(int planId);
        Task<SubscriptionPlan> CreatePlanAsync(SubscriptionPlan plan);
        Task<SubscriptionPlan> UpdatePlanAsync(SubscriptionPlan plan);
        Task<bool> DeletePlanAsync(int planId);

        // User Subscriptions
        Task<UserSubscription?> GetActiveUserSubscriptionAsync(string userId);
        Task<IEnumerable<UserSubscription>> GetUserSubscriptionsAsync(string userId);
        Task<UserSubscription> CreateUserSubscriptionAsync(string userId, int planId, decimal amountPaid, string paymentMethod = "Demo");
        Task<bool> CancelSubscriptionAsync(int subscriptionId, string userId);
        Task<bool> RenewSubscriptionAsync(int subscriptionId);

        // Usage & Quota Management
        Task<int> GetRemainingQuotaAsync(string userId);
        Task<bool> CanSendMessagesAsync(string userId, int messageCount);
        Task UpdateUsageAsync(string userId, int messagesSent);
        Task ResetDailyQuotasAsync();

        // Analytics
        Task<UsageStatistic> GetDailyUsageAsync(string userId, DateTime date);
        Task<IEnumerable<UsageStatistic>> GetUsageHistoryAsync(string userId, DateTime fromDate, DateTime toDate);
        Task UpdateUsageStatisticsAsync(string userId, int messagesSent, int delivered, int failed, decimal cost);
    }

    public interface ISaaSApiKeyService
    {
        Task<ApiKey?> GetApiKeyAsync(string key);
        Task<IEnumerable<ApiKey>> GetUserApiKeysAsync(string userId);
        Task<ApiKey> CreateApiKeyAsync(string userId, int subscriptionPlanId, string name);
        Task<bool> RevokeApiKeyAsync(string key, string userId);
        Task<bool> ValidateApiKeyAsync(string key);
        Task UpdateLastUsedAsync(string key);
        Task<bool> CanMakeApiCallAsync(string key);
        Task IncrementUsageAsync(string key, int messageCount);
    }

    public interface ISaaSSystemService
    {
        Task<string?> GetSettingAsync(string key);
        Task<T?> GetSettingAsync<T>(string key) where T : class;
        Task UpdateSettingAsync(string key, string value, string? updatedBy = null);
        Task<IEnumerable<SystemSetting>> GetSettingsByCategoryAsync(string category);
        Task<IEnumerable<SystemSetting>> GetAllSettingsAsync();
    }

    public interface ISaaSPaymentService
    {
        Task<PaymentTransaction> CreateTransactionAsync(string userId, decimal amount, string paymentMethod);
        Task<PaymentTransaction> ProcessPaymentAsync(int transactionId, string paymentReference);
        Task<PaymentTransaction> CompleteTransactionAsync(int transactionId, string gatewayResponse);
        Task<PaymentTransaction> FailTransactionAsync(int transactionId, string errorMessage);
        Task<IEnumerable<PaymentTransaction>> GetUserTransactionsAsync(string userId);
        Task<PaymentTransaction?> GetTransactionAsync(int transactionId);
    }

    public interface ISaaSAnalyticsService
    {
        Task<SaaSDashboardStats> GetDashboardStatsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<SaaSUserStats> GetUserStatsAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<IEnumerable<SaaSRevenueStats>> GetRevenueStatsAsync(int months = 12);
        Task<IEnumerable<SaasPlanStats>> GetPlanUsageStatsAsync();
    }

    // Analytics DTOs
    public class SaaSDashboardStats
    {
        public int TotalUsers { get; set; }
        public int ActiveSubscriptions { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public long TotalMessagesSent { get; set; }
        public int NewUsersThisMonth { get; set; }
        public decimal AverageRevenuePerUser { get; set; }
    }

    public class SaaSUserStats
    {
        public int MessagesSent { get; set; }
        public int MessagesDelivered { get; set; }
        public int MessagesFailed { get; set; }
        public decimal TotalSpent { get; set; }
        public int ApiCallsMade { get; set; }
        public string? CurrentPlan { get; set; }
        public DateTime? LastActivity { get; set; }
    }

    public class SaaSRevenueStats
    {
        public DateTime Month { get; set; }
        public decimal Revenue { get; set; }
        public int NewSubscriptions { get; set; }
        public int CancelledSubscriptions { get; set; }
    }

    public class SaasPlanStats
    {
        public string PlanName { get; set; } = string.Empty;
        public int ActiveUsers { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public double AverageUsagePercent { get; set; }
    }
}