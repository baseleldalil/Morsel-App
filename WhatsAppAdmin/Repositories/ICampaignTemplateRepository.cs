using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Repositories
{
    /// <summary>
    /// Interface for campaign template repository operations
    /// Defines async CRUD operations for campaign template management
    /// </summary>
    public interface ICampaignTemplateRepository
    {
        Task<IEnumerable<CampaignTemplate>> GetAllAsync();
        Task<IEnumerable<CampaignTemplate>> GetActiveCampaignTemplatesAsync();
        Task<IEnumerable<CampaignTemplate>> GetByCategoryAsync(string category);
        Task<CampaignTemplate?> GetByIdAsync(int id);
        Task<CampaignTemplate> CreateAsync(CampaignTemplate template);
        Task<CampaignTemplate> UpdateAsync(CampaignTemplate template);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<IEnumerable<string>> GetCategoriesAsync();
    }
}