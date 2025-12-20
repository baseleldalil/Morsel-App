using ExcelDataReader;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;

namespace WhatsAppSender.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactsV1Controller : ControllerBase
    {

        private readonly SaaSDbContext _context;

        public ContactsV1Controller(SaaSDbContext context)
        {
            _context = context;
        }


        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromQuery] string apiKeyId, [FromQuery] string userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (string.IsNullOrWhiteSpace(apiKeyId) || string.IsNullOrWhiteSpace(userId))
                return BadRequest("ApiKeyId and UserId are required.");

            string extension = Path.GetExtension(file.FileName).ToLower();
            List<Contact> contacts = new();

            if (extension == ".xlsx" || extension == ".xls")
            {
                contacts = await ReadExcelFileAsync(file, apiKeyId, userId);
            }
            else if (extension == ".csv")
            {
                contacts = await ReadCsvFileAsync(file, apiKeyId, userId);
            }
            else
            {
                return BadRequest("Unsupported file format. Please upload .xlsx, .xls, or .csv");
            }

            if (!contacts.Any())
                return BadRequest("No records found.");

            _context.Contacts.AddRange(contacts);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Contacts imported successfully.",
                Total = contacts.Count,
                Valid = contacts.Count(c => c.Status == ContactStatus.Pending),
                Invalid = contacts.Count(c => c.Status == ContactStatus.HasIssues)
            });
        }

        // -----------------------
        // Excel Reader
        // -----------------------
        private async Task<List<Contact>> ReadExcelFileAsync(IFormFile file, string apiKeyId, string userId)
        {
            var contacts = new List<Contact>();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using var stream = file.OpenReadStream();
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var result = reader.AsDataSet();
            var table = result.Tables[0];

            for (int i = 1; i < table.Rows.Count; i++) // skip header
            {
                var name = table.Rows[i][0]?.ToString()?.Trim();
                var number = table.Rows[i][1]?.ToString()?.Trim();
                var gender = table.Rows[i][2]?.ToString()?.Trim();

                contacts.Add(CreateContact(name, number, gender, apiKeyId, userId));
            }

            await Task.CompletedTask;
            return contacts;
        }

        // -----------------------
        // CSV Reader
        // -----------------------
        private async Task<List<Contact>> ReadCsvFileAsync(IFormFile file, string apiKeyId, string userId)
        {
            var contacts = new List<Contact>();

            using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
            string? line;
            int lineNumber = 0;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;
                if (lineNumber == 1) continue; // skip header

                var cols = line.Split(',');

                if (cols.Length < 3)
                    continue;

                var name = cols[0].Trim();
                var number = cols[1].Trim();
                var gender = cols[2].Trim();

                contacts.Add(CreateContact(name, number, gender, apiKeyId, userId));
            }

            return contacts;
        }

        // -----------------------
        // Validation + Mapping
        // -----------------------
        private Contact CreateContact(string? name, string? number, string? gender, string apiKeyId, string userId)
        {
            var contact = new Contact
            {
                ApiKeyId = apiKeyId,
                UserId = userId,
                FirstName = name ?? string.Empty,
                ArabicName = name ?? string.Empty,
                EnglishName = name ?? string.Empty,
                Name = name ?? string.Empty,
                Number = number ?? string.Empty,
                FormattedPhone = number ?? string.Empty,
                Gender = string.IsNullOrWhiteSpace(gender) ? "U" : gender.ToUpper(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = ContactStatus.Pending
            };

            var validation = ValidateContact(contact);
            if (!validation.IsValid)
            {
                contact.Status = ContactStatus.HasIssues;
                contact.IssueDescription = validation.Error;
            }

            return contact;
        }

        private (bool IsValid, string Error) ValidateContact(Contact c)
        {
            if (string.IsNullOrWhiteSpace(c.FirstName))
                return (false, "Missing name");

            if (string.IsNullOrWhiteSpace(c.FormattedPhone) || !c.FormattedPhone.All(char.IsDigit))
                return (false, "Invalid phone number");

            if (c.Gender != "M" && c.Gender != "F" && c.Gender != "U")
                return (false, "Invalid gender value (must be M, F, or U)");

            return (true, "");
        }
    }
}
