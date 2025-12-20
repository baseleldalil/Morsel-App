using Microsoft.EntityFrameworkCore;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;
using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Repositories
{
    /// <summary>
    /// Repository implementation for subscription operations
    /// Handles all database operations related to subscriptions with async support
    /// </summary>
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly SaaSDbContext _saasContext;

        public SubscriptionRepository(SaaSDbContext saasContext)
        {
            _saasContext = saasContext;
        }

        public async Task<IEnumerable<Subscription>> GetAllAsync()
        {
            // Get subscription plans from SaaSDbContext and map to admin Subscription model
            var subscriptionPlans = await _saasContext.SubscriptionPlans
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return subscriptionPlans.Select(sp => new Subscription
            {
                Id = sp.Id,
                Name = sp.Name,
                Description = sp.Description,
                MaxMessagesPerDay = sp.MaxMessagesPerDay,
                Price = sp.Price,
                MaxApiKeys = sp.MaxApiKeys,
                HasPrioritySupport = sp.HasPrioritySupport,
                HasCustomIntegrations = sp.HasCustomIntegrations,
                HasAdvancedAnalytics = sp.HasAdvancedAnalytics,
                IsActive = sp.IsActive,
                CreatedAt = sp.CreatedAt,
                UpdatedAt = sp.UpdatedAt ?? sp.CreatedAt
            }).ToList();
        }

        public async Task<Subscription?> GetByIdAsync(int id)
        {
            var sp = await _saasContext.SubscriptionPlans.FindAsync(id);
            if (sp == null) return null;

            return new Subscription
            {
                Id = sp.Id,
                Name = sp.Name,
                Description = sp.Description,
                MaxMessagesPerDay = sp.MaxMessagesPerDay,
                Price = sp.Price,
                MaxApiKeys = sp.MaxApiKeys,
                HasPrioritySupport = sp.HasPrioritySupport,
                HasCustomIntegrations = sp.HasCustomIntegrations,
                HasAdvancedAnalytics = sp.HasAdvancedAnalytics,
                IsActive = sp.IsActive,
                CreatedAt = sp.CreatedAt,
                UpdatedAt = sp.UpdatedAt ?? sp.CreatedAt
            };
        }

        public async Task<Subscription?> GetByIdWithPermissionsAsync(int id)
        {
            // For now, return same as GetByIdAsync since permissions are handled separately
            return await GetByIdAsync(id);
        }

        public async Task<Subscription> CreateAsync(Subscription subscription)
        {
            var subscriptionPlan = new WhatsApp.Shared.Models.SubscriptionPlan
            {
                Name = subscription.Name,
                Description = subscription.Description,
                MaxMessagesPerDay = subscription.MaxMessagesPerDay,
                Price = subscription.Price,
                MaxApiKeys = subscription.MaxApiKeys,
                HasPrioritySupport = subscription.HasPrioritySupport,
                HasCustomIntegrations = subscription.HasCustomIntegrations,
                HasAdvancedAnalytics = subscription.HasAdvancedAnalytics,
                IsActive = subscription.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _saasContext.SubscriptionPlans.Add(subscriptionPlan);
            await _saasContext.SaveChangesAsync();

            subscription.Id = subscriptionPlan.Id;
            subscription.CreatedAt = subscriptionPlan.CreatedAt;
            subscription.UpdatedAt = subscriptionPlan.UpdatedAt ?? subscriptionPlan.CreatedAt;
            return subscription;
        }

        public async Task<Subscription> UpdateAsync(Subscription subscription)
        {
            var subscriptionPlan = await _saasContext.SubscriptionPlans.FindAsync(subscription.Id);
            if (subscriptionPlan == null)
                throw new InvalidOperationException($"Subscription {subscription.Id} not found");

            subscriptionPlan.Name = subscription.Name;
            subscriptionPlan.Description = subscription.Description;
            subscriptionPlan.MaxMessagesPerDay = subscription.MaxMessagesPerDay;
            subscriptionPlan.Price = subscription.Price;
            subscriptionPlan.MaxApiKeys = subscription.MaxApiKeys;
            subscriptionPlan.HasPrioritySupport = subscription.HasPrioritySupport;
            subscriptionPlan.HasCustomIntegrations = subscription.HasCustomIntegrations;
            subscriptionPlan.HasAdvancedAnalytics = subscription.HasAdvancedAnalytics;
            subscriptionPlan.IsActive = subscription.IsActive;
            subscriptionPlan.UpdatedAt = DateTime.UtcNow;

            await _saasContext.SaveChangesAsync();

            subscription.UpdatedAt = subscriptionPlan.UpdatedAt ?? DateTime.UtcNow;
            return subscription;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var subscriptionPlan = await _saasContext.SubscriptionPlans.FindAsync(id);
            if (subscriptionPlan == null)
                return false;

            _saasContext.SubscriptionPlans.Remove(subscriptionPlan);
            await _saasContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _saasContext.SubscriptionPlans.AnyAsync(s => s.Id == id);
        }

        public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
        {
            var query = _saasContext.SubscriptionPlans.Where(s => s.Name == name);

            if (excludeId.HasValue)
                query = query.Where(s => s.Id != excludeId.Value);

            return await query.AnyAsync();
        }
    }
}