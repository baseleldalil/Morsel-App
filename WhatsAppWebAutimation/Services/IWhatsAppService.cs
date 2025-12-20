using WhatsAppWebAutomation.DTOs;

namespace WhatsAppWebAutomation.Services;

/// <summary>
/// Interface for WhatsApp Web automation service
/// </summary>
public interface IWhatsAppService
{
    /// <summary>
    /// Get current WhatsApp status (browser open, logged in)
    /// </summary>
    Task<StatusResultDto> GetStatusAsync();

    /// <summary>
    /// Initialize browser and navigate to WhatsApp Web
    /// </summary>
    /// <param name="browserType">Browser type: Chrome or Firefox (default: Chrome)</param>
    Task<StatusResultDto> InitializeAsync(string? browserType = null);

    /// <summary>
    /// Send message with optional attachments to a single contact
    /// </summary>
    Task<SendResultDto> SendMessageAsync(SendMessageRequest request);

    /// <summary>
    /// Send messages to multiple contacts with delays and breaks
    /// </summary>
    Task<BulkResultDto> SendBulkAsync(SendBulkRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start bulk send operation (runs in background)
    /// </summary>
    Task<BulkControlResponse> StartBulkAsync(SendBulkRequest request);

    /// <summary>
    /// Get current bulk operation status
    /// </summary>
    BulkControlResponse GetBulkStatus();

    /// <summary>
    /// Pause current bulk operation
    /// </summary>
    BulkControlResponse PauseBulk();

    /// <summary>
    /// Resume paused bulk operation
    /// </summary>
    BulkControlResponse ResumeBulk();

    /// <summary>
    /// Stop current bulk operation
    /// </summary>
    BulkControlResponse StopBulk();

    /// <summary>
    /// Close browser and cleanup resources
    /// </summary>
    Task CloseAsync();
}
