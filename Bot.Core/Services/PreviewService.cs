// Bot.Core.Services/PreviewService.cs

using Bot.Shared.DTOs;

namespace Bot.Core.Services;

public interface IPreviewService
{
    /// <summary>
    ///     Returns a text “card” preview for the given transaction.
    /// </summary>
    Task<string> GenerateTransactionPreviewAsync(TransactionPreviewModel model);
}

public class PreviewService(ITemplateRenderingService templater) : IPreviewService
{
    public Task<string> GenerateTransactionPreviewAsync(TransactionPreviewModel model)
    {
        // Uses the Scriban template “TransactionPreview.scriban”
        return templater.RenderAsync("TransactionPreview", model);
    }
}