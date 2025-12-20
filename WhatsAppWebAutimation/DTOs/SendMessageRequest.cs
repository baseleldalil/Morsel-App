using System.ComponentModel.DataAnnotations;

namespace WhatsAppWebAutomation.DTOs;

/// <summary>
/// Request model for sending a message to a single contact
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// Phone number with country code (e.g., "+1234567890" or "1234567890")
    /// </summary>
    [Required(ErrorMessage = "Phone number is required")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Text message to send (optional if attachments provided)
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// List of attachments to send (base64 encoded files)
    /// </summary>
    public List<AttachmentDto>? Attachments { get; set; }

    /// <summary>
    /// Custom delay settings (overrides default)
    /// </summary>
    public DelaySettingsDto? DelaySettings { get; set; }
}
