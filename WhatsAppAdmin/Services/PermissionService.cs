using WhatsAppAdmin.Models;
using WhatsAppAdmin.Repositories;
using WhatsAppAdmin.Data;
using Microsoft.EntityFrameworkCore;

namespace WhatsAppAdmin.Services
{
    /// <summary>
    /// Service implementation for permission business logic
    /// Handles complex permission operations with business rules and validation
    /// NOTE: Uses AdminDbContext because Permission model doesn't exist in SaaSDbContext
    /// This service is not registered in DI - kept for future use if Permission is added to SaaSDbContext
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly AdminDbContext _context;

        public PermissionService(IPermissionRepository permissionRepository, AdminDbContext context)
        {
            _permissionRepository = permissionRepository;
            _context = context;
        }

        public async Task<IEnumerable<Permission>> GetAllPermissionsAsync()
        {
            return await _permissionRepository.GetAllAsync();
        }

        public async Task<Permission?> GetPermissionByIdAsync(int id)
        {
            return await _permissionRepository.GetByIdAsync(id);
        }

        public async Task<Permission> CreatePermissionAsync(Permission permission)
        {
            if (!await ValidatePermissionAsync(permission))
                throw new ArgumentException("Invalid permission data");

            return await _permissionRepository.CreateAsync(permission);
        }

        public async Task<Permission> UpdatePermissionAsync(Permission permission)
        {
            if (!await ValidatePermissionAsync(permission))
                throw new ArgumentException("Invalid permission data");

            return await _permissionRepository.UpdateAsync(permission);
        }

        public async Task<bool> DeletePermissionAsync(int id)
        {
            if (!await CanDeletePermissionAsync(id))
                return false;

            return await _permissionRepository.DeleteAsync(id);
        }

        public async Task<bool> CanDeletePermissionAsync(int id)
        {
            // SubscriptionPermissions not in SaaSDbContext - allow deletion
            return await Task.FromResult(true);
        }

        public async Task<bool> ValidatePermissionAsync(Permission permission)
        {
            // Check if name already exists
            if (await _permissionRepository.NameExistsAsync(permission.Name, permission.Id))
                return false;

            // Validate required fields
            if (string.IsNullOrWhiteSpace(permission.Name))
                return false;

            return true;
        }
    }
}