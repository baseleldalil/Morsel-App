using WhatsApp.Shared.Models;

namespace WhatsAppAdmin.Services
{
    /// <summary>
    /// Interface for timing control business logic operations
    /// Manages message timing, random delays, and video-specific timing rules
    /// </summary>
    public interface ITimingControlService
    {
        // Message Timing Control
        Task<IEnumerable<MessageTimingControl>> GetAllMessageTimingControlsAsync();
        Task<MessageTimingControl?> GetMessageTimingControlByIdAsync(int id);
        Task<MessageTimingControl> CreateMessageTimingControlAsync(MessageTimingControl control);
        Task<MessageTimingControl> UpdateMessageTimingControlAsync(MessageTimingControl control);
        Task<bool> DeleteMessageTimingControlAsync(int id);
        Task<MessageTimingControl?> GetActiveMessageTimingControlAsync(int? subscriptionId = null);

        // Random Delay Rules
        Task<IEnumerable<RandomDelayRule>> GetAllRandomDelayRulesAsync();
        Task<RandomDelayRule?> GetRandomDelayRuleByIdAsync(int id);
        Task<RandomDelayRule> CreateRandomDelayRuleAsync(RandomDelayRule rule);
        Task<RandomDelayRule> UpdateRandomDelayRuleAsync(RandomDelayRule rule);
        Task<bool> DeleteRandomDelayRuleAsync(int id);
        Task<IEnumerable<RandomDelayRule>> GetActiveRandomDelayRulesAsync(int? subscriptionId = null);

        // Video Timing Control
        Task<IEnumerable<VideoTimingControl>> GetAllVideoTimingControlsAsync();
        Task<VideoTimingControl?> GetVideoTimingControlByIdAsync(int id);
        Task<VideoTimingControl> CreateVideoTimingControlAsync(VideoTimingControl control);
        Task<VideoTimingControl> UpdateVideoTimingControlAsync(VideoTimingControl control);
        Task<bool> DeleteVideoTimingControlAsync(int id);
        Task<VideoTimingControl?> GetActiveVideoTimingControlAsync(int? subscriptionId = null);

        // Helper methods for applying timing logic
        Task<int> CalculateMessageDelayAsync(int? subscriptionId = null);
        Task<RandomDelayRule?> GetApplicableDelayRuleAsync(int messageCount, int? subscriptionId = null);
        Task<(int beforeUpload, int upload, int afterUpload)> CalculateVideoTimingAsync(int? subscriptionId = null);
    }
}
