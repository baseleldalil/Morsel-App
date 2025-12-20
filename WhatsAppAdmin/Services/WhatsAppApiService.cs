using System;
using System.Text;
using System.Text.Json;
using WhatsAppAdmin.Models.API;

namespace WhatsAppAdmin.Services
{
    public interface IWhatsAppApiService
    {
        Task<UserApiKey?> CreateApiKeyAsync(string userEmail, int subscriptionId, string name);
        Task<List<UserApiKey>> GetUserApiKeysAsync(string userEmail);
        Task<bool> RevokeApiKeyAsync(int apiKeyId, string userEmail);
        Task<SendMessageResult> SendMessagesAsync(string apiKey, SendBulkMessageRequest request);
        Task<ApiUsageResponse?> GetUsageStatsAsync(string apiKey);
        Task<bool> TestApiConnectionAsync(string apiKey);
        Task<string?> AssignSubscriptionAsync(int userId, string userEmail, int subscriptionId, DateTime? expiresAt, bool isActive, string? password);
    }

    public class WhatsAppApiService : IWhatsAppApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WhatsAppApiService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl;

        public WhatsAppApiService(HttpClient httpClient, ILogger<WhatsAppApiService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _apiBaseUrl = _configuration["WhatsAppAPI:BaseUrl"] ?? "https://localhost:7001/api";
        }

