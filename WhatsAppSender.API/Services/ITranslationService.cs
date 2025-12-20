namespace WhatsAppSender.API.Services;

public interface ITranslationService
{
    Task<string> TranslateAsync(string text, string targetLanguage, string sourceLanguage = "auto");
    Task<List<string>> GetSupportedLanguagesAsync();
    Task<string> DetectLanguageAsync(string text);
}
