namespace CloudInteractive.CloudPos.Services;

public class ConfigurationService
{
    public readonly string StoreName;
    public readonly string WelcomeMessage;
    public readonly string ImageBaseUrl;

    public ConfigurationService(IConfiguration config)
    {
        StoreName = config.GetValue<string?>("StoreName") ?? "DefaultStore";
        ImageBaseUrl = config.GetValue<string?>("ImageBaseUrl") ?? "https://please-check-appsettings.json";
        WelcomeMessage = config.GetValue<string?>("WelcomeMessage") ?? "DefaultWelcomeMessage";
    }
    
}