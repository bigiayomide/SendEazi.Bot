using Bot.Infrastructure.Configuration;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Bot.Core.Services;

public interface ITextToSpeechService
{
    Task<Stream> SynthesizeAsync(string text, string localeCode);
}

public record SynthesisResult(ResultReason Reason, byte[] AudioData);

public interface ISpeechSynthesizer : IAsyncDisposable
{
    Task<SynthesisResult> SpeakTextAsync(string text);
    Task<IReadOnlyList<VoiceInfo>> GetVoicesAsync();
}

public interface ISpeechSynthesizerFactory
{
    ISpeechSynthesizer Create(SpeechConfig config);
}

public class SpeechSynthesizerWrapper(ISpeechSynthesizer synthesizer) : ISpeechSynthesizer
{
    public async Task<SynthesisResult> SpeakTextAsync(string text)
    {
        var result = await synthesizer.SpeakTextAsync(text);
        return new SynthesisResult(result.Reason, result.AudioData);
    }

    public async Task<IReadOnlyList<VoiceInfo>> GetVoicesAsync()
    {
        var result = await synthesizer.GetVoicesAsync();
        return result;
    }

    public ValueTask DisposeAsync()
    {
        return synthesizer.DisposeAsync();
    }
}

public class DefaultSpeechSynthesizerFactory : ISpeechSynthesizerFactory
{
    public ISpeechSynthesizer Create(SpeechConfig config)
    {
        return new SpeechSynthesizerWrapper(new MsSpeechSynthesizerWrapper(new SpeechSynthesizer(config, null)));
    }

    private class MsSpeechSynthesizerWrapper : ISpeechSynthesizer
    {
        private readonly SpeechSynthesizer _synthesizer;

        public MsSpeechSynthesizerWrapper(SpeechSynthesizer synthesizer)
        {
            _synthesizer = synthesizer;
        }

        public async Task<SynthesisResult> SpeakTextAsync(string text)
        {
            var result = await _synthesizer.SpeakTextAsync(text);
            return new SynthesisResult(result.Reason, result.AudioData);
        }

        public async Task<IReadOnlyList<VoiceInfo>> GetVoicesAsync()
        {
            var result = await _synthesizer.GetVoicesAsync();
            return result.Voices;
        }

        public ValueTask DisposeAsync()
        {
            _synthesizer.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>
///     Picks the best matching neural voice for a locale from the provided VoiceInfo list.
/// </summary>
public class VoicePicker
{
    private const string FallbackVoice = "en-US-JennyNeural";
    private readonly IReadOnlyList<VoiceInfo> _voices;

    public VoicePicker(IEnumerable<VoiceInfo>? voices)
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
    private readonly ISpeechSynthesizerFactory _factory;
    private readonly SpeechConfig _speechConfig;
    private readonly Func<string, string> _voiceSelector;

    public TextToSpeechService(IMemoryCache cache, IOptions<TextToSpeechOptions> options,
        ISpeechSynthesizerFactory? factory = null, Func<string, string>? voiceSelector = null)
    {
        if (string.IsNullOrWhiteSpace(options.Value.SubscriptionKey))
            throw new ArgumentNullException(nameof(options.Value.SubscriptionKey));
        if (string.IsNullOrWhiteSpace(options.Value.Region))
            throw new ArgumentNullException(nameof(options.Value.Region));

        _speechConfig = SpeechConfig.FromSubscription(options.Value.SubscriptionKey, options.Value.Region);
        _factory = factory ?? new DefaultSpeechSynthesizerFactory();

        _voiceSelector = voiceSelector ?? CreateDefaultVoiceSelector(cache).GetAwaiter().GetResult();
    }

    public async Task<Stream> SynthesizeAsync(string text, string localeCode)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentNullException(nameof(text));
        if (string.IsNullOrWhiteSpace(localeCode))
            throw new ArgumentNullException(nameof(localeCode));

        // Pick the best voice for this locale
        var voiceName = _voiceSelector(localeCode);

        // Configure synthesizer
        _speechConfig.SpeechSynthesisVoiceName = voiceName;

        // Perform synthesis to an in-memory stream
        await using var synthesizer = _factory.Create(_speechConfig);
        var result = await synthesizer.SpeakTextAsync(text);
        if (result.Reason != ResultReason.SynthesizingAudioCompleted)
            throw new InvalidOperationException($"TTS failed ({result.Reason}): {result}");

        // Return the raw audio bytes
        return new MemoryStream(result.AudioData);
    }

    private async Task<Func<string, string>> CreateDefaultVoiceSelector(IMemoryCache cache)
    {
        if (!cache.TryGetValue("voices_list", out IReadOnlyList<VoiceInfo>? voices))
        {
            await using var synth = _factory.Create(_speechConfig);
            voices = synth.GetVoicesAsync().GetAwaiter().GetResult();
            cache.Set("voices_list", voices, TimeSpan.FromDays(1));
        }

        var picker = new VoicePicker(voices);
        return picker.PickVoice;
    }
}