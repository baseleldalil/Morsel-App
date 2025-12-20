using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsAppAdmin.Models;
using WhatsAppAdmin.Services;

namespace WhatsAppAdmin.Controllers
{
    /// <summary>
    /// Controller for managing user subscription assignments
    /// Handles assigning users to subscriptions and managing their usage
    /// </summary>
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class UserSubscriptionsController : Controller
    {
        private readonly IUserSubscriptionService _userSubscriptionService;
        private readonly ISubscriptionService _subscriptionService;

        public UserSubscriptionsController(
            IUserSubscriptionService userSubscriptionService,
            ISubscriptionService subscriptionService)
        {
            _userSubscriptionService = userSubscriptionService;
            _subscriptionService = subscriptionService;
        }

        /// <summary>
        /// Display list of all user subscriptions
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var userSubscriptions = await _userSubscriptionService.GetAllUserSubscriptionsAsync();
            return View(userSubscriptions);
        }

        /// <summary>
        /// Display user subscription details
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var userSubscription = await _userSubscriptionService.GetUserSubscriptionByIdAsync(id);
            if (userSubscription == null)
                return NotFound();

            return View(userSubscription);
        }

        /// <summary>
        /// Display form to create new user subscription
        /// </summary>
        public async Task<IActionResult> Create()
        {
            ViewBag.Subscriptions = await _subscriptionService.GetAllSubscriptionsAsync();
            return View();
        }

        /// <summary>
        /// Process user subscription creation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserSubscription userSubscription)
        {
                try
                {
               
                    await _userSubscriptionService.CreateUserSubscriptionAsync(userSubscription);

                    var subscription = await _subscriptionService.GetSubscriptionByIdAsync(userSubscription.SubscriptionId);
                    var successMessage = $"✓ Success! User '{userSubscription.UserEmail}' has been assigned to '{subscription?.Name ?? "subscription"}' plan.";

                    if (!string.IsNullOrWhiteSpace(userSubscription.Password))
                    {
                        var userExists = await _userSubscriptionService.GetUserSubscriptionByUserIdAsync(userSubscription.UserId);
                        if (userSubscription.UserId != null)
                        {
                            successMessage += " New user account created with the provided password.";
                        }
                        else
                        {
                            successMessage += " Password has been updated.";
                        }
                    }

                    TempData["Success"] = successMessage;

                    // Clear the form by returning a new model
                    ModelState.Clear();
                    ViewBag.Subscriptions = await _subscriptionService.GetAllSubscriptionsAsync();
                    return View(new UserSubscription());
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    var innerMessage = ex.InnerException?.Message ?? ex.Message;
                    ModelState.AddModelError("", $"Error creating user subscription: {innerMessage}");

                    // Log the full exception for debugging
                    Console.WriteLine($"Full Error: {ex}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException}");
                    }
                }

            ViewBag.Subscriptions = await _subscriptionService.GetAllSubscriptionsAsync();
            return View(userSubscription);
        }

        /// <summary>
        /// Display form to edit user subscription
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            var userSubscription = await _userSubscriptionService.GetUserSubscriptionByIdAsync(id);
            if (userSubscription == null)
                return NotFound();

            ViewBag.Subscriptions = await _subscriptionService.GetAllSubscriptionsAsync();
            return View(userSubscription);
        }

        /// <summary>
        /// Process user subscription update
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserSubscription userSubscription)
        {
            if (id != userSubscription.Id)
                return NotFound();

            try
            {
                await _userSubscriptionService.UpdateUserSubscriptionAsync(userSubscription);

                var subscription = await _subscriptionService.GetSubscriptionByIdAsync(userSubscription.SubscriptionId);
                var successMessage = $"✓ Subscription updated successfully! User '{userSubscription.UserEmail}' subscription has been updated.";

                if (!string.IsNullOrWhiteSpace(userSubscription.Password))
                {
                    successMessage += " Password has been updated.";
                }

                TempData["Success"] = successMessage;

                // Stay on edit page
                ViewBag.Subscriptions = await _subscriptionService.GetAllSubscriptionsAsync();
                ModelState.Clear();
                return View(userSubscription);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating user subscription: {ex.Message}");
            }

            ViewBag.Subscriptions = await _subscriptionService.GetAllSubscriptionsAsync();
            return View(userSubscription);
        }

        /// <summary>
        /// Display confirmation page for user subscription deletion
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            var userSubscription = await _userSubscriptionService.GetUserSubscriptionByIdAsync(id);
            if (userSubscription == null)
                return NotFound();

            return View(userSubscription);
        }

        /// <summary>
        /// Process user subscription deletion
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _userSubscriptionService.DeleteUserSubscriptionAsync(id);
                if (result)
                {
                    TempData["Success"] = "User subscription deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "User subscription not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting user subscription: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Display expiring subscriptions
        /// </summary>
        public async Task<IActionResult> Expiring()
        {
            var expiringSubscriptions = await _userSubscriptionService.GetExpiringSubscriptionsAsync(30);
            return View(expiringSubscriptions);
        }

        /// <summary>
        /// Reset daily message counts for all users
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ResetDailyCounts()
        {
            try
            {
                await _userSubscriptionService.ResetDailyMessageCountsAsync();
                TempData["Success"] = "Daily message counts reset successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error resetting daily counts: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}