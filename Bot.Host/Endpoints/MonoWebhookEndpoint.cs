using System.Text.Json;
using Bot.Core.StateMachine;
using FastEndpoints;
using MassTransit;

namespace Bot.Host.Endpoints;

public class MonoWebhookEndpoint(IPublishEndpoint bus, ILogger<MonoWebhookEndpoint> log) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/webhooks/mono");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        using var reader = new StreamReader(HttpContext.Request.Body);
        var json = await reader.ReadToEndAsync(ct);
        var doc = JsonDocument.Parse(json);

        try
        {
            var type = doc.RootElement.GetProperty("event").GetString();
            var data = doc.RootElement.GetProperty("data");

            var reference = data.GetProperty("reference").GetString()!;
            var mandateId = data.GetProperty("mandate_id").GetString()!;
            var userId = ExtractUserIdFromReference(reference);

            switch (type)
            {
                case "mandate.approved":
                case "mandate.ready_to_debit":
                    await bus.Publish(new MandateReadyToDebit(userId, mandateId, "Mono"), ct);
                    break;

                case "debit.successful":
                    var txnId = data.GetProperty("transaction_reference").GetString()!;
                    await bus.Publish(new TransferCompleted(userId, txnId), ct);
                    break;

                case "debit.failed":
                    var reason = data.GetProperty("failure_reason").GetString()!;
                    await bus.Publish(new TransferFailed(userId, reason), ct);
                    break;
            }

            await SendOkAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to handle Mono webhook.");
            await SendAsync(new { error = "bad payload" }, 400, ct);
        }
    }

    private static Guid ExtractUserIdFromReference(string reference)
    {
        // e.g., "user-uid:123e4567-..."
        var match = reference.Split(":");
        return Guid.TryParse(match.Length > 1 ? match[1] : match[0], out var id) ? id : Guid.Empty;
    }
}
