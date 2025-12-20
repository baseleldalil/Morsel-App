using Microsoft.EntityFrameworkCore;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;

namespace WhatsAppAdmin.Services
{
    /// <summary>
    /// Service for managing timing control operations
    /// Implements business logic for message timing, random delays, and video timing
    /// </summary>
    public class TimingControlService : ITimingControlService
    {
        private readonly SaaSDbContext _context;
        private readonly Random _random = new Random();

        public TimingControlService(SaaSDbContext context)
        {
            _context = context;
        }

        #region Message Timing Control

        public async Task<IEnumerable<MessageTimingControl>> GetAllMessageTimingControlsAsync()
        {
            return await _context.MessageTimingControls
                .Include(m => m.SubscriptionPlan)
                .OrderByDescending(m => m.IsActive)
                .ThenByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<MessageTimingControl?> GetMessageTimingControlByIdAsync(int id)
        {
            return await _context.MessageTimingControls
                .Include(m => m.SubscriptionPlan)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<MessageTimingControl> CreateMessageTimingControlAsync(MessageTimingControl control)
        {
            control.CreatedAt = DateTime.UtcNow;
            control.UpdatedAt = DateTime.UtcNow;

            _context.MessageTimingControls.Add(control);
            await _context.SaveChangesAsync();

            return control;
        }

        public async Task<MessageTimingControl> UpdateMessageTimingControlAsync(MessageTimingControl control)
        {
            control.UpdatedAt = DateTime.UtcNow;

            _context.MessageTimingControls.Update(control);
            await _context.SaveChangesAsync();

            return control;
        }

        public async Task<bool> DeleteMessageTimingControlAsync(int id)
        {
            var control = await _context.MessageTimingControls.FindAsync(id);
            if (control == null)
                return false;

            _context.MessageTimingControls.Remove(control);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<MessageTimingControl?> GetActiveMessageTimingControlAsync(int? subscriptionId = null)
        {
            // Priority: subscription-specific > global default
            var query = _context.MessageTimingControls.Where(m => m.IsActive);

            if (subscriptionId.HasValue)
            {
                var subscriptionControl = await query
                    .FirstOrDefaultAsync(m => m.SubscriptionPlanId == subscriptionId);

                if (subscriptionControl != null)
                    return subscriptionControl;
            }

            // Fallback to global default
            return await query
                .FirstOrDefaultAsync(m => m.SubscriptionPlanId == null);
        }

        #endregion

        #region Random Delay Rules

        public async Task<IEnumerable<RandomDelayRule>> GetAllRandomDelayRulesAsync()
        {
            return await _context.RandomDelayRules
                .Include(r => r.SubscriptionPlan)
                .OrderByDescending(r => r.IsActive)
                .ThenBy(r => r.Priority)
                .ThenBy(r => r.AfterMessageCount)
                .ToListAsync();
        }

        public async Task<RandomDelayRule?> GetRandomDelayRuleByIdAsync(int id)
        {
            return await _context.RandomDelayRules
                .Include(r => r.SubscriptionPlan)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<RandomDelayRule> CreateRandomDelayRuleAsync(RandomDelayRule rule)
        {
            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;

            _context.RandomDelayRules.Add(rule);
            await _context.SaveChangesAsync();

            return rule;
        }

        public async Task<RandomDelayRule> UpdateRandomDelayRuleAsync(RandomDelayRule rule)
        {
            rule.UpdatedAt = DateTime.UtcNow;

            _context.RandomDelayRules.Update(rule);
            await _context.SaveChangesAsync();

            return rule;
        }

        public async Task<bool> DeleteRandomDelayRuleAsync(int id)
        {
            var rule = await _context.RandomDelayRules.FindAsync(id);
            if (rule == null)
                return false;

            _context.RandomDelayRules.Remove(rule);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<RandomDelayRule>> GetActiveRandomDelayRulesAsync(int? subscriptionId = null)
        {
            var query = _context.RandomDelayRules.Where(r => r.IsActive);

            if (subscriptionId.HasValue)
            {
                // Get subscription-specific rules
                var subscriptionRules = await query
                    .Where(r => r.SubscriptionPlanId == subscriptionId)
                    .OrderBy(r => r.Priority)
                    .ToListAsync();

                if (subscriptionRules.Any())
                    return subscriptionRules;
            }

            // Fallback to global rules
            return await query
                .Where(r => r.SubscriptionPlanId == null)
                .OrderBy(r => r.Priority)
                .ToListAsync();
        }

        #endregion

        #region Video Timing Control

        public async Task<IEnumerable<VideoTimingControl>> GetAllVideoTimingControlsAsync()
        {
            return await _context.VideoTimingControls
                .Include(v => v.SubscriptionPlan)
                .OrderByDescending(v => v.IsActive)
                .ThenByDescending(v => v.CreatedAt)
                .ToListAsync();
        }

        public async Task<VideoTimingControl?> GetVideoTimingControlByIdAsync(int id)
        {
            return await _context.VideoTimingControls
                .Include(v => v.SubscriptionPlan)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<VideoTimingControl> CreateVideoTimingControlAsync(VideoTimingControl control)
        {
            control.CreatedAt = DateTime.UtcNow;
            control.UpdatedAt = DateTime.UtcNow;

            _context.VideoTimingControls.Add(control);
            await _context.SaveChangesAsync();

            return control;
        }

        public async Task<VideoTimingControl> UpdateVideoTimingControlAsync(VideoTimingControl control)
        {
            control.UpdatedAt = DateTime.UtcNow;

            _context.VideoTimingControls.Update(control);
            await _context.SaveChangesAsync();

            return control;
        }

        public async Task<bool> DeleteVideoTimingControlAsync(int id)
        {
            var control = await _context.VideoTimingControls.FindAsync(id);
            if (control == null)
                return false;

            _context.VideoTimingControls.Remove(control);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<VideoTimingControl?> GetActiveVideoTimingControlAsync(int? subscriptionId = null)
        {
            var query = _context.VideoTimingControls.Where(v => v.IsActive);

            if (subscriptionId.HasValue)
            {
                var subscriptionControl = await query
                    .FirstOrDefaultAsync(v => v.SubscriptionPlanId == subscriptionId);

                if (subscriptionControl != null)
                    return subscriptionControl;
            }

            // Fallback to global default
            return await query
                .FirstOrDefaultAsync(v => v.SubscriptionPlanId == null);
        }

        #endregion

        #region Helper Methods

        public async Task<int> CalculateMessageDelayAsync(int? subscriptionId = null)
        {
            var control = await GetActiveMessageTimingControlAsync(subscriptionId);

            if (control == null)
                return 2; // Default 2 seconds if no control found

            // Generate random delay between min and max
            return _random.Next(control.MinDelaySeconds, control.MaxDelaySeconds + 1);
        }

        public async Task<RandomDelayRule?> GetApplicableDelayRuleAsync(int messageCount, int? subscriptionId = null)
        {
            var rules = await GetActiveRandomDelayRulesAsync(subscriptionId);

            // Find the first rule that matches the message count
            return rules.FirstOrDefault(r => messageCount >= r.AfterMessageCount &&
                                            messageCount % r.AfterMessageCount == 0);
        }

        public async Task<(int beforeUpload, int upload, int afterUpload)> CalculateVideoTimingAsync(int? subscriptionId = null)
        {
            var control = await GetActiveVideoTimingControlAsync(subscriptionId);

            if (control == null)
            {
                // Default values if no control found
                return (beforeUpload: 5, upload: 15, afterUpload: 5);
            }

            var beforeUpload = _random.Next(control.MinDelayBeforeUploadSeconds, control.MaxDelayBeforeUploadSeconds + 1);
            var upload = _random.Next(control.MinUploadTimeSeconds, control.MaxUploadTimeSeconds + 1);
            var afterUpload = _random.Next(control.MinDelayAfterUploadSeconds, control.MaxDelayAfterUploadSeconds + 1);

            return (beforeUpload, upload, afterUpload);
        }

        #endregion
    }
}
