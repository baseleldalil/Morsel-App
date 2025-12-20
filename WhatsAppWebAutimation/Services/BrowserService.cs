using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace WhatsAppWebAutomation.Services;

/// <summary>
/// Browser management service supporting Chrome and Firefox
/// </summary>
public class BrowserService : IBrowserService, IDisposable
{
    private IWebDriver? _driver;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BrowserService> _logger;
    private readonly object _lock = new();
    private bool _disposed;
    private string _currentBrowserType = "Chrome";

    public BrowserService(IConfiguration configuration, ILogger<BrowserService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _currentBrowserType = _configuration["BrowserSettings:BrowserType"] ?? "Chrome";
    }

    /// <inheritdoc />
    public IWebDriver GetDriver()
    {
        lock (_lock)
        {
            if (_driver == null)
            {
                throw new InvalidOperationException("Browser is not initialized. Call InitializeBrowser() first.");
            }

            try
            {
                // Check if browser is still responsive
                _ = _driver.WindowHandles;
                return _driver;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Browser appears to be closed or unresponsive");
                _driver = null;
                throw new InvalidOperationException("Browser is closed or unresponsive. Call InitializeBrowser() to restart.");
            }
        }
    }

    /// <inheritdoc />
    public void InitializeBrowser(string? browserType = null)
    {
        lock (_lock)
        {
            if (_driver != null)
            {
                try
                {
                    _ = _driver.WindowHandles;
                    _logger.LogInformation("Browser is already open");
                    return;
                }
                catch
                {
                    _driver = null;
                }
            }

            // Use provided browser type, or fall back to configuration
            var selectedBrowser = browserType ?? _configuration["BrowserSettings:BrowserType"] ?? "Chrome";

            // Validate browser type
            if (!selectedBrowser.Equals("Chrome", StringComparison.OrdinalIgnoreCase) &&
                !selectedBrowser.Equals("Firefox", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid browser type: {selectedBrowser}. Supported types: Chrome, Firefox");
            }

            _currentBrowserType = selectedBrowser;
            var headless = bool.Parse(_configuration["BrowserSettings:Headless"] ?? "false");
            var userDataDir = _configuration["BrowserSettings:UserDataDirectory"] ?? "./BrowserProfile";

            _logger.LogInformation("Initializing {BrowserType} browser", _currentBrowserType);

            if (_currentBrowserType.Equals("Firefox", StringComparison.OrdinalIgnoreCase))
            {
                InitializeFirefox(headless, userDataDir);
            }
            else
            {
                InitializeChrome(headless, userDataDir);
            }

            ConfigureTimeouts();
            _driver!.Manage().Window.Maximize();

            _logger.LogInformation("{BrowserType} browser initialized successfully", _currentBrowserType);
        }
    }

    private void InitializeChrome(bool headless, string userDataDir)
    {
        try
        {
            new DriverManager().SetUpDriver(new ChromeConfig());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WebDriverManager failed to setup ChromeDriver, attempting to use system Chrome");
        }

        var options = new ChromeOptions();

        // Chrome profile to save login session
        var profilePath = Path.GetFullPath(userDataDir);
        if (!Directory.Exists(profilePath))
        {
            Directory.CreateDirectory(profilePath);
        }
        options.AddArgument($"--user-data-dir={profilePath}");

        if (headless)
        {
            options.AddArgument("--headless=new");
        }

        // Disable notifications
        options.AddArgument("--disable-notifications");

        // Disable automation flags (avoid detection)
        options.AddExcludedArgument("enable-automation");
        options.AddAdditionalOption("useAutomationExtension", false);

        // Additional options for stability
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--disable-blink-features=AutomationControlled");

        // Set user agent to appear more like regular browser
        options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        _driver = new ChromeDriver(options);

        // Execute script to remove webdriver property
        ((ChromeDriver)_driver).ExecuteScript("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
    }

    private void InitializeFirefox(bool headless, string userDataDir)
    {
        try
        {
            new DriverManager().SetUpDriver(new FirefoxConfig());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WebDriverManager failed to setup GeckoDriver, attempting to use system Firefox");
        }

        var options = new FirefoxOptions();

        // Firefox profile to save login session
        var profilePath = Path.GetFullPath(userDataDir + "_Firefox");
        if (!Directory.Exists(profilePath))
        {
            Directory.CreateDirectory(profilePath);
        }

        var profile = new FirefoxProfile(profilePath);

        // Disable notifications
        profile.SetPreference("dom.webnotifications.enabled", false);

        // Disable automation detection
        profile.SetPreference("dom.webdriver.enabled", false);
        profile.SetPreference("useAutomationExtension", false);

        options.Profile = profile;

        if (headless)
        {
            options.AddArgument("--headless");
        }

        _driver = new FirefoxDriver(options);
    }

    private void ConfigureTimeouts()
    {
        if (_driver == null) return;

        var implicitWait = int.Parse(_configuration["BrowserSettings:ImplicitWaitSeconds"] ?? "15");
        var pageLoad = int.Parse(_configuration["BrowserSettings:PageLoadTimeoutSeconds"] ?? "60");

        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(implicitWait);
        _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(pageLoad);
    }

    /// <inheritdoc />
    public void CloseBrowser()
    {
        lock (_lock)
        {
            if (_driver == null)
            {
                _logger.LogInformation("Browser is already closed");
                return;
            }

            try
            {
                _driver.Quit();
                _logger.LogInformation("Browser closed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing browser");
            }
            finally
            {
                _driver = null;
            }
        }
    }

    /// <inheritdoc />
    public bool IsBrowserOpen()
    {
        lock (_lock)
        {
            if (_driver == null) return false;

            try
            {
                _ = _driver.WindowHandles;
                return true;
            }
            catch
            {
                _driver = null;
                return false;
            }
        }
    }

    /// <inheritdoc />
    public string GetBrowserType()
    {
        return _currentBrowserType;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            CloseBrowser();
        }

        _disposed = true;
    }

    ~BrowserService()
    {
        Dispose(false);
    }
}
