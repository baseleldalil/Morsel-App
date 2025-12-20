using System.Text.RegularExpressions;

namespace WhatsAppSender.API.Services
{
    public interface IMessageTemplateService
    {
        string ProcessTemplate(string template, string name, string phone, string? company, string gender);
        string SelectTemplateByGender(string defaultTemplate, string? maleTemplate, string? femaleTemplate, string gender);
    }

    public class MessageTemplateService : IMessageTemplateService
    {
        private readonly ILogger<MessageTemplateService> _logger;

        public MessageTemplateService(ILogger<MessageTemplateService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Process template and substitute all variables
        /// </summary>
        public string ProcessTemplate(string template, string name, string phone, string? company, string gender)
        {
            if (string.IsNullOrWhiteSpace(template))
                return string.Empty;

            try
            {
                var result = template;

                // Substitute {name} - properly formatted name (not reversed)
                result = Regex.Replace(result, @"\{name\}", name ?? "", RegexOptions.IgnoreCase);

                // Substitute {company}
                result = Regex.Replace(result, @"\{company\}", company ?? "", RegexOptions.IgnoreCase);

                // Substitute {phone}
                result = Regex.Replace(result, @"\{phone\}", phone ?? "", RegexOptions.IgnoreCase);

                // Substitute {date} - Current date
                result = Regex.Replace(result, @"\{date\}", DateTime.Now.ToString("yyyy-MM-dd"), RegexOptions.IgnoreCase);

                // Substitute {time} - Current time
                result = Regex.Replace(result, @"\{time\}", DateTime.Now.ToString("HH:mm"), RegexOptions.IgnoreCase);

                _logger.LogDebug("Template processed successfully. Gender: {Gender}, Name: {Name}", gender, name);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing template");
                return template; // Return original template on error
            }
        }

        /// <summary>
        /// Select appropriate template based on gender
        /// </summary>
        public string SelectTemplateByGender(string defaultTemplate, string? maleTemplate, string? femaleTemplate, string gender)
        {
            if (string.IsNullOrWhiteSpace(gender) || gender == "U")
            {
                // Unknown gender - use default template
                return defaultTemplate;
            }

            if (gender == "M" && !string.IsNullOrWhiteSpace(maleTemplate))
            {
                // Male - use male template if available
                return maleTemplate;
            }

            if (gender == "F" && !string.IsNullOrWhiteSpace(femaleTemplate))
            {
                // Female - use female template if available
                return femaleTemplate;
            }

            // Fallback to default template
            return defaultTemplate;
        }
    }
}
