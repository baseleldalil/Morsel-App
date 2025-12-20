using System.ComponentModel.DataAnnotations;

namespace WhatsAppWebAutomation.DTOs;

/// <summary>
/// Settings for taking breaks during bulk messaging
/// </summary>
public class BreakSettingsDto
{
    /// <summary>
    /// Enable/disable break feature
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Minimum number of messages before taking a break (randomized with MaxBreakAfterMessages)
    /// </summary>
    [Range(1, 1000, ErrorMessage = "MinBreakAfterMessages must be between 1 and 1000")]
    public int MinBreakAfterMessages { get; set; } = 8;

    /// <summary>
    /// Maximum number of messages before taking a break (randomized with MinBreakAfterMessages)
    /// Each break cycle gets a DIFFERENT random message count between min and max
    /// </summary>
    [Range(1, 1000, ErrorMessage = "MaxBreakAfterMessages must be between 1 and 1000")]
    public int MaxBreakAfterMessages { get; set; } = 15;

    /// <summary>
    /// Minimum break duration in minutes
    /// </summary>
    [Range(1, 120, ErrorMessage = "MinBreakMinutes must be between 1 and 120")]
    public int MinBreakMinutes { get; set; } = 5;

    /// <summary>
    /// Maximum break duration in minutes
    /// Each break gets a DIFFERENT random duration between min and max
    /// </summary>
    [Range(1, 120, ErrorMessage = "MaxBreakMinutes must be between 1 and 120")]
    public int MaxBreakMinutes { get; set; } = 15;

    /// <summary>
    /// Legacy property for backward compatibility - maps to MinBreakAfterMessages
    /// </summary>
    [Obsolete("Use MinBreakAfterMessages and MaxBreakAfterMessages instead")]
    public int BreakAfterMessages
    {
        get => MinBreakAfterMessages;
        set => MinBreakAfterMessages = value;
    }
}
