using Microsoft.EntityFrameworkCore;
using WhatsAppAdmin.Data;
using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Repositories
{
    /// <summary>
    /// Repository implementation for permission operations
    /// Handles all database operations related to permissions with async support
    /// NOTE: Uses AdminDbContext because Permission model doesn't exist in SaaSDbContext
    /// This repository is not registered in DI - kept for future use if Permission is added to SaaSDbContext
    /// </summary>
    public class PermissionRepository : IPermissionRepository
    {
        private readonly AdminDbContext _context;

        public PermissionRepository(AdminDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Permission>> GetAllAsync()
        {
            return await _context.Permissions
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Permission?> GetByIdAsync(int id)
        {
            return await _context.Permissions.FindAsync(id);
        }

        public async Task<Permission> CreateAsync(Permission permission)
        {
            permission.CreatedAt = DateTime.UtcNow;
            permission.UpdatedAt = DateTime.UtcNow;

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();
            return permission;
        }

        public async Task<Permission> UpdateAsync(Permission permission)
        {
            permission.UpdatedAt = DateTime.UtcNow;

            _context.Permissions.Update(permission);
            await _context.SaveChangesAsync();
            return permission;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
                return false;

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Permissions.AnyAsync(p => p.Id == id);
        }

        public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
        {
            var query = _context.Permissions.Where(p => p.Name == name);

            if (excludeId.HasValue)
                query = query.Where(p => p.Id != excludeId.Value);

            return await query.AnyAsync();
        }
    }
}