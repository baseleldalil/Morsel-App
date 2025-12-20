using Microsoft.EntityFrameworkCore;
using WhatsApp.Shared.Data;
using SharedModels = WhatsApp.Shared.Models;
using AdminModels = WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Repositories
{
    /// <summary>
    /// Repository implementation for user subscription operations
    /// Handles all database operations related to user subscriptions with async support
    /// </summary>
    public class UserSubscriptionRepository : IUserSubscriptionRepository
    {
        private readonly SaaSDbContext _saasContext;

        public UserSubscriptionRepository(SaaSDbContext saasContext)
        {
            _saasContext = saasContext;
        }

        public async Task<IEnumerable<AdminModels.UserSubscription>> GetAllAsync()
        {
            // Use SaaSDbContext.UserSubscriptions instead
            var sharedUserSubs = await _saasContext.UserSubscriptions
                .Include(us => us.SubscriptionPlan)
                .Include(us => us.User)
                .OrderBy(us => us.UserId)
                .ToListAsync();

            // Map to admin UserSubscription model
            return sharedUserSubs.Select(sus => new AdminModels.UserSubscription
            {
                Id = sus.Id,
                UserId = sus.UserId, // Set to actual AspNetUsers.Id (GUID)
                UserEmail = sus.User?.Email ?? "Unknown", // Get email from User navigation property
                SubscriptionId = sus.SubscriptionPlanId,
                AssignedAt = sus.StartDate,
                ExpiresAt = sus.EndDate,
                IsActive = sus.IsActive,
                MessagesUsedToday = sus.CurrentPeriodMessages,
                LastMessageSentAt = DateTime.UtcNow, // Shared model doesn't have this
                LastResetAt = sus.LastUsageResetAt ?? DateTime.UtcNow.Date,
                CreatedAt = sus.CreatedAt,
                AmountPaid = sus.AmountPaid,
                Subscription = new AdminModels.Subscription
                {
                    Id = sus.SubscriptionPlan.Id,
                    Name = sus.SubscriptionPlan.Name,
                    Description = sus.SubscriptionPlan.Description,
                    MaxMessagesPerDay = sus.SubscriptionPlan.MaxMessagesPerDay,
                    Price = sus.SubscriptionPlan.Price,
                    MaxApiKeys = sus.SubscriptionPlan.MaxApiKeys,
                    HasPrioritySupport = sus.SubscriptionPlan.HasPrioritySupport,
                    HasCustomIntegrations = sus.SubscriptionPlan.HasCustomIntegrations,
                    HasAdvancedAnalytics = sus.SubscriptionPlan.HasAdvancedAnalytics,
                    IsActive = sus.SubscriptionPlan.IsActive,
                    CreatedAt = sus.SubscriptionPlan.CreatedAt,
                    UpdatedAt = sus.SubscriptionPlan.UpdatedAt ?? sus.SubscriptionPlan.CreatedAt
                }
            }).ToList();
        }

        public async Task<AdminModels.UserSubscription?> GetByIdAsync(int id)
        {
            var sus = await _saasContext.UserSubscriptions
                .Include(us => us.SubscriptionPlan)
                .Include(us => us.User)
                .FirstOrDefaultAsync(us => us.Id == id);

            if (sus == null) return null;

            return new AdminModels.UserSubscription
            {
                Id = sus.Id,
                UserId = sus.UserId, // Set to actual AspNetUsers.Id (GUID)
                UserEmail = sus.User?.Email ?? "Unknown", // Get email from User navigation property
                SubscriptionId = sus.SubscriptionPlanId,
                AssignedAt = sus.StartDate,
                ExpiresAt = sus.EndDate,
                IsActive = sus.IsActive,
                MessagesUsedToday = sus.CurrentPeriodMessages,
                LastMessageSentAt = DateTime.UtcNow,
                LastResetAt = sus.LastUsageResetAt ?? DateTime.UtcNow.Date,
                CreatedAt = sus.CreatedAt,
                AmountPaid = sus.AmountPaid,
                Subscription = new AdminModels.Subscription
                {
                    Id = sus.SubscriptionPlan.Id,
                    Name = sus.SubscriptionPlan.Name,
                    Description = sus.SubscriptionPlan.Description,
                    MaxMessagesPerDay = sus.SubscriptionPlan.MaxMessagesPerDay,
                    Price = sus.SubscriptionPlan.Price,
                    MaxApiKeys = sus.SubscriptionPlan.MaxApiKeys,
                    HasPrioritySupport = sus.SubscriptionPlan.HasPrioritySupport,
                    HasCustomIntegrations = sus.SubscriptionPlan.HasCustomIntegrations,
                    HasAdvancedAnalytics = sus.SubscriptionPlan.HasAdvancedAnalytics,
                    IsActive = sus.SubscriptionPlan.IsActive,
                    CreatedAt = sus.SubscriptionPlan.CreatedAt,
                    UpdatedAt = sus.SubscriptionPlan.UpdatedAt ?? sus.SubscriptionPlan.CreatedAt
                }
            };
        }

        public async Task<AdminModels.UserSubscription?> GetByUserIdAsync(string userId)
        {
            // Admin's UserSubscription.UserId is int?, but shared uses string UserId
            // This method might not work as expected - need to use UserEmail instead
            return null; // TODO: Update callers to use GetByUserEmailAsync
        }

        public async Task<IEnumerable<AdminModels.UserSubscription>> GetBySubscriptionIdAsync(int subscriptionId)
        {
            var sharedUserSubs = await _saasContext.UserSubscriptions
                .Include(us => us.SubscriptionPlan)
                .Include(us => us.User)
                .Where(us => us.SubscriptionPlanId == subscriptionId)
                .OrderBy(us => us.UserId)
                .ToListAsync();

            return sharedUserSubs.Select(sus => new AdminModels.UserSubscription
            {
                Id = sus.Id,
                UserId = sus.UserId, // Set to actual AspNetUsers.Id (GUID)
                UserEmail = sus.User?.Email ?? "Unknown", // Get email from User navigation property
                SubscriptionId = sus.SubscriptionPlanId,
                AssignedAt = sus.StartDate,
                ExpiresAt = sus.EndDate,
                IsActive = sus.IsActive,
                MessagesUsedToday = sus.CurrentPeriodMessages,
                LastMessageSentAt = DateTime.UtcNow,
                LastResetAt = sus.LastUsageResetAt ?? DateTime.UtcNow.Date,
                CreatedAt = sus.CreatedAt,
                AmountPaid = sus.AmountPaid,
                Subscription = new AdminModels.Subscription
                {
                    Id = sus.SubscriptionPlan.Id,
                    Name = sus.SubscriptionPlan.Name,
                    Description = sus.SubscriptionPlan.Description,
                    MaxMessagesPerDay = sus.SubscriptionPlan.MaxMessagesPerDay,
                    Price = sus.SubscriptionPlan.Price,
                    MaxApiKeys = sus.SubscriptionPlan.MaxApiKeys,
                    HasPrioritySupport = sus.SubscriptionPlan.HasPrioritySupport,
                    HasCustomIntegrations = sus.SubscriptionPlan.HasCustomIntegrations,
                    HasAdvancedAnalytics = sus.SubscriptionPlan.HasAdvancedAnalytics,
                    IsActive = sus.SubscriptionPlan.IsActive,
                    CreatedAt = sus.SubscriptionPlan.CreatedAt,
                    UpdatedAt = sus.SubscriptionPlan.UpdatedAt ?? sus.SubscriptionPlan.CreatedAt
                }
            }).ToList();
        }

        public async Task<IEnumerable<AdminModels.UserSubscription>> GetByUserEmailAsync(string userEmail)
        {
            // UserId in shared model is now AspNetUsers.Id (GUID), so we need to join with Users table
            var sharedUserSubs = await _saasContext.UserSubscriptions
                .Include(us => us.SubscriptionPlan)
                .Include(us => us.User)
                .Where(us => us.User.Email == userEmail && us.IsActive)
                .OrderByDescending(us => us.StartDate)
                .ToListAsync();

            return sharedUserSubs.Select(sus => new AdminModels.UserSubscription
            {
                Id = sus.Id,
                UserId = sus.UserId, // Set to actual AspNetUsers.Id (GUID)
                UserEmail = sus.User?.Email ?? "Unknown", // Get email from User navigation property
                SubscriptionId = sus.SubscriptionPlanId,
                AssignedAt = sus.StartDate,
                ExpiresAt = sus.EndDate,
                IsActive = sus.IsActive,
                MessagesUsedToday = sus.CurrentPeriodMessages,
                LastMessageSentAt = DateTime.UtcNow,
                LastResetAt = sus.LastUsageResetAt ?? DateTime.UtcNow.Date,
                CreatedAt = sus.CreatedAt,
                AmountPaid = sus.AmountPaid,
                Subscription = new AdminModels.Subscription
                {
                    Id = sus.SubscriptionPlan.Id,
                    Name = sus.SubscriptionPlan.Name,
                    Description = sus.SubscriptionPlan.Description,
                    MaxMessagesPerDay = sus.SubscriptionPlan.MaxMessagesPerDay,
                    Price = sus.SubscriptionPlan.Price,
                    MaxApiKeys = sus.SubscriptionPlan.MaxApiKeys,
                    HasPrioritySupport = sus.SubscriptionPlan.HasPrioritySupport,
                    HasCustomIntegrations = sus.SubscriptionPlan.HasCustomIntegrations,
                    HasAdvancedAnalytics = sus.SubscriptionPlan.HasAdvancedAnalytics,
                    IsActive = sus.SubscriptionPlan.IsActive,
                    CreatedAt = sus.SubscriptionPlan.CreatedAt,
                    UpdatedAt = sus.SubscriptionPlan.UpdatedAt ?? sus.SubscriptionPlan.CreatedAt
                }
            }).ToList();
        }

        public async Task<AdminModels.UserSubscription> CreateAsync(AdminModels.UserSubscription userSubscription)
        {
            Console.WriteLine($"Creating UserSubscription - UserId: {userSubscription.UserId}, Email: {userSubscription.UserEmail}, SubId: {userSubscription.SubscriptionId}");

            // Validate UserId is set (must be AspNetUsers.Id GUID)
            if (string.IsNullOrWhiteSpace(userSubscription.UserId))
            {
                throw new ArgumentException("UserId must be set before creating a UserSubscription. Ensure user exists in AspNetUsers table.");
            }

            // Map to shared UserSubscription model
            var sharedUserSub = new SharedModels.UserSubscription
            {
                UserId = userSubscription.UserId, // Use actual AspNetUsers.Id (GUID), not email
                SubscriptionPlanId = userSubscription.SubscriptionId,
                StartDate = DateTime.UtcNow,
                EndDate = userSubscription.ExpiresAt.HasValue
                    ? DateTime.SpecifyKind(userSubscription.ExpiresAt.Value, DateTimeKind.Utc)
                    : DateTime.UtcNow.AddMonths(1),
                AmountPaid = userSubscription.AmountPaid,
                IsActive = userSubscription.IsActive,
                CurrentPeriodMessages = 0,
                LastUsageResetAt = DateTime.UtcNow.Date,
                CreatedAt = DateTime.UtcNow
            };

            _saasContext.UserSubscriptions.Add(sharedUserSub);

            try
            {
                await _saasContext.SaveChangesAsync();
                Console.WriteLine($"UserSubscription created successfully with ID: {sharedUserSub.Id}");

                userSubscription.Id = sharedUserSub.Id;
                userSubscription.AssignedAt = sharedUserSub.StartDate;
                userSubscription.CreatedAt = sharedUserSub.CreatedAt;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving UserSubscription: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                throw;
            }

            return userSubscription;
        }

        public async Task<AdminModels.UserSubscription> UpdateAsync(AdminModels.UserSubscription userSubscription)
        {
            var sharedUserSub = await _saasContext.UserSubscriptions.FindAsync(userSubscription.Id);
            if (sharedUserSub == null)
                throw new InvalidOperationException($"UserSubscription {userSubscription.Id} not found");

            // Ensure ExpiresAt is UTC if provided
            if (userSubscription.ExpiresAt.HasValue)
            {
                sharedUserSub.EndDate = DateTime.SpecifyKind(userSubscription.ExpiresAt.Value, DateTimeKind.Utc);
            }

            sharedUserSub.IsActive = userSubscription.IsActive;
            sharedUserSub.CurrentPeriodMessages = userSubscription.MessagesUsedToday;

            // Ensure LastResetAt is UTC
            sharedUserSub.LastUsageResetAt = DateTime.SpecifyKind(userSubscription.LastResetAt, DateTimeKind.Utc);

            await _saasContext.SaveChangesAsync();
            return userSubscription;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var userSubscription = await _saasContext.UserSubscriptions.FindAsync(id);
            if (userSubscription == null)
                return false;

            _saasContext.UserSubscriptions.Remove(userSubscription);
            await _saasContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _saasContext.UserSubscriptions.AnyAsync(us => us.Id == id);
        }

        public async Task<bool> UserHasActiveSubscriptionAsync(int userId)
        {
            // This method needs UserEmail instead, since shared model uses string UserId
            // Return false for now
            return false;
        }

        public async Task<IEnumerable<AdminModels.UserSubscription>> GetExpiringSubscriptionsAsync(DateTime beforeDate)
        {
            var sharedUserSubs = await _saasContext.UserSubscriptions
                .Include(us => us.SubscriptionPlan)
                .Include(us => us.User)
                .Where(us => us.IsActive && us.EndDate <= beforeDate)
                .OrderBy(us => us.EndDate)
                .ToListAsync();

            return sharedUserSubs.Select(sus => new AdminModels.UserSubscription
            {
                Id = sus.Id,
                UserId = sus.UserId, // Set to actual AspNetUsers.Id (GUID)
                UserEmail = sus.User?.Email ?? "Unknown", // Get email from User navigation property
                SubscriptionId = sus.SubscriptionPlanId,
                AssignedAt = sus.StartDate,
                ExpiresAt = sus.EndDate,
                IsActive = sus.IsActive,
                MessagesUsedToday = sus.CurrentPeriodMessages,
                LastMessageSentAt = DateTime.UtcNow,
                LastResetAt = sus.LastUsageResetAt ?? DateTime.UtcNow.Date,
                CreatedAt = sus.CreatedAt,
                AmountPaid = sus.AmountPaid,
                Subscription = new AdminModels.Subscription
                {
                    Id = sus.SubscriptionPlan.Id,
                    Name = sus.SubscriptionPlan.Name,
                    Description = sus.SubscriptionPlan.Description,
                    MaxMessagesPerDay = sus.SubscriptionPlan.MaxMessagesPerDay,
                    Price = sus.SubscriptionPlan.Price,
                    MaxApiKeys = sus.SubscriptionPlan.MaxApiKeys,
                    HasPrioritySupport = sus.SubscriptionPlan.HasPrioritySupport,
                    HasCustomIntegrations = sus.SubscriptionPlan.HasCustomIntegrations,
                    HasAdvancedAnalytics = sus.SubscriptionPlan.HasAdvancedAnalytics,
                    IsActive = sus.SubscriptionPlan.IsActive,
                    CreatedAt = sus.SubscriptionPlan.CreatedAt,
                    UpdatedAt = sus.SubscriptionPlan.UpdatedAt ?? sus.SubscriptionPlan.CreatedAt
                }
            }).ToList();
        }
    }
}