using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;
using WhatsAppSender.API.Services;
using WhatsAppSender.API.Models;

namespace WhatsAppSender.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactsController : ControllerBase
    {
        private readonly SaaSDbContext _context;
        private readonly IApiKeyService _apiKeyService;
        private readonly IContactProcessingService _contactProcessingService;
        private readonly ILogger<ContactsController> _logger;
        private readonly ContactsSettings _settings;

        public ContactsController(
            SaaSDbContext context,
            IApiKeyService apiKeyService,
            IContactProcessingService contactProcessingService,
            ILogger<ContactsController> logger,
            IOptions<ContactsSettings> settings)
        {
            _context = context;
            _apiKeyService = apiKeyService;
            _contactProcessingService = contactProcessingService;
            _logger = logger;
            _settings = settings.Value;
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportContacts(IFormFile file, [FromQuery] bool allowInternational = true, [FromQuery] bool skipDuplicates = true)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                // Validate file
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                var allowedExtensions = new[] { ".csv", ".xlsx", ".xls" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { error = "Only CSV and Excel files are supported" });
                }

                ImportResult importResult;

                if (fileExtension == ".csv")
                {
                    importResult = await ProcessCsvFileWithValidation(file, apiKeyEntity.Id.ToString(), apiKeyEntity.UserId, allowInternational);
                }
                else if (fileExtension == ".xlsx" || fileExtension == ".xls")
                {
                    importResult = await ProcessExcelFileWithValidation(file, apiKeyEntity.Id.ToString(), apiKeyEntity.UserId, allowInternational);
                }
                else
                {
                    return BadRequest(new { error = "Unsupported file format" });
                }

                // ✅ Check if any contacts were found
                if (!importResult.ValidContacts.Any() && !importResult.HasIssuesContacts.Any() && !importResult.InvalidContacts.Any())
                {
                    return BadRequest(new { error = "No contacts found in the file" });
                }

                var pendingContacts = importResult.ValidContacts.ToList();
                var hasIssuesContacts = importResult.HasIssuesContacts.ToList();
                var skippedContacts = importResult.InvalidContacts.ToList();

                _logger.LogInformation($"Processing import: {pendingContacts.Count} valid, {hasIssuesContacts.Count} with issues, {skippedContacts.Count} skipped");

                // ✅ Combine both valid (Pending) and HasIssues contacts for saving
                var allContactsToSave = new List<Contact>();
                allContactsToSave.AddRange(pendingContacts);
                allContactsToSave.AddRange(hasIssuesContacts);

                if (!allContactsToSave.Any())
                {
                    return Ok(new
                    {
                        message = "No contacts to import (all skipped due to missing phone numbers)",
                        total_rows = importResult.TotalRows,
                        pending_contacts = 0,
                        has_issues_contacts = 0,
                        skipped_contacts = skippedContacts.Count
                    });
                }

                // ✅ Check for duplicates in DB
                var existingNumbers = _context.Contacts
                     .Where(c => c.ApiKeyId == apiKeyEntity.Id.ToString())
                     .Select(c => c.FormattedPhone)
                     .ToHashSet();

                // Track numbers already added in this batch to avoid duplicates
                var batchNumbers = new HashSet<string>();
                int duplicateCount = 0;

                var uniqueContacts = allContactsToSave
                    .Where(c =>
                    {
                        if (existingNumbers.Contains(c.FormattedPhone) || batchNumbers.Contains(c.FormattedPhone))
                        {
                            duplicateCount++;
                            return false;
                        }
                        batchNumbers.Add(c.FormattedPhone);
                        return true;
                    })
                    .ToList();

                if (uniqueContacts.Count == 0)
                {
                    return Ok(new
                    {
                        message = "All contacts are duplicates - nothing to import",
                        total_rows = importResult.TotalRows,
                        pending_contacts = 0,
                        has_issues_contacts = 0,
                        duplicates_skipped = duplicateCount,
                        skipped_contacts = skippedContacts.Count
                    });
                }

                // ✅ Save unique contacts to DB
                _context.Contacts.AddRange(uniqueContacts);
                await _context.SaveChangesAsync();

                // ✅ Count imported by status
                var importedPending = uniqueContacts.Count(c => c.Status == ContactStatus.Pending);
                var importedHasIssues = uniqueContacts.Count(c => c.Status == ContactStatus.HasIssues);

                _logger.LogInformation($"Successfully imported {uniqueContacts.Count} contacts ({importedPending} pending, {importedHasIssues} with issues) for API key {apiKey}");

                return Ok(new
                {
                    message = "Contacts imported successfully",
                    total_rows = importResult.TotalRows,
                    imported_count = uniqueContacts.Count,
                    pending_contacts = importedPending,
                    has_issues_contacts = importedHasIssues,
                    duplicates_skipped = duplicateCount,
                    skipped_contacts = skippedContacts.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing contacts");
                return StatusCode(500, new { error = "Internal server error while importing contacts" });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetContacts([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null)
        {
            try
            {
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var apiKeyIdStr = apiKeyEntity.Id.ToString();
                var query = _context.Contacts
                    .AsNoTracking()
                    .Where(c => c.ApiKeyId == apiKeyIdStr);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(c =>
                        c.FirstName.ToLower().Contains(searchLower) ||
                        c.ArabicName.ToLower().Contains(searchLower) ||
                        c.EnglishName.ToLower().Contains(searchLower) ||
                        c.FormattedPhone.Contains(searchLower)
                    );
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // ✅ PRESERVE EXCEL ORDER: Order by ID (insertion order) to maintain Excel row sequence
                var contacts = await query
                    .OrderBy(c => c.Id)
                    .Select(c => new
                    {
                        id = c.Id,
                        first_name = c.FirstName,
                        arabic_name = (c.ArabicName ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "",
                        english_name = (c.EnglishName ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "",
                        formatted_phone = c.FormattedPhone,
                        gender = c.Gender,
                        created_at = c.CreatedAt,
                        updated_at = c.UpdatedAt,
                        status = c.Status.ToString(),
                        issue_description = c.IssueDescription,
                        last_message_sent_at = c.LastMessageSentAt,
                        last_status_update_at = c.LastStatusUpdateAt,
                        send_attempt_count = c.SendAttemptCount
                    })
                    .ToListAsync();

                return Ok(new
                {
                    total_count = totalCount,
                    page = page,
                    page_size = pageSize,
                    total_pages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    contacts = contacts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contacts");
                return StatusCode(500, new { error = "Internal server error while retrieving contacts" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContact(int id, [FromBody] UpdateContactRequest request)
        {
            try
            {
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                // Find the contact
                var apiKeyIdStr = apiKeyEntity.Id.ToString();
                var contact = await _context.Contacts
                    .FirstOrDefaultAsync(c => c.Id == id && c.ApiKeyId == apiKeyIdStr);

                if (contact == null)
                {
                    return NotFound(new { error = "Contact not found" });
                }

                // Process the updated data
                var name = request.Name ?? request.ArabicName ?? request.EnglishName ?? contact.FirstName;
                var phone = request.Phone ?? contact.FormattedPhone;

                var processedData = await _contactProcessingService.ProcessContactDataAsync(name, phone);

                // Validation tracking
                var validationIssues = new List<string>();
                var isValid = true;

                // Validate phone number
                if (string.IsNullOrWhiteSpace(processedData.FormattedPhone))
                {
                    validationIssues.Add("Invalid phone number format");
                    isValid = false;
                }
                else
                {
                    // Check for duplicate phone number (excluding current contact)
                    var duplicateExists = await _context.Contacts
                        .AnyAsync(c => c.FormattedPhone == processedData.FormattedPhone
                                    && c.ApiKeyId == apiKeyIdStr
                                    && c.Id != id);

                    if (duplicateExists)
                    {
                        return BadRequest(new { error = "A contact with this phone number already exists" });
                    }
                }

                // Validate names
                var hasArabicName = !string.IsNullOrWhiteSpace(request.ArabicName);
                var hasEnglishName = !string.IsNullOrWhiteSpace(request.EnglishName);

                if (!hasArabicName && !hasEnglishName)
                {
                    validationIssues.Add("Missing both Arabic and English names");
                    isValid = false;
                }

                // Validate gender
                var gender = request.Gender ?? contact.Gender;
                if (string.IsNullOrWhiteSpace(gender))
                {
                    gender = "U";
                }
                else
                {
                    gender = gender.Substring(0, 1).ToUpper();
                    if (gender != "M" && gender != "F" && gender != "U")
                    {
                        validationIssues.Add("Invalid gender (use M, F, or U)");
                        isValid = false;
                    }
                }

                // Update contact fields
                contact.FirstName = processedData.FirstName;
                contact.ArabicName = request.ArabicName ?? contact.ArabicName;
                contact.EnglishName = request.EnglishName ?? contact.EnglishName;
                contact.FormattedPhone = processedData.FormattedPhone ?? contact.FormattedPhone;
                contact.Gender = gender;
                contact.Name = request.ArabicName ?? request.EnglishName ?? contact.Name;
                contact.Number = processedData.FormattedPhone ?? contact.Number;
                contact.UpdatedAt = DateTime.UtcNow;

                // Update status based on validation
                if (!isValid)
                {
                    contact.Status = ContactStatus.HasIssues;
                    contact.IssueDescription = string.Join("; ", validationIssues);
                    contact.LastStatusUpdateAt = DateTime.UtcNow;
                    _logger.LogWarning($"Contact {id} has validation issues: {contact.IssueDescription}");
                }
                else
                {
                    // If everything is valid and contact was HasIssues, change to Pending
                    if (contact.Status == ContactStatus.HasIssues)
                    {
                        contact.Status = ContactStatus.Pending;
                        contact.IssueDescription = null;
                        contact.LastStatusUpdateAt = DateTime.UtcNow;
                        _logger.LogInformation($"Contact {id} status changed from HasIssues to Pending after validation");
                    }
                }

                _context.Contacts.Update(contact);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully updated contact {id} for API key {apiKey}");

                return Ok(new
                {
                    message = isValid ? "Contact updated successfully" : "Contact updated with validation issues",
                    is_valid = isValid,
                    validation_issues = validationIssues.Any() ? validationIssues : null,
                    contact = new
                    {
                        id = contact.Id,
                        first_name = contact.FirstName,
                        arabic_name = contact.ArabicName,
                        english_name = contact.EnglishName,
                        formatted_phone = contact.FormattedPhone,
                        gender = contact.Gender,
                        status = contact.Status.ToString(),
                        issue_description = contact.IssueDescription,
                        updated_at = contact.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact");
                return StatusCode(500, new { error = "Internal server error while updating contact" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContact(int id)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var apiKeyIdStr = apiKeyEntity.Id.ToString();
                var contact = await _context.Contacts
                    .FirstOrDefaultAsync(c => c.Id == id && c.ApiKeyId == apiKeyIdStr);

                if (contact == null)
                {
                    return NotFound(new { error = "Contact not found" });
                }

                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Contact deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact");
                return StatusCode(500, new { error = "Internal server error while deleting contact" });
            }
        }

        /// <summary>
        /// Update contact status (used during campaign sending)
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateContactStatus(int id, [FromBody] UpdateContactStatusRequest request)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var apiKeyIdStr = apiKeyEntity.Id.ToString();
                var contact = await _context.Contacts
                    .FirstOrDefaultAsync(c => c.Id == id && c.ApiKeyId == apiKeyIdStr);

                if (contact == null)
                {
                    return NotFound(new { error = "Contact not found" });
                }

                // Update status
                contact.Status = request.Status;
                contact.UpdatedAt = DateTime.UtcNow;
                contact.LastStatusUpdateAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(request.IssueDescription))
                {
                    contact.IssueDescription = request.IssueDescription;
                }

                if (request.Status == WhatsApp.Shared.Models.ContactStatus.Sent ||
                    request.Status == WhatsApp.Shared.Models.ContactStatus.Delivered)
                {
                    contact.LastMessageSentAt = DateTime.UtcNow;
                    contact.SendAttemptCount++;
                }
                else if (request.Status == WhatsApp.Shared.Models.ContactStatus.Failed ||
                         request.Status == WhatsApp.Shared.Models.ContactStatus.NotValid)
                {
                    contact.SendAttemptCount++;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact {ContactId} status updated to {Status}", id, request.Status);

                return Ok(new
                {
                    message = "Contact status updated successfully",
                    contact = new
                    {
                        id = contact.Id,
                        status = contact.Status.ToString(),
                        issue_description = contact.IssueDescription,
                        last_message_sent_at = contact.LastMessageSentAt,
                        send_attempt_count = contact.SendAttemptCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact status");
                return StatusCode(500, new { error = "Internal server error while updating contact status" });
            }
        }

        /// <summary>
        /// Bulk update contact statuses (efficient for campaign sending)
        /// </summary>
        [HttpPatch("bulk-status")]
        public async Task<IActionResult> BulkUpdateContactStatus([FromBody] BulkUpdateContactStatusRequest request)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var apiKeyIdStr = apiKeyEntity.Id.ToString();
                var contacts = await _context.Contacts
                    .Where(c => request.ContactIds.Contains(c.Id) && c.ApiKeyId == apiKeyIdStr)
                    .ToListAsync();

                if (!contacts.Any())
                {
                    return NotFound(new { error = "No contacts found" });
                }

                int updatedCount = 0;
                foreach (var contact in contacts)
                {
                    contact.Status = request.Status;
                    contact.UpdatedAt = DateTime.UtcNow;
                    contact.LastStatusUpdateAt = DateTime.UtcNow;

                    if (!string.IsNullOrEmpty(request.IssueDescription))
                    {
                        contact.IssueDescription = request.IssueDescription;
                    }

                    if (request.Status == WhatsApp.Shared.Models.ContactStatus.Sent ||
                        request.Status == WhatsApp.Shared.Models.ContactStatus.Delivered)
                    {
                        contact.LastMessageSentAt = DateTime.UtcNow;
                        contact.SendAttemptCount++;
                    }
                    else if (request.Status == WhatsApp.Shared.Models.ContactStatus.Failed ||
                             request.Status == WhatsApp.Shared.Models.ContactStatus.NotValid)
                    {
                        contact.SendAttemptCount++;
                    }

                    updatedCount++;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Bulk updated {Count} contacts to status {Status}", updatedCount, request.Status);

                return Ok(new
                {
                    message = "Contacts status updated successfully",
                    updated_count = updatedCount,
                    status = request.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating contact statuses");
                return StatusCode(500, new { error = "Internal server error while updating contact statuses" });
            }
        }

        /// <summary>
        /// Get contacts filtered by status
        /// </summary>
        [HttpGet("by-status/{status}")]
        public async Task<IActionResult> GetContactsByStatus(int status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50;

                var contactStatus = (WhatsApp.Shared.Models.ContactStatus)status;
                var apiKeyIdStr = apiKeyEntity.Id.ToString();

                var query = _context.Contacts
                    .AsNoTracking()
                    .Where(c => c.ApiKeyId == apiKeyIdStr && c.Status == contactStatus);

                var totalCount = await query.CountAsync();
                var contacts = await query
                    .OrderByDescending(c => c.UpdatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new
                    {
                        id = c.Id,
                        first_name = c.FirstName,
                        formatted_phone = c.FormattedPhone,
                        gender = c.Gender,
                        status = c.Status.ToString(),
                        issue_description = c.IssueDescription,
                        last_message_sent_at = c.LastMessageSentAt,
                        send_attempt_count = c.SendAttemptCount,
                        created_at = c.CreatedAt,
                        updated_at = c.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    contacts,
                    total_count = totalCount,
                    page,
                    page_size = pageSize,
                    total_pages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    status = contactStatus.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contacts by status");
                return StatusCode(500, new { error = "Internal server error while retrieving contacts" });
            }
        }

        /// <summary>
        /// Get contact status statistics - counts for all statuses
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetContactStatistics()
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var apiKeyIdStr = apiKeyEntity.Id.ToString();

                // Get counts grouped by status
                var statusCounts = await _context.Contacts
                    .Where(c => c.ApiKeyId == apiKeyIdStr)
                    .GroupBy(c => c.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                var statistics = new ContactStatusStatistics
                {
                    TotalContacts = statusCounts.Sum(s => s.Count),
                    PendingCount = statusCounts.FirstOrDefault(s => s.Status == ContactStatus.Pending)?.Count ?? 0,
                    SendingCount = statusCounts.FirstOrDefault(s => s.Status == ContactStatus.Sending)?.Count ?? 0,
                    SentCount = statusCounts.FirstOrDefault(s => s.Status == ContactStatus.Sent)?.Count ?? 0,
                    DeliveredCount = statusCounts.FirstOrDefault(s => s.Status == ContactStatus.Delivered)?.Count ?? 0,
                    FailedCount = statusCounts.FirstOrDefault(s => s.Status == ContactStatus.Failed)?.Count ?? 0,
                    NotValidCount = statusCounts.FirstOrDefault(s => s.Status == ContactStatus.NotValid)?.Count ?? 0,
                    HasIssuesCount = statusCounts.FirstOrDefault(s => s.Status == ContactStatus.HasIssues)?.Count ?? 0,
                    BlockedCount = statusCounts.FirstOrDefault(s => s.Status == ContactStatus.Blocked)?.Count ?? 0,
                    NotInterestedCount = statusCounts.FirstOrDefault(s => s.Status == ContactStatus.NotInterested)?.Count ?? 0,
                    RespondedCount = statusCounts.FirstOrDefault(s => s.Status == ContactStatus.Responded)?.Count ?? 0
                };

                return Ok(new
                {
                    statistics,
                    status_breakdown = statusCounts.Select(s => new
                    {
                        status = s.Status.ToString(),
                        count = s.Count,
                        percentage = statistics.TotalContacts > 0 ? Math.Round((double)s.Count / statistics.TotalContacts * 100, 2) : 0
                    }).OrderByDescending(s => s.count)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contact statistics");
                return StatusCode(500, new { error = "Internal server error while retrieving statistics" });
            }
        }

        /// <summary>
        /// Get contacts with enhanced filtering: All, Pending, Sent, HasIssues, NotInterested, Responded
        /// </summary>
        [HttpGet("filter")]
        public async Task<IActionResult> GetContactsWithFilter(
            [FromQuery] string filterType = "All",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50;

                var apiKeyIdStr = apiKeyEntity.Id.ToString();
                var query = _context.Contacts
                    .AsNoTracking()
                    .Where(c => c.ApiKeyId == apiKeyIdStr);

                // Apply status filter based on filterType
                switch (filterType.ToLower())
                {
                    case "all":
                        // No additional filter - show all contacts
                        break;
                    case "pending":
                        query = query.Where(c => c.Status == ContactStatus.Pending);
                        break;
                    case "sent":
                        query = query.Where(c => c.Status == ContactStatus.Sent);
                        break;
                    case "hasissues":
                        query = query.Where(c => c.Status == ContactStatus.HasIssues);
                        break;
                    case "notinterested":
                        query = query.Where(c => c.Status == ContactStatus.NotInterested);
                        break;
                    case "responded":
                        query = query.Where(c => c.Status == ContactStatus.Responded);
                        break;
                    case "delivered":
                        query = query.Where(c => c.Status == ContactStatus.Delivered);
                        break;
                    case "failed":
                        query = query.Where(c => c.Status == ContactStatus.Failed);
                        break;
                    case "sending":
                        query = query.Where(c => c.Status == ContactStatus.Sending);
                        break;
                    case "blocked":
                        query = query.Where(c => c.Status == ContactStatus.Blocked);
                        break;
                    case "notvalid":
                        query = query.Where(c => c.Status == ContactStatus.NotValid);
                        break;
                    default:
                        return BadRequest(new { error = $"Invalid filter type: {filterType}. Valid options: All, Pending, Sent, HasIssues, NotInterested, Responded, Delivered, Failed, Sending, Blocked, NotValid" });
                }

                // Apply search filter if provided
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(c =>
                        c.FirstName.ToLower().Contains(searchLower) ||
                        c.ArabicName.ToLower().Contains(searchLower) ||
                        c.EnglishName.ToLower().Contains(searchLower) ||
                        c.FormattedPhone.Contains(searchLower)
                    );
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Get paginated results
                var contacts = await query
                    .OrderByDescending(c => c.UpdatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new
                    {
                        id = c.Id,
                        first_name = c.FirstName,
                        arabic_name = (c.ArabicName ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "",
                        english_name = (c.EnglishName ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "",
                        formatted_phone = c.FormattedPhone,
                        gender = c.Gender,
                        status = c.Status.ToString(),
                        issue_description = c.IssueDescription,
                        last_message_sent_at = c.LastMessageSentAt,
                        last_status_update_at = c.LastStatusUpdateAt,
                        send_attempt_count = c.SendAttemptCount,
                        created_at = c.CreatedAt,
                        updated_at = c.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    filter_type = filterType,
                    total_count = totalCount,
                    page,
                    page_size = pageSize,
                    total_pages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    contacts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contacts with filter");
                return StatusCode(500, new { error = "Internal server error while retrieving contacts" });
            }
        }

        /// <summary>
        /// Mark contact as Not Interested (only available for Sent contacts)
        /// </summary>
        [HttpPost("{id}/not-interested")]
        public async Task<IActionResult> MarkAsNotInterested(int id)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var apiKeyIdStr = apiKeyEntity.Id.ToString();
                var contact = await _context.Contacts
                    .FirstOrDefaultAsync(c => c.Id == id && c.ApiKeyId == apiKeyIdStr);

                if (contact == null)
                {
                    return NotFound(new { error = "Contact not found" });
                }

                // Validate that contact was sent
                if (contact.Status != ContactStatus.Sent && contact.Status != ContactStatus.Delivered)
                {
                    return BadRequest(new { error = "Not Interested action is only available for sent contacts" });
                }

                contact.Status = ContactStatus.NotInterested;
                contact.UpdatedAt = DateTime.UtcNow;
                contact.LastStatusUpdateAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact {ContactId} marked as Not Interested", id);

                return Ok(new
                {
                    message = "Contact marked as Not Interested successfully",
                    contact_id = id,
                    status = contact.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking contact as Not Interested");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Mark contact as Responded (only available for Sent contacts)
        /// </summary>
        [HttpPost("{id}/responded")]
        public async Task<IActionResult> MarkAsResponded(int id)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var apiKeyIdStr = apiKeyEntity.Id.ToString();
                var contact = await _context.Contacts
                    .FirstOrDefaultAsync(c => c.Id == id && c.ApiKeyId == apiKeyIdStr);

                if (contact == null)
                {
                    return NotFound(new { error = "Contact not found" });
                }

                // Validate that contact was sent
                if (contact.Status != ContactStatus.Sent && contact.Status != ContactStatus.Delivered)
                {
                    return BadRequest(new { error = "Responded action is only available for sent contacts" });
                }

                contact.Status = ContactStatus.Responded;
                contact.UpdatedAt = DateTime.UtcNow;
                contact.LastStatusUpdateAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact {ContactId} marked as Responded", id);

                return Ok(new
                {
                    message = "Contact marked as Responded successfully",
                    contact_id = id,
                    status = contact.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking contact as Responded");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Resend to contact (resets status to Pending for re-sending)
        /// </summary>
        [HttpPost("{id}/resend")]
        public async Task<IActionResult> ResendToContact(int id)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var apiKeyIdStr = apiKeyEntity.Id.ToString();
                var contact = await _context.Contacts
                    .FirstOrDefaultAsync(c => c.Id == id && c.ApiKeyId == apiKeyIdStr);

                if (contact == null)
                {
                    return NotFound(new { error = "Contact not found" });
                }

                // Allow resend from ANY status except Pending (already pending)
                if (contact.Status == ContactStatus.Pending)
                {
                    return BadRequest(new { error = "Contact is already in Pending status" });
                }

                // Remove from duplicate prevention if in persistent mode
                var sentPhoneRecord = await _context.SentPhoneNumbers
                    .FirstOrDefaultAsync(s => s.UserId == apiKeyEntity.UserId && s.PhoneNumber == contact.FormattedPhone);

                if (sentPhoneRecord != null)
                {
                    _context.SentPhoneNumbers.Remove(sentPhoneRecord);
                    _logger.LogInformation("Removed phone {Phone} from duplicate prevention for user {UserId}",
                        contact.FormattedPhone, apiKeyEntity.UserId);
                }

                // Reset contact to Pending status
                contact.Status = ContactStatus.Pending;
                contact.IssueDescription = null;
                contact.UpdatedAt = DateTime.UtcNow;
                contact.LastStatusUpdateAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact {ContactId} reset to Pending for resend", id);

                return Ok(new
                {
                    message = "Contact reset to Pending for resending",
                    contact_id = id,
                    status = contact.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending to contact");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Bulk resend to all failed contacts (resets status to Pending for re-sending)
        /// </summary>
        [HttpPost("resend-all-failed")]
        public async Task<IActionResult> ResendAllFailed()
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var apiKeyIdStr = apiKeyEntity.Id.ToString();

                // Get all failed contacts for this user
                var failedContacts = await _context.Contacts
                    .Where(c => c.ApiKeyId == apiKeyIdStr && c.Status == ContactStatus.Failed)
                    .ToListAsync();

                if (!failedContacts.Any())
                {
                    return Ok(new
                    {
                        message = "No failed contacts to resend",
                        resent_count = 0
                    });
                }

                // Remove from duplicate prevention and reset status
                var phoneNumbers = failedContacts.Select(c => c.FormattedPhone).ToList();
                var sentPhoneRecords = await _context.SentPhoneNumbers
                    .Where(s => s.UserId == apiKeyEntity.UserId && phoneNumbers.Contains(s.PhoneNumber))
                    .ToListAsync();

                if (sentPhoneRecords.Any())
                {
                    _context.SentPhoneNumbers.RemoveRange(sentPhoneRecords);
                    _logger.LogInformation("Removed {Count} phone numbers from duplicate prevention for user {UserId}",
                        sentPhoneRecords.Count, apiKeyEntity.UserId);
                }

                // Reset all failed contacts to Pending
                foreach (var contact in failedContacts)
                {
                    contact.Status = ContactStatus.Pending;
                    contact.IssueDescription = null;
                    contact.UpdatedAt = DateTime.UtcNow;
                    contact.LastStatusUpdateAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Bulk resend: {Count} contacts reset to Pending for user {UserId}",
                    failedContacts.Count, apiKeyEntity.UserId);

                return Ok(new
                {
                    message = $"Successfully reset {failedContacts.Count} failed contacts to Pending",
                    resent_count = failedContacts.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk resending failed contacts");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Toggle contact selection (for campaign inclusion)
        /// </summary>
        [HttpPost("{id}/toggle-selection")]
        public async Task<IActionResult> ToggleContactSelection(int id)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var apiKeyIdStr = apiKeyEntity.Id.ToString();
                var contact = await _context.Contacts
                    .FirstOrDefaultAsync(c => c.Id == id && c.ApiKeyId == apiKeyIdStr);

                if (contact == null)
                {
                    return NotFound(new { error = "Contact not found" });
                }

                contact.IsSelected = !contact.IsSelected;
                contact.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact {ContactId} selection toggled to {IsSelected}", id, contact.IsSelected);

                return Ok(new
                {
                    message = "Contact selection toggled successfully",
                    contact_id = id,
                    is_selected = contact.IsSelected
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling contact selection");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Upload attachment (image/file) for contact
        /// IMPORTANT: When deleting an attachment, use DELETE endpoint - it will NOT auto-open upload dialog
        /// </summary>
        [HttpPost("{id}/upload-attachment")]
        public async Task<IActionResult> UploadAttachment(int id, IFormFile file)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                // Note: ContactAttachmentService will be injected via DI
                // For now, create inline (you should inject it in constructor)
                var attachmentService = new Services.ContactAttachmentService(
                    _context,
                    HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>(),
                    HttpContext.RequestServices.GetRequiredService<ILogger<Services.ContactAttachmentService>>()
                );

                var result = await attachmentService.UploadAttachmentAsync(id, file, apiKeyEntity.UserId);

                if (!result.Success)
                {
                    return BadRequest(new { error = result.ErrorMessage });
                }

                _logger.LogInformation("Attachment uploaded for contact {ContactId}", id);

                return Ok(new
                {
                    message = "Attachment uploaded successfully",
                    contact_id = result.ContactId,
                    file_path = result.FilePath,
                    file_name = result.FileName,
                    file_type = result.FileType,
                    file_size = result.FileSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading attachment for contact {ContactId}", id);
                return StatusCode(500, new { error = "Internal server error while uploading attachment" });
            }
        }

        /// <summary>
        /// Delete attachment from contact
        /// IMPORTANT: This will NOT auto-open the upload dialog - it simply removes the file
        /// </summary>
        [HttpDelete("{id}/attachment")]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var attachmentService = new Services.ContactAttachmentService(
                    _context,
                    HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>(),
                    HttpContext.RequestServices.GetRequiredService<ILogger<Services.ContactAttachmentService>>()
                );

                var result = await attachmentService.DeleteAttachmentAsync(id, apiKeyEntity.UserId);

                if (!result.Success)
                {
                    return BadRequest(new { error = result.ErrorMessage });
                }

                _logger.LogInformation("Attachment deleted for contact {ContactId}", id);

                return Ok(new
                {
                    message = result.Message ?? "Attachment deleted successfully",
                    contact_id = id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attachment for contact {ContactId}", id);
                return StatusCode(500, new { error = "Internal server error while deleting attachment" });
            }
        }

        /// <summary>
        /// Get contact with attachment info
        /// </summary>
        [HttpGet("{id}/attachment")]
        public async Task<IActionResult> GetAttachmentInfo(int id)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var apiKeyIdStr = apiKeyEntity.Id.ToString();
                var contact = await _context.Contacts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id && c.ApiKeyId == apiKeyIdStr);

                if (contact == null)
                {
                    return NotFound(new { error = "Contact not found" });
                }

                return Ok(new
                {
                    contact_id = contact.Id,
                    has_attachment = !string.IsNullOrEmpty(contact.AttachmentPath),
                    attachment_path = contact.AttachmentPath,
                    attachment_file_name = contact.AttachmentFileName,
                    attachment_type = contact.AttachmentType,
                    attachment_size = contact.AttachmentSize,
                    attachment_uploaded_at = contact.AttachmentUploadedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attachment info for contact {ContactId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private async Task<ImportResult> ProcessExcelFileWithValidation(IFormFile file, string apiKeyId, string userId, bool allowInternational)
        {
            var result = new ImportResult();

            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);

                var worksheet = workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    _logger.LogWarning("No worksheet found in Excel file");
                    return result;
                }

                var rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                var columnCount = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

                if (rowCount < 1)
                {
                    _logger.LogWarning("Excel file has no rows");
                    return result;
                }

                result.TotalRows = rowCount - 1; // Exclude header row

                _logger.LogInformation("Processing Excel file with {RowCount} rows and {ColumnCount} columns", rowCount, columnCount);

                // Get headers from first row
                var headers = GetExcelHeaders(worksheet, columnCount);
                var nameColumn = FindColumnIndex(headers, "name", "names", "client", "user name", "username");
                var phoneColumn = FindColumnIndex(headers, "phone", "number", "phone number", "phoneNumber", "numbers", "assigned number", "assigned", "assigned phone", "contact number", "mobile");
                var genderColumn = FindColumnIndex(headers, "gender", "sex");

                if (nameColumn < 0 || phoneColumn < 0)
                {
                    _logger.LogWarning("Could not find name or phone column in Excel headers");
                }

                // Process rows
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var rowData = GetExcelRowData(worksheet, row, columnCount);

                        if (IsEmptyExcelRow(rowData))
                            continue;

                        var name = nameColumn >= 0 && nameColumn < rowData.Count ? rowData[nameColumn] : string.Empty;
                        var phone = phoneColumn >= 0 && phoneColumn < rowData.Count ? rowData[phoneColumn] : string.Empty;
                        var gender = genderColumn >= 0 && genderColumn < rowData.Count ? rowData[genderColumn] : string.Empty;

                        // Split phone numbers if multiple in same cell
                        var phoneNumbers = SplitMultiplePhoneNumbers(phone);

                        foreach (var phoneNumber in phoneNumbers)
                        {
                            await ProcessSingleContact(result, row, name, phoneNumber.Trim(), gender, apiKeyId, userId, allowInternational);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing Excel row {Row}", row);
                        result.InvalidContacts.Add(new InvalidContactInfo
                        {
                            RowNumber = row,
                            Name = "",
                            Phone = "",
                            Gender = "",
                            Reason = $"Error processing row: {ex.Message}"
                        });
                    }
                }

                _logger.LogInformation("Excel processing complete: {Valid} valid, {Invalid} invalid", result.ValidContacts.Count, result.InvalidContacts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error processing Excel file");
                throw;
            }

            return result;
        }

        private async Task<ImportResult> ProcessCsvFileWithValidation(IFormFile file, string apiKeyId, string userId, bool allowInternational)
        {
            var result = new ImportResult();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null,
                HeaderValidated = null,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = context =>
                {
                    _logger.LogWarning($"Bad data found on row {context.RawRecord}: {context.RawRecord}");
                }
            };

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                using var csv = new CsvReader(reader, config);

                csv.Context.RegisterClassMap<EnhancedCsvContactRecordMap>();

                var records = csv.GetRecordsAsync<EnhancedCsvContactRecord>();
                int rowNumber = 2; // Start from 2 (1 is header)

                await foreach (var record in records)
                {
                    try
                    {
                        if (!IsEmptyRow(record))
                        {
                            var name = GetBestNameFromRecord(record);
                            var phone = GetBestPhoneFromRecord(record);
                            var gender = record.Gender ?? string.Empty;

                            // Split phone numbers if multiple in same cell
                            var phoneNumbers = SplitMultiplePhoneNumbers(phone);

                            foreach (var phoneNumber in phoneNumbers)
                            {
                                await ProcessSingleContact(result, rowNumber, name, phoneNumber.Trim(), gender, apiKeyId, userId, allowInternational);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing CSV record at row {Row}", rowNumber);
                        result.InvalidContacts.Add(new InvalidContactInfo
                        {
                            RowNumber = rowNumber,
                            Name = "",
                            Phone = "",
                            Gender = "",
                            Reason = $"Error processing row: {ex.Message}"
                        });
                    }
                    rowNumber++;
                }

                result.TotalRows = rowNumber - 2; // Exclude header
                _logger.LogInformation("CSV processing complete: {Valid} valid, {Invalid} invalid", result.ValidContacts.Count, result.InvalidContacts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CSV file");
                throw;
            }

            return result;
        }

        private async Task ProcessSingleContact(ImportResult result, int rowNumber, string name, string phone, string gender, string apiKeyId, string userId, bool allowInternational)
        {
            var validationIssues = new List<string>();
            bool hasValidPhone = true;
            string formattedPhone = phone;
            string arabicName = string.Empty;  // Will be set based on language detection
            string englishName = string.Empty; // Will be set based on language detection
            string firstName = string.Empty;

            // ✅ First, detect language and set Arabic/English name based on input name
            // Also extract FIRST NAME only (not full name)
            if (!string.IsNullOrWhiteSpace(name))
            {
                var trimmedName = name.Trim();
                // Extract first name (first word before space)
                firstName = ExtractFirstName(trimmedName);

                if (ContainsArabicCharacters(trimmedName))
                {
                    // Arabic name - store first name ONLY in ArabicName column
                    arabicName = firstName;
                    englishName = string.Empty;
                }
                else
                {
                    // English name - store first name ONLY in EnglishName column
                    englishName = firstName;
                    arabicName = string.Empty;
                }
            }

            // ✅ Validate phone number is provided
            if (string.IsNullOrWhiteSpace(phone))
            {
                validationIssues.Add("Missing phone number");
                hasValidPhone = false;
            }
            else
            {
                // Process contact data to validate phone
                var processedData = await ProcessContactWithTimeoutAsync(name, phone);

                if (processedData == null || string.IsNullOrWhiteSpace(processedData.FormattedPhone))
                {
                    validationIssues.Add("Invalid phone number format");
                    hasValidPhone = false;
                }
                else
                {
                    formattedPhone = processedData.FormattedPhone;
                    // Use processed names if available (they should match our detection above)
                    if (!string.IsNullOrWhiteSpace(processedData.ArabicName))
                        arabicName = processedData.ArabicName;
                    if (!string.IsNullOrWhiteSpace(processedData.EnglishName))
                        englishName = processedData.EnglishName;
                    if (!string.IsNullOrWhiteSpace(processedData.FirstName))
                        firstName = processedData.FirstName;

                    // Validate Egyptian numbers only if international not allowed
                    if (!allowInternational && !processedData.FormattedPhone.StartsWith("+20"))
                    {
                        validationIssues.Add("Non-Egyptian phone number (+20 required)");
                        hasValidPhone = false;
                    }
                }
            }

            // ✅ Validate FirstName is provided (required)
            if (string.IsNullOrWhiteSpace(firstName))
            {
                validationIssues.Add("Missing name");
            }

            // Handle gender - use provided gender if valid, otherwise use auto-detected
            string finalGender = "U";
            if (!string.IsNullOrWhiteSpace(gender))
            {
                var normalizedGender = NormalizeGender(gender);
                if (!string.IsNullOrEmpty(normalizedGender))
                {
                    finalGender = normalizedGender;
                }
            }

            // Validate gender is present
            if (string.IsNullOrWhiteSpace(finalGender) || finalGender == "U")
            {
                validationIssues.Add("Missing gender (M or F required)");
            }

            // ✅ Determine status based on validation
            var contactStatus = validationIssues.Any() ? ContactStatus.HasIssues : ContactStatus.Pending;
            var issueDescription = validationIssues.Any() ? string.Join("; ", validationIssues) : null;

            // ✅ If phone is completely invalid (empty/null), skip this contact entirely
            if (!hasValidPhone && string.IsNullOrWhiteSpace(phone))
            {
                result.InvalidContacts.Add(new InvalidContactInfo
                {
                    RowNumber = rowNumber,
                    Name = name,
                    Phone = phone,
                    Gender = gender,
                    Reason = "Contact skipped - no phone number provided"
                });
                return;
            }

            // ✅ Create contact with appropriate status
            var contact = new Contact
            {
                FirstName = firstName,
                ArabicName = arabicName,
                EnglishName = englishName,
                FormattedPhone = hasValidPhone ? formattedPhone : phone, // Use original if invalid
                Gender = finalGender != "U" ? finalGender : "U",
                Status = contactStatus,
                IssueDescription = issueDescription,
                Name = firstName,
                Number = hasValidPhone ? formattedPhone : phone,
                ApiKeyId = apiKeyId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add to appropriate list
            if (contactStatus == ContactStatus.HasIssues)
            {
                result.HasIssuesContacts.Add(contact);
                _logger.LogWarning($"Row {rowNumber}: Contact has issues - {issueDescription}");
            }
            else
            {
                result.ValidContacts.Add(contact);
            }
        }

        private List<string> SplitMultiplePhoneNumbers(string phoneCell)
        {
            if (string.IsNullOrWhiteSpace(phoneCell))
                return new List<string>();

            // ✅ NEW: Split by common delimiters including "-" and "/"
            var delimiters = new[] { ',', ';', '|', '\n', '\r', '-', '/' };
            var numbers = phoneCell.Split(delimiters, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(n => n.Trim())
                                  .Where(n => !string.IsNullOrWhiteSpace(n))
                                  .ToList();

            // ✅ NEW: Extract ONLY first number with 10+ digits
            foreach (var number in numbers)
            {
                // Count digits only (ignore +, spaces, etc.)
                var digitCount = number.Count(char.IsDigit);

                if (digitCount >= 10)
                {
                    _logger.LogInformation($"Extracted first valid number: {number} (from cell: {phoneCell})");
                    return new List<string> { number }; // Return ONLY first valid number
                }
            }

            // If no valid number found, return original (will be validated later)
            _logger.LogWarning($"No valid number with 10+ digits found in: {phoneCell}");
            return new List<string> { phoneCell };
        }

        private string NormalizeGender(string gender)
        {
            if (string.IsNullOrWhiteSpace(gender))
                return string.Empty;

            var normalized = gender.Trim().ToUpperInvariant();

            // Handle various gender formats
            if (normalized == "M" || normalized == "MALE" || normalized == "ذكر" || normalized == "رجل")
                return "M";

            if (normalized == "F" || normalized == "FEMALE" || normalized == "أنثى" || normalized == "انثى" || normalized == "امرأة")
                return "F";

            return string.Empty; // Invalid gender
        }

        /// <summary>
        /// Checks if a string contains Arabic characters
        /// </summary>
        private bool ContainsArabicCharacters(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // Arabic Unicode ranges:
            // \u0600-\u06FF - Arabic
            // \u0750-\u077F - Arabic Supplement
            // \u08A0-\u08FF - Arabic Extended-A
            // \uFB50-\uFDFF - Arabic Presentation Forms-A
            // \uFE70-\uFEFF - Arabic Presentation Forms-B
            return System.Text.RegularExpressions.Regex.IsMatch(text, @"[\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF\uFB50-\uFDFF\uFE70-\uFEFF]");
        }

        /// <summary>
        /// Extracts the first name from a full name (first word before space)
        /// </summary>
        private string ExtractFirstName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return string.Empty;

            var trimmed = fullName.Trim();
            var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : trimmed;
        }

        private async Task<List<Contact>> ProcessCsvFile(IFormFile file, int apiKeyId)
        {
            var contacts = new List<Contact>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null,
                HeaderValidated = null,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = context =>
                {
                    _logger.LogWarning($"Bad data found on row {context.RawRecord.ToString()}: {context.RawRecord}");
                }
            };

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                using var csv = new CsvReader(reader, config);

                csv.Context.RegisterClassMap<EnhancedCsvContactRecordMap>();

                var records = csv.GetRecordsAsync<EnhancedCsvContactRecord>();

                var processedCount = 0;
                var errorCount = 0;
                const int batchSize = 50;

                await foreach (var record in records)
                {
                    try
                    {
                        if (!IsEmptyRow(record))
                        {
                            var name = GetBestNameFromRecord(record);
                            var phone = GetBestPhoneFromRecord(record);

                            // Skip if both name and phone are empty
                            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(phone))
                                continue;

                            // Process with timeout protection
                            var processedData = await ProcessContactWithTimeoutAsync(name, phone);

                            if (processedData != null && !string.IsNullOrWhiteSpace(processedData.FormattedPhone))
                            {
                                contacts.Add(new Contact
                                {
                                    FirstName = processedData.FirstName,
                                    ArabicName = processedData.ArabicName,
                                    EnglishName = processedData.EnglishName,
                                    FormattedPhone = processedData.FormattedPhone,
                                    Gender = processedData.Gender,
                                    Name = processedData.FirstName,
                                    Number = processedData.FormattedPhone,
                                    ApiKeyId = apiKeyId.ToString(),
                                    UserId = string.Empty, // Old method doesn't have userId
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                });
                                processedCount++;
                            }
                            else
                            {
                                errorCount++;
                            }

                            // Yield control every batch to prevent UI freezing
                            if (processedCount % batchSize == 0)
                            {
                                await Task.Yield();
                                _logger.LogInformation("Processed {Count} CSV records...", processedCount);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogWarning(ex, "Error processing CSV record");
                        // Continue processing other records
                    }
                }

                _logger.LogInformation("CSV processing complete: {Processed} successful, {Errors} errors", processedCount, errorCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CSV file");
                throw;
            }

            return contacts;
        }


        private async Task<List<Contact>> ProcessExcelFile(IFormFile file, int apiKeyId)
        {
            var contacts = new List<Contact>();

            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);

                var worksheet = workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    _logger.LogWarning("No worksheet found in Excel file");
                    return contacts;
                }

                var rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                var columnCount = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

                if (rowCount < 1)
                {
                    _logger.LogWarning("Excel file has no rows");
                    return contacts;
                }

                _logger.LogInformation("Processing Excel file with {RowCount} rows and {ColumnCount} columns", rowCount, columnCount);

                // Get headers from first row
                var headers = GetExcelHeaders(worksheet, columnCount);
                var nameColumn = FindColumnIndex(headers, "name", "names", "client", "user name", "username");
                // Expanded phone column search to include "assigned number", "assigned", etc.
                var phoneColumn = FindColumnIndex(headers, "phone", "number", "phone number", "phoneNumber", "numbers", "assigned number", "assigned", "assigned phone", "contact number", "mobile");

                if (nameColumn < 0 || phoneColumn < 0)
                {
                    _logger.LogWarning("Could not find name or phone column in Excel headers");
                }

                // Process rows in batches to prevent memory issues
                const int batchSize = 50;
                var processedCount = 0;
                var errorCount = 0;

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var rowData = GetExcelRowData(worksheet, row, columnCount);

                        if (IsEmptyExcelRow(rowData))
                            continue;

                        var name = nameColumn >= 0 && nameColumn < rowData.Count ? rowData[nameColumn] : string.Empty;
                        var phone = phoneColumn >= 0 && phoneColumn < rowData.Count ? rowData[phoneColumn] : string.Empty;

                        // Skip if both name and phone are empty
                        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(phone))
                            continue;

                        // Process contact data with timeout protection
                        var processedData = await ProcessContactWithTimeoutAsync(name, phone);

                        if (processedData != null && !string.IsNullOrWhiteSpace(processedData.FormattedPhone))
                        {
                            contacts.Add(new Contact
                            {
                                FirstName = processedData.FirstName,
                                ArabicName = processedData.ArabicName,
                                EnglishName = processedData.EnglishName,
                                FormattedPhone = processedData.FormattedPhone,
                                Gender = processedData.Gender,
                                Name = processedData.FirstName,
                                Number = processedData.FormattedPhone,
                                ApiKeyId = apiKeyId.ToString(),
                                UserId = string.Empty, // Old method doesn't have userId
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            });
                            processedCount++;
                        }
                        else
                        {
                            errorCount++;
                            _logger.LogDebug("Skipped invalid row {Row}: name='{Name}', phone='{Phone}'", row, name, phone);
                        }

                        // Yield control every batch to prevent UI freezing
                        if (row % batchSize == 0)
                        {
                            await Task.Yield(); // Allow other tasks to run
                            _logger.LogInformation("Processed {Count}/{Total} rows...", row - 1, rowCount - 1);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogWarning(ex, "Error processing Excel row {Row}", row);
                        // Continue processing other rows
                    }
                }

                _logger.LogInformation("Excel processing complete: {Processed} successful, {Errors} errors", processedCount, errorCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error processing Excel file");
                throw; // Re-throw to be handled by the controller
            }

            return contacts;
        }

        private async Task<ProcessedContactData?> ProcessContactWithTimeoutAsync(string name, string phone)
        {
            try
            {
                // Create cancellation token with timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.TranslationTimeoutSeconds));

                // Process with timeout protection
                var task = _contactProcessingService.ProcessContactDataAsync(name, phone);

                if (await Task.WhenAny(task, Task.Delay(_settings.TranslationTimeoutSeconds * 1000, cts.Token)) == task)
                {
                    return await task;
                }
                else
                {
                    _logger.LogWarning("Contact processing timed out for: name='{Name}', phone='{Phone}'", name, phone);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing contact: name='{Name}', phone='{Phone}'", name, phone);
                return null;
            }
        }

        private List<string> GetExcelHeaders(IXLWorksheet worksheet, int columnCount)
        {
            var headers = new List<string>();
            for (int col = 1; col <= columnCount; col++)
            {
                var header = worksheet.Cell(1, col).GetString().Trim();
                headers.Add(header.ToLowerInvariant());
            }
            return headers;
        }

        private int FindColumnIndex(List<string> headers, params string[] possibleNames)
        {
            for (int i = 0; i < headers.Count; i++)
            {
                foreach (var name in possibleNames)
                {
                    if (headers[i].Contains(name.ToLowerInvariant()))
                        return i;
                }
            }
            return -1;
        }

        private List<string> GetExcelRowData(IXLWorksheet worksheet, int row, int columnCount)
        {
            var rowData = new List<string>();
            for (int col = 1; col <= columnCount; col++)
            {
                try
                {
                    var cell = worksheet.Cell(row, col);

                    // Handle different cell types safely
                    string cellValue;
                    if (cell.IsEmpty())
                    {
                        cellValue = string.Empty;
                    }
                    else if (cell.DataType == XLDataType.Number)
                    {
                        // Handle numeric cells (including phone numbers stored as numbers)
                        cellValue = cell.GetValue<double>().ToString("0");
                    }
                    else if (cell.DataType == XLDataType.DateTime)
                    {
                        // Handle date cells
                        cellValue = cell.GetDateTime().ToString("yyyy-MM-dd");
                    }
                    else if (cell.DataType == XLDataType.Boolean)
                    {
                        cellValue = cell.GetBoolean().ToString();
                    }
                    else
                    {
                        // Default to string
                        cellValue = cell.GetString().Trim();
                    }

                    rowData.Add(cellValue);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading cell at row {Row}, col {Col}", row, col);
                    rowData.Add(string.Empty); // Add empty string on error
                }
            }
            return rowData;
        }

        private bool IsEmptyExcelRow(List<string> rowData)
        {
            return rowData.All(cell => string.IsNullOrWhiteSpace(cell));
        }

        private static bool IsEmptyRow(EnhancedCsvContactRecord record)
        {
            return string.IsNullOrWhiteSpace(record.Name) &&
                   string.IsNullOrWhiteSpace(record.Number) &&
                   string.IsNullOrWhiteSpace(record.UserName) &&
                   string.IsNullOrWhiteSpace(record.Client) &&
                   string.IsNullOrWhiteSpace(record.PhoneNumber) &&
                   string.IsNullOrWhiteSpace(record.Numbers);
        }

        private string GetBestNameFromRecord(EnhancedCsvContactRecord record)
        {
            if (!string.IsNullOrWhiteSpace(record.Name))
                return record.Name;
            if (!string.IsNullOrWhiteSpace(record.UserName))
                return record.UserName;
            if (!string.IsNullOrWhiteSpace(record.Client))
                return record.Client;
            return string.Empty;
        }

        private string GetBestPhoneFromRecord(EnhancedCsvContactRecord record)
        {
            if (!string.IsNullOrWhiteSpace(record.Number))
                return record.Number;
            if (!string.IsNullOrWhiteSpace(record.PhoneNumber))
                return record.PhoneNumber;
            if (!string.IsNullOrWhiteSpace(record.Numbers))
                return record.Numbers;
            return string.Empty;
        }
    }

    public class EnhancedCsvContactRecord
    {
        public string Name { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Client { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Numbers { get; set; } = string.Empty;
        public string? Gender { get; set; } = string.Empty;
    }

    public class EnhancedCsvContactRecordMap : ClassMap<EnhancedCsvContactRecord>
    {
        public EnhancedCsvContactRecordMap()
        {
            Map(m => m.Name).Name("name", "names", "Name", "Names").Optional();
            Map(m => m.Number).Name("number", "numbers", "Number", "Numbers").Optional();
            Map(m => m.UserName).Name("user name", "username", "User Name", "Username", "UserName").Optional();
            Map(m => m.Client).Name("client", "Client").Optional();
            Map(m => m.PhoneNumber).Name("phone number", "phoneNumber", "Phone Number", "PhoneNumber", "phone").Optional();
            Map(m => m.Numbers).Name("numbers", "Numbers").Optional();
            Map(m => m.Gender).Name("gender", "Gender", "sex", "Sex").Optional();
        }
    }

    public class CsvContactRecord
    {
        public string Name { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
    }

    public class CsvContactRecordMap : ClassMap<CsvContactRecord>
    {
        public CsvContactRecordMap()
        {
            Map(m => m.Name).Name("names", "name", "Name");
            Map(m => m.Number).Name("numbers", "number", "Number");
            Map(m => m.Gender).Name("gender", "Gender");
        }
    }

    public class UpdateContactRequest
    {
        public string? Name { get; set; }
        public string? ArabicName { get; set; }
        public string? EnglishName { get; set; }
        public string? Phone { get; set; }
        public string? Gender { get; set; }

    }

    public class ImportResult
    {
        public int TotalRows { get; set; }
        public List<Contact> ValidContacts { get; set; } = new List<Contact>();
        public List<Contact> HasIssuesContacts { get; set; } = new List<Contact>(); // ✅ Contacts with validation issues
        public List<InvalidContactInfo> InvalidContacts { get; set; } = new List<InvalidContactInfo>(); // Completely invalid (skipped)
    }

    public class InvalidContactInfo
    {
        public int RowNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}