namespace Bot.Core.Services;

public interface ISpeechRecognizer : IAsyncDisposable
{
    Task<RecognitionResult> RecognizeOnceAsync();
}
