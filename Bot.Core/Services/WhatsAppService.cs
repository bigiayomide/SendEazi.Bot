using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bot.Core.Services;

public interface IWhatsAppService
{
    Task<string> SendTextMessageAsync(string toPhoneNumber, string message);
    Task<string> SendMediaAsync(string toPhoneNumber, string mediaUrl, string caption = "");
    Task<string> SendQuickReplyAsync(string toPhoneNumber, string header, string body, string[] buttonLabels);
    Task<string> SendTemplateAsync(string toPhoneNumber, object template);
    Task<string> SendVoiceAsync(string toPhoneNumber, Stream audio);
    Task DeleteMessageAsync(string messageId);
}

// Bot.Infrastructure/Services/WhatsAppService.cs


public class WhatsAppOptions
{
    public string BaseUrl { get; set; } = "https://graph.facebook.com/v18.0";
    public string PhoneNumberId { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public TimeSpan EphemeralTtl { get; set; } = TimeSpan.FromMinutes(5);
}

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _http;
    private readonly ILogger<WhatsAppService> _logger;
    private readonly WhatsAppOptions _opts;

    public WhatsAppService(HttpClient http, IOptions<WhatsAppOptions> opts, ILogger<WhatsAppService> logger)
    {
        _http = http;
        _opts = opts.Value;
        _logger = logger;

        _http.BaseAddress = new Uri(_opts.BaseUrl);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _opts.AccessToken);
    }

    public async Task<string> SendTextMessageAsync(string toPhoneNumber, string message)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to = toPhoneNumber,
            type = "text",
            text = new { preview_url = false, body = message }
        };

        return await SendPayloadAsync(payload);
    }

    public async Task<string> SendMediaAsync(string toPhoneNumber, string mediaUrl, string caption = "")
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to = toPhoneNumber,
            type = "image",
            image = new { link = mediaUrl, caption }
        };

        return await SendPayloadAsync(payload);
    }

    public async Task<string> SendQuickReplyAsync(string toPhoneNumber, string header, string body, string[] buttonLabels)
    {
        var buttons = buttonLabels.Select((label, index) => new
        {
            type = "reply",
            reply = new { id = $"btn_{index + 1}", title = label }
        }).ToArray();

        var payload = new
        {
            messaging_product = "whatsapp",
            to = toPhoneNumber,
            type = "interactive",
            interactive = new
            {
                type = "button",
                body = new { text = body },
                action = new { buttons }
            }
        };

        return await SendPayloadAsync(payload);
    }

    public async Task<string> SendTemplateAsync(string toPhoneNumber, object template)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to = toPhoneNumber,
            type = "template",
            template
        };

        return await SendPayloadAsync(payload);
    }

    public async Task<string> SendVoiceAsync(string toPhoneNumber, Stream audio)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(audio), "file", "audio.ogg");

        var uploadRes = await _http.PostAsync($"/{_opts.PhoneNumberId}/media", content);
        var uploadJson = await uploadRes.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var mediaId = uploadJson?["id"] ?? throw new Exception("Failed to upload audio");

        var payload = new
        {
            messaging_product = "whatsapp",
            to = toPhoneNumber,
            type = "audio",
            audio = new { id = mediaId }
        };

        return await SendPayloadAsync(payload);
    }

    public async Task DeleteMessageAsync(string messageId)
    {
        try
        {
            var res = await _http.DeleteAsync($"/{_opts.PhoneNumberId}/messages/{messageId}");
            res.EnsureSuccessStatusCode();
            _logger.LogInformation("Deleted WhatsApp message {Id}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete WhatsApp message {Id}", messageId);
        }
    }

    private async Task<string> SendPayloadAsync(object payload)
    {
        var res = await _http.PostAsJsonAsync($"/{_opts.PhoneNumberId}/messages", payload);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadFromJsonAsync<SendMessageResponse>();
        var id = json?.Messages?[0]?.Id ?? "(unknown)";

        _logger.LogInformation("Sent WhatsApp message: {Id}", id);

        if (_opts.EphemeralTtl > TimeSpan.Zero && id != null)
            _ = Task.Delay(_opts.EphemeralTtl).ContinueWith(_ => DeleteMessageAsync(id));

        return id;
    }

    private record SendMessageResponse(SendMessageWrapper[]? Messages);
    private record SendMessageWrapper(string? Id);
}