using System.Text.RegularExpressions;
using WhatsApp.Shared.Models;

namespace WhatsAppSender.API.Services
{
    public interface ITemplateValidationService
    {
        TemplateValidationResult ValidateTemplate(string templateContent, List<string> availableColumns);
        TemplateValidationResult ValidateTemplateForCampaign(string templateContent, List<Contact> contacts);
        List<string> ExtractVariables(string templateContent);
    }

    public class TemplateValidationService : ITemplateValidationService
    {
        private readonly ILogger<TemplateValidationService> _logger;

        // Regex to match {variable_name} or {Variable Name} patterns
        private static readonly Regex VariableRegex = new Regex(
            @"\{([^}]+)\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public TemplateValidationService(ILogger<TemplateValidationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validate template against available columns
        /// </summary>
        public TemplateValidationResult ValidateTemplate(string templateContent, List<string> availableColumns)
        {
            var result = new TemplateValidationResult
            {
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string>(),
                Variables = new List<string>()
            };

            if (string.IsNullOrWhiteSpace(templateContent))
            {
                result.IsValid = false;
                result.Errors.Add("Template content cannot be empty.");
                return result;
            }

            // Extract all variables
            var variables = ExtractVariables(templateContent);
            result.Variables = variables;

            // Check minimum variable count requirement (must have at least 3 unique variables)
            if (variables.Count < 3)
            {
                result.IsValid = false;
                result.Errors.Add($"Message template requires at least 3 distinct variables. Found: {string.Join(", ", variables.Select(v => $"{{{v}}}"))}. Please add at least {3 - variables.Count} more variable(s).");
                return result;
            }

            // Normalize available columns for case-insensitive matching
            var normalizedColumns = availableColumns
                .Select(col => NormalizeVariableName(col))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Check if all variables match available columns
            var missingVariables = new List<string>();
            foreach (var variable in variables)
            {
                var normalized = NormalizeVariableName(variable);

                // Check if this variable exists in available columns
                if (!normalizedColumns.Contains(normalized) && !IsSystemVariable(variable))
                {
                    missingVariables.Add(variable);
                }
            }

            if (missingVariables.Any())
            {
                result.IsValid = false;
                result.Errors.Add($"The following variables are not found in your dataset: {string.Join(", ", missingVariables.Select(v => $"{{{v}}}"))}. Please update the template or add these columns to your Excel file.");
            }

            _logger.LogInformation("Template validation result: {IsValid}, Variables: {Variables}, Missing: {Missing}",
                result.IsValid, variables.Count, missingVariables.Count);

            return result;
        }

        /// <summary>
        /// Validate template for a campaign with specific contacts
        /// </summary>
        public TemplateValidationResult ValidateTemplateForCampaign(string templateContent, List<Contact> contacts)
        {
            if (!contacts.Any())
            {
                return new TemplateValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "No contacts available for template validation." }
                };
            }

            // Get available columns from first contact (assuming all contacts have same structure)
            var availableColumns = GetAvailableColumnsFromContact(contacts.First());

            return ValidateTemplate(templateContent, availableColumns);
        }

        /// <summary>
        /// Extract all unique variables from template
        /// </summary>
        public List<string> ExtractVariables(string templateContent)
        {
            if (string.IsNullOrWhiteSpace(templateContent))
                return new List<string>();

            var matches = VariableRegex.Matches(templateContent);
            var variables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var variableName = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(variableName))
                    {
                        // Normalize to title case for consistency
                        variables.Add(NormalizeVariableName(variableName));
                    }
                }
            }

            return variables.ToList();
        }

        /// <summary>
        /// Normalize variable name for consistent matching
        /// Converts "arabic name", "Arabic Name", "ARABIC NAME" to "Arabic name"
        /// </summary>
        private string NormalizeVariableName(string variableName)
        {
            if (string.IsNullOrWhiteSpace(variableName))
                return string.Empty;

            // Trim and handle multiple spaces
            variableName = Regex.Replace(variableName.Trim(), @"\s+", " ");

            // Convert to title case (first letter uppercase, rest lowercase)
            var words = variableName.Split(' ');
            var normalizedWords = words.Select(word =>
            {
                if (string.IsNullOrEmpty(word))
                    return word;

                return char.ToUpper(word[0]) + word.Substring(1).ToLower();
            });

            return string.Join(" ", normalizedWords);
        }

        /// <summary>
        /// Check if variable is a system variable (date, time, etc.)
        /// </summary>
        private bool IsSystemVariable(string variableName)
        {
            var systemVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "date", "time", "datetime", "year", "month", "day"
            };

            return systemVariables.Contains(variableName.Trim());
        }

        /// <summary>
        /// Get list of available columns from a Contact object
        /// </summary>
        private List<string> GetAvailableColumnsFromContact(Contact contact)
        {
            var columns = new List<string>
            {
                "Arabic name",
                "English name",
                "First name",
                "Phone",
                "Gender"
            };

            // Add optional columns if they have values
            if (!string.IsNullOrWhiteSpace(contact.City))
                columns.Add("City");

            if (!string.IsNullOrWhiteSpace(contact.Company))
                columns.Add("Company");

            if (!string.IsNullOrWhiteSpace(contact.CustomField1))
                columns.Add("Custom field1");

            if (!string.IsNullOrWhiteSpace(contact.CustomField2))
                columns.Add("Custom field2");

            if (!string.IsNullOrWhiteSpace(contact.CustomField3))
                columns.Add("Custom field3");

            return columns;
        }
    }

    /// <summary>
    /// Result of template validation
    /// </summary>
    public class TemplateValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Variables { get; set; } = new List<string>();

        public string GetErrorMessage()
        {
            return string.Join(" ", Errors);
        }
    }
}
