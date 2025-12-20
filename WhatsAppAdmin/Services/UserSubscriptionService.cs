using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using WhatsApp.Shared.Models;
using WhatsAppAdmin.Models;
using WhatsAppAdmin.Repositories;
using AdminUserSubscription = WhatsAppAdmin.Models.UserSubscription;

namespace WhatsAppAdmin.Services
{
    /// <summary>
    /// Service implementation for user subscription business logic
    /// Handles complex user subscription operations with business rules and validation
    /// </summary>
    public class UserSubscriptionService : IUserSubscriptionService
    {
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IWhatsAppApiService _whatsAppApiService;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserSubscriptionService(
            IUserSubscriptionRepository userSubscriptionRepository,
            ISubscriptionRepository subscriptionRepository,
            IWhatsAppApiService whatsAppApiService,
            UserManager<ApplicationUser> userManager)
        {
            _userSubscriptionRepository = userSubscriptionRepository;
            _subscriptionRepository = subscriptionRepository;
            _whatsAppApiService = whatsAppApiService;
            _userManager = userManager;
        }

        public async Task<IEnumerable<AdminUserSubscription>> GetAllUserSubscriptionsAsync()
        {
            return await _userSubscriptionRepository.GetAllAsync();
        }

        public async Task<AdminUserSubscription?> GetUserSubscriptionByIdAsync(int id)
        {
            return await _userSubscriptionRepository.GetByIdAsync(id);
        }

        public async Task<AdminUserSubscription?> GetUserSubscriptionByUserIdAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            return await _userSubscriptionRepository.GetByUserIdAsync(userId);
        }

        public async Task<IEnumerable<AdminUserSubscription>> GetUserSubscriptionsBySubscriptionIdAsync(int subscriptionId)
        {
            return await _userSubscriptionRepository.GetBySubscriptionIdAsync(subscriptionId);
        }

        public async Task<IEnumerable<AdminUserSubscription>> GetUserSubscriptionsAsync(string userEmail)
        {
            return await _userSubscriptionRepository.GetByUserEmailAsync(userEmail);
        }

        public async Task<AdminUserSubscription> CreateUserSubscriptionAsync(AdminUserSubscription userSubscription)
        {

            if (!await ValidateUserSubscriptionAsync(userSubscription))
                throw new ArgumentException("Invalid user subscription data");

            // Check if user exists in AspNetUsers table
            var existingUser = await _userManager.FindByEmailAsync(userSubscription.UserEmail);

            if (existingUser == null)
            {
                // User doesn't exist, create them
                if (string.IsNullOrWhiteSpace(userSubscription.Password))
                {
                    throw new ArgumentException("Password is required when creating a new user");
                }

                var newUser = new ApplicationUser
                {
                    UserName = userSubscription.UserEmail,
                    Email = userSubscription.UserEmail,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var createResult = await _userManager.CreateAsync(newUser, userSubscription.Password);

                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create user: {errors}");
                }

                userSubscription.UserId = newUser.Id;
                Console.WriteLine($"Created new user in AspNetUsers - UserId: {newUser.Id}, Email: {userSubscription.UserEmail}");
            }
            else
            {
                // User exists, update password if provided
                if (!string.IsNullOrWhiteSpace(userSubscription.Password))
                {
                    var removePasswordResult = await _userManager.RemovePasswordAsync(existingUser);
                    if (removePasswordResult.Succeeded)
                    {
                        var addPasswordResult = await _userManager.AddPasswordAsync(existingUser, userSubscription.Password);
                        if (!addPasswordResult.Succeeded)
                        {
                            var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
                            throw new Exception($"Failed to update password: {errors}");
                        }
                    }
                }

                userSubscription.UserId = existingUser.Id;
                Console.WriteLine($"Using existing user - UserId: {existingUser.Id}, Email: {userSubscription.UserEmail}");
            }

            // Call API to assign subscription (optional - for API sync)
            int apiUserId = 0;
            if (!string.IsNullOrWhiteSpace(userSubscription.UserId))
            {
                int.TryParse(userSubscription.UserId, out apiUserId);
            }

            try
            {
                Console.WriteLine($"Calling API to assign subscription - UserId: {apiUserId}, Email: {userSubscription.UserEmail}");

                await _whatsAppApiService.AssignSubscriptionAsync(
                    apiUserId,
                    userSubscription.UserEmail,
                    userSubscription.SubscriptionId,
                    userSubscription.ExpiresAt,
                    userSubscription.IsActive,
                    userSubscription.Password
                );

                Console.WriteLine($"API call successful");
            }
            catch (Exception ex)
            {
                // Log but don't fail - API sync is optional
                Console.WriteLine($"Warning: Failed to sync subscription to API: {ex.Message}");
            }

            // Deactivate existing subscription for this email if exists
            if (!string.IsNullOrWhiteSpace(userSubscription.UserEmail))
            {
                var existingSubscription = (await _userSubscriptionRepository.GetByUserEmailAsync(userSubscription.UserEmail)).FirstOrDefault();
                if (existingSubscription != null)
                {
                    existingSubscription.IsActive = false;
                    await _userSubscriptionRepository.UpdateAsync(existingSubscription);
                }
            }

            return await _userSubscriptionRepository.CreateAsync(userSubscription);
        }

