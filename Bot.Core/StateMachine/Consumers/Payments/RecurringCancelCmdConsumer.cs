using Bot.Infrastructure.Data;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.Payments;

public class RecurringCancelCmdConsumer(ApplicationDbContext db) : IConsumer<RecurringCancelCmd>
{
    public async Task Consume(ConsumeContext<RecurringCancelCmd> ctx)
    {
        var rec = await db.RecurringTransfers.FindAsync(ctx.Message.RecurringId);

        if (rec != null)
        {
            rec.IsActive = false;
            await db.SaveChangesAsync();
        }

        await ctx.Publish(new RecurringCancelled(
            ctx.Message.CorrelationId,
            ctx.Message.RecurringId));
    }
}