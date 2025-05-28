namespace Bot.Core.Services;

public interface ISpeechService
{
    Task<(string Text, string DetectedLanguage)> TranscribeAsync(Stream audioStream);
}
