using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Services
{
    /// <summary>
    /// Interface for permission business logic operations
    /// Defines high-level operations for permission management with business rules
    /// </summary>
    public interface IPermissionService
    {
        Task<IEnumerable<Permission>> GetAllPermissionsAsync();
        Task<Permission?> GetPermissionByIdAsync(int id);
        Task<Permission> CreatePermissionAsync(Permission permission);
        Task<Permission> UpdatePermissionAsync(Permission permission);
        Task<bool> DeletePermissionAsync(int id);
        Task<bool> CanDeletePermissionAsync(int id);
        Task<bool> ValidatePermissionAsync(Permission permission);
    }
}