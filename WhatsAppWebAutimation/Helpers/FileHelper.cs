namespace WhatsAppWebAutomation.Helpers;

/// <summary>
/// Helper class for file operations related to attachments
/// </summary>
public static class FileHelper
{
    private static readonly string TempUploadDirectory = Path.Combine(Path.GetTempPath(), "WhatsAppUploads");

    /// <summary>
    /// Initialize temp upload directory
    /// </summary>
    static FileHelper()
    {
        if (!Directory.Exists(TempUploadDirectory))
        {
            Directory.CreateDirectory(TempUploadDirectory);
        }
    }

    /// <summary>
    /// Convert base64 string to a temporary file
    /// </summary>
    /// <param name="base64Content">Base64 encoded file content</param>
    /// <param name="fileName">Original file name with extension</param>
    /// <returns>Full path to the temporary file</returns>
    public static async Task<string> SaveBase64ToTempFileAsync(string base64Content, string fileName)
    {
        // Remove data URL prefix if present (e.g., "data:image/png;base64,")
        if (base64Content.Contains(','))
        {
            base64Content = base64Content.Split(',')[1];
        }

        // Clean the base64 string (remove whitespace)
        base64Content = base64Content.Trim();

        byte[] fileBytes = Convert.FromBase64String(base64Content);

        // Ensure temp directory exists
        if (!Directory.Exists(TempUploadDirectory))
        {
            Directory.CreateDirectory(TempUploadDirectory);
        }

        // Generate unique file name to avoid conflicts
        var uniqueFileName = $"{Guid.NewGuid()}_{SanitizeFileName(fileName)}";
        var tempFilePath = Path.Combine(TempUploadDirectory, uniqueFileName);

        await File.WriteAllBytesAsync(tempFilePath, fileBytes);

        return tempFilePath;
    }

    /// <summary>
    /// Delete a temporary file
    /// </summary>
    /// <param name="filePath">Path to the file to delete</param>
    public static void DeleteTempFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Ignore deletion errors for temp files
        }
    }

    /// <summary>
    /// Clean up all temporary files older than specified minutes
    /// </summary>
    /// <param name="olderThanMinutes">Delete files older than this many minutes</param>
    public static void CleanupOldTempFiles(int olderThanMinutes = 60)
    {
        try
        {
            if (!Directory.Exists(TempUploadDirectory))
                return;

            var cutoffTime = DateTime.UtcNow.AddMinutes(-olderThanMinutes);
            var files = Directory.GetFiles(TempUploadDirectory);

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTimeUtc < cutoffTime)
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                    // Ignore individual file deletion errors
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Sanitize file name to remove invalid characters
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }

    /// <summary>
    /// Get the temp upload directory path
    /// </summary>
    public static string GetTempUploadDirectory() => TempUploadDirectory;
}
