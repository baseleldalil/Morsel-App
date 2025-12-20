using System.Text.RegularExpressions;

namespace WhatsAppSender.API.Services
{
    public interface IPhoneExtractionService
    {
        (bool isValid, string? normalizedPhone, string? error) ExtractFirstValidPhone(string phoneRaw);
    }

    public class PhoneExtractionService : IPhoneExtractionService
    {
        /// <summary>
        /// Extracts the first valid phone number from a raw phone string that may contain multiple numbers.
        /// Supports splitting by '/' or '-' characters.
        /// Validates that the extracted number has at least 10 digits.
        /// </summary>
        /// <param name="phoneRaw">Raw phone string (e.g., "1234567890/0987654321" or "123-456-7890")</param>
        /// <returns>Tuple containing: validity flag, normalized phone (digits only), and error message if invalid</returns>
        public (bool isValid, string? normalizedPhone, string? error) ExtractFirstValidPhone(string phoneRaw)
        {
            if (string.IsNullOrWhiteSpace(phoneRaw))
            {
                return (false, null, "Phone number is empty");
            }

            // Split by / or - to handle multiple numbers
            var parts = phoneRaw.Split(new[] { '/', '-' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                return (false, null, "No phone number found");
            }

            // Get first number
            var firstNumber = parts[0].Trim();

            // Remove all non-digit characters (spaces, parentheses, etc.)
            var digitsOnly = Regex.Replace(firstNumber, @"\D", "");

            // Validate >= 10 digits
            if (digitsOnly.Length < 10)
            {
                return (false, null, $"Invalid phone (fewer than 10 digits): {digitsOnly.Length} digits found");
            }

            return (true, digitsOnly, null);
        }
    }
}
