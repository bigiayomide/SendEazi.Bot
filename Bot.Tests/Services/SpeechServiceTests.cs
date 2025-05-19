using Bot.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Bot.Tests.Services;

public class SpeechServiceTests
{
    [Fact]
    public async Task TranscribeAsync_Calls_Transcriber_With_Configured_Languages()
    {
        // Arrange
        var expectedResult = ("hello", "en-US");
        var transcriber = new Mock<ITranscriptionService>();
        Stream? receivedStream = null;
        string[]? receivedLangs = null;
        transcriber.Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string[]>()))
            .Callback<Stream, string[]>((s, l) =>
            {
                receivedStream = s;
                receivedLangs = l;
            })
            .ReturnsAsync(expectedResult);

        var options = Options.Create(new SpeechOptions { SupportedLanguages = new[] { "en-US", "ig-NG" } });
        var service = new SpeechService(transcriber.Object, options);
        await using var ms = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var result = await service.TranscribeAsync(ms);

        // Assert
        receivedStream.Should().BeSameAs(ms);
        receivedLangs.Should().BeSameAs(options.Value.SupportedLanguages);
        result.Should().Be(expectedResult);
    }
}