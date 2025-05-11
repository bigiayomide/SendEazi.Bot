// Bot.Core/Services/SpeechService.cs

using Microsoft.Extensions.Options;

namespace Bot.Core.Services;

/// <summary>
///     Configuration for speech transcription: which languages to auto-detect.
/// </summary>
public class SpeechOptions
{
    /// <summary>
    ///     A list of BCP-47 codes the TranscriptionService will try to auto-detect.
    ///     E.g. "en-US","ig-NG","yo-NG","ha-NG","pcm-NG" (Nigerian Pidgin).
    /// </summary>
    public string[] SupportedLanguages { get; set; } = new[]
    {
        "en-US",
        "ig-NG",
        "yo-NG",
        "ha-NG",
        "pcm-NG"
    };
}

public interface ISpeechService
{
    Task<(string Text, string DetectedLanguage)> TranscribeAsync(Stream audioStream);
}

public class SpeechService(
    ITranscriptionService transcriber,
    IOptions<SpeechOptions> opts)
    : ISpeechService
{
    private readonly string[] _languageCodes = opts.Value.SupportedLanguages;

    public Task<(string Text, string DetectedLanguage)> TranscribeAsync(Stream audioStream)
    {
        return transcriber.TranscribeAsync(audioStream, _languageCodes);
    }
}
