using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace Bot.Core.Services;

public interface ITranscriptionService
{
    Task<(string Text, string DetectedLanguage)> TranscribeAsync(Stream audioStream, string[] languageCodes);
}

public record RecognitionResult(ResultReason Reason, string Text, string DetectedLanguage);

public interface ISpeechRecognizer : IAsyncDisposable
{
    Task<RecognitionResult> RecognizeOnceAsync();
}

public interface ISpeechRecognizerFactory
{
    ISpeechRecognizer Create(SpeechConfig config, AutoDetectSourceLanguageConfig autoDetectConfig, AudioConfig audioConfig);
}

public class SpeechRecognizerWrapper(SpeechRecognizer recognizer) : ISpeechRecognizer
{
    public async Task<RecognitionResult> RecognizeOnceAsync()
    {
        var result = await recognizer.RecognizeOnceAsync();
        var lang = result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult);
        return new RecognitionResult(result.Reason, result.Text, lang);
    }

    public ValueTask DisposeAsync() => recognizer.DisposeAsync();
}

public class DefaultSpeechRecognizerFactory : ISpeechRecognizerFactory
{
    public ISpeechRecognizer Create(SpeechConfig config, AutoDetectSourceLanguageConfig autoDetectConfig, AudioConfig audioConfig)
        => new SpeechRecognizerWrapper(new SpeechRecognizer(config, autoDetectConfig, audioConfig));
}

public class TranscriptionService(string subscriptionKey, string region, ISpeechRecognizerFactory? factory = null) : ITranscriptionService
{
    private readonly SpeechConfig _speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
    private readonly ISpeechRecognizerFactory _factory = factory ?? new DefaultSpeechRecognizerFactory();

    public async Task<(string Text, string DetectedLanguage)> TranscribeAsync(Stream audioStream,
        string[] languageCodes)
    {
        var autoDetectConfig = AutoDetectSourceLanguageConfig.FromLanguages(languageCodes);

        // Push audio into SDK
        var pushStream = AudioInputStream.CreatePushStream();
        audioStream.Position = 0;
        // Manually read and write in chunks since PushAudioInputStream is not a Stream
        var buffer = new byte[8192];
        int bytesRead;
        while ((bytesRead = await audioStream.ReadAsync(buffer)) > 0) pushStream.Write(buffer[..bytesRead]);
        pushStream.Close();

        using var audioConfig = AudioConfig.FromStreamInput(pushStream);
        await using var recognizer = _factory.Create(_speechConfig, autoDetectConfig, audioConfig);
        var result = await recognizer.RecognizeOnceAsync();

        if (result.Reason == ResultReason.RecognizedSpeech)
        {
            return (result.Text, result.DetectedLanguage);
        }

        throw new InvalidOperationException($"Speech recognition failed: {result.Reason}");
    }
}