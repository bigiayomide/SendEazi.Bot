using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace Bot.Core.Services;

public class DefaultSpeechRecognizerFactory : ISpeechRecognizerFactory
{
    public ISpeechRecognizer Create(SpeechConfig config, AutoDetectSourceLanguageConfig autoDetectConfig,
        AudioConfig audioConfig)
    {
        return new SpeechRecognizerWrapper(new SpeechRecognizer(config, autoDetectConfig, audioConfig));
    }
}
