using Microsoft.EntityFrameworkCore;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;

namespace WhatsAppSender.API.Services
{
    /// <summary>
    /// RESTRUCTURED: Attachment service with atomic operations and no entity tracking issues
    /// </summary>
    public interface IContactAttachmentService
    {
        Task<AttachmentResult> UploadAttachmentAsync(int contactId, IFormFile file, string userId);
        Task<AttachmentResult> DeleteAttachmentAsync(int contactId, string userId);
        Task<AttachmentResult> GetAttachmentInfoAsync(int contactId, string userId);
        bool IsValidFileType(string fileName);
        string GetAttachmentType(string extension);
    }

    public class ContactAttachmentService : IContactAttachmentService
    {
        private readonly SaaSDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ContactAttachmentService> _logger;

        private readonly Dictionary<string, string> _supportedTypes = new()
        {
            // Images (most common for WhatsApp)
            { ".jpg", "Image" }, { ".jpeg", "Image" }, { ".png", "Image" }, { ".gif", "Image" },
            { ".bmp", "Image" }, { ".webp", "Image" },
            // Videos
            { ".mp4", "Video" }, { ".mov", "Video" }, { ".avi", "Video" }, { ".mkv", "Video" },
            // Documents
            { ".pdf", "Document" }, { ".doc", "Document" }, { ".docx", "Document" },
            { ".txt", "Document" }, { ".xls", "Document" }, { ".xlsx", "Document" }
        };

        private readonly long _maxFileSize = 25 * 1024 * 1024; // 25MB max for WhatsApp

        public ContactAttachmentService(
            SaaSDbContext context,
            IWebHostEnvironment environment,
            ILogger<ContactAttachmentService> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// RESTRUCTURED: Upload attachment with atomic transaction (file + database)
        /// Handles both initial upload and re-upload (replace) in single operation
        /// </summary>
        public async Task<AttachmentResult> UploadAttachmentAsync(int contactId, IFormFile file, string userId)
        {
            // Step 1: Validate input
            var validationResult = ValidateFile(file);
            if (!validationResult.Success)
            {
                return validationResult;
            }

            // Step 2: Get contact (no tracking to avoid conflicts)
            var contact = await _context.Contacts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contactId && c.UserId == userId);

            if (contact == null)
            {
                return AttachmentResult.CreateError("Contact not found or access denied");
            }

            // Step 3: Prepare file paths
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueFileName = $"{contactId}_{Guid.NewGuid()}{fileExtension}";
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "contacts");
            var physicalFilePath = Path.Combine(uploadsPath, uniqueFileName);
            var relativeFilePath = $"/uploads/contacts/{uniqueFileName}";

            // Step 4: Save old attachment path for cleanup
            var oldAttachmentPath = contact.AttachmentPath;

            // Step 5: Execute atomic operation (file + database)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create directory if needed
                Directory.CreateDirectory(uploadsPath);

                // Save new file to disk
                await SaveFileToDiskAsync(file, physicalFilePath);

                // Update database with new attachment info
                await UpdateContactAttachmentAsync(
                    contactId,
                    userId,
                    relativeFilePath,
                    file.FileName,
                    GetAttachmentType(fileExtension),
                    file.Length
                );

                // Commit transaction
                await transaction.CommitAsync();

                // Clean up old file AFTER successful commit
                if (!string.IsNullOrEmpty(oldAttachmentPath))
                {
                    DeletePhysicalFile(oldAttachmentPath);
                }

                _logger.LogInformation(
                    "Attachment uploaded for contact {ContactId}: {FileName} ({Size} bytes)",
                    contactId, file.FileName, file.Length);

