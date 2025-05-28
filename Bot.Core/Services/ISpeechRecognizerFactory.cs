using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace Bot.Core.Services;

public interface ISpeechRecognizerFactory
{
    ISpeechRecognizer Create(SpeechConfig config, AutoDetectSourceLanguageConfig autoDetectConfig,
        AudioConfig audioConfig);
}
