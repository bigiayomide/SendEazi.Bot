// Bot.Infrastructure.Configuration/SpeechOptions.cs

namespace Bot.Infrastructure.Configuration;

public class SpeechOptions
{
    /// <summary>Set of BCP-47 codes passed to AutoDetectSourceLanguageConfig.</summary>
    public string[] SupportedLanguages { get; set; } = Array.Empty<string>();
}