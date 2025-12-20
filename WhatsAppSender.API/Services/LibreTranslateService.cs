using GTranslate.Translators;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using GTranslate.Translators;
using WhatsAppSender.API.Models;

namespace WhatsAppSender.API.Services;

public class LibreTranslateService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LibreTranslateService> _logger;
    private readonly TranslationSettings _settings;
    private readonly string _baseUrl;
    private readonly string? _apiKey;

    public LibreTranslateService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<LibreTranslateService> logger,
        IOptions<TranslationSettings> settings)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _settings = settings.Value;

        // Use public LibreTranslate instance or custom URL from configuration
        _baseUrl = _configuration["Translation:LibreTranslate:BaseUrl"]
            ?? "https://libretranslate.com";

        // API key is optional for public instance but recommended to avoid rate limits
        _apiKey = _configuration["Translation:LibreTranslate:ApiKey"];

        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.HttpClientTimeoutSeconds);
    }

    public async Task<string> TranslateAsync(string text, string targetLanguage, string sourceLanguage = "auto")
    {
        try
        {
            // Create an instance of the Google Translator
            var translator = new GoogleTranslator();

            var result = await translator.TranslateAsync(text, targetLanguage);

            return result.Translation;
        }
        catch (HttpRequestException ex)
        {
            // Handle HTTP errors (including 502 Bad Gateway) gracefully
            _logger.LogWarning(ex,
                "HTTP error during translation (Status: {StatusCode}). Returning original text: {Text}",
                ex.StatusCode, text);

            // Return original text instead of throwing - allows import to continue
            return text;
        }
        catch (TaskCanceledException ex)
        {
            // Handle timeout errors
            _logger.LogWarning(ex,
                "Translation timeout. Returning original text: {Text}", text);

            // Return original text instead of throwing
            return text;
        }
        catch (Exception ex)
        {
            // Handle any other errors
            _logger.LogWarning(ex,
                "Translation failed. Returning original text: {Text}", text);

            // Return original text instead of throwing - allows import to continue
            return text;
        }
    }

    public async Task<List<string>> GetSupportedLanguagesAsync()
    {
        try
        {
            var url = string.IsNullOrEmpty(_apiKey)
                ? "/languages"
                : $"/languages?api_key={_apiKey}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to get supported languages: {response.StatusCode}");
                throw new Exception($"Failed to get supported languages: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var languages = JsonSerializer.Deserialize<List<LanguageInfo>>(json);

            return languages?.Select(l => l.Code).ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported languages");
            throw;
        }
    }

    public async Task<string> DetectLanguageAsync(string text)
    {
        try
        {
            var requestBody = new
            {
                q = text,
                api_key = _apiKey
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/detect", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"LibreTranslate detect API error: {response.StatusCode} - {error}");
                throw new Exception($"Language detection failed: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<DetectionResponse>>(responseJson);

            if (result == null || result.Count == 0)
            {
                throw new Exception("Language detection response was empty");
            }

            // Return the most confident detection
            return result.OrderByDescending(r => r.Confidence).First().Language;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during language detection");
            throw;
        }
    }

    private class TranslationResponse
    {
        public string TranslatedText { get; set; } = string.Empty;
    }

    private class LanguageInfo
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private class DetectionResponse
    {
        public double Confidence { get; set; }
        public string Language { get; set; } = string.Empty;
    }
}
