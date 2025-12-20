using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WhatsApp.Shared.Data;
using WhatsAppSender.API.Models;
using WhatsAppSender.API.Services;

namespace WhatsAppSender.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly SaaSDbContext _context;
        private readonly IApiKeyService _apiKeyService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            SaaSDbContext context,
            IApiKeyService apiKeyService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _apiKeyService = apiKeyService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Validate user credentials
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null)
                {
                    return Ok(new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    });
                }

                // Verify password
                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    return Ok(new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    });
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Check if user has an active API key
                var existingApiKey = await _context.ApiKeys
                    .Include(k => k.SubscriptionPlan)
                    .Where(k => k.UserId == user.Id.ToString() && k.IsActive)
                    .FirstOrDefaultAsync();

                WhatsApp.Shared.Models.ApiKey apiKey;
                if (existingApiKey != null)
                {
                    // Return existing API key
                    apiKey = existingApiKey;
                    _logger.LogInformation("User {Email} logged in with existing API key", user.Email);
                }
                else
                {
                    // Create new API key with Free Trial subscription (ID = 1)
                    var freeTrialSubscription = await _context.SubscriptionPlans
                        .FirstOrDefaultAsync(s => s.Id == 1);

                    if (freeTrialSubscription == null)
                    {
                        return BadRequest(new LoginResponse
                        {
                            Success = false,
                            Message = "No subscription plans available. Please contact administrator."
                        });
                    }

                    apiKey = await _apiKeyService.CreateApiKeyAsync(
                        user.Id,
                        freeTrialSubscription.Id,
                        "Default API Key");

                    // Reload to get subscription details
                    apiKey = await _context.ApiKeys
                        .Include(k => k.SubscriptionPlan)
                        .FirstOrDefaultAsync(k => k.Id == apiKey.Id);

                    _logger.LogInformation("New API key created for user {Email}", user.Email);
                }

                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    ApiKey = apiKey!.KeyValue,
                    User = new UserInfo
                    {
                        Email = user.Email,
                        Name = $"{user.FirstName} {user.LastName}".Trim(),
                        SubscriptionPlan = apiKey.SubscriptionPlan?.Name ?? "Unknown",
                        LastLoginAt = user.LastLoginAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Email}", request.Email);
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred during login"
                });
            }
        }

        private static bool VerifyPassword(string password, string passwordHash)
        {
            using var sha256 = SHA256.Create();
            var hash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
            return hash == passwordHash;
        }

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hash);
        }
    }
}
