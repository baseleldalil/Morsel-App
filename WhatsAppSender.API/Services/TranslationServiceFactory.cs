namespace WhatsAppSender.API.Services;

public class TranslationServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public TranslationServiceFactory(
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public ITranslationService CreateTranslationService()
    {
        var provider = _configuration["Translation:Provider"] ?? "LibreTranslate";

        return provider.ToLower() switch
        {
            "libretranslate" => _serviceProvider.GetRequiredService<LibreTranslateService>(),
            _ => _serviceProvider.GetRequiredService<LibreTranslateService>() // Default to LibreTranslate
        };
    }
}
