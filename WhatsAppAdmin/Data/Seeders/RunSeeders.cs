using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WhatsAppAdmin.Data;
using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Data.Seeders
{
    /// <summary>
    /// Standalone seeder runner for manual execution
    /// Use this for testing or manual seeding operations
    /// </summary>
    public class RunSeeders
    {
        /// <summary>
        /// Run this method to manually execute seeders
        /// Useful for development and testing
        /// </summary>
        public static async Task ExecuteAsync(
            AdminDbContext adminContext,
            UserManager<AdminUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            Console.WriteLine("🌱 Manual Seeder Execution Started");
            Console.WriteLine("=====================================");

            try
            {
                // Check current database state
                await DisplayCurrentState(adminContext);

                // Execute seeders
                await MasterSeeder.SeedAllAsync(userManager, roleManager, adminContext);

                // Display final state
                Console.WriteLine("\n📊 Final Database State:");
                await DisplayCurrentState(adminContext);

                Console.WriteLine("\n✅ Manual seeding completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Seeding failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("=====================================");
        }

        private static async Task DisplayCurrentState(AdminDbContext context)
        {
            var userCount = await context.Users.CountAsync();
            var subscriptionCount = await context.Subscriptions.CountAsync();
            var assignmentCount = await context.UserSubscriptions.CountAsync();

            Console.WriteLine($"👥 Admin Users: {userCount}");
            Console.WriteLine($"📦 Subscriptions: {subscriptionCount}");
            Console.WriteLine($"📋 Assignments: {assignmentCount}");
        }

        /// <summary>
        /// Clear all seeded data (use with caution!)
        /// </summary>
        public static async Task ClearAllDataAsync(AdminDbContext context)
        {
            Console.WriteLine("⚠️ WARNING: Clearing all seeded data...");

            // Remove user subscriptions
            var userSubscriptions = await context.UserSubscriptions.ToListAsync();
            context.UserSubscriptions.RemoveRange(userSubscriptions);

            await context.SaveChangesAsync();

            Console.WriteLine("✅ Seeded data cleared");
        }

        /// <summary>
        /// Reset specific data type
        /// </summary>
        public static async Task ResetUserAssignmentsAsync(AdminDbContext context)
        {
            Console.WriteLine("🔄 Resetting user assignments...");

            var assignments = await context.UserSubscriptions.ToListAsync();
            context.UserSubscriptions.RemoveRange(assignments);
            await context.SaveChangesAsync();

            // Re-seed assignments
            await UserSubscriptionSeeder.SeedUserSubscriptionsAsync(context);

            Console.WriteLine("✅ User assignments reset and re-seeded");
        }
    }
}