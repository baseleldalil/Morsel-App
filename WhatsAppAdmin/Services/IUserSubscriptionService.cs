using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Services
{
    /// <summary>
    /// Interface for user subscription business logic operations
    /// Defines high-level operations for user subscription management with business rules
    /// </summary>
    public interface IUserSubscriptionService
    {
        Task<IEnumerable<UserSubscription>> GetAllUserSubscriptionsAsync();
        Task<UserSubscription?> GetUserSubscriptionByIdAsync(int id);
        Task<UserSubscription?> GetUserSubscriptionByUserIdAsync(string? userId);
        Task<IEnumerable<UserSubscription>> GetUserSubscriptionsBySubscriptionIdAsync(int subscriptionId);
        Task<IEnumerable<UserSubscription>> GetUserSubscriptionsAsync(string userEmail);
        Task<UserSubscription> CreateUserSubscriptionAsync(UserSubscription userSubscription);
        Task<UserSubscription> UpdateUserSubscriptionAsync(UserSubscription userSubscription);
        Task<bool> DeleteUserSubscriptionAsync(int id);
        Task<bool> ValidateUserSubscriptionAsync(UserSubscription userSubscription);
        Task<IEnumerable<UserSubscription>> GetExpiringSubscriptionsAsync(int daysBeforeExpiry = 7);
        Task<bool> CanUserSendMessageAsync(string? userId);
        Task IncrementUserMessageCountAsync(string? userId);
        Task ResetDailyMessageCountsAsync();
    }
}