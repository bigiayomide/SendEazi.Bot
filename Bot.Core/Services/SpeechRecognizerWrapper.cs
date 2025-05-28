using Microsoft.CognitiveServices.Speech;

namespace Bot.Core.Services;

public class SpeechRecognizerWrapper(SpeechRecognizer recognizer) : ISpeechRecognizer
{
    public async Task<RecognitionResult> RecognizeOnceAsync()
    {
        var result = await recognizer.RecognizeOnceAsync();
        var lang = result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult);
        return new RecognitionResult(result.Reason, result.Text, lang);
    }

    public ValueTask DisposeAsync()
    {
        recognizer.Dispose();
        return ValueTask.CompletedTask;
    }
}
