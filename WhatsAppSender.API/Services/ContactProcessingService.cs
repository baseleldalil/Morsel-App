using PhoneNumbers;
using System.Text.RegularExpressions;
using WhatsAppSender.API.Services;

namespace WhatsAppSender.API.Services
{
    public interface IContactProcessingService
    {
        Task<ProcessedContactData> ProcessContactDataAsync(string name, string phone);
    }

    public class ContactProcessingService : IContactProcessingService
    {
        private readonly ILogger<ContactProcessingService> _logger;

        public ContactProcessingService(
            ILogger<ContactProcessingService> logger)
        {
            _logger = logger;
        }

        public async Task<ProcessedContactData> ProcessContactDataAsync(string name, string phone)
        {
            var result = new ProcessedContactData();

            // Process phone number
            result.FormattedPhone = FormatPhoneNumber(phone);

            // Process name - stores name in appropriate column based on language detection
            if (!string.IsNullOrWhiteSpace(name))
            {
                // Store the full name (trimmed)
                result.FirstName = name.Trim();

                // Detect if name is Arabic or English and store in correct column ONLY
                if (ContainsArabic(name))
                {
                    // Arabic name - store ONLY in ArabicName column
                    result.ArabicName = name.Trim();
                    result.EnglishName = string.Empty; // Leave English empty
                    _logger.LogInformation("Detected Arabic name: {Name} -> stored in ArabicName column", name.Trim());
                }
                else
                {
                    // English name - store ONLY in EnglishName column
                    result.EnglishName = name.Trim();
                    result.ArabicName = string.Empty; // Leave Arabic empty
                    _logger.LogInformation("Detected English name: {Name} -> stored in EnglishName column", name.Trim());
                }

                // Determine gender based on FULL name
                result.Gender = DetermineGender(name.Trim(), result.ArabicName);
            }

            return await Task.FromResult(result);
        }

        private string FormatPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            // Clean input: remove spaces, hyphens, slashes, parentheses, dots, but keep "+"
            var cleaned = Regex.Replace(phone, @"[\s\-/\(\)\.]", "");

            // Check if this is an international number (starts with +)
            bool hasPlus = cleaned.StartsWith("+");

            // Remove all non-digit characters except leading +
            string digits;
            if (hasPlus)
            {
                // Keep the leading + and remove everything else except digits
                digits = "+" + Regex.Replace(cleaned.Substring(1), @"[^\d]", "");
            }
            else
            {
                // Remove all non-digits
                digits = Regex.Replace(cleaned, @"[^\d]", "");
            }

            // Pre-validation: Check if we have a reasonable number of digits
            var digitCount = digits.Count(char.IsDigit);
            if (digitCount < 7 || digitCount > 15)
            {
                _logger.LogWarning("Invalid phone number length: {Phone}, digit count: {Count}", phone, digitCount);
                return string.Empty; // Return empty for clearly invalid numbers
            }

            var phoneUtil = PhoneNumberUtil.GetInstance();

            try
            {
                PhoneNumber parsedNumber;

                // Try to parse the number intelligently
                if (hasPlus)
                {
                    // Number already has +, parse directly
                    _logger.LogInformation("Parsing number with +: {Phone}", digits);
                    parsedNumber = phoneUtil.Parse(digits, null);
                }
                else
                {
                    // Number doesn't have +, try to detect country code
                    var detectedNumber = TryParseInternationalNumber(digits, phoneUtil);

                    if (detectedNumber != null)
                    {
                        parsedNumber = detectedNumber;
                        _logger.LogInformation("Detected international number: {Phone} -> Country Code: {CC}",
                            digits, parsedNumber.CountryCode);
                    }
                    else
                    {
                        // Default to Egypt for local numbers
                        _logger.LogInformation("Treating as local Egyptian number: {Phone}", digits);

                        // Remove leading zero if present (common in local numbers)
                        if (digits.StartsWith("0"))
                        {
                            digits = digits.Substring(1);
                        }

                        parsedNumber = phoneUtil.Parse(digits, "EG");
                    }
                }

                // Validate that the number is possible
                if (!phoneUtil.IsPossibleNumber(parsedNumber))
                {
                    _logger.LogWarning("Phone number not possible: {Phone}", phone);
                    return string.Empty;
                }

                // Validate that the number is valid (stricter check)
                if (!phoneUtil.IsValidNumber(parsedNumber))
                {
                    _logger.LogWarning("Phone number not valid: {Phone}", phone);
                    // Still return it if possible, just log warning
                }

                // Format to E164 (international format with +)
                string formatted = phoneUtil.Format(parsedNumber, PhoneNumberFormat.E164);

                _logger.LogInformation("Formatted phone: {Original} -> {Formatted}", phone, formatted);
                return formatted;
            }
            catch (NumberParseException ex)
            {
                _logger.LogWarning(ex, "Failed to parse phone number: {Phone}", phone);
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error formatting phone: {Phone}", phone);
                return string.Empty;
            }
        }

