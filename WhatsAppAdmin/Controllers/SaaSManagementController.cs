using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WhatsApp.Shared.Models;
using WhatsAppAdmin.Services;
using WhatsAppAdmin.Models;
using Microsoft.EntityFrameworkCore;
using WhatsApp.Shared.Data;

namespace WhatsAppAdmin.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class SaaSManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISubscriptionService _subscriptionService;
        private readonly SaaSDbContext _saasContext;
        private readonly ILogger<SaaSManagementController> _logger;

        public SaaSManagementController(
            UserManager<ApplicationUser> userManager,
            ISubscriptionService subscriptionService,
            SaaSDbContext saasContext,
            ILogger<SaaSManagementController> logger)
        {
            _userManager = userManager;
            _subscriptionService = subscriptionService;
            _saasContext = saasContext;
            _logger = logger;
        }

        // Dashboard
        public async Task<IActionResult> Index()
        {
            var stats = await GetDashboardStatsAsync();
            return View(stats);
        }

        // Users Management
        public async Task<IActionResult> Users(int page = 1, int pageSize = 20)
        {
            var skip = (page - 1) * pageSize;
            var users = await _userManager.Users
                .Include(u => u.Subscriptions)
                    .ThenInclude(s => s.SubscriptionPlan)
                .OrderByDescending(u => u.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            var totalUsers = await _userManager.Users.CountAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize);
            ViewBag.PageSize = pageSize;

            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> UserDetails(string userId)
        {
            var user = await _userManager.Users
                .Include(u => u.Subscriptions)
                    .ThenInclude(s => s.SubscriptionPlan)
                .Include(u => u.ApiKeys)
                .Include(u => u.MessageHistory)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            user.IsActive = !user.IsActive;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Json(new { success = true, isActive = user.IsActive });
            }

            return Json(new { success = false, message = "Failed to update user status" });
        }

        // Subscription Plans Management
        public async Task<IActionResult> Plans()
        {
            var plans = await _saasContext.SubscriptionPlans.Where(s => s.IsActive).ToListAsync();
            return View(plans);
        }

        [HttpGet]
        public IActionResult CreatePlan()
        {
            return View(new SubscriptionPlan());
        }

        [HttpPost]
        public async Task<IActionResult> CreatePlan(SubscriptionPlan plan)
        {
            if (!ModelState.IsValid)
                return View(plan);

            try
            {
                _saasContext.SubscriptionPlans.Add(plan);
                await _saasContext.SaveChangesAsync();
                TempData["Success"] = $"Plan '{plan.Name}' created successfully!";
                return RedirectToAction("Plans");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription plan");
                ModelState.AddModelError("", "Failed to create plan. Please try again.");
                return View(plan);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditPlan(int id)
        {
            var plan = await _saasContext.SubscriptionPlans.FindAsync(id);
            if (plan == null)
                return NotFound();

            return View(plan);
        }

        [HttpPost]
        public async Task<IActionResult> EditPlan(SubscriptionPlan plan)
        {
            if (!ModelState.IsValid)
                return View(plan);

            try
            {
                _saasContext.SubscriptionPlans.Update(plan);
                await _saasContext.SaveChangesAsync();
                TempData["Success"] = $"Plan '{plan.Name}' updated successfully!";
                return RedirectToAction("Plans");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription plan");
                ModelState.AddModelError("", "Failed to update plan. Please try again.");
                return View(plan);
            }
        }

        // Analytics
        public async Task<IActionResult> Analytics()
        {
            var stats = await GetAnalyticsDataAsync();
            return View(stats);
        }

        [HttpGet]
        public async Task<IActionResult> GetRevenueData()
        {
            var revenueData = await GetRevenueDataAsync();
            return Json(revenueData);
        }

        [HttpGet]
        public async Task<IActionResult> GetUsageData()
        {
            var usageData = await GetUsageDataAsync();
            return Json(usageData);
        }

        // Message History
        public async Task<IActionResult> Messages(int page = 1, int pageSize = 50)
        {
            var skip = (page - 1) * pageSize;
            var messages = await _saasContext.MessageHistory
                .Include(mh => mh.User)
                .OrderByDescending(mh => mh.SentAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            var totalMessages = await _saasContext.MessageHistory.CountAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalMessages / pageSize);
            ViewBag.PageSize = pageSize;

            return View(messages);
        }

        // API Keys Management
        public async Task<IActionResult> ApiKeys(int page = 1, int pageSize = 30)
        {
            var skip = (page - 1) * pageSize;
            var apiKeys = await _saasContext.ApiKeys
                .Include(ak => ak.User)
                .Include(ak => ak.SubscriptionPlan)
                .OrderByDescending(ak => ak.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            var totalApiKeys = await _saasContext.ApiKeys.CountAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalApiKeys / pageSize);
            ViewBag.PageSize = pageSize;

            return View(apiKeys);
        }

        [HttpPost]
        public async Task<IActionResult> RevokeApiKey(string apiKey)
        {
            try
            {
                var key = await _saasContext.ApiKeys.FirstOrDefaultAsync(ak => ak.Key == apiKey);
                if (key == null)
                    return Json(new { success = false, message = "API key not found" });

                key.IsActive = false;
                key.RevokedAt = DateTime.UtcNow;
                _saasContext.ApiKeys.Update(key);
                await _saasContext.SaveChangesAsync();

                return Json(new { success = true, message = "API key revoked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking API key");
                return Json(new { success = false, message = "An error occurred while revoking API key" });
            }
        }

        // Private helper methods
        private async Task<SaaSDashboardStats> GetDashboardStatsAsync()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var activeSubscriptions = await _saasContext.UserSubscriptions
                .CountAsync(us => us.IsActive && us.EndDate > DateTime.UtcNow);

            var currentMonth = DateTime.UtcNow.Date.AddDays(1 - DateTime.UtcNow.Day);
            var monthlyRevenue = await _saasContext.UserSubscriptions
                .Where(us => us.CreatedAt >= currentMonth)
                .SumAsync(us => us.AmountPaid);

            var totalMessages = await _saasContext.MessageHistory.CountAsync();

            var newUsersThisMonth = await _userManager.Users
                .CountAsync(u => u.CreatedAt >= currentMonth);

            var averageRevenue = totalUsers > 0 ? monthlyRevenue / totalUsers : 0;

            return new SaaSDashboardStats
            {
                TotalUsers = totalUsers,
                ActiveSubscriptions = activeSubscriptions,
                MonthlyRevenue = monthlyRevenue,
                TotalMessagesSent = totalMessages,
                NewUsersThisMonth = newUsersThisMonth,
                AverageRevenuePerUser = averageRevenue
            };
        }

        private async Task<object> GetAnalyticsDataAsync()
        {
            var last30Days = DateTime.UtcNow.AddDays(-30);

            var userGrowth = await _userManager.Users
                .Where(u => u.CreatedAt >= last30Days.Date)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var messageStats = await _saasContext.MessageHistory
                .Where(mh => mh.SentAt >= last30Days)
                .GroupBy(mh => mh.SentAt.Date)
                .Select(g => new {
                    Date = g.Key,
                    Total = g.Count(),
                    Sent = g.Count(m => m.Status == "Sent"),
                    Failed = g.Count(m => m.Status == "Failed")
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var planStats = await _saasContext.UserSubscriptions
                .Include(us => us.SubscriptionPlan)
                .Where(us => us.IsActive)
                .GroupBy(us => us.SubscriptionPlan.Name)
                .Select(g => new {
                    Plan = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(us => us.AmountPaid)
                })
                .ToListAsync();

            return new {
                UserGrowth = userGrowth,
                MessageStats = messageStats,
                PlanStats = planStats
            };
        }

        private async Task<object> GetRevenueDataAsync()
        {
            var last12Months = DateTime.UtcNow.AddMonths(-12);

            var revenueData = await _saasContext.UserSubscriptions
                .Where(us => us.CreatedAt >= last12Months)
                .GroupBy(us => new { us.CreatedAt.Year, us.CreatedAt.Month })
                .Select(g => new {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(us => us.AmountPaid),
                    Subscriptions = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            return revenueData;
        }

        private async Task<object> GetUsageDataAsync()
        {
            var last30Days = DateTime.UtcNow.AddDays(-30);

            var usageData = await _saasContext.UsageStatistics
                .Where(us => us.Date >= last30Days)
                .GroupBy(us => us.Date)
                .Select(g => new {
                    Date = g.Key,
                    Messages = g.Sum(us => us.MessagesSent),
                    Delivered = g.Sum(us => us.MessagesDelivered),
                    Failed = g.Sum(us => us.MessagesFailed),
                    Users = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return usageData;
        }
    }
}