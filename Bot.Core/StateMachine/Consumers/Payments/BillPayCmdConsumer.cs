using Bot.Core.Services;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.Payments;

public class BillPayCmdConsumer(IBillPayService bills, IUserService users) : IConsumer<BillPayCmd>
{
    public async Task Consume(ConsumeContext<BillPayCmd> ctx)
    {
        var user = await users.GetByIdAsync(ctx.Message.CorrelationId);
        var result = await bills.PayBillAsync(user!.Id, ctx.Message.Payload.BillerCode, ctx.Message.Payload.Amount,
            DateTime.Now);

        if (result.IsPaid)
            await ctx.Publish(new BillPaid(user.Id, result.Id, ctx.Message.Payload.Amount, result.Biller.ToString()));
        else
            await ctx.Publish(new BillPayFailed(user.Id, "Failed"));
    }
}