        /// <summary>
        /// Tries to detect and parse international numbers without + prefix
        /// by testing against common country codes
        /// </summary>
        private PhoneNumber? TryParseInternationalNumber(string digits, PhoneNumberUtil phoneUtil)
        {
            // List of common country codes to try (ordered by likelihood)
            var commonCountryCodes = new[]
            {
                // Middle East & North Africa
                ("20", "EG"),   // Egypt
                ("971", "AE"),  // UAE
                ("966", "SA"),  // Saudi Arabia
                ("965", "KW"),  // Kuwait
                ("974", "QA"),  // Qatar
                ("973", "BH"),  // Bahrain
                ("968", "OM"),  // Oman
                ("962", "JO"),  // Jordan
                ("961", "LB"),  // Lebanon
                ("970", "PS"),  // Palestine
                ("972", "IL"),  // Israel
                ("964", "IQ"),  // Iraq
                ("963", "SY"),  // Syria
                ("216", "TN"),  // Tunisia
                ("213", "DZ"),  // Algeria
                ("212", "MA"),  // Morocco
                ("218", "LY"),  // Libya
                ("249", "SD"),  // Sudan

                // Major countries worldwide
                ("1", "US"),     // USA/Canada
                ("44", "GB"),    // UK
                ("91", "IN"),    // India
                ("92", "PK"),    // Pakistan
                ("880", "BD"),   // Bangladesh
                ("86", "CN"),    // China
                ("81", "JP"),    // Japan
                ("82", "KR"),    // South Korea
                ("49", "DE"),    // Germany
                ("33", "FR"),    // France
                ("39", "IT"),    // Italy
                ("34", "ES"),    // Spain
                ("7", "RU"),     // Russia
                ("90", "TR"),    // Turkey
                ("98", "IR"),    // Iran
                ("62", "ID"),    // Indonesia
                ("60", "MY"),    // Malaysia
                ("65", "SG"),    // Singapore
                ("66", "TH"),    // Thailand
                ("84", "VN"),    // Vietnam
                ("63", "PH"),    // Philippines
                ("55", "BR"),    // Brazil
                ("52", "MX"),    // Mexico
                ("54", "AR"),    // Argentina
                ("27", "ZA"),    // South Africa
                ("234", "NG"),   // Nigeria
                ("254", "KE"),   // Kenya
                ("61", "AU"),    // Australia
                ("64", "NZ")     // New Zealand
            };

            // Try to match against known country codes
            foreach (var (code, region) in commonCountryCodes)
            {
                if (digits.StartsWith(code))
                {
                    try
                    {
                        // Add + and try to parse
                        var withPlus = "+" + digits;
                        var parsed = phoneUtil.Parse(withPlus, null);

                        // Verify it matches the expected region and is valid
                        if (parsed.CountryCode.ToString() == code && phoneUtil.IsPossibleNumber(parsed))
                        {
                            _logger.LogInformation("Detected country code {Code} ({Region}) in number: {Digits}",
                                code, region, digits);
                            return parsed;
                        }
                    }
                    catch
                    {
                        // Continue to next code
                    }
                }
            }

            // If number is long enough (11+ digits), try parsing with + anyway
            if (digits.Length >= 11)
            {
                try
                {
                    var withPlus = "+" + digits;
                    var parsed = phoneUtil.Parse(withPlus, null);

                    if (phoneUtil.IsPossibleNumber(parsed))
                    {
                        _logger.LogInformation("Parsed as international number (auto-detected): {Digits} -> +{CC}",
                            digits, parsed.CountryCode);
                        return parsed;
                    }
                }
                catch
                {
                    // Not a valid international number
                }
            }

            return null; // Not detected as international
        }
        private string ExtractFirstName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return string.Empty;

            var parts = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : fullName.Trim();
        }

        private bool ContainsArabic(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return Regex.IsMatch(text, @"[\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF\uFB50-\uFDFF\uFE70-\uFEFF]");
        }

        private string DetermineGender(string fullName, string arabicName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "U"; // Unknown

            // Extract first name for gender determination
            var firstName = ExtractFirstName(fullName);

            // Check Arabic name patterns first if available
            if (!string.IsNullOrWhiteSpace(arabicName) && ContainsArabic(arabicName))
            {
                // Common Arabic female name endings
                if (arabicName.EndsWith("ة") || arabicName.EndsWith("اء") || arabicName.EndsWith("ى"))
                    return "F"; // Female

                // Common Arabic female names
                var femaleNames = new[] { "فاطمة", "عائشة", "خديجة", "زينب", "مريم", "سارة", "نور", "هدى", "أمل", "ليلى" };
                if (femaleNames.Any(n => arabicName.Contains(n)))
                    return "F"; // Female

                return "M"; // Male
            }

            // English name patterns
            var lowerName = firstName.ToLowerInvariant();

            // Common female name endings
            if (lowerName.EndsWith("a") || lowerName.EndsWith("e") || lowerName.EndsWith("ie") ||
                lowerName.EndsWith("y") || lowerName.EndsWith("een") || lowerName.EndsWith("ine"))
                return "F"; // Female

            // Common female names
            var commonFemaleNames = new[] { "sarah", "mary", "maria", "lisa", "anna", "emma", "sophia", "olivia", "emily" };
            if (commonFemaleNames.Any(n => lowerName.Contains(n)))
                return "F"; // Female

            return "M"; // Male
        }
    }

    public class ProcessedContactData
    {
        public string FirstName { get; set; } = string.Empty;
        public string ArabicName { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
        public string FormattedPhone { get; set; } = string.Empty;
        public string Gender { get; set; } = "Unknown";
    }
}
