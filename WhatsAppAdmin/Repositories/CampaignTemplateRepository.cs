using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WhatsApp.Shared.Data;
using SharedModels = WhatsApp.Shared.Models;
using AdminModels = WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Repositories
{
    /// <summary>
    /// Repository implementation for campaign template operations
    /// Handles all database operations related to campaign templates with async support
    /// </summary>
    public class CampaignTemplateRepository : ICampaignTemplateRepository
    {
        private readonly SaaSDbContext _saasContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CampaignTemplateRepository(SaaSDbContext saasContext, IHttpContextAccessor httpContextAccessor)
        {
            _saasContext = saasContext;
            _httpContextAccessor = httpContextAccessor;
        }

        private AdminModels.CampaignTemplate MapToAdminModel(SharedModels.CampaignTemplate shared)
        {
            return new AdminModels.CampaignTemplate
            {
                Id = shared.Id,
                Name = shared.Name,
                Description = shared.Description ?? string.Empty,
                MessageContent = shared.MessageTemplate,
                ImageUrl = null, // Shared model doesn't have ImageUrl
                IsActive = shared.IsActive,
                CreatedAt = shared.CreatedAt,
                UpdatedAt = shared.UpdatedAt ?? shared.CreatedAt,
                Category = shared.Category,
                IsGlobal = shared.IsGlobal,
                TimesUsed = shared.TimesUsed
            };
        }

        public async Task<IEnumerable<AdminModels.CampaignTemplate>> GetAllAsync()
        {
            var templates = await _saasContext.CampaignTemplates
                .OrderBy(ct => ct.Category)
                .ThenBy(ct => ct.Name)
                .ToListAsync();

            return templates.Select(MapToAdminModel).ToList();
        }

        public async Task<IEnumerable<AdminModels.CampaignTemplate>> GetActiveCampaignTemplatesAsync()
        {
            var templates = await _saasContext.CampaignTemplates
                .Where(ct => ct.IsActive)
                .OrderBy(ct => ct.Category)
                .ThenBy(ct => ct.Name)
                .ToListAsync();

            return templates.Select(MapToAdminModel).ToList();
        }

        public async Task<IEnumerable<AdminModels.CampaignTemplate>> GetByCategoryAsync(string category)
        {
            var templates = await _saasContext.CampaignTemplates
                .Where(ct => ct.Category == category)
                .OrderBy(ct => ct.Name)
                .ToListAsync();

            return templates.Select(MapToAdminModel).ToList();
        }

        public async Task<AdminModels.CampaignTemplate?> GetByIdAsync(int id)
        {
            var template = await _saasContext.CampaignTemplates.FindAsync(id);
            return template == null ? null : MapToAdminModel(template);
        }

        public async Task<AdminModels.CampaignTemplate> CreateAsync(AdminModels.CampaignTemplate template)
        {
            // Get current logged-in user ID
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("User must be logged in to create templates");
            }

            var sharedTemplate = new SharedModels.CampaignTemplate
            {
                UserId = userId, // Use actual logged-in user ID
                Name = template.Name,
                Description = template.Description,
                MessageTemplate = template.MessageContent,
                Category = template.Category,
                FemaleContent  = template.FemaleContent,
                MaleContent = template.MaleContent,
                IsActive = template.IsActive,
                IsGlobal = template.IsGlobal,
                IsSystemTemplate = true, // Mark as system template for admins
                TimesUsed = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _saasContext.CampaignTemplates.Add(sharedTemplate);
            await _saasContext.SaveChangesAsync();

            template.Id = sharedTemplate.Id;
            template.CreatedAt = sharedTemplate.CreatedAt;
            template.UpdatedAt = sharedTemplate.UpdatedAt ?? sharedTemplate.CreatedAt;
            return template;
        }

        public async Task<AdminModels.CampaignTemplate> UpdateAsync(AdminModels.CampaignTemplate template)
        {
            var sharedTemplate = await _saasContext.CampaignTemplates.FindAsync(template.Id);
            if (sharedTemplate == null)
                throw new InvalidOperationException($"CampaignTemplate {template.Id} not found");

            sharedTemplate.Name = template.Name;
            sharedTemplate.Description = template.Description;
            sharedTemplate.MessageTemplate = template.MessageContent;
            sharedTemplate.Category = template.Category;
            sharedTemplate.IsActive = template.IsActive;
            sharedTemplate.IsGlobal = template.IsGlobal;
            sharedTemplate.UpdatedAt = DateTime.UtcNow;

            await _saasContext.SaveChangesAsync();

            template.UpdatedAt = sharedTemplate.UpdatedAt ?? DateTime.UtcNow;
            return template;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var template = await _saasContext.CampaignTemplates.FindAsync(id);
            if (template == null)
                return false;

            _saasContext.CampaignTemplates.Remove(template);
            await _saasContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _saasContext.CampaignTemplates.AnyAsync(ct => ct.Id == id);
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            return await _saasContext.CampaignTemplates
                .Select(ct => ct.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
    }
}