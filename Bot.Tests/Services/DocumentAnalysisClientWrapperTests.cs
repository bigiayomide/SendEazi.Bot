using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Bot.Core.Services;
using FluentAssertions;
using Moq;
using Assert = Xunit.Assert;

namespace Bot.Tests.Services;

public class DocumentAnalysisClientWrapperTests
{
    [Fact]
    public async Task ExtractLinesAsync_Should_Flatten_Pages()
    {
        var line1 = DocumentAnalysisModelFactory.DocumentLine("hello");
        var line2 = DocumentAnalysisModelFactory.DocumentLine("world");
        var page1 = DocumentAnalysisModelFactory.DocumentPage(lines: new[] { line1 });
        var page2 = DocumentAnalysisModelFactory.DocumentPage(lines: new[] { line2 });
        var result = DocumentAnalysisModelFactory.AnalyzeResult(pages: new[] { page1, page2 });
        var operation = new FakeOperation(result);

        var client = new Mock<DocumentAnalysisClient>();
        client.Setup(c =>
                c.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", It.IsAny<Stream>(), null,
                    CancellationToken.None))
            .ReturnsAsync(operation);

        var wrapper = new DocumentAnalysisClientWrapper(client.Object);
        await using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var lines = await wrapper.ExtractLinesAsync(ms);

        lines.Should().BeEquivalentTo("hello", "world");
    }

    [Fact]
    public async Task ExtractLinesAsync_Should_Return_Empty_When_No_Pages()
    {
        var result = DocumentAnalysisModelFactory.AnalyzeResult(pages: Array.Empty<DocumentPage>());
        var operation = new FakeOperation(result);

        var client = new Mock<DocumentAnalysisClient>();
        client.Setup(c =>
                c.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", It.IsAny<Stream>(), null,
                    CancellationToken.None))
            .ReturnsAsync(operation);

        var wrapper = new DocumentAnalysisClientWrapper(client.Object);
        await using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var lines = await wrapper.ExtractLinesAsync(ms);

        lines.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractLinesAsync_Should_Throw_When_Client_Fails()
    {
        var expected = new InvalidOperationException("fail");

        var client = new Mock<DocumentAnalysisClient>();
        client.Setup(c =>
                c.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", It.IsAny<Stream>(), null,
                    CancellationToken.None))
            .ThrowsAsync(expected);

        var wrapper = new DocumentAnalysisClientWrapper(client.Object);
        await using var ms = new MemoryStream(new byte[] { 1, 2, 3 });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => wrapper.ExtractLinesAsync(ms));
        ex.Should().BeSameAs(expected);
    }

    private class FakeOperation : AnalyzeDocumentOperation
    {
        private readonly AnalyzeResult _value;

        public FakeOperation(AnalyzeResult value)
        {
            _value = value;
        }

        public override string Id => "id";
        public override AnalyzeResult Value => _value;
        public override bool HasCompleted => true;
        public override bool HasValue => true;

        public override Response GetRawResponse()
        {
            return new Mock<Response>().Object;
        }

        public override ValueTask<Response> UpdateStatusAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask<Response>(GetRawResponse());
        }

        public override Response UpdateStatus(CancellationToken cancellationToken = default)
        {
            return GetRawResponse();
        }

        public override ValueTask<Response<AnalyzeResult>> WaitForCompletionAsync(
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<Response<AnalyzeResult>>(Response.FromValue(_value, GetRawResponse()));
        }

        public override ValueTask<Response<AnalyzeResult>> WaitForCompletionAsync(TimeSpan pollingInterval,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<Response<AnalyzeResult>>(Response.FromValue(_value, GetRawResponse()));
        }
    }
}