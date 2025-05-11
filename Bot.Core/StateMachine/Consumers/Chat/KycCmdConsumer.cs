using Bot.Core.Services;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.Chat;

public class KycCmdConsumer(IUserService users) : IConsumer<KycCmd>
{
    public async Task Consume(ConsumeContext<KycCmd> ctx)
    {
        var ok = await users.RunKycAsync(ctx.Message.CorrelationId);
        if (ok)
            await ctx.Publish(new KycApproved(ctx.Message.CorrelationId));
        else
            await ctx.Publish(new KycRejected(ctx.Message.CorrelationId, "Unknown"));
    }
}