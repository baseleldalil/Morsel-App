using System.ComponentModel.DataAnnotations;

namespace WhatsAppWebAutomation.DTOs;

/// <summary>
/// Attachment data for sending files via WhatsApp
/// </summary>
public class AttachmentDto
{
    /// <summary>
    /// Base64 encoded file content (without data:image/png;base64, prefix)
    /// </summary>
    [Required(ErrorMessage = "Base64 content is required")]
    public string Base64 { get; set; } = string.Empty;

    /// <summary>
    /// File name with extension (e.g., "photo.png", "document.pdf", "video.mp4")
    /// </summary>
    [Required(ErrorMessage = "File name is required")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Type of media: "image", "video", "document"
    /// </summary>
    [Required(ErrorMessage = "Media type is required")]
    [RegularExpression("^(image|video|document)$", ErrorMessage = "MediaType must be 'image', 'video', or 'document'")]
    public string MediaType { get; set; } = "image";

    /// <summary>
    /// Optional caption for images and videos
    /// </summary>
    public string? Caption { get; set; }
}
