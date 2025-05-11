// Bot.Infrastructure.Configuration/TextToSpeechOptions.cs

namespace Bot.Infrastructure.Configuration;

public class TextToSpeechOptions
{
    /// <summary>Azure Speech resource key.</summary>
    public string SubscriptionKey { get; set; } = null!;

    /// <summary>Azure region, e.g. "westeurope".</summary>
    public string Region { get; set; } = null!;

    /// <summary>How long the cached voices list stays in memory.</summary>
    public TimeSpan VoiceListTtl { get; set; } = TimeSpan.FromDays(1);
}