        public async Task<UserApiKey?> CreateApiKeyAsync(string userEmail, int subscriptionId, string name)
        {
            try
            {
                var request = new
                {
                    UserEmail = userEmail,
                    SubscriptionId = subscriptionId,
                    Name = name
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/apikeys/create", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<dynamic>(responseJson);

                    // Parse the response and create UserApiKey
                    return new UserApiKey
                    {
                        KeyValue = result?.GetProperty("apiKey").GetString() ?? "",
                        Name = result?.GetProperty("name").GetString() ?? "",
                        CreatedAt = DateTime.TryParse(result?.GetProperty("createdAt").GetString(), out DateTime createdAt) ? createdAt : DateTime.Now,
                        ExpiresAt = DateTime.TryParse(result?.GetProperty("expiresAt").GetString(), out DateTime expiresAt) ? expiresAt : null,
                        IsActive = true
                    };
                }
                else
                {
                    _logger.LogError("Failed to create API key. Status: {StatusCode}", response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API key for user: {UserEmail}", userEmail);
                return null;
            }
        }

        public async Task<List<UserApiKey>> GetUserApiKeysAsync(string userEmail)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/apikeys/user/{userEmail}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(json);
                    var apiKeysArray = document.RootElement.GetProperty("apiKeys");

                    var apiKeys = new List<UserApiKey>();
                    foreach (var keyElement in apiKeysArray.EnumerateArray())
                    {
                        apiKeys.Add(new UserApiKey
                        {
                            Id = keyElement.GetProperty("id").GetInt32(),
                            Name = keyElement.GetProperty("name").GetString() ?? "",
                            KeyPreview = keyElement.GetProperty("keyPreview").GetString() ?? "",
                            SubscriptionPlan = keyElement.GetProperty("subscriptionPlan").GetString() ?? "",
                            IsActive = keyElement.GetProperty("isActive").GetBoolean(),
                            UsageCount = keyElement.GetProperty("usageCount").GetInt32(),
                            DailyUsageCount = keyElement.GetProperty("dailyUsageCount").GetInt32(),
                            LastUsedAt = keyElement.TryGetProperty("lastUsedAt", out var lastUsed) && lastUsed.ValueKind != JsonValueKind.Null
                                ? DateTime.Parse(lastUsed.GetString()!) : null,
                            CreatedAt = DateTime.Parse(keyElement.GetProperty("createdAt").GetString()!),
                            ExpiresAt = keyElement.TryGetProperty("expiresAt", out var expires) && expires.ValueKind != JsonValueKind.Null
                                ? DateTime.Parse(expires.GetString()!) : null
                        });
                    }

                    return apiKeys;
                }

                return new List<UserApiKey>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API keys for user: {UserEmail}", userEmail);
                return new List<UserApiKey>();
            }
        }

        public async Task<bool> RevokeApiKeyAsync(int apiKeyId, string userEmail)
        {
            try
            {
                var request = new
                {
                    ApiKeyId = apiKeyId,
                    UserEmail = userEmail
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/apikeys/revoke", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking API key {ApiKeyId} for user: {UserEmail}", apiKeyId, userEmail);
                return false;
            }
        }

        public async Task<SendMessageResult> SendMessagesAsync(string apiKey, SendBulkMessageRequest request)
        {
            try
            {
                // Convert request to API format
                var apiRequest = new
                {
                    Messages = request.Messages.Select(m => new
                    {
                        Phone = m.Phone,
                        Messages = m.Messages,
                        FileBase64 = m.FileBase64,
                        FileName = m.FileName,
                        FileType = m.FileType
                    }).ToList(),
                    DelayBetweenMessages = request.DelayBetweenMessages,
                    SendImmediately = request.SendImmediately
                };

                var json = JsonSerializer.Serialize(apiRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Remove("X-API-Key");
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);

                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/whatsapp/send", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                using var document = JsonDocument.Parse(responseJson);
                var root = document.RootElement;

                return new SendMessageResult
                {
                    Success = root.GetProperty("success").GetBoolean(),
                    Message = root.GetProperty("message").GetString() ?? "",
                    ProcessedCount = root.TryGetProperty("processedCount", out var pc) ? pc.GetInt32() : 0,
                    FailedCount = root.TryGetProperty("failedCount", out var fc) ? fc.GetInt32() : 0,
                    RemainingQuota = root.TryGetProperty("remainingQuota", out var rq) ? rq.GetInt32() : 0,
                    Errors = root.TryGetProperty("errors", out var errors)
                        ? errors.EnumerateArray().Select(e => e.GetString() ?? "").ToList()
                        : new List<string>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp messages");
                return new SendMessageResult
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiUsageResponse?> GetUsageStatsAsync(string apiKey)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("X-API-Key");
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/whatsapp/usage");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(json);
                    var root = document.RootElement;

                    return new ApiUsageResponse
                    {
                        TotalMessages = root.GetProperty("totalMessages").GetInt32(),
                        TodayMessages = root.GetProperty("todayMessages").GetInt32(),
                        RemainingQuota = root.GetProperty("remainingQuota").GetInt32(),
                        LastUsed = DateTime.Parse(root.GetProperty("lastUsed").GetString()!),
                        SubscriptionPlan = root.GetProperty("subscriptionPlan").GetString() ?? ""
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage stats");
                return null;
            }
        }

        public async Task<bool> TestApiConnectionAsync(string apiKey)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("X-API-Key");
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/whatsapp/test");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing API connection");
                return false;
            }
        }

        public async Task<string?> AssignSubscriptionAsync(int userId, string userEmail, int subscriptionId, DateTime? expiresAt, bool isActive, string? password)
        {
            try
            {

                _logger.LogInformation("Preparing request - UserId: {UserIdInt} (from '{UserId}'), Email: {Email}, SubId: {SubId}",
                    userId, userId, userEmail, subscriptionId);

                var request = new
                {
                    UserId = userId,
                    UserEmail = userEmail,
                    SubscriptionId = subscriptionId,
                    ExpiresAt = expiresAt,
                    IsActive = isActive,
                    Password = password
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending request to API: {Json}", json);

                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/admin/subscriptions/assign", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(responseJson);
                    var root = document.RootElement;

                    var returnedUserId = root.GetProperty("userId").ToString();

                    _logger.LogInformation("Successfully assigned subscription {SubscriptionId} to user {UserEmail}, UserId: {UserId}",
                        subscriptionId, userEmail, returnedUserId);

                    return returnedUserId;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to assign subscription. Status: {StatusCode}, Error: {Error}, Request JSON: {Json}",
                        response.StatusCode, errorContent, json);
                    throw new Exception($"API Error: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning subscription to user: {UserEmail}", userEmail);
                return null;
            }
        }
    }
}