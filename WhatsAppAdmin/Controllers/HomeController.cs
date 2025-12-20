using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsAppAdmin.Services;

namespace WhatsAppAdmin.Controllers
{
    /// <summary>
    /// Home controller for the admin dashboard
    /// Provides overview statistics and navigation to admin features
    /// </summary>
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IUserSubscriptionService _userSubscriptionService;
        private readonly ICampaignTemplateService _campaignTemplateService;

        public HomeController(
            ISubscriptionService subscriptionService,
            IUserSubscriptionService userSubscriptionService,
            ICampaignTemplateService campaignTemplateService)
        {
            _subscriptionService = subscriptionService;
            _userSubscriptionService = userSubscriptionService;
            _campaignTemplateService = campaignTemplateService;
        }

        /// <summary>
        /// Display the admin dashboard with overview statistics
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var subscriptions = await _subscriptionService.GetAllSubscriptionsAsync();
            var userSubscriptions = await _userSubscriptionService.GetAllUserSubscriptionsAsync();
            var templates = await _campaignTemplateService.GetAllTemplatesAsync();

            ViewBag.TotalSubscriptions = subscriptions.Count();
            ViewBag.ActiveUserSubscriptions = userSubscriptions.Count(us => us.IsActive);
            ViewBag.TotalTemplates = templates.Count();
            ViewBag.ExpiringSubscriptions = (await _userSubscriptionService.GetExpiringSubscriptionsAsync()).Count();

            return View();
        }

        /// <summary>
        /// Display error page
        /// </summary>
        public IActionResult Error()
        {
            return View();
        }
    }
}