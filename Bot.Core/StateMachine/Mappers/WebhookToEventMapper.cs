using System.Text.Json;

namespace Bot.Core.StateMachine.Mappers;

public static class WebhookToEventMapper
{
    public static MandateReadyToDebit MapMonoMandate(JsonElement data)
    {
        var refStr = data.GetProperty("reference").GetString()!;
        var mandateId = data.GetProperty("mandate_id").GetString()!;
        return new MandateReadyToDebit(GetUserId(refStr), mandateId, "Mono");
    }

    public static MandateReadyToDebit MapOnePipeMandate(JsonElement txn)
    {
        var refStr = txn.GetProperty("transaction_ref").GetString()!;
        var mandateId = txn.GetProperty("mandate_id").GetString()!;
        return new MandateReadyToDebit(GetUserId(refStr), mandateId, "OnePipe");
    }

    public static TransferCompleted MapTransferSuccess(JsonElement payload, string provider)
    {
        var refStr = payload.GetProperty("transaction_ref").GetString()!;
        var txnId  = payload.GetProperty("transaction_id").GetString()!;
        return new TransferCompleted(GetUserId(refStr), txnId);
    }

    public static TransferFailed MapTransferFailed(JsonElement payload)
    {
        var refStr = payload.GetProperty("transaction_ref").GetString()!;
        var reason = payload.GetProperty("failure_reason").GetString()!;
        return new TransferFailed(GetUserId(refStr), reason);
    }

    private static Guid GetUserId(string reference)
    {
        var match = reference.Split(":");
        return Guid.TryParse(match.Length > 1 ? match[1] : match[0], out var id) ? id : Guid.Empty;
    }
}