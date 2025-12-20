using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WhatsAppAdmin.Data;
using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Data.Seeders
{
    /// <summary>
    /// Seeds realistic campaign templates for UAE businesses
    /// Includes Arabic and English templates for various business scenarios
    /// </summary>
    public static class CampaignTemplateSeeder
    {
        public static async Task SeedCampaignTemplatesAsync(AdminDbContext context, UserManager<AdminUser> userManager)
        {
            // Check if templates already exist
            if (await context.CampaignTemplates.AnyAsync())
            {
                Console.WriteLine("ℹ️ Campaign templates already exist, skipping seeding");
                return;
            }

            Console.WriteLine("📧 Seeding campaign templates...");

            var templates = new List<CampaignTemplate>
            {
                // Welcome Messages
                new CampaignTemplate
                {
                    Name = "Welcome Message - English",
                    Description = "Professional welcome message for new customers",
                    MessageContent = "Welcome to {COMPANY_NAME}! 🌟\n\nThank you for choosing our services. We're excited to have you on board.\n\nYour account is now active and ready to use. If you need any assistance, our support team is here to help 24/7.\n\nBest regards,\n{COMPANY_NAME} Team",
                    IsActive = true,
                    Category = "Welcome",
                    CreatedAt = DateTime.UtcNow.AddDays(-45),
                    UpdatedAt = DateTime.UtcNow.AddDays(-10)
                },

                new CampaignTemplate
                {
                    Name = "رسالة ترحيب - عربي",
                    Description = "رسالة ترحيب احترافية للعملاء الجدد باللغة العربية",
                    MessageContent = "أهلاً وسهلاً بك في {COMPANY_NAME}! 🌟\n\nشكراً لك لاختيارك خدماتنا. نحن متحمسون لانضمامك إلينا.\n\nحسابك أصبح نشطاً وجاهزاً للاستخدام. إذا كنت بحاجة لأي مساعدة، فريق الدعم متاح 24/7.\n\nمع أطيب التحيات،\nفريق {COMPANY_NAME}",
                    IsActive = true,
                    Category = "Welcome",
                    CreatedAt = DateTime.UtcNow.AddDays(-40),
                    UpdatedAt = DateTime.UtcNow.AddDays(-8)
                },

                // Order Confirmations
                new CampaignTemplate
                {
                    Name = "Order Confirmation",
                    Description = "Order confirmation with tracking details",
                    MessageContent = "Order Confirmed! 📦\n\nHi {CUSTOMER_NAME},\n\nYour order #{ORDER_NUMBER} has been confirmed and is being prepared.\n\n💰 Total: AED {TOTAL_AMOUNT}\n📅 Expected Delivery: {DELIVERY_DATE}\n🚚 Tracking: {TRACKING_URL}\n\nThank you for your business!",
                    IsActive = true,
                    Category = "Orders",
                    CreatedAt = DateTime.UtcNow.AddDays(-35),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5)
                },

                // Appointment Reminders
                new CampaignTemplate
                {
                    Name = "Appointment Reminder - Healthcare",
                    Description = "Medical appointment reminder for UAE healthcare providers",
                    MessageContent = "Appointment Reminder 🏥\n\nDear {PATIENT_NAME},\n\nThis is a reminder for your appointment:\n\n📅 Date: {APPOINTMENT_DATE}\n🕐 Time: {APPOINTMENT_TIME}\n👨‍⚕️ Doctor: Dr. {DOCTOR_NAME}\n📍 Location: {CLINIC_ADDRESS}\n\nPlease arrive 15 minutes early. If you need to reschedule, call us at {PHONE_NUMBER}.\n\nThank you!",
                    IsActive = true,
                    Category = "Appointments",
                    CreatedAt = DateTime.UtcNow.AddDays(-50),
                    UpdatedAt = DateTime.UtcNow.AddDays(-12)
                },

                // Promotional Messages
                new CampaignTemplate
                {
                    Name = "Ramadan Special Offer",
                    Description = "Special promotional offer for Ramadan season",
                    MessageContent = "🌙 Ramadan Mubarak! 🌙\n\nCelebrate this holy month with our special offer!\n\n🎁 Get 30% OFF on all services\n📅 Valid until: {EXPIRY_DATE}\n🔖 Code: RAMADAN2024\n\n✨ Perfect time to treat yourself or your loved ones.\n\nOrder now: {ORDER_LINK}\n\nRamadan Kareem! 🕌",
                    IsActive = true,
                    Category = "Promotions",
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-3)
                },

                // Delivery Updates
                new CampaignTemplate
                {
                    Name = "Delivery Update - Out for Delivery",
                    Description = "Notification when package is out for delivery",
                    MessageContent = "Your order is on the way! 🚚\n\nHi {CUSTOMER_NAME},\n\nGreat news! Your order #{ORDER_NUMBER} is out for delivery.\n\n📦 Package: {PACKAGE_DESCRIPTION}\n🕐 Expected: {DELIVERY_TIME}\n📱 Driver: {DRIVER_NAME} - {DRIVER_PHONE}\n📍 Address: {DELIVERY_ADDRESS}\n\nPlease be available to receive your order.\n\nTrack live: {TRACKING_LINK}",
                    IsActive = true,
                    Category = "Delivery",
                    CreatedAt = DateTime.UtcNow.AddDays(-25),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },

                // Payment Reminders
                new CampaignTemplate
                {
                    Name = "Payment Due Reminder",
                    Description = "Friendly payment reminder for outstanding invoices",
                    MessageContent = "Payment Reminder 💳\n\nDear {CUSTOMER_NAME},\n\nThis is a friendly reminder that payment for invoice #{INVOICE_NUMBER} is due.\n\n💰 Amount: AED {AMOUNT_DUE}\n📅 Due Date: {DUE_DATE}\n🏦 Payment Options: {PAYMENT_METHODS}\n\nPlease settle at your earliest convenience to avoid service interruption.\n\nNeed help? Contact us at {SUPPORT_PHONE}\n\nThank you for your business!",
                    IsActive = true,
                    Category = "Billing",
                    CreatedAt = DateTime.UtcNow.AddDays(-40),
                    UpdatedAt = DateTime.UtcNow.AddDays(-7)
                },

                // Event Invitations
                new CampaignTemplate
                {
                    Name = "UAE National Day Celebration",
                    Description = "Invitation to UAE National Day corporate event",
                    MessageContent = "🇦🇪 UAE National Day Celebration 🇦🇪\n\nYou're invited to our special celebration!\n\n🎉 Event: UAE National Day Gala\n📅 Date: December 2nd, 2024\n🕰️ Time: 7:00 PM onwards\n📍 Venue: {VENUE_NAME}, {VENUE_ADDRESS}\n👔 Dress Code: National colors encouraged\n\n🎁 Join us for dinner, entertainment, and networking.\n\nRSVP by {RSVP_DATE}: {RSVP_LINK}\n\nZayed's vision lives on! 🌟",
                    IsActive = true,
                    Category = "Events",
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                    UpdatedAt = DateTime.UtcNow.AddDays(-4)
                },

                // Customer Satisfaction
                new CampaignTemplate
                {
                    Name = "Feedback Request",
                    Description = "Request for customer feedback and review",
                    MessageContent = "How was your experience? ⭐\n\nHi {CUSTOMER_NAME},\n\nThank you for choosing {COMPANY_NAME}! We hope you loved our service.\n\n🤔 Would you mind sharing your feedback?\n⏰ It takes just 2 minutes\n🎁 Get 10% off your next order\n\n📝 Rate us here: {REVIEW_LINK}\n\nYour opinion helps us serve you better!\n\nBest regards,\n{COMPANY_NAME} Team",
                    IsActive = true,
                    Category = "Feedback",
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                },

                // Support Messages
                new CampaignTemplate
                {
                    Name = "Technical Support Follow-up",
                    Description = "Follow-up message after technical support ticket resolution",
                    MessageContent = "Support Ticket Resolved ✅\n\nHi {CUSTOMER_NAME},\n\nYour support ticket #{TICKET_NUMBER} has been resolved.\n\n🔧 Issue: {ISSUE_DESCRIPTION}\n✅ Resolution: {RESOLUTION_SUMMARY}\n👨‍💻 Handled by: {TECHNICIAN_NAME}\n\nIs everything working well now?\n\n😊 Satisfied? Rate our support: {RATING_LINK}\n🤔 Need more help? Reply to this message\n\nWe're always here to help!\n\n{COMPANY_NAME} Support Team",
                    IsActive = true,
                    Category = "Support",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },

                // Holiday Messages
                new CampaignTemplate
                {
                    Name = "Eid Mubarak Greeting",
                    Description = "Professional Eid greeting for customers and partners",
                    MessageContent = "🌙✨ Eid Mubarak! ✨🌙\n\nMay this blessed Eid bring joy, peace, and prosperity to you and your loved ones.\n\n🤲 Our prayers for your happiness\n🎁 Special Eid offers coming soon\n🕌 Celebrating community and togetherness\n\nThank you for being part of our {COMPANY_NAME} family.\n\nEid Mubarak from all of us!\n\n{COMPANY_NAME} Team 💚",
                    IsActive = true,
                    Category = "Holidays",
                    CreatedAt = DateTime.UtcNow.AddDays(-60),
                    UpdatedAt = DateTime.UtcNow.AddDays(-45)
                },

                // Business Updates
                new CampaignTemplate
                {
                    Name = "New Branch Opening",
                    Description = "Announcement for new branch or location opening",
                    MessageContent = "🎊 We're Expanding! 🎊\n\nExciting news! We're opening a new branch in {BRANCH_LOCATION}!\n\n📅 Grand Opening: {OPENING_DATE}\n📍 Address: {BRANCH_ADDRESS}\n🕐 Hours: {OPERATING_HOURS}\n📞 Phone: {BRANCH_PHONE}\n\n🎁 Grand opening specials:\n   • 20% off all services\n   • Free consultations\n   • Exclusive opening day gifts\n\n🚗 Free parking available\n🌟 Same great service, closer to you!\n\nSee you there!",
                    IsActive = true,
                    Category = "Business",
                    CreatedAt = DateTime.UtcNow.AddDays(-8),
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.CampaignTemplates.AddRangeAsync(templates);
            await context.SaveChangesAsync();

            Console.WriteLine($"✅ Seeded {templates.Count} campaign templates");

            // Display summary
            var totalTemplates = templates.Count;
            var activeTemplates = templates.Count(t => t.IsActive);

            Console.WriteLine("📊 Campaign Templates Summary:");
            Console.WriteLine($"   - Total Templates: {totalTemplates}");
            Console.WriteLine($"   - Active Templates: {activeTemplates}");
            Console.WriteLine($"   - Inactive Templates: {totalTemplates - activeTemplates}");

            Console.WriteLine("\n📧 Template Categories:");
            var categories = templates.GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(c => c.Count)
                .ToList();

            foreach (var category in categories)
            {
                Console.WriteLine($"   - {category.Category}: {category.Count} templates");
            }

            Console.WriteLine("\n🔤 Language Distribution:");
            var arabicTemplates = templates.Count(t => t.Name.Contains("عربي") || t.MessageContent.Contains("أهلاً"));
            var englishTemplates = templates.Count - arabicTemplates;
            Console.WriteLine($"   - English: {englishTemplates} templates");
            Console.WriteLine($"   - Arabic: {arabicTemplates} templates");
        }
    }
}