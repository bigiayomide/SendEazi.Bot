using System.Text.Json;
using Bot.Core.StateMachine.Mappers;
using FastEndpoints;
using MassTransit;

namespace Bot.Host.Endpoints;

public class MonoWebhookEventConsumer(IPublishEndpoint bus, ILogger<MonoWebhookEventConsumer> log)
    : EndpointWithoutRequest
{
    private readonly ILogger<MonoWebhookEventConsumer> _log = log;

    public override void Configure()
    {
        Post("/webhooks/mono");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        using var reader = new StreamReader(HttpContext.Request.Body);
        var raw = await reader.ReadToEndAsync();
        var json = JsonDocument.Parse(raw);
        var type = json.RootElement.GetProperty("event").GetString();
        var data = json.RootElement.GetProperty("data");

        switch (type)
        {
            case "mandate.ready_to_debit":
                var evt = WebhookToEventMapper.MapMonoMandate(data);
                await bus.Publish(evt, ct);
                break;

            case "debit.successful":
                var success = WebhookToEventMapper.MapTransferSuccess(data, "Mono");
                await bus.Publish(success, ct);
                break;

            case "debit.failed":
                var fail = WebhookToEventMapper.MapTransferFailed(data);
                await bus.Publish(fail, ct);
                break;
        }

        await SendOkAsync(ct);
    }
}