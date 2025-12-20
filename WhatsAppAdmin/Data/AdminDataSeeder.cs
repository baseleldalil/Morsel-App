using Microsoft.AspNetCore.Identity;
using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Data
{
    /// <summary>
    /// Data seeder for initial admin setup
    /// Creates default permissions, subscriptions, and admin user
    /// </summary>
    public static class AdminDataSeeder
    {
        public static async Task SeedAsync(AdminDbContext context, UserManager<AdminUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed roles first
            await SeedRolesAsync(roleManager);

            // Seed admin users
            await SeedAdminUsersAsync(userManager);

            // Seed permissions if they don't exist
            await SeedPermissionsAsync(context);

            // Seed subscriptions if they don't exist
            await SeedSubscriptionsAsync(context);

            await context.SaveChangesAsync();
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "SuperAdmin", "Admin", "SemiAdmin" };

            foreach (string role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task SeedAdminUsersAsync(UserManager<AdminUser> userManager)
        {
            // Create initial Super Admin user if it doesn't exist
            var superAdminEmail = "admin@whatsappadmin.com";
            var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);

            if (superAdmin == null)
            {
                superAdmin = new AdminUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(superAdmin, "Admin123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                }
            }
        }

        private static async Task SeedPermissionsAsync(AdminDbContext context)
        {
            if (!context.Permissions.Any())
            {
                var permissions = new[]
                {
                    new Permission
                    {
                        Name = "CanCreateCampaign",
                        Description = "Allows user to create and manage WhatsApp campaigns"
                    },
                    new Permission
                    {
                        Name = "CanUseAPI",
                        Description = "Allows user to access the WhatsApp API endpoints"
                    },
                    new Permission
                    {
                        Name = "CanAccessTemplates",
                        Description = "Allows user to view and use campaign templates"
                    },
                    new Permission
                    {
                        Name = "CanSendBulkMessages",
                        Description = "Allows user to send messages to multiple recipients at once"
                    },
                    new Permission
                    {
                        Name = "CanViewAnalytics",
                        Description = "Allows user to view message delivery and engagement analytics"
                    },
                    new Permission
                    {
                        Name = "CanExportData",
                        Description = "Allows user to export campaign data and reports"
                    },
                    new Permission
                    {
                        Name = "CanScheduleMessages",
                        Description = "Allows user to schedule messages for future delivery"
                    },
                    new Permission
                    {
                        Name = "CanUseCustomFields",
                        Description = "Allows user to use custom fields in message templates"
                    }
                };

                await context.Permissions.AddRangeAsync(permissions);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedSubscriptionsAsync(AdminDbContext context)
        {
            if (!context.Subscriptions.Any())
            {
                // Get permissions for subscription assignments
                var permissions = context.Permissions.ToList();

                // Basic Plan
                var basicPlan = new Subscription
                {
                    Name = "Basic Plan",
                    Description = "Perfect for small businesses getting started with WhatsApp marketing",
                    MaxMessagesPerDay = 100,
                    Price = 9.99m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    TimerSettings = new TimerSettings
                    {
                        MinDelaySeconds = 5,
                        MaxDelaySeconds = 10,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                context.Subscriptions.Add(basicPlan);
                await context.SaveChangesAsync();

                // Add permissions to Basic Plan
                var basicPermissions = new[]
                {
                    "CanUseAPI",
                    "CanAccessTemplates"
                };

                foreach (var permName in basicPermissions)
                {
                    var permission = permissions.First(p => p.Name == permName);
                    context.SubscriptionPermissions.Add(new SubscriptionPermission
                    {
                        SubscriptionId = basicPlan.Id,
                        PermissionId = permission.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Professional Plan
                var proPlan = new Subscription
                {
                    Name = "Professional Plan",
                    Description = "Ideal for growing businesses with advanced messaging needs",
                    MaxMessagesPerDay = 500,
                    Price = 29.99m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    TimerSettings = new TimerSettings
                    {
                        MinDelaySeconds = 3,
                        MaxDelaySeconds = 8,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                context.Subscriptions.Add(proPlan);
                await context.SaveChangesAsync();

                // Add permissions to Professional Plan
                var proPermissions = new[]
                {
                    "CanUseAPI",
                    "CanAccessTemplates",
                    "CanCreateCampaign",
                    "CanSendBulkMessages",
                    "CanScheduleMessages"
                };

                foreach (var permName in proPermissions)
                {
                    var permission = permissions.First(p => p.Name == permName);
                    context.SubscriptionPermissions.Add(new SubscriptionPermission
                    {
                        SubscriptionId = proPlan.Id,
                        PermissionId = permission.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Enterprise Plan
                var enterprisePlan = new Subscription
                {
                    Name = "Enterprise Plan",
                    Description = "Complete solution for large businesses with unlimited messaging needs",
                    MaxMessagesPerDay = 2000,
                    Price = 99.99m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    TimerSettings = new TimerSettings
                    {
                        MinDelaySeconds = 1,
                        MaxDelaySeconds = 5,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                context.Subscriptions.Add(enterprisePlan);
                await context.SaveChangesAsync();

                // Add all permissions to Enterprise Plan
                foreach (var permission in permissions)
                {
                    context.SubscriptionPermissions.Add(new SubscriptionPermission
                    {
                        SubscriptionId = enterprisePlan.Id,
                        PermissionId = permission.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Free Trial Plan
                var trialPlan = new Subscription
                {
                    Name = "Free Trial",
                    Description = "Try WhatsApp messaging with limited features",
                    MaxMessagesPerDay = 10,
                    Price = 0.00m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    TimerSettings = new TimerSettings
                    {
                        MinDelaySeconds = 10,
                        MaxDelaySeconds = 15,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                context.Subscriptions.Add(trialPlan);
                await context.SaveChangesAsync();

                // Add basic permission to Trial Plan
                var trialPermission = permissions.First(p => p.Name == "CanUseAPI");
                context.SubscriptionPermissions.Add(new SubscriptionPermission
                {
                    SubscriptionId = trialPlan.Id,
                    PermissionId = trialPermission.Id,
                    CreatedAt = DateTime.UtcNow
                });

                await context.SaveChangesAsync();
            }

            // Seed sample campaign templates
            if (!context.CampaignTemplates.Any())
            {
                var templates = new[]
                {
                    new CampaignTemplate
                    {
                        Name = "Welcome Message",
                        Description = "Welcome new customers to your service",
                        MessageContent = "Hello {name}! 👋 Welcome to {company}. We're excited to have you on board. If you have any questions, feel free to reach out!",
                        Category = "Welcome",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new CampaignTemplate
                    {
                        Name = "Order Confirmation",
                        Description = "Confirm order details with customers",
                        MessageContent = "Hi {name}, your order has been confirmed! 📦 Order ID: {order_id}. Expected delivery: {delivery_date}. Track your order at {tracking_url}",
                        Category = "Transactional",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new CampaignTemplate
                    {
                        Name = "Promotional Offer",
                        Description = "Promote special offers and discounts",
                        MessageContent = "🎉 Special offer for {name}! Get 20% off your next purchase at {company}. Use code: SAVE20. Valid until {expiry_date}. Shop now: {shop_url}",
                        Category = "Promotional",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new CampaignTemplate
                    {
                        Name = "Appointment Reminder",
                        Description = "Remind customers about upcoming appointments",
                        MessageContent = "Hi {name}, this is a reminder about your appointment with {company} on {date} at {time}. Please reply 'CONFIRM' to confirm or 'RESCHEDULE' to change.",
                        Category = "Reminder",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new CampaignTemplate
                    {
                        Name = "Support Follow-up",
                        Description = "Follow up on support tickets",
                        MessageContent = "Hello {name}, we wanted to follow up on your recent support request. Was your issue resolved? Please rate your experience: {feedback_url}",
                        Category = "Support",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                await context.CampaignTemplates.AddRangeAsync(templates);
                await context.SaveChangesAsync();
            }
        }
    }
}