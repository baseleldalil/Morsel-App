using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Services
{
    /// <summary>
    /// Interface for campaign template business logic operations
    /// Defines high-level operations for campaign template management with business rules
    /// </summary>
    public interface ICampaignTemplateService
    {
        Task<IEnumerable<CampaignTemplate>> GetAllTemplatesAsync();
        Task<IEnumerable<CampaignTemplate>> GetActiveTemplatesAsync();
        Task<IEnumerable<CampaignTemplate>> GetTemplatesByCategoryAsync(string category);
        Task<CampaignTemplate?> GetTemplateByIdAsync(int id);
        Task<CampaignTemplate> CreateTemplateAsync(CampaignTemplate template);
        Task<CampaignTemplate> UpdateTemplateAsync(CampaignTemplate template);
        Task<bool> DeleteTemplateAsync(int id);
        Task<bool> ValidateTemplateAsync(CampaignTemplate template);
        Task<IEnumerable<string>> GetCategoriesAsync();
    }
}