using System.Text.RegularExpressions;
using Bot.Core.Models;
using Bot.Shared.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scriban;

namespace Bot.Core.Services;

public interface ITemplateRenderingService
{
    Task<string> RenderAsync(string templateName, object model);
    string MaskSensitiveData(string input);
    string RenderTransactionPreview(TransactionPreviewModel preview);
}

public class TemplateRenderingService(IOptions<TemplateSettings> opts, ILogger<TemplateRenderingService> log)
    : ITemplateRenderingService
{
    private readonly Dictionary<string, Template> _cache = new();
    private readonly string _templatesPath = opts.Value.TemplatesPath;

    public async Task<string> RenderAsync(string templateName, object model)
    {
        try
        {
            if (_cache.TryGetValue(templateName, out var tpl)) return tpl.Render(model, m => m.Name);
            var path = Path.Combine(_templatesPath, $"{templateName}.scriban");
            var text = await File.ReadAllTextAsync(path);
            tpl = Template.Parse(text);
            if (tpl.HasErrors)
                log.LogError("Template parse errors for {Template}: {Errors}", templateName, tpl.Messages);
            _cache[templateName] = tpl;
            return tpl.Render(model, m => m.Name);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to render template {Template}", templateName);
            return "⚠ Template error";
        }
    }

    public string MaskSensitiveData(string input)
    {
        return Regex.Replace(input, @"\b\d{6,}\b", match =>
            new string('*', match.Length));
    }

    public string RenderTransactionPreview(TransactionPreviewModel preview)
    {
        return $"📄 *Transaction Preview*\n" +
               $"• Payee: {MaskSensitiveData(preview.PayeeName)}\n" +
               $"• Amount: ₦{preview.Amount:N2}\n" +
               $"• Fee: ₦{preview.Fee:N2}\n" +
               $"• New Balance: ₦{preview.NewBalance:N2}\n" +
               $"• Time: {preview.Timestamp:yyyy-MM-dd HH:mm}\n";
    }
}
