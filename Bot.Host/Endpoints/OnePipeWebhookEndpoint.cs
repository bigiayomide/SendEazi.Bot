// using System.Security.Cryptography;
// using System.Text;
// using System.Text.Json;
// using Bot.Core.StateMachine;
// using FastEndpoints;
// using MassTransit;
//
// namespace Bot.Host.Endpoints;
//
// public class OnePipeWebhookEndpoint(IPublishEndpoint bus, ILogger<OnePipeWebhookEndpoint> log, IConfiguration cfg)
//     : EndpointWithoutRequest
// {
//     public override void Configure()
//     {
//         Post("/webhooks/onepipe");
//         AllowAnonymous();
//     }
//
//     public override async Task HandleAsync(CancellationToken ct)
//     {
//         using var reader = new StreamReader(HttpContext.Request.Body);
//         var raw = await reader.ReadToEndAsync(ct);
//
//         if (!ValidateSignature(raw))
//         {
//             log.LogWarning("OnePipe webhook failed signature validation.");
//             await SendAsync(new { error = "unauthorized" }, 401, ct);
//             return;
//         }
//
//         var doc = JsonDocument.Parse(raw);
//         var root = doc.RootElement;
//         var eventType = root.GetProperty("event_type").GetString();
//         var txn = root.GetProperty("data");
//
//         var reference = txn.GetProperty("transaction_ref").GetString()!;
//         var userId = ExtractUserIdFromReference(reference);
//
//         switch (eventType)
//         {
//             case "mandate.approved":
//                 var mandateId = txn.GetProperty("mandate_id").GetString()!;
//                 await bus.Publish(new MandateReadyToDebit(userId, mandateId, "OnePipe"), ct);
//                 break;
//
//             case "debit.successful":
//                 var txnId = txn.GetProperty("transaction_id").GetString()!;
//                 await bus.Publish(new TransferCompleted(userId, txnId), ct);
//                 break;
//
//             case "debit.failed":
//                 var reason = txn.GetProperty("failure_reason").GetString()!;
//                 await bus.Publish(new TransferFailed(userId, reason, reference), ct);
//                 break;
//         }
//
//         await SendOkAsync(ct);
//     }
//
//     private bool ValidateSignature(string raw)
//     {
//         var key = cfg["OnePipe:WebhookSecret"];
//         if (string.IsNullOrWhiteSpace(key))
//             return false;
//
//         if (!HttpContext.Request.Headers.TryGetValue("Signature", out var signatureHeader))
//             return false;
//
//         var computed = ComputeHmac(raw, key);
//         return computed.Equals(signatureHeader, StringComparison.OrdinalIgnoreCase);
//     }
//
//     private static string ComputeHmac(string raw, string secret)
//     {
//         var bytes = Encoding.UTF8.GetBytes(secret);
//         using var hmac = new HMACSHA256(bytes);
//         var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
//         return Convert.ToHexStringLower(hash);
//     }
//
//     private static Guid ExtractUserIdFromReference(string reference)
//     {
//         var match = reference.Split(":");
//         return Guid.TryParse(match.Length > 1 ? match[1] : match[0], out var id) ? id : Guid.Empty;
//     }
// }

