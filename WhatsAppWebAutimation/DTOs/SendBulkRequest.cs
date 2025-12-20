using System.ComponentModel.DataAnnotations;

namespace WhatsAppWebAutomation.DTOs;

/// <summary>
/// Request model for sending messages to multiple contacts
/// </summary>
public class SendBulkRequest
{
    /// <summary>
    /// List of contacts to send messages to
    /// </summary>
    [Required(ErrorMessage = "Contacts list is required")]
    [MinLength(1, ErrorMessage = "At least one contact is required")]
    public List<ContactDto> Contacts { get; set; } = new();

    /// <summary>
    /// Message template with placeholders: {{name}}, {{phone}}, {{firstName}}
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Message template for male contacts (used when gender-based messages are needed)
    /// </summary>
    public string? MaleMessage { get; set; }

    /// <summary>
    /// Message template for female contacts (used when gender-based messages are needed)
    /// </summary>
    public string? FemaleMessage { get; set; }

    /// <summary>
    /// Attachments to send to each contact (base64 encoded)
    /// </summary>
    public List<AttachmentDto>? Attachments { get; set; }

    /// <summary>
    /// Random delay between each contact
    /// </summary>
    public DelaySettingsDto? DelaySettings { get; set; }

    /// <summary>
    /// Break settings - pause after X messages
    /// </summary>
    public BreakSettingsDto? BreakSettings { get; set; }
}

/// <summary>
/// Contact information for bulk messaging
/// </summary>
public class ContactDto
{
    /// <summary>
    /// Contact ID from database (used for status updates)
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// Phone number with country code
    /// </summary>
    [Required(ErrorMessage = "Phone number is required")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Contact name (used for message personalization)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Arabic name for personalization
    /// </summary>
    public string? ArabicName { get; set; }

    /// <summary>
    /// English name for personalization
    /// </summary>
    public string? EnglishName { get; set; }

    /// <summary>
    /// Gender: M (Male), F (Female), U (Unknown)
    /// </summary>
    public string? Gender { get; set; }
}
