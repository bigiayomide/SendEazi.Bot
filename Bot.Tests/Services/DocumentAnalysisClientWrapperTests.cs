using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Bot.Core.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bot.Tests.Services;

public class DocumentAnalysisClientWrapperTests
{
    private class FakeOperation : Operation<AnalyzeResult>
    {
        private readonly AnalyzeResult _value;
        public FakeOperation(AnalyzeResult value) => _value = value;
        public override string Id => "id";
        public override AnalyzeResult Value => _value;
        public override bool HasCompleted => true;
        public override bool HasValue => true;
        public override Response GetRawResponse() => new Mock<Response>().Object;
        public override ValueTask<Response> UpdateStatusAsync(CancellationToken cancellationToken = default) => new(GetRawResponse());
        public override Response UpdateStatus(CancellationToken cancellationToken = default) => GetRawResponse();
        public override ValueTask<Response<AnalyzeResult>> WaitForCompletionAsync(CancellationToken cancellationToken = default) => new(Response.FromValue(_value, GetRawResponse()));
        public override ValueTask<Response<AnalyzeResult>> WaitForCompletionAsync(TimeSpan pollingInterval, CancellationToken cancellationToken = default) => new(Response.FromValue(_value, GetRawResponse()));
    }

    [Fact]
    public async Task ExtractLinesAsync_Should_Flatten_Pages()
    {
        var line1 = DocumentAnalysisModelFactory.DocumentLine(content: "hello");
        var line2 = DocumentAnalysisModelFactory.DocumentLine(content: "world");
        var page1 = DocumentAnalysisModelFactory.DocumentPage(lines: new[] { line1 });
        var page2 = DocumentAnalysisModelFactory.DocumentPage(lines: new[] { line2 });
        var result = DocumentAnalysisModelFactory.AnalyzeResult(pages: new[] { page1, page2 });
        var operation = new FakeOperation(result);

        var client = new Mock<DocumentAnalysisClient>();
        client.Setup(c => c.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", It.IsAny<Stream>(), default))
            .ReturnsAsync(operation);

        var wrapper = new DocumentAnalysisClientWrapper(client.Object);
        await using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var lines = await wrapper.ExtractLinesAsync(ms);

        lines.Should().BeEquivalentTo(new[] { "hello", "world" });
    }
}
