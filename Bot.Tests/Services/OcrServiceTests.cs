using Bot.Core.Services;
using FluentAssertions;
using Moq;

namespace Bot.Tests.Services;

public class OcrServiceTests
{
    [Fact]
    public async Task ExtractTextAsync_Joins_Lines()
    {
        var mockClient = new Mock<IDocumentAnalysisClient>();
        mockClient.Setup(x => x.ExtractLinesAsync(It.IsAny<Stream>()))
            .ReturnsAsync(new[] { "hello", "world" });

        var service = new OcrService(mockClient.Object);
        await using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await service.ExtractTextAsync(ms);

        result.Should().Be("hello world");
    }
}