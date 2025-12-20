using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Repositories
{
    /// <summary>
    /// Interface for user subscription repository operations
    /// Defines async CRUD operations for user subscription management
    /// </summary>
    public interface IUserSubscriptionRepository
    {
        Task<IEnumerable<UserSubscription>> GetAllAsync();
        Task<UserSubscription?> GetByIdAsync(int id);
        Task<UserSubscription?> GetByUserIdAsync(string userId);
        Task<IEnumerable<UserSubscription>> GetBySubscriptionIdAsync(int subscriptionId);
        Task<IEnumerable<UserSubscription>> GetByUserEmailAsync(string userEmail);
        Task<UserSubscription> CreateAsync(UserSubscription userSubscription);
        Task<UserSubscription> UpdateAsync(UserSubscription userSubscription);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> UserHasActiveSubscriptionAsync(int userId);
        Task<IEnumerable<UserSubscription>> GetExpiringSubscriptionsAsync(DateTime beforeDate);
    }
}