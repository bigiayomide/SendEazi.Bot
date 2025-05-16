using Bot.Shared.Models;

namespace Bot.Core.Helpers;

public static class PreviewCardBuilder
{
    public static object BuildTransactionCard(Transaction tx)
    {
        return new
        {
            title = "✅ Transfer Successful",
            body = $"You sent ₦{tx.Amount:N0} to {tx.RecipientName ?? "recipient"}",
            footer = $"Ref: {tx.Reference} — {tx.CompletedAt?.ToLocalTime():dd MMM, HH:mm}",
            actions = new[]
            {
                new { type = "reply", label = "📤 Send Again", value = "repeat last transfer" },
                new { type = "reply", label = "📄 Main Menu", value = "menu" }
            }
        };
    }

    public static object BuildBillCard(BillPayment bill)
    {
        return new
        {
            title = "✅ Bill Payment Successful",
            body = $"You paid ₦{bill.Amount:N0} to {bill.Biller}",
            footer = $"Ref: {bill.Id} — {bill.PaidAt?.ToLocalTime():dd MMM, HH:mm}",
            actions = new[]
            {
                new { type = "reply", label = "💡 Pay Another", value = "billpay" },
                new { type = "reply", label = "📄 Main Menu", value = "menu" }
            }
        };
    }

    public static object BuildNoPreviewCard()
    {
        return new
        {
            title = "Info",
            body = "No preview available.",
            actions = new[]
            {
                new { type = "reply", label = "🏠 Main Menu", value = "menu" }
            }
        };
    }
}