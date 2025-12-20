using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management;

namespace WhatsAppSender.API.Services
{
    public interface IBrowserSessionManager
    {
        Task<IWebDriver> GetOrCreateBrowserAsync(string userEmail, BrowserSettings settings);
        void CloseBrowser(string userEmail);
        void CloseAllBrowsers();
    }

    public class BrowserSettings
    {
        public BrowserType Type { get; set; } = BrowserType.Chrome;
        public string? ProfilePath { get; set; }
        public bool KeepSessionOpen { get; set; } = true;
    }

    public enum BrowserType
    {
        Chrome,
        Firefox
    }

    public class BrowserSessionManager : IBrowserSessionManager, IDisposable
    {
        private readonly ILogger<BrowserSessionManager> _logger;
        private readonly ConcurrentDictionary<string, IWebDriver> _activeSessions = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public BrowserSessionManager(ILogger<BrowserSessionManager> logger)
        {
            _logger = logger;
        }

        public async Task<IWebDriver> GetOrCreateBrowserAsync(string userEmail, BrowserSettings settings)
        {
            await _semaphore.WaitAsync();
            try
            {
                // Check if session exists and is still valid
                if (_activeSessions.TryGetValue(userEmail, out var existingDriver))
                {
                    try
                    {
                        // Test if browser is still responsive
                        _ = existingDriver.Title;
                        _logger.LogInformation("Reusing existing browser session for user: {UserEmail}", userEmail);
                        return existingDriver;
                    }
                    catch
                    {
                        // Browser is dead, remove it and create new one
                        _logger.LogWarning("Existing browser session for {UserEmail} is no longer valid. Creating new session.", userEmail);
                        _activeSessions.TryRemove(userEmail, out _);
                        try { existingDriver.Quit(); } catch { }
                    }
                }

                // Create new browser session
                _logger.LogInformation("Creating new browser session for user: {UserEmail} using {BrowserType}",
                    userEmail, settings.Type);

                IWebDriver driver = settings.Type switch
                {
                    BrowserType.Chrome => CreateChromeDriver(userEmail, settings),
                    BrowserType.Firefox => CreateFirefoxDriver(userEmail, settings),
                    _ => throw new NotSupportedException($"Browser type {settings.Type} is not supported")
                };

                if (settings.KeepSessionOpen)
                {
                    _activeSessions[userEmail] = driver;
                }

                return driver;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private IWebDriver CreateChromeDriver(string userEmail, BrowserSettings settings)
        {
            var options = new ChromeOptions();

            // Use custom profile path if provided, otherwise use default
            string userDataDir = !string.IsNullOrEmpty(settings.ProfilePath)
                ? settings.ProfilePath
                : Path.Combine(Environment.CurrentDirectory, "ChromeWhatsAppProfiles", userEmail);

            Directory.CreateDirectory(userDataDir);

            options.AddArgument($"--user-data-dir={userDataDir}");
            options.AddArgument("--start-maximized");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);

            _logger.LogDebug("Chrome profile path: {ProfilePath}", userDataDir);

            return new ChromeDriver(options);
        }

        private IWebDriver CreateFirefoxDriver(string userEmail, BrowserSettings settings)
        {
            var options = new FirefoxOptions();

            // Use custom profile path if provided, otherwise use default
            string profilePath = !string.IsNullOrEmpty(settings.ProfilePath)
                ? settings.ProfilePath
                : Path.Combine(Environment.CurrentDirectory, "FirefoxWhatsAppProfiles", userEmail);

            Directory.CreateDirectory(profilePath);

            // Firefox uses profile directory differently than Chrome
            var profileManager = new FirefoxProfileManager();
            var profile = new FirefoxProfile(profilePath);
            options.Profile = profile;

            options.AddArgument("--width=1920");
            options.AddArgument("--height=1080");

            _logger.LogDebug("Firefox profile path: {ProfilePath}", profilePath);

            return new FirefoxDriver(options);
        }

        public void CloseBrowser(string userEmail)
        {
            bool sessionFound = false;

            if (_activeSessions.TryRemove(userEmail, out var driver))
            {
                sessionFound = true;
                try
                {
                    // First try graceful quit
                    driver.Quit();
                    _logger.LogInformation("Closed browser session for user: {UserEmail}", userEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing browser gracefully for user: {UserEmail}", userEmail);
                }
                finally
                {
                    // Try to dispose the driver
                    try
                    {
                        driver.Dispose();
                    }
                    catch { }
                }
            }

            // Always try to kill Chrome/ChromeDriver processes as a fallback
            // This ensures browser closes even if session wasn't tracked
            try
            {
                KillChromeProcesses(userEmail);
                if (!sessionFound)
                {
                    _logger.LogWarning("No active session found for user {UserEmail}, but attempted to kill Chrome processes", userEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error killing Chrome processes for user: {UserEmail}", userEmail);
            }
        }

        private void KillChromeProcesses(string userEmail)
        {
            try
            {
                var profilePath = Path.Combine(Environment.CurrentDirectory, "ChromeWhatsAppProfiles", userEmail);
                _logger.LogInformation("Attempting to kill Chrome processes using profile: {ProfilePath}", profilePath);

                // Find and kill Chrome processes using this profile
                var chromeProcesses = System.Diagnostics.Process.GetProcessesByName("chrome");
                var chromedriverProcesses = System.Diagnostics.Process.GetProcessesByName("chromedriver");

                int killedCount = 0;
                int totalProcesses = chromeProcesses.Length + chromedriverProcesses.Length;

                _logger.LogInformation("Found {ChromeCount} chrome.exe and {DriverCount} chromedriver.exe processes",
                    chromeProcesses.Length, chromedriverProcesses.Length);

                foreach (var process in chromeProcesses.Concat(chromedriverProcesses))
                {
                    try
                    {
                        // Check if process already exited
                        if (process.HasExited)
                        {
                            _logger.LogDebug("Process {ProcessId} already exited", process.Id);
                            process.Dispose();
                            continue;
                        }

                        // Check if process command line contains our profile path
                        string cmdLine = GetProcessCommandLine(process);

                        if (!string.IsNullOrEmpty(cmdLine))
                        {
                            _logger.LogDebug("Process {ProcessId} command line: {CmdLine}", process.Id, cmdLine);

                            // ONLY kill if this process is using our specific profile
                            if (cmdLine.Contains(profilePath, StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogInformation("Killing Chrome process {ProcessId} (matches profile path: {UserEmail})", process.Id, userEmail);
                                process.Kill(entireProcessTree: true);

                                if (process.WaitForExit(3000))
                                {
                                    killedCount++;
                                    _logger.LogInformation("✓ Successfully killed process {ProcessId}", process.Id);
                                }
                                else
                                {
                                    _logger.LogWarning("⚠️ Process {ProcessId} did not exit within 3 seconds", process.Id);
                                }
                            }
                            else
                            {
                                _logger.LogDebug("Process {ProcessId} does NOT match profile path, skipping (belongs to different user or non-Selenium Chrome)", process.Id);
                            }
                        }
                        else
                        {
                            // Can't get command line - skip this process (don't kill unknown Chrome instances)
                            _logger.LogDebug("Could not get command line for process {ProcessId}, skipping to avoid killing other users' Chrome", process.Id);
                        }
                    }
                    catch (System.ComponentModel.Win32Exception win32Ex)
                    {
                        _logger.LogWarning("Win32Exception killing process {ProcessId}: {Message} (Error: {ErrorCode})",
                            process.Id, win32Ex.Message, win32Ex.NativeErrorCode);
                    }
                    catch (InvalidOperationException)
                    {
                        _logger.LogDebug("Process {ProcessId} already exited during kill attempt", process.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Could not kill process {ProcessId}: {ExceptionType}", process.Id, ex.GetType().Name);
                    }
                    finally
                    {
                        try { process.Dispose(); } catch { }
                    }
                }

                if (killedCount > 0)
                {
                    _logger.LogInformation("✅ Forcefully killed {KilledCount} out of {TotalCount} Chrome/ChromeDriver processes for user {UserEmail}",
                        killedCount, totalProcesses, userEmail);
                }
                else if (totalProcesses > 0)
                {
                    _logger.LogWarning("⚠️ Found {TotalCount} Chrome processes but killed 0 - they may not be using the expected profile",
                        totalProcesses);
                }
                else
                {
                    _logger.LogInformation("No Chrome processes found to kill for user {UserEmail}", userEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in KillChromeProcesses for user: {UserEmail}", userEmail);
            }
        }

        private string GetProcessCommandLine(System.Diagnostics.Process process)
        {
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");

                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    return obj["CommandLine"]?.ToString() ?? string.Empty;
                }
            }
            catch
            {
                // On Linux/Mac or if WMI fails, we can't get command line
            }

            return string.Empty;
        }

        public void CloseAllBrowsers()
        {
            var users = _activeSessions.Keys.ToList();

            foreach (var kvp in _activeSessions)
            {
                try
                {
                    kvp.Value.Quit();
                    kvp.Value.Dispose();
                    _logger.LogInformation("Closed browser session for user: {UserEmail}", kvp.Key);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing browser for user: {UserEmail}", kvp.Key);
                }
            }

            _activeSessions.Clear();

            // Kill any remaining Chrome processes for all users
            foreach (var userEmail in users)
            {
                try
                {
                    KillChromeProcesses(userEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error killing Chrome processes for user: {UserEmail}", userEmail);
                }
            }
        }

        public void Dispose()
        {
            CloseAllBrowsers();
            _semaphore.Dispose();
        }
    }
}
