using Bot.Core.Services;
using Bot.Shared.DTOs;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.Chat;

public class ValidateBvnCmdConsumer(IIdentityVerificationService idv) : IConsumer<ValidateBvnCmd>
{
    public async Task Consume(ConsumeContext<ValidateBvnCmd> ctx)
    {
        var valid = await idv.VerifyBvnAsync(ctx.Message.Bvn);
        if (valid)
            await ctx.Publish(new BvnVerified(ctx.Message.CorrelationId, ctx.Message.Bvn));
        else
            await ctx.Publish(new BvnRejected(ctx.Message.CorrelationId, "InvalidOrServiceDown"));
    }
}