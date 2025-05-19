using Bot.Core.Services;
using FluentAssertions;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Moq;
using RecognitionResult = Bot.Core.Services.RecognitionResult;

namespace Bot.Tests.Services;

public class TranscriptionServiceTests
{
    [Fact]
    public async Task TranscribeAsync_Returns_Text_And_Language()
    {
        var recogResult = new RecognitionResult(ResultReason.RecognizedSpeech, "hi", "en-US");
        var mockRecognizer = new Mock<ISpeechRecognizer>();
        mockRecognizer.Setup(r => r.RecognizeOnceAsync()).ReturnsAsync(recogResult);
        mockRecognizer.Setup(r => r.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var factory = new Mock<ISpeechRecognizerFactory>();
        factory.Setup(f => f.Create(It.IsAny<SpeechConfig>(), It.IsAny<AutoDetectSourceLanguageConfig>(),
                It.IsAny<AudioConfig>()))
            .Returns(mockRecognizer.Object);

        var service = new TranscriptionService("key", "region", factory.Object);
        await using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await service.TranscribeAsync(ms, new[] { "en-US" });

        result.Text.Should().Be("hi");
        result.DetectedLanguage.Should().Be("en-US");
    }
}