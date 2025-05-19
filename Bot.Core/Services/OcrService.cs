using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;

namespace Bot.Core.Services;

public interface IOcrService
{
    Task<string> ExtractTextAsync(Stream stream);
}

public class OcrService(IDocumentAnalysisClient client) : IOcrService
{
    public async Task<string> ExtractTextAsync(Stream stream)
    {
        var lines = await client.ExtractLinesAsync(stream);
        return string.Join(" ", lines);
    }
}