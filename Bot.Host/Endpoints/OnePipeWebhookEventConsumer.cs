using System.Text.Json;
using Bot.Core.StateMachine.Helpers;
using Bot.Core.StateMachine.Mappers;
using FastEndpoints;
using MassTransit;

namespace Bot.Host.Endpoints;

public class OnePipeWebhookEventConsumer(
    IPublishEndpoint bus,
    IConfiguration cfg,
    ILogger<OnePipeWebhookEventConsumer> log)
    : EndpointWithoutRequest
{
    private readonly ILogger<OnePipeWebhookEventConsumer> _log = log;

    public override void Configure()
    {
        Post("/webhooks/onepipe");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        using var reader = new StreamReader(HttpContext.Request.Body);
        var raw = await reader.ReadToEndAsync(ct);

        if (!ValidateSignature(raw))
        {
            await SendAsync(new { error = "Invalid signature" }, 401, ct);
            return;
        }

        var doc = JsonDocument.Parse(raw);
        var evtType = doc.RootElement.GetProperty("event_type").GetString();
        var txn = doc.RootElement.GetProperty("data");

        switch (evtType)
        {
            case "mandate.approved":
                var mandateEvt = WebhookToEventMapper.MapOnePipeMandate(txn);
                await bus.Publish(mandateEvt, ct);
                break;

            case "debit.successful":
                var success = WebhookToEventMapper.MapTransferSuccess(txn, "OnePipe");
                await bus.Publish(success, ct);
                break;

            case "debit.failed":
                var fail = WebhookToEventMapper.MapTransferFailed(txn);
                await bus.Publish(fail, ct);
                break;
        }

        await SendOkAsync(ct);
    }

    private bool ValidateSignature(string raw)
    {
        var key = cfg["OnePipe:WebhookSecret"];
        return HttpContext.Request.Headers.TryGetValue("Signature", out var sig) && SignatureVerifier.HmacIsValid(raw, key!, sig!);
    }
}
