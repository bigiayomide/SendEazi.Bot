// using System.Text.Json;
// using Bot.Core.StateMachine.Helpers;
// using Bot.Core.StateMachine.Mappers;
// using FastEndpoints;
// using MassTransit;
//
// namespace Bot.Host.Endpoints;
//
// public class MonoWebhookEventConsumer(
//     IPublishEndpoint bus,
//     ILogger<MonoWebhookEventConsumer> log,
//     IConfiguration cfg)
//     : EndpointWithoutRequest
// {
//     public override void Configure()
//     {
//         Post("/webhooks/mono");
//         AllowAnonymous();
//     }
//
//     public override async Task HandleAsync(CancellationToken ct)
//     {
//         using var reader = new StreamReader(HttpContext.Request.Body);
//         var raw = await reader.ReadToEndAsync(ct);
//         
//         
//         if (!ValidateSignature(raw))
//         {
//             log.LogWarning("Mono webhook failed signature validation.");
//             await SendAsync(new { error = "unauthorized" }, 401, ct);
//             return;
//         }
//
//         try
//         {
//             var doc = JsonDocument.Parse(raw);
//             var type = doc.RootElement.GetProperty("event").GetString();
//             var data = doc.RootElement.GetProperty("data");
//
//             object? evt = type switch
//             {
//                 "mandate.ready_to_debit" or "mandate.approved" =>
//                     WebhookToEventMapper.MapMonoMandate(data),
//
//                 "debit.successful" =>
//                     WebhookToEventMapper.MapTransferSuccess(data, "Mono"),
//
//                 "debit.failed" =>
//                     WebhookToEventMapper.MapTransferFailed(data),
//
//                 _ => null
//             };
//
//             if (evt is CorrelatedBy<Guid> { CorrelationId: var id } && id != Guid.Empty)
//             {
//                 await bus.Publish(evt, ct);
//             }
//             else
//             {
//                 log.LogWarning("⚠️ Unrecognized or invalid Mono event: {Type}", type);
//             }
//
//             await SendOkAsync(ct);
//         }
//         catch (Exception ex)
//         {
//             log.LogError(ex, "❌ Failed to handle Mono webhook");
//             await SendAsync(new { error = "bad payload" }, 400, ct);
//         }
//     }
//     private bool ValidateSignature(string raw)
//     {
//         var key = cfg["Mono:WebhookSecret"];
//         if (string.IsNullOrWhiteSpace(key)) return false;
//
//         return HttpContext.Request.Headers.TryGetValue("x-mono-signature", out var sig) &&
//                SignatureVerifier.HmacIsValid(raw, key!, sig!);
//     }
// }

