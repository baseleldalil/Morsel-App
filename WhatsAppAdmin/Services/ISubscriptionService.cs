using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Services
{
    /// <summary>
    /// Interface for subscription business logic operations
    /// Defines high-level operations for subscription management with business rules
    /// </summary>
    public interface ISubscriptionService
    {
        Task<IEnumerable<Subscription>> GetAllSubscriptionsAsync();
        Task<Subscription?> GetSubscriptionByIdAsync(int id);
        Task<Subscription?> GetSubscriptionWithPermissionsAsync(int id);
        Task<Subscription> CreateSubscriptionAsync(Subscription subscription, IEnumerable<int> permissionIds);
        Task<Subscription> UpdateSubscriptionAsync(Subscription subscription, IEnumerable<int> permissionIds);
        Task<bool> DeleteSubscriptionAsync(int id);
        Task<bool> CanDeleteSubscriptionAsync(int id);
        Task<bool> ValidateSubscriptionAsync(Subscription subscription);
        Task<IEnumerable<Permission>> GetAvailablePermissionsAsync();
    }
}