using Microsoft.AspNetCore.Identity;
using WhatsApp.Shared.Models;

namespace WhatsApp.Shared.Data.Seeders
{
    /// <summary>
    /// Seeds admin users with different roles for the WhatsApp SaaS platform
    /// </summary>
    public static class AdminUserSeeder
    {
        /// <summary>
        /// Seeds roles and admin users into the database
        /// </summary>
        /// <param name="userManager">UserManager for ApplicationUser</param>
        /// <param name="roleManager">RoleManager for IdentityRole</param>
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Ensure roles exist
            await SeedRolesAsync(roleManager);

            // Seed admin users
            await SeedAdminUsersAsync(userManager);
        }

        /// <summary>
        /// Creates all required roles for the system
        /// </summary>
        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "SuperAdmin", "Admin", "SemiAdmin", "Manager", "Support" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));
                    if (result.Succeeded)
                    {
                        Console.WriteLine($"✅ Created role: {role}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to create role {role}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    Console.WriteLine($"ℹ️  Role {role} already exists");
                }
            }
        }

        /// <summary>
        /// Seeds default admin users with various roles
        /// </summary>
        private static async Task SeedAdminUsersAsync(UserManager<ApplicationUser> userManager)
        {
            var users = new List<(ApplicationUser User, string Password, string[] Roles)>
            {
                // Super Admin - Full system access
                (new ApplicationUser
                {
                    UserName = "admin@whatsapp-saas.com",
                    Email = "admin@whatsapp-saas.com",
                    FirstName = "System",
                    LastName = "Administrator",
                    CompanyName = "WhatsApp SaaS Platform",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }, "Admin@123456", new[] { "SuperAdmin" }),

                // Main Admin
                (new ApplicationUser
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

                // Regular Admin
                (new ApplicationUser
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

                // Semi Admin - Limited administrative access
                (new ApplicationUser
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

                // Manager - Operational management
                (new ApplicationUser
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

                // Support Staff - Customer support access
                (new ApplicationUser
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

                // Regional Manager - Multi-role access
                (new ApplicationUser
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

                // Demo Admin - For testing and demonstrations
                (new ApplicationUser
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
                            // Ensure role exists before assigning
                            var roleExists = await userManager.GetUsersInRoleAsync(role);
                            if (roleExists != null)
                            {
                                await userManager.AddToRoleAsync(user, role);
                            }
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
                    Console.WriteLine($"ℹ️  User {user.Email} already exists");
                }
            }
        }

        /// <summary>
        /// Quick summary of seeded admin users
        /// </summary>
        public static async Task DisplaySummaryAsync(UserManager<ApplicationUser> userManager)
        {
            Console.WriteLine("\n📊 === ADMIN USER SUMMARY ===");

            string[] roles = { "SuperAdmin", "Admin", "SemiAdmin", "Manager", "Support" };

            foreach (var role in roles)
            {
                var usersInRole = await userManager.GetUsersInRoleAsync(role);
                Console.WriteLine($"👥 {role}: {usersInRole.Count} users");

                foreach (var user in usersInRole)
                {
                    Console.WriteLine($"   - {user.Email} ({user.FirstName} {user.LastName})");
                }
            }

            Console.WriteLine("=============================\n");
        }
    }
}
