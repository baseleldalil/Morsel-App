using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsAppAdmin.Models;
using WhatsAppAdmin.Services;

namespace WhatsAppAdmin.Controllers
{
    /// <summary>
    /// Controller for managing subscriptions and plans
    /// Handles CRUD operations for subscription management with permissions and timer settings
    /// </summary>
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class SubscriptionsController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionsController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        /// <summary>
        /// Display list of all subscriptions
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var subscriptions = await _subscriptionService.GetAllSubscriptionsAsync();
            return View(subscriptions);
        }

        /// <summary>
        /// Display subscription details
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var subscription = await _subscriptionService.GetSubscriptionWithPermissionsAsync(id);
            if (subscription == null)
                return NotFound();

            return View(subscription);
        }

        /// <summary>
        /// Display form to create new subscription
        /// </summary>
        public async Task<IActionResult> Create()
        {
            ViewBag.Permissions = await _subscriptionService.GetAvailablePermissionsAsync();
            return View();
        }

        /// <summary>
        /// Process subscription creation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Subscription subscription, int[]? selectedPermissions, TimerSettings? timerSettings)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (!await _subscriptionService.ValidateSubscriptionAsync(subscription))
                    {
                        ModelState.AddModelError("", "Subscription validation failed. Name may already exist.");
                        ViewBag.Permissions = await _subscriptionService.GetAvailablePermissionsAsync();
                        return View(subscription);
                    }

                    // Add timer settings if provided
                    if (timerSettings?.MinDelaySeconds.HasValue == true || timerSettings?.MaxDelaySeconds.HasValue == true)
                    {
                        subscription.TimerSettings = timerSettings;
                    }

                    await _subscriptionService.CreateSubscriptionAsync(subscription, selectedPermissions ?? Array.Empty<int>());
                    TempData["Success"] = "Subscription created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating subscription: {ex.Message}");
                }
            }

            ViewBag.Permissions = await _subscriptionService.GetAvailablePermissionsAsync();
            return View(subscription);
        }

        /// <summary>
        /// Display form to edit subscription
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            var subscription = await _subscriptionService.GetSubscriptionWithPermissionsAsync(id);
            if (subscription == null)
                return NotFound();

            ViewBag.Permissions = await _subscriptionService.GetAvailablePermissionsAsync();
            ViewBag.SelectedPermissions = subscription.SubscriptionPermissions.Select(sp => sp.PermissionId).ToArray();
            return View(subscription);
        }

        /// <summary>
        /// Process subscription update
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Subscription subscription, int[]? selectedPermissions, TimerSettings? timerSettings)
        {
            if (id != subscription.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (!await _subscriptionService.ValidateSubscriptionAsync(subscription))
                    {
                        ModelState.AddModelError("", "Subscription validation failed. Name may already exist.");
                        ViewBag.Permissions = await _subscriptionService.GetAvailablePermissionsAsync();
                        return View(subscription);
                    }

                    // Update timer settings
                    if (timerSettings?.MinDelaySeconds.HasValue == true || timerSettings?.MaxDelaySeconds.HasValue == true)
                    {
                        subscription.TimerSettings = timerSettings;
                        subscription.TimerSettings.SubscriptionId = subscription.Id;
                    }

                    await _subscriptionService.UpdateSubscriptionAsync(subscription, selectedPermissions ?? Array.Empty<int>());
                    TempData["Success"] = "Subscription updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating subscription: {ex.Message}");
                }
            }

            ViewBag.Permissions = await _subscriptionService.GetAvailablePermissionsAsync();
            return View(subscription);
        }

        /// <summary>
        /// Display confirmation page for subscription deletion
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);
            if (subscription == null)
                return NotFound();

            ViewBag.CanDelete = await _subscriptionService.CanDeleteSubscriptionAsync(id);
            return View(subscription);
        }

        /// <summary>
        /// Process subscription deletion
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _subscriptionService.DeleteSubscriptionAsync(id);
                if (result)
                {
                    TempData["Success"] = "Subscription deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Cannot delete subscription. It may have active users assigned.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting subscription: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}