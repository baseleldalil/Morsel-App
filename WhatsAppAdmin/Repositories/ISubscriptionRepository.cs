using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Repositories
{
    /// <summary>
    /// Interface for subscription repository operations
    /// Defines async CRUD operations for subscription management
    /// </summary>
    public interface ISubscriptionRepository
    {
        Task<IEnumerable<Subscription>> GetAllAsync();
        Task<Subscription?> GetByIdAsync(int id);
        Task<Subscription?> GetByIdWithPermissionsAsync(int id);
        Task<Subscription> CreateAsync(Subscription subscription);
        Task<Subscription> UpdateAsync(Subscription subscription);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> NameExistsAsync(string name, int? excludeId = null);
    }
}