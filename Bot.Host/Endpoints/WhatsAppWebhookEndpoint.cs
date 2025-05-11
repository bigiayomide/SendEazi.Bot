using System.Text.Json;
using Bot.Core.Services;
using Bot.Core.StateMachine;
using Bot.Infrastructure.Data;
using FastEndpoints;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Bot.Host.Endpoints;

public class WhatsAppWebhookEndpoint(
    IConfiguration cfg,
    IConversationStateService state,
    IPublishEndpoint bus,
    ApplicationDbContext db,
    ILogger<WhatsAppWebhookEndpoint> log)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Verbs(Http.POST, Http.GET);
        Routes("/webhooks/whatsapp");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (HttpContext.Request.Method == nameof(Http.GET))
        {
            var mode = Query<string>("hub.mode");
            var token = Query<string>("hub.verify_token");
            var challenge = Query<string>("hub.challenge");

            if (mode == "subscribe" && token == cfg["WhatsApp:VerifyToken"])
            {
                await SendStringAsync(challenge, cancellation: ct);
                return;
            }

            await SendAsync(null, 401, ct);
            return;
        }

        using var reader = new StreamReader(HttpContext.Request.Body);
        var json = await reader.ReadToEndAsync(ct);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        try
        {
            var entry = root.GetProperty("entry")[0];
            var changes = entry.GetProperty("changes")[0];
            var value = changes.GetProperty("value");

            if (!value.TryGetProperty("messages", out var messagesElement))
            {
                log.LogInformation("No messages in webhook.");
                await SendOkAsync(ct);
                return;
            }

            var msg = messagesElement[0];
            var from = msg.GetProperty("from").GetString();
            var msgId = msg.GetProperty("id").GetString();
            var type = msg.GetProperty("type").GetString();

            var phone = "+234" + from![(from.Length - 10)..];
            var text = type switch
            {
                "text" => msg.GetProperty("text").GetProperty("body").GetString()!,
                "button" => msg.GetProperty("button").GetProperty("text").GetString()!,
                "interactive" => msg.GetProperty("interactive").GetProperty("button_reply").GetProperty("title")
                    .GetString()!,
                _ => "[[unsupported]]"
            };

            var session = await state.GetOrCreateSessionAsync(phone);
            var correlationId = session.UserId != Guid.Empty ? session.UserId : Guid.NewGuid();

            var payeeMatch = await db.Payees.AnyAsync(p =>
                p.UserId == correlationId &&
                p.Nickname != null &&
                p.Nickname.ToLower() == text.ToLower(), ct);

            if (payeeMatch)
                await bus.Publish(new ResolveQuickReplyCmd(correlationId, text), ct);
            else
                await bus.Publish(new RawInboundMsgCmd(correlationId, phone, text, msgId), ct);

            await state.UpdateLastMessageAsync(session.SessionId, text);
            await SendOkAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to handle WhatsApp webhook.");
            await SendAsync(new { error = "error parsing payload" }, 400, ct);
        }
    }
}