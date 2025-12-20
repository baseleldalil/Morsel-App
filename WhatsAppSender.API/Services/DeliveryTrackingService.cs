using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using WhatsAppSender.API.Models;

namespace WhatsAppSender.API.Services
{
    public interface IDeliveryTrackingService
    {
        Task<string> CheckDeliveryStatusAsync(IWebDriver driver, string phone);
        Task<bool> WaitForSentConfirmationAsync(IWebDriver driver, int timeoutSeconds = 15);
    }

    public class DeliveryTrackingService : IDeliveryTrackingService
    {
        private readonly ILogger<DeliveryTrackingService> _logger;

        public DeliveryTrackingService(ILogger<DeliveryTrackingService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Check delivery status by looking for WhatsApp delivery indicators
        /// Returns: "Sent", "Delivered", "Read", or "Pending"
        /// </summary>
        public async Task<string> CheckDeliveryStatusAsync(IWebDriver driver, string phone)
        {
            try
            {
                // WhatsApp delivery indicators:
                // Single checkmark (✓) = Sent to server
                // Double checkmark (✓✓) = Delivered to recipient
                // Blue double checkmark = Read by recipient

                await Task.Delay(2000); // Give WhatsApp time to update status

                // Look for delivery status indicators
                var deliveryIndicators = new[]
                {
                    "span[data-icon='msg-dblcheck']",           // Double checkmark (delivered)
                    "span[data-icon='msg-dblcheck-blue']",      // Blue double checkmark (read)
                    "span[data-icon='msg-check']",              // Single checkmark (sent)
                    "span[data-icon='status-dblcheck']",        // Alternative double check
                    "span[data-icon='status-check']"            // Alternative single check
                };

                foreach (var selector in deliveryIndicators)
                {
                    try
                    {
                        var elements = driver.FindElements(By.CssSelector(selector));
                        if (elements.Any())
                        {
                            var lastElement = elements.Last(); // Check the most recent message

                            // Determine status based on icon type
                            if (selector.Contains("dblcheck-blue"))
                            {
                                _logger.LogDebug("Message to {Phone} was READ", phone);
                                return "Read";
                            }
                            else if (selector.Contains("dblcheck"))
                            {
                                _logger.LogDebug("Message to {Phone} was DELIVERED", phone);
                                return "Delivered";
                            }
                            else if (selector.Contains("check"))
                            {
                                _logger.LogDebug("Message to {Phone} was SENT", phone);
                                return "Sent";
                            }
                        }
                    }
                    catch
                    {
                        // Continue checking other selectors
                    }
                }

                // If no delivery indicator found, message is still pending
                _logger.LogWarning("No delivery indicator found for {Phone}, status is PENDING", phone);
                return "Pending";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking delivery status for {Phone}", phone);
                return "Pending";
            }
        }

        /// <summary>
        /// Wait for sent confirmation (at least single checkmark appears)
        /// </summary>
        public async Task<bool> WaitForSentConfirmationAsync(IWebDriver driver, int timeoutSeconds = 15)
        {
            try
            {
                var checkmarkSelectors = new[]
                {
                    "span[data-icon='msg-dblcheck']",
                    "span[data-icon='msg-dblcheck-blue']",
                    "span[data-icon='msg-check']",
                    "span[data-icon='status-dblcheck']",
                    "span[data-icon='status-check']"
                };

                var endTime = DateTime.Now.AddSeconds(timeoutSeconds);

                while (DateTime.Now < endTime)
                {
                    foreach (var selector in checkmarkSelectors)
                    {
                        try
                        {
                            var elements = driver.FindElements(By.CssSelector(selector));
                            if (elements.Any())
                            {
                                _logger.LogDebug("Sent confirmation detected");
                                return true; // Message was sent
                            }
                        }
                        catch { }
                    }

                    await Task.Delay(500); // Check every 500ms
                }

                _logger.LogWarning("Sent confirmation timeout after {Timeout} seconds", timeoutSeconds);
                return false; // Timeout without confirmation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error waiting for sent confirmation");
                return false;
            }
        }
    }
}
