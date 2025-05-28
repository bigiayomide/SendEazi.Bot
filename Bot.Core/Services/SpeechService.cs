// Bot.Core/Services/SpeechService.cs

using Microsoft.Extensions.Options;

namespace Bot.Core.Services;


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
