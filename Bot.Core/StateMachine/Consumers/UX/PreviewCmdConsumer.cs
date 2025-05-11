using Bot.Core.Services;
using Bot.Infrastructure.Data;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.UX;

public class PreviewCmdConsumer(IWhatsAppService wa, ApplicationDbContext db, IUserService users) : IConsumer<PreviewCmd>
{
    public async Task Consume(ConsumeContext<PreviewCmd> ctx)
    {
        var user = await users.GetByIdAsync(ctx.Message.CorrelationId);
        object card;

        if (ctx.Message.TransactionId.HasValue)
        {
            var tx = await db.Transactions.FindAsync(ctx.Message.TransactionId.Value);
            card = new
            {
                title = "Transaction Receipt",
                body = $"You sent ₦{tx!.Amount:N2} to {tx.RecipientName ?? "recipient"}",
                @ref = tx.Reference
            };
        }
        else if (ctx.Message.BillId.HasValue)
        {
            var bill = await db.BillPayments.FindAsync(ctx.Message.BillId.Value);
            card = new
            {
                title = "Bill Receipt",
                body = $"You paid ₦{bill!.Amount:N2} to {bill.Biller}",
                date = bill.PaidAt?.ToString("g")
            };
        }
        else
        {
            card = new { title = "Info", body = "No preview available." };
        }

        await wa.SendTemplateAsync(user!.PhoneNumber, card);
        await ctx.Publish(new PreviewSent(ctx.Message.CorrelationId));
    }
}