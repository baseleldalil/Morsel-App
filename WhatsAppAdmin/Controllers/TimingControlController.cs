using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsApp.Shared.Models;
using WhatsAppAdmin.Services;

namespace WhatsAppAdmin.Controllers
{
    /// <summary>
    /// Controller for managing timing controls
    /// Handles message timing, random delays, and video-specific timing configurations
    /// </summary>
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class TimingControlController : Controller
    {
        private readonly ITimingControlService _timingService;

        public TimingControlController(ITimingControlService timingService)
        {
            _timingService = timingService;
        }

        /// <summary>
        /// Display timing control dashboard with tabs
        /// </summary>
        public async Task<IActionResult> Index()
        {
            ViewBag.MessageTimingControls = await _timingService.GetAllMessageTimingControlsAsync();
            ViewBag.RandomDelayRules = await _timingService.GetAllRandomDelayRulesAsync();
            ViewBag.VideoTimingControls = await _timingService.GetAllVideoTimingControlsAsync();

            return View();
        }

        #region Message Timing Control

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMessageTiming(MessageTimingControl control)
        {
          
                try
                {
                    if (control.MinDelaySeconds > control.MaxDelaySeconds)
                    {
                        TempData["Error"] = "Minimum delay cannot be greater than maximum delay.";
                        return RedirectToAction(nameof(Index));
                    }

                    await _timingService.CreateMessageTimingControlAsync(control);
                    TempData["Success"] = "Message timing control created successfully.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error creating message timing: {ex.Message}";
                }
           
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMessageTiming(MessageTimingControl control)
        {
            
                try
                {
                    if (control.MinDelaySeconds > control.MaxDelaySeconds)
                    {
                        TempData["Error"] = "Minimum delay cannot be greater than maximum delay.";
                        return RedirectToAction(nameof(Index));
                    }

                    await _timingService.UpdateMessageTimingControlAsync(control);
                    TempData["Success"] = "Message timing control updated successfully.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error updating message timing: {ex.Message}";
                }
           

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessageTiming(int id)
        {
            try
            {
                var result = await _timingService.DeleteMessageTimingControlAsync(id);
                if (result)
                {
                    TempData["Success"] = "Message timing control deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Message timing control not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting message timing: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Random Delay Rules

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRandomDelay(RandomDelayRule rule)
        {
           
                try
                {
                    await _timingService.CreateRandomDelayRuleAsync(rule);
                    TempData["Success"] = "Random delay rule created successfully.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error creating random delay rule: {ex.Message}";
                }
           

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRandomDelay(RandomDelayRule rule)
        {
            
                try
                {
                    await _timingService.UpdateRandomDelayRuleAsync(rule);
                    TempData["Success"] = "Random delay rule updated successfully.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error updating random delay rule: {ex.Message}";
                }
            

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRandomDelay(int id)
        {
            try
            {
                var result = await _timingService.DeleteRandomDelayRuleAsync(id);
                if (result)
                {
                    TempData["Success"] = "Random delay rule deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Random delay rule not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting random delay rule: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Video Timing Control

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVideoTiming(VideoTimingControl control)
        {
           
                try
                {
                    await _timingService.CreateVideoTimingControlAsync(control);
                    TempData["Success"] = "Video timing control created successfully.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error creating video timing: {ex.Message}";
                }
           

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVideoTiming(VideoTimingControl control)
        {
         
                try
                {
                    await _timingService.UpdateVideoTimingControlAsync(control);
                    TempData["Success"] = "Video timing control updated successfully.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error updating video timing: {ex.Message}";
                }
           

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVideoTiming(int id)
        {
            try
            {
                var result = await _timingService.DeleteVideoTimingControlAsync(id);
                if (result)
                {
                    TempData["Success"] = "Video timing control deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Video timing control not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting video timing: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region API Endpoints for Frontend Integration

        /// <summary>
        /// API: Get active message timing configuration
        /// </summary>
        /// <param name="subscriptionPlanId">Optional subscription plan ID (null for global)</param>
        /// <returns>Active message timing configuration</returns>
        [HttpGet]
        [Route("api/TimingControl/GetMessageTiming")]
        [AllowAnonymous] // Allow public access for frontend apps
        public async Task<IActionResult> GetMessageTiming([FromQuery] int? subscriptionPlanId = null)
        {
            try
            {
                var timing = await _timingService.GetActiveMessageTimingControlAsync(subscriptionPlanId);

                if (timing == null)
                {
                    // Return default timing if none configured
                    return Ok(new
                    {
                        name = "Default (Not Configured)",
                        minDelaySeconds = 2,
                        maxDelaySeconds = 5,
                        description = "No timing configuration found. Using default values.",
                        subscriptionPlanId = (int?)null,
                        isActive = true
                    });
                }

                return Ok(new
                {
                    id = timing.Id,
                    name = timing.Name,
                    minDelaySeconds = timing.MinDelaySeconds,
                    maxDelaySeconds = timing.MaxDelaySeconds,
                    description = timing.Description,
                    subscriptionPlanId = timing.SubscriptionPlanId,
                    isActive = timing.IsActive
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// API: Get active random delay rules
        /// </summary>
        /// <param name="subscriptionPlanId">Optional subscription plan ID (null for global)</param>
        /// <returns>List of active random delay rules</returns>
        [HttpGet]
        [Route("api/TimingControl/GetRandomDelayRules")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRandomDelayRules([FromQuery] int? subscriptionPlanId = null)
        {
            try
            {
                var rules = await _timingService.GetActiveRandomDelayRulesAsync(subscriptionPlanId);

                var rulesList = rules.Select(r => new
                {
                    id = r.Id,
                    name = r.Name,
                    afterMessageCount = r.AfterMessageCount,
                    pauseDurationMinutes = r.PauseDurationMinutes,
                    randomVarianceSeconds = r.RandomVarianceSeconds,
                    priority = r.Priority,
                    description = r.Description,
                    subscriptionPlanId = r.SubscriptionPlanId,
                    isActive = r.IsActive
                }).ToList();

                return Ok(rulesList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// API: Get active video timing configuration
        /// </summary>
        /// <param name="subscriptionPlanId">Optional subscription plan ID (null for global)</param>
        /// <returns>Active video timing configuration</returns>
        [HttpGet]
        [Route("api/TimingControl/GetVideoTiming")]
        [AllowAnonymous]
        public async Task<IActionResult> GetVideoTiming([FromQuery] int? subscriptionPlanId = null)
        {
            try
            {
                var timing = await _timingService.GetActiveVideoTimingControlAsync(subscriptionPlanId);

                if (timing == null)
                {
                    // Return default timing if none configured
                    return Ok(new
                    {
                        name = "Default (Not Configured)",
                        minDelayBeforeUploadSeconds = 5,
                        maxDelayBeforeUploadSeconds = 15,
                        minUploadTimeSeconds = 10,
                        maxUploadTimeSeconds = 30,
                        minDelayAfterUploadSeconds = 3,
                        maxDelayAfterUploadSeconds = 8,
                        description = "No video timing configured. Using default values.",
                        subscriptionPlanId = (int?)null,
                        isActive = true
                    });
                }

                return Ok(new
                {
                    id = timing.Id,
                    name = timing.Name,
                    minDelayBeforeUploadSeconds = timing.MinDelayBeforeUploadSeconds,
                    maxDelayBeforeUploadSeconds = timing.MaxDelayBeforeUploadSeconds,
                    minUploadTimeSeconds = timing.MinUploadTimeSeconds,
                    maxUploadTimeSeconds = timing.MaxUploadTimeSeconds,
                    minDelayAfterUploadSeconds = timing.MinDelayAfterUploadSeconds,
                    maxDelayAfterUploadSeconds = timing.MaxDelayAfterUploadSeconds,
                    description = timing.Description,
                    subscriptionPlanId = timing.SubscriptionPlanId,
                    isActive = timing.IsActive
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        #endregion
    }
}
