using Microsoft.AspNetCore.Identity;
using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Data.Seeders
{
    /// <summary>
    /// Seeds admin users with different roles and permissions
    /// </summary>
    public static class UserSeeder
    {
        public static async Task SeedUsersAsync(UserManager<AdminUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Ensure roles exist
            await SeedRolesAsync(roleManager);

            // Seed admin users
            await SeedAdminUsersAsync(userManager);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "SuperAdmin", "Admin", "SemiAdmin", "Manager", "Support" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task SeedAdminUsersAsync(UserManager<AdminUser> userManager)
        {
            var users = new List<(AdminUser User, string Password, string[] Roles)>
            {
                // Super Admin
                (new AdminUser
                {
                    UserName = "superadmin@morsel.com",
                    Email = "superadmin@morsel.com",
                    FirstName = "Super",
                    LastName = "Admin",
                    CompanyName = "Morsel Technologies",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }, "SuperAdmin123!", new[] { "SuperAdmin" }),

                // Main Admin
                (new AdminUser
                {
                    UserName = "admin@morsel.com",
                    Email = "admin@morsel.com",
                    FirstName = "Main",
                    LastName = "Administrator",
                    CompanyName = "Morsel Technologies",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }, "Admin123!", new[] { "Admin" }),

                // Semi Admin
                (new AdminUser
                {
                    UserName = "semiadmin@morsel.com",
                    Email = "semiadmin@morsel.com",
                    FirstName = "Semi",
                    LastName = "Admin",
                    CompanyName = "Morsel Technologies",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }, "SemiAdmin123!", new[] { "SemiAdmin" }),

                // Manager
                (new AdminUser
                {
                    UserName = "manager@morsel.com",
                    Email = "manager@morsel.com",
                    FirstName = "John",
                    LastName = "Manager",
                    CompanyName = "Morsel Technologies",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }, "Manager123!", new[] { "Manager" }),

                // Support Staff
                (new AdminUser
                {
                    UserName = "support@morsel.com",
                    Email = "support@morsel.com",
                    FirstName = "Support",
                    LastName = "Staff",
                    CompanyName = "Morsel Technologies",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }, "Support123!", new[] { "Support" }),

                // Regional Manager
                (new AdminUser
                {
                    UserName = "regional@morsel.com",
                    Email = "regional@morsel.com",
                    FirstName = "Sarah",
                    LastName = "Regional",
                    CompanyName = "Morsel UAE",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }, "Regional123!", new[] { "Manager", "Support" }),

                // Demo Admin (for testing)
                (new AdminUser
                {
                    UserName = "demo@morsel.com",
                    Email = "demo@morsel.com",
                    FirstName = "Demo",
                    LastName = "User",
                    CompanyName = "Demo Company",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }, "Demo123!", new[] { "SemiAdmin" })
            };

            foreach (var (user, password, roles) in users)
            {
                var existingUser = await userManager.FindByEmailAsync(user.Email);
                if (existingUser == null)
                {
                    var result = await userManager.CreateAsync(user, password);
                    if (result.Succeeded)
                    {
                        // Assign roles to user
                        foreach (var role in roles)
                        {
                            await userManager.AddToRoleAsync(user, role);
                        }

                        Console.WriteLine($"✅ Created user: {user.Email} with roles: {string.Join(", ", roles)}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to create user {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    Console.WriteLine($"ℹ️ User {user.Email} already exists");
                }
            }
        }
    }
}