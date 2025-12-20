using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

/// <summary>
/// Advanced timing settings with true randomization and decimal delays
/// </summary>
public partial class AdvancedTimingSetting
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    /// <summary>
    /// Minimum delay between messages in seconds (supports decimals, e.g., 30.5)
    /// </summary>
    public decimal MinDelaySeconds { get; set; }

    /// <summary>
    /// Maximum delay between messages in seconds (supports decimals, e.g., 60.8)
    /// </summary>
    public decimal MaxDelaySeconds { get; set; }

    /// <summary>
    /// Enable random breaks after sending multiple messages
    /// </summary>
    public bool EnableRandomBreaks { get; set; }

    /// <summary>
    /// Minimum number of messages before triggering a break (e.g., 13)
    /// </summary>
    public int MinMessagesBeforeBreak { get; set; }

    /// <summary>
    /// Maximum number of messages before triggering a break (e.g., 20)
    /// </summary>
    public int MaxMessagesBeforeBreak { get; set; }

    /// <summary>
    /// Minimum break duration in minutes (supports decimals, e.g., 4.2)
    /// </summary>
    public decimal MinBreakMinutes { get; set; }

    /// <summary>
    /// Maximum break duration in minutes (supports decimals, e.g., 9.7)
    /// </summary>
    public decimal MaxBreakMinutes { get; set; }

    /// <summary>
    /// Use strong randomization with decimal values (true: 32.7s, false: 32s)
    /// </summary>
    public bool UseDecimalRandomization { get; set; }

    /// <summary>
    /// Number of decimal places for randomization (1-3): 1=32.7s, 2=32.73s, 3=32.735s
    /// </summary>
    public int DecimalPrecision { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
