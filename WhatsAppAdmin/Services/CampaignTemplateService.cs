using WhatsAppAdmin.Models;
using WhatsAppAdmin.Repositories;

namespace WhatsAppAdmin.Services
{
    /// <summary>
    /// Service implementation for campaign template business logic
    /// Handles complex template operations with business rules and validation
    /// </summary>
    public class CampaignTemplateService : ICampaignTemplateService
    {
        private readonly ICampaignTemplateRepository _campaignTemplateRepository;

        public CampaignTemplateService(ICampaignTemplateRepository campaignTemplateRepository)
        {
            _campaignTemplateRepository = campaignTemplateRepository;
        }

        public async Task<IEnumerable<CampaignTemplate>> GetAllTemplatesAsync()
        {
            return await _campaignTemplateRepository.GetAllAsync();
        }

        public async Task<IEnumerable<CampaignTemplate>> GetActiveTemplatesAsync()
        {
            return await _campaignTemplateRepository.GetActiveCampaignTemplatesAsync();
        }

        public async Task<IEnumerable<CampaignTemplate>> GetTemplatesByCategoryAsync(string category)
        {
            return await _campaignTemplateRepository.GetByCategoryAsync(category);
        }

        public async Task<CampaignTemplate?> GetTemplateByIdAsync(int id)
        {
            return await _campaignTemplateRepository.GetByIdAsync(id);
        }

        public async Task<CampaignTemplate> CreateTemplateAsync(CampaignTemplate template)
        {
            if (!await ValidateTemplateAsync(template))
                throw new ArgumentException("Invalid template data");

            return await _campaignTemplateRepository.CreateAsync(template);
        }

        public async Task<CampaignTemplate> UpdateTemplateAsync(CampaignTemplate template)
        {
            if (!await ValidateTemplateAsync(template))
                throw new ArgumentException("Invalid template data");

            return await _campaignTemplateRepository.UpdateAsync(template);
        }

        public async Task<bool> DeleteTemplateAsync(int id)
        {
            return await _campaignTemplateRepository.DeleteAsync(id);
        }

        public Task<bool> ValidateTemplateAsync(CampaignTemplate template)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(template.Name))
                return Task.FromResult(false);

            if (string.IsNullOrWhiteSpace(template.MessageContent))
                return Task.FromResult(false);

            if (template.MessageContent.Length > 2000)
                return Task.FromResult(false);

            // Validate category
            if (string.IsNullOrWhiteSpace(template.Category))
                template.Category = "General";

            return Task.FromResult(true);
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            return await _campaignTemplateRepository.GetCategoriesAsync();
        }
    }
}