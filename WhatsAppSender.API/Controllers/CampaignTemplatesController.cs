using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WhatsAppSender.API.Services;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;
using WhatsAppSender.API.Models;

namespace WhatsAppSender.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CampaignTemplatesController : ControllerBase
    {
        private readonly SaaSDbContext _context;
        private readonly IApiKeyService _apiKeyService;
        private readonly ILogger<CampaignTemplatesController> _logger;

        public CampaignTemplatesController(
            SaaSDbContext context,
            IApiKeyService apiKeyService,
            ILogger<CampaignTemplatesController> logger)
        {
            _context = context;
            _apiKeyService = apiKeyService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetCampaignTemplates([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                // Validate pagination parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50;

                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                // Get user ID from cached API key (no extra DB query needed!)
                var userId = apiKeyEntity.UserId;

                // OPTIMIZED: Single query with OR condition to get both admin and user templates
                // Use _context since CampaignTemplates table belongs to SaaSDbContext with shared model
                var allTemplates = await _context.CampaignTemplates
                    .AsNoTracking()
                    .Where(t => t.IsActive && (t.IsSystemTemplate || t.UserId == userId))
                    .OrderByDescending(t => t.IsSystemTemplate)
                    .ThenByDescending(t => t.CreatedAt)
                    .Select(t => new CampaignTemplateResponse
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Content = t.MessageTemplate, // Database column is MessageTemplate
                        MaleContent = t.MaleContent,
                        FemaleContent = t.FemaleContent,
                        Description = t.Description,
                        IsSystemTemplate = t.IsSystemTemplate,
                        IsActive = t.IsActive,
                        UserId = t.UserId,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt
                    })
                    .ToListAsync();

                // Separate in memory (already loaded)
                var adminTemplates = allTemplates.Where(t => t.IsSystemTemplate).ToList();
                var userTemplates = allTemplates.Where(t => !t.IsSystemTemplate).ToList();

                return Ok(new CampaignTemplatesGroupedResponse
                {
                    AdminTemplates = adminTemplates,
                    UserTemplates = userTemplates,
                    TotalCount = allTemplates.Count,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving campaign templates");
                return StatusCode(500, new { error = "Internal server error while retrieving campaign templates" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCampaignTemplate(int id)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                // Get user from API key using shared SaaS context (Identity table)
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == apiKeyEntity.UserEmail);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found" });
                }

                var template = await _context.CampaignTemplates
                    .Where(t => t.Id == id && (t.IsSystemTemplate || t.UserId == user.Id))
                    .Select(t => new CampaignTemplateResponse
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Content = t.MessageTemplate, // Database column is MessageTemplate
                        MaleContent = t.MaleContent,
                        FemaleContent = t.FemaleContent,
                        Description = t.Description,
                        IsSystemTemplate = t.IsSystemTemplate,
                        IsActive = t.IsActive,
                        UserId = t.UserId,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (template == null)
                {
                    return NotFound(new { error = "Campaign template not found" });
                }

                return Ok(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving campaign template");
                return StatusCode(500, new { error = "Internal server error while retrieving campaign template" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCampaignTemplate([FromBody] CreateCampaignTemplateRequest request)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                // Get user from API key using shared SaaS context (Identity table)
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == apiKeyEntity.UserId);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found" });
                }

                // Auto-generate male/female content if not provided
                var maleContent = request.MaleContent ?? request.Content;
                var femaleContent = request.FemaleContent ?? request.Content;

                var template = new WhatsApp.Shared.Models.CampaignTemplate
                {
                    Name = request.Name,
                    MessageTemplate = request.Content, // Map Content to MessageTemplate
                    MaleContent = maleContent,
                    FemaleContent = femaleContent,
                    Description = request.Description,
                    UserId = user.Id,
                    IsSystemTemplate = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.CampaignTemplates.AddAsync(template);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Campaign template created: {template.Name} for user {user.Email}");

                return CreatedAtAction(
                    nameof(GetCampaignTemplate),
                    new { id = template.Id },
                    new CampaignTemplateResponse
                    {
                        Id = template.Id,
                        Name = template.Name,
                        Content = template.MessageTemplate, // Map MessageTemplate to Content
                        MaleContent = template.MaleContent,
                        FemaleContent = template.FemaleContent,
                        Description = template.Description,
                        IsSystemTemplate = template.IsSystemTemplate,
                        IsActive = template.IsActive,
                        UserId = template.UserId,
                        CreatedAt = template.CreatedAt,
                        UpdatedAt = template.UpdatedAt
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating campaign template");
                return StatusCode(500, new { error = "Internal server error while creating campaign template" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCampaignTemplate(int id, [FromBody] UpdateCampaignTemplateRequest request)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                // Get user from API key using shared SaaS context (Identity table)
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == apiKeyEntity.UserEmail);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found" });
                }

                // Only allow users to update their own templates, not system templates
                var template = await _context.CampaignTemplates
                    .FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id && !t.IsSystemTemplate);

                if (template == null)
                {
                    return NotFound(new { error = "Campaign template not found or cannot be modified" });
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    template.Name = request.Name;
                }

                if (!string.IsNullOrWhiteSpace(request.Content))
                {
                    template.MessageTemplate = request.Content; // Map Content to MessageTemplate
                }

                if (request.MaleContent != null)
                {
                    template.MaleContent = request.MaleContent;
                }

                if (request.FemaleContent != null)
                {
                    template.FemaleContent = request.FemaleContent;
                }

                if (request.Description != null)
                {
                    template.Description = request.Description;
                }

                if (request.IsActive.HasValue)
                {
                    template.IsActive = request.IsActive.Value;
                }

                template.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Campaign template updated: {template.Name} for user {user.Email}");

                return Ok(new CampaignTemplateResponse
                {
                    Id = template.Id,
                    Name = template.Name,
                    Content = template.MessageTemplate, // Map MessageTemplate to Content
                    MaleContent = template.MaleContent,
                    FemaleContent = template.FemaleContent,
                    Description = template.Description,
                    IsSystemTemplate = template.IsSystemTemplate,
                    IsActive = template.IsActive,
                    UserId = template.UserId,
                    CreatedAt = template.CreatedAt,
                    UpdatedAt = template.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating campaign template");
                return StatusCode(500, new { error = "Internal server error while updating campaign template" });
            }
        }

        [HttpPost("admin/system-template")]
        public async Task<IActionResult> CreateSystemCampaignTemplate([FromBody] CreateCampaignTemplateRequest request)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                // Get user from API key using shared SaaS context (Identity table)
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == apiKeyEntity.UserEmail);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found" });
                }

                // Check if user is admin (check roles or other logic)
                // For now, allow any valid user to create system templates
                // TODO: Implement proper admin role checking

                // Auto-generate male/female content if not provided
                var maleContent = request.MaleContent ?? request.Content;
                var femaleContent = request.FemaleContent ?? request.Content;

                var template = new WhatsApp.Shared.Models.CampaignTemplate
                {
                    Name = request.Name,
                    MessageTemplate = request.Content, // Map Content to MessageTemplate
                    MaleContent = maleContent,
                    FemaleContent = femaleContent,
                    Description = request.Description,
                    UserId = user.Id, // Use creator's ID for system templates
                    IsSystemTemplate = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.CampaignTemplates.AddAsync(template);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"System campaign template created: {template.Name} by admin {user.Email}");

                return CreatedAtAction(
                    nameof(GetCampaignTemplate),
                    new { id = template.Id },
                    new CampaignTemplateResponse
                    {
                        Id = template.Id,
                        Name = template.Name,
                        Content = template.MessageTemplate, // Map MessageTemplate to Content
                        MaleContent = template.MaleContent,
                        FemaleContent = template.FemaleContent,
                        Description = template.Description,
                        IsSystemTemplate = template.IsSystemTemplate,
                        IsActive = template.IsActive,
                        UserId = template.UserId,
                        CreatedAt = template.CreatedAt,
                        UpdatedAt = template.UpdatedAt
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system campaign template");
                return StatusCode(500, new { error = "Internal server error while creating system campaign template" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCampaignTemplate(int id)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                // Get user from API key using shared SaaS context (Identity table)
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == apiKeyEntity.UserEmail);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found" });
                }

                // Only allow users to delete their own templates, not system templates
                var template = await _context.CampaignTemplates
                    .FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id && !t.IsSystemTemplate);

                if (template == null)
                {
                    return NotFound(new { error = "Campaign template not found or cannot be deleted" });
                }

                _context.CampaignTemplates.Remove(template);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Campaign template deleted: {template.Name} for user {user.Email}");

                return Ok(new { message = "Campaign template deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting campaign template");
                return StatusCode(500, new { error = "Internal server error while deleting campaign template" });
            }
        }
    }
}
