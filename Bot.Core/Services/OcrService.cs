using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;

namespace Bot.Core.Services;

public interface IOcrService
{
    Task<string> ExtractTextAsync(Stream stream);
}

public class OcrService(DocumentAnalysisClient client) : IOcrService
{
    public async Task<string> ExtractTextAsync(Stream stream)
    {
        var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", stream);
        var result = operation.Value;

        return string.Join(" ", result.Pages.SelectMany(p => p.Lines).Select(l => l.Content));
    }
}