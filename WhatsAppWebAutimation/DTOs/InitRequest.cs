namespace WhatsAppWebAutomation.DTOs;

/// <summary>
/// Request DTO for initializing the browser
/// </summary>
public class InitRequest
{
    /// <summary>
    /// Browser type to use (Chrome or Firefox)
    /// Default is Chrome if not specified
    /// </summary>
    public string BrowserType { get; set; } = "Chrome";
}
