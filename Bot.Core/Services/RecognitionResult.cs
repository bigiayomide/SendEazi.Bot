using Microsoft.CognitiveServices.Speech;

namespace Bot.Core.Services;

public record RecognitionResult(ResultReason Reason, string Text, string DetectedLanguage);
