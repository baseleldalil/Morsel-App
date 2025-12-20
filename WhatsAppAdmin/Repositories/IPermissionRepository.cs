using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Repositories
{
    /// <summary>
    /// Interface for permission repository operations
    /// Defines async CRUD operations for permission management
    /// </summary>
    public interface IPermissionRepository
    {
        Task<IEnumerable<Permission>> GetAllAsync();
        Task<Permission?> GetByIdAsync(int id);
        Task<Permission> CreateAsync(Permission permission);
        Task<Permission> UpdateAsync(Permission permission);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> NameExistsAsync(string name, int? excludeId = null);
    }
}