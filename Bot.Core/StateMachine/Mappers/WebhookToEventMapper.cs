using System.Text.Json;

namespace Bot.Core.StateMachine.Mappers;

public static class WebhookToEventMapper
{
    public static MandateReadyToDebit MapMonoMandate(JsonElement data)
    {
        var reference = data.GetProperty("reference").GetString()!;
        var mandateId = data.GetProperty("mandate_id").GetString()!;
        var correlationId = ExtractUserId(reference);

        return new MandateReadyToDebit(correlationId, mandateId, "Mono");
    }

    public static MandateReadyToDebit MapOnePipeMandate(JsonElement txn)
    {
        var reference = txn.GetProperty("transaction_ref").GetString()!;
        var mandateId = txn.GetProperty("mandate_id").GetString()!;
        var correlationId = ExtractUserId(reference);

        return new MandateReadyToDebit(correlationId, mandateId, "OnePipe");
    }

    public static TransferCompleted MapTransferSuccess(JsonElement payload, string provider)
    {
        var reference = payload.GetProperty("transaction_ref").GetString()!;
        var txnId = payload.GetProperty("transaction_id").GetString()!;
        var correlationId = ExtractUserId(reference);

        return new TransferCompleted(correlationId, txnId);
    }

    public static TransferFailed MapTransferFailed(JsonElement payload)
    {
        var reference = payload.GetProperty("transaction_ref").GetString()!;
        var reason = payload.GetProperty("failure_reason").GetString()!;
        var correlationId = ExtractUserId(reference);

        return new TransferFailed(correlationId, reason);
    }

    private static Guid ExtractUserId(string reference)
    {
        var parts = reference.Split(":");
        if (parts.Length >= 2 && Guid.TryParse(parts[1], out var id)) return id;
        Console.WriteLine($"⚠️ Invalid reference format: {reference}");
        return Guid.Empty;
    }
}