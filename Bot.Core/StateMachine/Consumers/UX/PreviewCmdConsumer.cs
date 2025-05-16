using Bot.Core.Helpers;
using Bot.Core.Services;
using Bot.Infrastructure.Data;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.UX;

public class PreviewCmdConsumer(IWhatsAppService wa, ApplicationDbContext db, IUserService users)
    : IConsumer<PreviewCmd>
{
    public async Task Consume(ConsumeContext<PreviewCmd> ctx)
    {
        var user = await users.GetByIdAsync(ctx.Message.CorrelationId);
        if (user is null)
            return;
        object card;
        if (ctx.Message.TransactionId.HasValue)
        {
            var tx = await db.Transactions.FindAsync(ctx.Message.TransactionId.Value);
            card = PreviewCardBuilder.BuildTransactionCard(tx!);
        }
        else if (ctx.Message.BillId.HasValue)
        {
            var bill = await db.BillPayments.FindAsync(ctx.Message.BillId.Value);
            card = PreviewCardBuilder.BuildBillCard(bill!);
        }
        else
        {
            card = PreviewCardBuilder.BuildNoPreviewCard();
        }

        await wa.SendTemplateAsync(user!.PhoneNumber, card);
        await ctx.Publish(new PreviewSent(ctx.Message.CorrelationId));
    }
}