using Bot.Core.Services;
using Bot.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Caching.Memory;
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
        var service = new TextToSpeechService(cache, opts, factory.Object, _ => "voice");

        var stream = await service.SynthesizeAsync("hello", "en-US");
        var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);

        buffer.ToArray().Should().BeEquivalentTo(audio);
    }
}