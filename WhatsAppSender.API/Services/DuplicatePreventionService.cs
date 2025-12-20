using Microsoft.EntityFrameworkCore;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;

namespace WhatsAppSender.API.Services
{
    public interface IDuplicatePreventionService
    {
        Task<bool> HasBeenSentAsync(int userId, string phoneNumber);
        Task MarkAsSentAsync(int userId, string phoneNumber, int? campaignId = null, string? messageContent = null);
        Task<List<string>> GetSentPhonesAsync(int userId);
        Task ClearSentPhonesAsync(int userId);
        Task<int> GetSentCountAsync(int userId);
    }

    public class DuplicatePreventionService : IDuplicatePreventionService
    {
        private readonly SaaSDbContext _context;
        private readonly ILogger<DuplicatePreventionService> _logger;

        public DuplicatePreventionService(SaaSDbContext context, ILogger<DuplicatePreventionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Checks if a phone number has been sent to by this user before
        /// </summary>
        public async Task<bool> HasBeenSentAsync(int userId, string phoneNumber)
        {
            try
            {
                return await _context.SentPhones
                    .AnyAsync(sp => sp.UserId == userId && sp.PhoneNumber == phoneNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if phone {Phone} was sent by user {UserId}", phoneNumber, userId);
                return false; // On error, don't block sending
            }
        }

        /// <summary>
        /// Marks a phone number as sent for duplicate prevention
        /// </summary>
        public async Task MarkAsSentAsync(int userId, string phoneNumber, int? campaignId = null, string? messageContent = null)
        {
            try
            {
                // Check if already exists
                var existing = await _context.SentPhones
                    .FirstOrDefaultAsync(sp => sp.UserId == userId && sp.PhoneNumber == phoneNumber);

                if (existing != null)
                {
                    // Update existing record
                    existing.SentAt = DateTime.UtcNow;
                    existing.CampaignId = campaignId;
                    existing.MessageContent = messageContent;
                    existing.Status = "sent";
                    _logger.LogInformation("Updated existing sent phone record for {Phone} by user {UserId}", phoneNumber, userId);
                }
                else
                {
                    // Create new record
                    var sentPhone = new SentPhone
                    {
                        UserId = userId,
                        PhoneNumber = phoneNumber,
                        CampaignId = campaignId,
                        MessageContent = messageContent,
                        SentAt = DateTime.UtcNow,
                        Status = "sent"
                    };

                    _context.SentPhones.Add(sentPhone);
                    _logger.LogInformation("Created new sent phone record for {Phone} by user {UserId}", phoneNumber, userId);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking phone {Phone} as sent for user {UserId}", phoneNumber, userId);
                // Don't throw - duplicate prevention is not critical enough to stop sending
            }
        }

        /// <summary>
        /// Gets all sent phone numbers for a user
        /// </summary>
        public async Task<List<string>> GetSentPhonesAsync(int userId)
        {
            try
            {
                return await _context.SentPhones
                    .Where(sp => sp.UserId == userId)
                    .Select(sp => sp.PhoneNumber)
                    .Distinct()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sent phones for user {UserId}", userId);
                return new List<string>();
            }
        }

        /// <summary>
        /// Clears all sent phone records for a user (for in-memory mode reset)
        /// </summary>
        public async Task ClearSentPhonesAsync(int userId)
        {
            try
            {
                var phones = await _context.SentPhones
                    .Where(sp => sp.UserId == userId)
                    .ToListAsync();

                _context.SentPhones.RemoveRange(phones);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleared {Count} sent phone records for user {UserId}", phones.Count, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing sent phones for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Gets the count of sent phones for a user
        /// </summary>
        public async Task<int> GetSentCountAsync(int userId)
        {
            try
            {
                return await _context.SentPhones
                    .Where(sp => sp.UserId == userId)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sent count for user {UserId}", userId);
                return 0;
            }
        }
    }
}