                return AttachmentResult.CreateSuccess(
                    contactId,
                    relativeFilePath,
                    file.FileName,
                    GetAttachmentType(fileExtension),
                    file.Length
                );
            }
            catch (Exception ex)
            {
                // Rollback transaction
                await transaction.RollbackAsync();

                // Clean up new file if it was created
                DeletePhysicalFile(relativeFilePath);

                _logger.LogError(ex, "Error uploading attachment for contact {ContactId}", contactId);
                return AttachmentResult.CreateError($"Upload failed: {ex.Message}");
            }
        }

        /// <summary>
        /// RESTRUCTURED: Delete attachment with atomic transaction
        /// </summary>
        public async Task<AttachmentResult> DeleteAttachmentAsync(int contactId, string userId)
        {
            // Get contact (no tracking)
            var contact = await _context.Contacts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contactId && c.UserId == userId);

            if (contact == null)
            {
                return AttachmentResult.CreateError("Contact not found or access denied");
            }

            if (string.IsNullOrEmpty(contact.AttachmentPath))
            {
                return AttachmentResult.CreateError("No attachment to delete");
            }

            var oldAttachmentPath = contact.AttachmentPath;

            // Execute atomic operation
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Clear attachment fields in database
                await ClearContactAttachmentAsync(contactId, userId);

                // Commit transaction
                await transaction.CommitAsync();

                // Delete physical file AFTER successful commit
                DeletePhysicalFile(oldAttachmentPath);

                _logger.LogInformation("Attachment deleted for contact {ContactId}", contactId);

                return AttachmentResult.CreateSuccess(contactId, null, null, null, 0, "Attachment deleted successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting attachment for contact {ContactId}", contactId);
                return AttachmentResult.CreateError($"Delete failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Get attachment information for a contact
        /// </summary>
        public async Task<AttachmentResult> GetAttachmentInfoAsync(int contactId, string userId)
        {
            var contact = await _context.Contacts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contactId && c.UserId == userId);

            if (contact == null)
            {
                return AttachmentResult.CreateError("Contact not found or access denied");
            }

            if (string.IsNullOrEmpty(contact.AttachmentPath))
            {
                return AttachmentResult.CreateSuccess(contactId, null, null, null, 0, "No attachment");
            }

            return AttachmentResult.CreateSuccess(
                contactId,
                contact.AttachmentPath,
                contact.AttachmentFileName,
                contact.AttachmentType,
                contact.AttachmentSize ?? 0,
                "Attachment found"
            );
        }

        /// <summary>
        /// PRIVATE: Update contact attachment fields in database (no tracking issues)
        /// </summary>
        private async Task UpdateContactAttachmentAsync(
            int contactId,
            string userId,
            string filePath,
            string fileName,
            string fileType,
            long fileSize)
        {
            // Execute raw SQL to avoid entity tracking issues
            await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE ""Contacts""
                SET
                    ""AttachmentPath"" = {0},
                    ""AttachmentFileName"" = {1},
                    ""AttachmentType"" = {2},
                    ""AttachmentSize"" = {3},
                    ""AttachmentUploadedAt"" = {4},
                    ""UpdatedAt"" = {5}
                WHERE ""Id"" = {6} AND ""UserId"" = {7}",
                filePath,
                fileName,
                fileType,
                fileSize,
                DateTime.UtcNow,
                DateTime.UtcNow,
                contactId,
                userId
            );
        }

        /// <summary>
        /// PRIVATE: Clear contact attachment fields in database (no tracking issues)
        /// </summary>
        private async Task ClearContactAttachmentAsync(int contactId, string userId)
        {
            // Execute raw SQL to avoid entity tracking issues
            await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE ""Contacts""
                SET
                    ""AttachmentPath"" = NULL,
                    ""AttachmentFileName"" = NULL,
                    ""AttachmentType"" = NULL,
                    ""AttachmentSize"" = NULL,
                    ""AttachmentUploadedAt"" = NULL,
                    ""UpdatedAt"" = {0}
                WHERE ""Id"" = {1} AND ""UserId"" = {2}",
                DateTime.UtcNow,
                contactId,
                userId
            );
        }

        /// <summary>
        /// PRIVATE: Save file to disk
        /// </summary>
        private async Task SaveFileToDiskAsync(IFormFile file, string physicalPath)
        {
            using var stream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(stream);
            await stream.FlushAsync();
        }

        /// <summary>
        /// PRIVATE: Delete physical file from disk (safe - no exception if not exists)
        /// </summary>
        private void DeletePhysicalFile(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return;

            try
            {
                var physicalPath = Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/'));

                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                    _logger.LogInformation("Deleted physical file: {FilePath}", physicalPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete physical file: {FilePath}", relativePath);
                // Don't throw - file cleanup failure shouldn't break the operation
            }
        }

        /// <summary>
        /// PRIVATE: Validate file input
        /// </summary>
        private AttachmentResult ValidateFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return AttachmentResult.CreateError("No file provided");
            }

            if (file.Length > _maxFileSize)
            {
                return AttachmentResult.CreateError($"File size exceeds maximum of {_maxFileSize / 1024 / 1024}MB");
            }

            if (!IsValidFileType(file.FileName))
            {
                return AttachmentResult.CreateError("Invalid file type. Supported: JPG, PNG, GIF, PDF, MP4, DOC, etc.");
            }

            return AttachmentResult.CreateSuccess(0, null, null, null, 0);
        }

        /// <summary>
        /// Check if file type is supported
        /// </summary>
        public bool IsValidFileType(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _supportedTypes.ContainsKey(extension);
        }

        /// <summary>
        /// Get attachment type from file extension
        /// </summary>
        public string GetAttachmentType(string extension)
        {
            return _supportedTypes.TryGetValue(extension.ToLowerInvariant(), out var type)
                ? type
                : "Unknown";
        }
    }

    /// <summary>
    /// RESTRUCTURED: Result of attachment operation
    /// </summary>
    public class AttachmentResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public int ContactId { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public long FileSize { get; set; }

        public static AttachmentResult CreateSuccess(
            int contactId,
            string? filePath,
            string? fileName,
            string? fileType,
            long fileSize,
            string? message = null)
        {
            return new AttachmentResult
            {
                Success = true,
                Message = message,
                ContactId = contactId,
                FilePath = filePath,
                FileName = fileName,
                FileType = fileType,
                FileSize = fileSize
            };
        }

        public static AttachmentResult CreateError(string errorMessage)
        {
            return new AttachmentResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
