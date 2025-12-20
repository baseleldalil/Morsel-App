using WhatsAppAdmin.Models;

namespace WhatsAppAdmin.Services
{
    /// <summary>
    /// Service for handling file uploads with support for multiple file types
    /// Supports images, videos, documents, and spreadsheets
    /// </summary>
    public interface IFileUploadService
    {
        Task<List<TemplateAttachment>> ProcessUploadsAsync(IList<IFormFile> files, int templateId);
        Task<bool> DeleteFileAsync(int attachmentId);
        string GetAttachmentTypeFromExtension(string extension);
        bool IsValidFileType(string fileName);
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;

        private readonly Dictionary<string, string> _supportedTypes = new()
        {
            // Images
            { ".jpg", "Image" }, { ".jpeg", "Image" }, { ".png", "Image" }, { ".gif", "Image" }, { ".bmp", "Image" },
            // Videos
            { ".mp4", "Video" }, { ".mov", "Video" }, { ".avi", "Video" }, { ".wmv", "Video" }, { ".mkv", "Video" },
            // Documents
            { ".pdf", "Document" }, { ".doc", "Document" }, { ".docx", "Document" }, { ".txt", "Document" },
            // Spreadsheets
            { ".csv", "Spreadsheet" }, { ".xls", "Spreadsheet" }, { ".xlsx", "Spreadsheet" }
        };

        private readonly long _maxFileSize = 50 * 1024 * 1024; // 50MB

        public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<List<TemplateAttachment>> ProcessUploadsAsync(IList<IFormFile> files, int templateId)
        {
            var attachments = new List<TemplateAttachment>();

            if (files == null || !files.Any())
                return attachments;

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "templates");
            Directory.CreateDirectory(uploadsPath);

            foreach (var file in files)
            {
                if (file.Length > 0 && IsValidFileType(file.FileName) && file.Length <= _maxFileSize)
                {
                    try
                    {
                        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        var fileName = $"{Guid.NewGuid()}{fileExtension}";
                        var filePath = Path.Combine(uploadsPath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var attachment = new TemplateAttachment
                        {
                            CampaignTemplateId = templateId,
                            FileName = file.FileName,
                            FilePath = $"/uploads/templates/{fileName}",
                            FileType = file.ContentType,
                            FileExtension = fileExtension,
                            FileSize = file.Length,
                            AttachmentType = GetAttachmentTypeFromExtension(fileExtension),
                            CreatedAt = DateTime.UtcNow
                        };

                        attachments.Add(attachment);

                        _logger.LogInformation("File uploaded successfully: {FileName} for template {TemplateId}",
                            file.FileName, templateId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid file or size too large: {FileName} (Size: {FileSize})",
                        file.FileName, file.Length);
                }
            }

            return attachments;
        }

        public async Task<bool> DeleteFileAsync(int attachmentId)
        {
            try
            {
                // This would typically involve database lookup and file deletion
                // For now, we'll just return true
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file with attachment ID: {AttachmentId}", attachmentId);
                return false;
            }
        }

        public string GetAttachmentTypeFromExtension(string extension)
        {
            return _supportedTypes.TryGetValue(extension.ToLowerInvariant(), out var type) ? type : "Unknown";
        }

        public bool IsValidFileType(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _supportedTypes.ContainsKey(extension);
        }
    }
}