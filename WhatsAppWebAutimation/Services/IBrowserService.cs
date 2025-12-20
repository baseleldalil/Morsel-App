using OpenQA.Selenium;

namespace WhatsAppWebAutomation.Services;

/// <summary>
/// Interface for browser management service
/// </summary>
public interface IBrowserService
{
    /// <summary>
    /// Get the current WebDriver instance
    /// </summary>
    /// <returns>Current WebDriver or throws if not initialized</returns>
    IWebDriver GetDriver();

    /// <summary>
    /// Initialize and start the browser with specified type
    /// </summary>
    /// <param name="browserType">Browser type: Chrome or Firefox (default: Chrome)</param>
    void InitializeBrowser(string? browserType = null);

    /// <summary>
    /// Close the browser and clean up resources
    /// </summary>
    void CloseBrowser();

    /// <summary>
    /// Check if browser is currently open
    /// </summary>
    bool IsBrowserOpen();

    /// <summary>
    /// Get the configured browser type (Chrome/Firefox)
    /// </summary>
    string GetBrowserType();
}
