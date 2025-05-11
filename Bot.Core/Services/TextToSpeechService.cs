using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Caching.Memory;

namespace Bot.Core.Services;

public interface ITextToSpeechService
{
    Task<Stream> SynthesizeAsync(string text, string localeCode);
}

/// <summary>
///     Picks the best matching neural voice for a locale from the provided VoiceInfo list.
/// </summary>
public class VoicePicker
{
    private const string FallbackVoice = "en-US-JennyNeural";
    private readonly IReadOnlyList<VoiceInfo> _voices;

    public VoicePicker(IEnumerable<VoiceInfo> voices)
    {
        if (voices == null) throw new ArgumentNullException(nameof(voices));
        _voices = voices.ToList();
    }

    public string PickVoice(string localeCode)
    {
        // 1) Exact locale match, e.g. "yo-NG" or "ig-NG"
        var exact = _voices.FirstOrDefault(v =>
            v.Locale.Equals(localeCode, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
            return exact.ShortName;

        // 2) Language-prefix match, e.g. "yo" or "ig"
        var lang = localeCode.Split('-')[0];
        var prefix = _voices.FirstOrDefault(v =>
            v.Locale.StartsWith(lang, StringComparison.OrdinalIgnoreCase));
        if (prefix != null)
            return prefix.ShortName;

        // 3) Fallback to a known voice
        return FallbackVoice;
    }
}

public class TextToSpeechService : ITextToSpeechService
{
    private readonly VoicePicker _picker;
    private readonly SpeechConfig _speechConfig;

    public TextToSpeechService(string subscriptionKey, string region, IMemoryCache cache)
    {
        if (string.IsNullOrWhiteSpace(subscriptionKey))
            throw new ArgumentNullException(nameof(subscriptionKey));
        if (string.IsNullOrWhiteSpace(region))
            throw new ArgumentNullException(nameof(region));

        _speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);

        // Load and cache the voices list on first use
        if (!cache.TryGetValue("voices_list", out IReadOnlyList<VoiceInfo> voices))
        {
            using var tempSynthesizer = new SpeechSynthesizer(_speechConfig, null);
            var result = tempSynthesizer.GetVoicesAsync().GetAwaiter().GetResult(); // sync for startup
            voices = result
                .Voices; // SynthesisVoicesResult.Voices is List<VoiceInfo> :contentReference[oaicite:2]{index=2}
            cache.Set("voices_list", voices, TimeSpan.FromDays(1));
        }

        _picker = new VoicePicker(voices);
    }

    public async Task<Stream> SynthesizeAsync(string text, string localeCode)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentNullException(nameof(text));
        if (string.IsNullOrWhiteSpace(localeCode))
            throw new ArgumentNullException(nameof(localeCode));

        // Pick the best voice for this locale
        var voiceName = _picker.PickVoice(localeCode);

        // Configure synthesizer
        _speechConfig.SpeechSynthesisVoiceName = voiceName;

        // Perform synthesis to an in-memory stream
        using var synthesizer = new SpeechSynthesizer(_speechConfig, null);
        var result =
            await synthesizer
                .SpeakTextAsync(text); // returns SpeechSynthesisResult :contentReference[oaicite:3]{index=3}
        if (result.Reason != ResultReason.SynthesizingAudioCompleted)
            throw new InvalidOperationException($"TTS failed ({result.Reason}): {result}");

        // Return the raw audio bytes
        return new MemoryStream(result.AudioData);
    }
}