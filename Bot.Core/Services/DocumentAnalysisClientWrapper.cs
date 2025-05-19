using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;

namespace Bot.Core.Services;

public interface IDocumentAnalysisClient
{
    Task<IEnumerable<string>> ExtractLinesAsync(Stream stream);
}

public class DocumentAnalysisClientWrapper(DocumentAnalysisClient client) : IDocumentAnalysisClient
{
    public async Task<IEnumerable<string>> ExtractLinesAsync(Stream stream)
    {
        var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", stream);
        var result = operation.Value;
        return result.Pages.SelectMany(p => p.Lines).Select(l => l.Content);
    }
}