        public async Task<AdminUserSubscription> UpdateUserSubscriptionAsync(AdminUserSubscription userSubscription)
        {
            if (!await ValidateUserSubscriptionAsync(userSubscription))
                throw new ArgumentException("Invalid user subscription data");

            // Check if user exists in AspNetUsers table
            var existingUser = await _userManager.FindByEmailAsync(userSubscription.UserEmail);

            if (existingUser == null)
            {
                // User doesn't exist, create them if password provided
                if (!string.IsNullOrWhiteSpace(userSubscription.Password))
                {
                    var newUser = new ApplicationUser
                    {
                        UserName = userSubscription.UserEmail,
                        Email = userSubscription.UserEmail,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    var createResult = await _userManager.CreateAsync(newUser, userSubscription.Password);

                    if (!createResult.Succeeded)
                    {
                        var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                        throw new Exception($"Failed to create user: {errors}");
                    }

                    userSubscription.UserId = newUser.Id;
                    Console.WriteLine($"Created new user during update - UserId: {newUser.Id}, Email: {userSubscription.UserEmail}");
                }
            }
            else
            {
                // User exists, update password if provided
                if (!string.IsNullOrWhiteSpace(userSubscription.Password))
                {
                    var removePasswordResult = await _userManager.RemovePasswordAsync(existingUser);
                    if (removePasswordResult.Succeeded)
                    {
                        var addPasswordResult = await _userManager.AddPasswordAsync(existingUser, userSubscription.Password);
                        if (!addPasswordResult.Succeeded)
                        {
                            var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
                            throw new Exception($"Failed to update password: {errors}");
                        }
                    }
                }

                userSubscription.UserId = existingUser.Id;
                Console.WriteLine($"Updating subscription for user - UserId: {existingUser.Id}, Email: {userSubscription.UserEmail}");
            }

            // Call API to update subscription (optional - for API sync)
            int apiUserId = 0;
            if (!string.IsNullOrWhiteSpace(userSubscription.UserId))
            {
                int.TryParse(userSubscription.UserId, out apiUserId);
            }

            try
            {
                await _whatsAppApiService.AssignSubscriptionAsync(
                    apiUserId,
                    userSubscription.UserEmail,
                    userSubscription.SubscriptionId,
                    userSubscription.ExpiresAt,
                    userSubscription.IsActive,
                    userSubscription.Password
                );
            }
            catch (Exception ex)
            {
                // Log but don't fail - API sync is optional
                Console.WriteLine($"Warning: Failed to sync subscription update to API: {ex.Message}");
            }

            return await _userSubscriptionRepository.UpdateAsync(userSubscription);
        }

        public async Task<bool> DeleteUserSubscriptionAsync(int id)
        {
            return await _userSubscriptionRepository.DeleteAsync(id);
        }

        public async Task<bool> ValidateUserSubscriptionAsync(AdminUserSubscription userSubscription)
        {
            // Validate required fields - UserId can be null (will be auto-generated)
            if (string.IsNullOrWhiteSpace(userSubscription.UserEmail))
                return false;

            // Validate subscription exists
            if (!await _subscriptionRepository.ExistsAsync(userSubscription.SubscriptionId))
                return false;

            // Validate expiry date if set
            if (userSubscription.ExpiresAt.HasValue && userSubscription.ExpiresAt.Value <= DateTime.UtcNow)
                return false;

            return true;
        }

        public async Task<IEnumerable<AdminUserSubscription>> GetExpiringSubscriptionsAsync(int daysBeforeExpiry = 7)
        {
            var beforeDate = DateTime.UtcNow.AddDays(daysBeforeExpiry);
            return await _userSubscriptionRepository.GetExpiringSubscriptionsAsync(beforeDate);
        }

        public async Task<bool> CanUserSendMessageAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            var userSubscription = await _userSubscriptionRepository.GetByUserIdAsync(userId);

            if (userSubscription == null || !userSubscription.IsActive)
                return false;

            // Check if subscription has expired
            if (userSubscription.ExpiresAt.HasValue && userSubscription.ExpiresAt.Value <= DateTime.UtcNow)
                return false;

            // Reset daily count if needed
            if (userSubscription.LastResetAt.Date < DateTime.UtcNow.Date)
            {
                userSubscription.MessagesUsedToday = 0;
                userSubscription.LastResetAt = DateTime.UtcNow.Date;
                await _userSubscriptionRepository.UpdateAsync(userSubscription);
            }

            // Check daily message limit
            return userSubscription.MessagesUsedToday < userSubscription.Subscription.MaxMessagesPerDay;
        }

        public async Task IncrementUserMessageCountAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            var userSubscription = await _userSubscriptionRepository.GetByUserIdAsync(userId);

            if (userSubscription != null)
            {
                userSubscription.MessagesUsedToday++;
                userSubscription.LastMessageSentAt = DateTime.UtcNow;
                await _userSubscriptionRepository.UpdateAsync(userSubscription);
            }
        }

        public async Task ResetDailyMessageCountsAsync()
        {
            var allSubscriptions = await _userSubscriptionRepository.GetAllAsync();

            foreach (var subscription in allSubscriptions.Where(s => s.LastResetAt.Date < DateTime.UtcNow.Date))
            {
                subscription.MessagesUsedToday = 0;
                subscription.LastResetAt = DateTime.UtcNow.Date;
                await _userSubscriptionRepository.UpdateAsync(subscription);
            }
        }
    }
}