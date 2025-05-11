using Bot.Shared.Models;

namespace Bot.Core.StateMachine.Mappers;

public static class TransactionToPreviewMapper
{
    public static object ToCardTemplate(Transaction tx)
    {
        return new
        {
            Amount = $"{tx.Amount:N2} NGN",
            Date   = tx.CreatedAt.ToLocalTime().ToString("dd MMM, HH:mm"),
            Status = tx.Status.ToString(),
            Ref    = tx.Reference,
            Summary = $"You sent {tx.Amount:N2} to {tx.RecipientName ?? "recipient"}"
        };
    }
}