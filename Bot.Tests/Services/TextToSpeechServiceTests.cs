using Bot.Core.Services;
using Bot.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Bot.Tests.Services;

public class TextToSpeechServiceTests
{
    [Fact]
    public async Task SynthesizeAsync_Returns_Audio_Stream()
    {
        var audio = new byte[] { 0x1, 0x2 };
        var mockSynth = new Mock<ISpeechSynthesizer>();
        mockSynth.Setup(s => s.SpeakTextAsync("hello"))
            .ReturnsAsync(new SynthesisResult(ResultReason.SynthesizingAudioCompleted, audio));
        mockSynth.Setup(s => s.GetVoicesAsync())
            .ReturnsAsync(Array.Empty<VoiceInfo>());
        mockSynth.Setup(s => s.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var factory = new Mock<ISpeechSynthesizerFactory>();
        factory.Setup(f => f.Create(It.IsAny<SpeechConfig>())).Returns(mockSynth.Object);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var opts = Options.Create(new TextToSpeechOptions { SubscriptionKey = "k", Region = "r" });
        var logger = new Mock<ILogger<TextToSpeechService>>();
        var service = new TextToSpeechService(cache, opts, logger.Object, factory.Object, _ => "voice");

        var stream = await service.SynthesizeAsync("hello", "en-US");
        var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);

        buffer.ToArray().Should().BeEquivalentTo(audio);
    }

    [Fact]
    public async Task SynthesizeAsync_Throws_And_Logs_On_Cancellation()
    {
        var mockSynth = new Mock<ISpeechSynthesizer>();
        mockSynth.Setup(s => s.SpeakTextAsync("bye"))
            .ReturnsAsync(new SynthesisResult(ResultReason.Canceled, []));
        mockSynth.Setup(s => s.GetVoicesAsync())
            .ReturnsAsync(Array.Empty<VoiceInfo>());
        mockSynth.Setup(s => s.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var factory = new Mock<ISpeechSynthesizerFactory>();
        factory.Setup(f => f.Create(It.IsAny<SpeechConfig>())).Returns(mockSynth.Object);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var opts = Options.Create(new TextToSpeechOptions { SubscriptionKey = "k", Region = "r" });
        var logger = new Mock<ILogger<TextToSpeechService>>();
        var service = new TextToSpeechService(cache, opts, logger.Object, factory.Object, _ => "voice");

        var act = async () => await service.SynthesizeAsync("bye", "en-US");
        await act.Should().ThrowAsync<InvalidOperationException>();

        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("TTS failed")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void Ctor_Throws_When_SubscriptionKey_Missing()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var opts = Options.Create(new TextToSpeechOptions { SubscriptionKey = "", Region = "r" });
        var logger = new Mock<ILogger<TextToSpeechService>>();

        var act = () => new TextToSpeechService(cache, opts, logger.Object);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_Throws_When_Region_Missing()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var opts = Options.Create(new TextToSpeechOptions { SubscriptionKey = "k", Region = "" });
        var logger = new Mock<ILogger<TextToSpeechService>>();

        var act = () => new TextToSpeechService(cache, opts, logger.Object);

        act.Should().Throw<ArgumentNullException>();
    }
}