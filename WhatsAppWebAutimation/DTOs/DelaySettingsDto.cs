using System.ComponentModel.DataAnnotations;

namespace WhatsAppWebAutomation.DTOs;

/// <summary>
/// Settings for random delay between messages
/// </summary>
public class DelaySettingsDto
{
    /// <summary>
    /// Minimum delay in seconds between messages (default: 30)
    /// </summary>
    [Range(1, 3600, ErrorMessage = "MinDelaySeconds must be between 1 and 3600")]
    public int MinDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Maximum delay in seconds between messages (default: 60)
    /// Each message gets a DIFFERENT random value between min and max
    /// </summary>
    [Range(1, 3600, ErrorMessage = "MaxDelaySeconds must be between 1 and 3600")]
    public int MaxDelaySeconds { get; set; } = 60;
}
