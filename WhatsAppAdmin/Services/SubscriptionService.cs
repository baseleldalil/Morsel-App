using WhatsAppAdmin.Models;
using WhatsAppAdmin.Repositories;
using WhatsApp.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace WhatsAppAdmin.Services
{
    /// <summary>
    /// Service implementation for subscription business logic
    /// Handles complex subscription operations with business rules and validation
    /// </summary>
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        // private readonly IPermissionRepository _permissionRepository; // Permission not in SaaSDbContext
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly SaaSDbContext _saasContext;

        public SubscriptionService(
            ISubscriptionRepository subscriptionRepository,
            // IPermissionRepository permissionRepository, // Permission not in SaaSDbContext
            IUserSubscriptionRepository userSubscriptionRepository,
            SaaSDbContext saasContext)
        {
            _subscriptionRepository = subscriptionRepository;
            // _permissionRepository = permissionRepository;
            _userSubscriptionRepository = userSubscriptionRepository;
            _saasContext = saasContext;
        }

        public async Task<IEnumerable<Subscription>> GetAllSubscriptionsAsync()
        {
            return await _subscriptionRepository.GetAllAsync();
        }

        public async Task<Subscription?> GetSubscriptionByIdAsync(int id)
        {
            return await _subscriptionRepository.GetByIdAsync(id);
        }

        public async Task<Subscription?> GetSubscriptionWithPermissionsAsync(int id)
        {
            return await _subscriptionRepository.GetByIdWithPermissionsAsync(id);
        }

        public async Task<Subscription> CreateSubscriptionAsync(Subscription subscription, IEnumerable<int> permissionIds)
        {
            // Permission functionality removed - not in SaaSDbContext
            // Simply create subscription without permissions
            return await _subscriptionRepository.CreateAsync(subscription);
        }

        public async Task<Subscription> UpdateSubscriptionAsync(Subscription subscription, IEnumerable<int> permissionIds)
        {
            // Permission functionality removed - not in SaaSDbContext
            // Simply update subscription without permissions
            return await _subscriptionRepository.UpdateAsync(subscription);
        }

        public async Task<bool> DeleteSubscriptionAsync(int id)
        {
            // Check if subscription can be deleted
            if (!await CanDeleteSubscriptionAsync(id))
                return false;

            return await _subscriptionRepository.DeleteAsync(id);
        }

        public async Task<bool> CanDeleteSubscriptionAsync(int id)
        {
            // Check if any users are assigned to this subscription
            var userSubscriptions = await _userSubscriptionRepository.GetBySubscriptionIdAsync(id);
            return !userSubscriptions.Any(us => us.IsActive);
        }

        public async Task<bool> ValidateSubscriptionAsync(Subscription subscription)
        {
            // Check if name already exists
            if (await _subscriptionRepository.NameExistsAsync(subscription.Name, subscription.Id))
                return false;

            // Validate business rules
            if (subscription.MaxMessagesPerDay <= 0)
                return false;

            if (subscription.Price < 0)
                return false;

            return true;
        }

        public async Task<IEnumerable<Permission>> GetAvailablePermissionsAsync()
        {
            // Permission functionality removed - not in SaaSDbContext
            return await Task.FromResult(Enumerable.Empty<Permission>());
        }

        // Permission functionality removed - not in SaaSDbContext
        // private async Task AssignPermissionsToSubscriptionAsync(int subscriptionId, IEnumerable<int> permissionIds)
        // {
        //     ...
        // }
    }
}