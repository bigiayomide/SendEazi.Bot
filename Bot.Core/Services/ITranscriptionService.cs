namespace Bot.Core.Services;

public interface ITranscriptionService
{
    Task<(string Text, string DetectedLanguage)> TranscribeAsync(Stream audioStream, string[] languageCodes);
}
