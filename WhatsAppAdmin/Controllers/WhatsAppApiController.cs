using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsAppAdmin.Models.API;
using WhatsAppAdmin.Services;

namespace WhatsAppAdmin.Controllers
{
    [Authorize]
    public class WhatsAppApiController : Controller
    {
        private readonly IWhatsAppApiService _whatsAppApiService;
        private readonly IUserSubscriptionService _userSubscriptionService;

        public WhatsAppApiController(
            IWhatsAppApiService whatsAppApiService,
            IUserSubscriptionService userSubscriptionService)
        {
            _whatsAppApiService = whatsAppApiService;
            _userSubscriptionService = userSubscriptionService;
        }

        // GET: WhatsAppApi
        public async Task<IActionResult> Index()
        {
            var userEmail = User.Identity?.Name ?? "";
            var apiKeys = await _whatsAppApiService.GetUserApiKeysAsync(userEmail);
            return View(apiKeys);
        }

        // GET: WhatsAppApi/Create
        public async Task<IActionResult> CreateApiKey()
        {
            var userEmail = User.Identity?.Name ?? "";
            var userSubscriptions = await _userSubscriptionService.GetUserSubscriptionsAsync(userEmail);

            ViewBag.Subscriptions = userSubscriptions;
            return View();
        }

        // POST: WhatsAppApi/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateApiKey(CreateApiKeyViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userEmail = User.Identity?.Name ?? "";
                var apiKey = await _whatsAppApiService.CreateApiKeyAsync(userEmail, model.SubscriptionId, model.Name);

                if (apiKey != null)
                {
                    TempData["Success"] = "API key created successfully!";
                    TempData["NewApiKey"] = apiKey.KeyValue; // Show the key once
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Failed to create API key. Please try again.");
                }
            }

            var userSubscriptions = await _userSubscriptionService.GetUserSubscriptionsAsync(User.Identity?.Name ?? "");
            ViewBag.Subscriptions = userSubscriptions;
            return View(model);
        }

        // POST: WhatsAppApi/Revoke
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeApiKey(int apiKeyId)
        {
            var userEmail = User.Identity?.Name ?? "";
            var success = await _whatsAppApiService.RevokeApiKeyAsync(apiKeyId, userEmail);

            if (success)
            {
                TempData["Success"] = "API key revoked successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to revoke API key.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: WhatsAppApi/SendMessages
        public IActionResult SendMessages()
        {
            return View();
        }

        // POST: WhatsAppApi/SendMessages
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessages(SendMessagesViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var request = new SendBulkMessageRequest
                    {
                        DelayBetweenMessages = model.DelayBetweenMessages,
                        SendImmediately = model.SendImmediately,
                        Messages = new List<WhatsAppMessageRequest>()
                    };

                    // Parse recipients and messages
                    var phoneLines = model.PhoneNumbers.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    var messageLines = model.MessageContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                    foreach (var phone in phoneLines)
                    {
                        var messageRequest = new WhatsAppMessageRequest
                        {
                            Phone = phone.Trim(),
                            Messages = messageLines.ToList()
                        };

                        // Handle file upload
                        if (model.AttachedFile != null && model.AttachedFile.Length > 0)
                        {
                            using var memoryStream = new MemoryStream();
                            await model.AttachedFile.CopyToAsync(memoryStream);
                            messageRequest.FileBase64 = Convert.ToBase64String(memoryStream.ToArray());
                            messageRequest.FileName = model.AttachedFile.FileName;
                            messageRequest.FileType = model.AttachedFile.ContentType;
                        }

                        request.Messages.Add(messageRequest);
                    }

                    var result = await _whatsAppApiService.SendMessagesAsync(model.ApiKey, request);

                    if (result.Success)
                    {
                        TempData["Success"] = $"Messages sent successfully! Processed: {result.ProcessedCount}, Failed: {result.FailedCount}";
                    }
                    else
                    {
                        TempData["Error"] = $"Failed to send messages: {result.Message}";
                        if (result.Errors.Any())
                        {
                            TempData["ErrorDetails"] = string.Join(", ", result.Errors);
                        }
                    }

                    return RedirectToAction(nameof(SendMessages));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error: {ex.Message}";
                }
            }

            return View(model);
        }

        // GET: WhatsAppApi/Usage
        public async Task<IActionResult> Usage(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                TempData["Error"] = "API key is required.";
                return RedirectToAction(nameof(Index));
            }

            var usage = await _whatsAppApiService.GetUsageStatsAsync(apiKey);
            if (usage == null)
            {
                TempData["Error"] = "Failed to get usage statistics.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ApiKey = apiKey;
            return View(usage);
        }

        // POST: WhatsAppApi/TestConnection
        [HttpPost]
        public async Task<IActionResult> TestConnection(string apiKey)
        {
            var isConnected = await _whatsAppApiService.TestApiConnectionAsync(apiKey);

            return Json(new { success = isConnected, message = isConnected ? "Connection successful" : "Connection failed" });
        }
    }

    public class CreateApiKeyViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int SubscriptionId { get; set; }
    }

    public class SendMessagesViewModel
    {
        public string ApiKey { get; set; } = string.Empty;
        public string PhoneNumbers { get; set; } = string.Empty; // One per line
        public string MessageContent { get; set; } = string.Empty; // Multiple lines
        public IFormFile? AttachedFile { get; set; }
        public int DelayBetweenMessages { get; set; } = 5000;
        public bool SendImmediately { get; set; } = true;
    }
}