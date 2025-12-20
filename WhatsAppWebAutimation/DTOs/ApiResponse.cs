namespace WhatsAppWebAutomation.DTOs;

/// <summary>
/// Standard API response wrapper
/// </summary>
/// <typeparam name="T">Type of data being returned</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> SuccessResponse(T data, string message = "Operation completed successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> ErrorResponse(string error, string message = "Operation failed")
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Error = error
        };
    }
}

/// <summary>
/// Result of sending a single message
/// </summary>
public class SendResultDto
{
    public string Phone { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int AttachmentsSent { get; set; }
    public int DelayAppliedSeconds { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result of bulk message sending operation
/// </summary>
public class BulkResultDto
{
    public int TotalContacts { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
    public int BreaksTaken { get; set; }
    public double TotalTimeMinutes { get; set; }
    public List<SendResultDto> Results { get; set; } = new();
}

/// <summary>
/// WhatsApp browser and login status
/// </summary>
public class StatusResultDto
{
    public bool BrowserOpen { get; set; }
    public bool LoggedIn { get; set; }
    public string BrowserType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
