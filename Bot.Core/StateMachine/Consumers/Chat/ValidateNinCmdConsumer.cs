using Bot.Core.Services;
using Bot.Shared.DTOs;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.Chat;

public class ValidateNinCmdConsumer(IIdentityVerificationService idv) : IConsumer<ValidateNinCmd>
{
    public async Task Consume(ConsumeContext<ValidateNinCmd> ctx)
    {
        var valid = await idv.VerifyNinAsync(ctx.Message.Nin);
        if (valid)
            await ctx.Publish(new NinVerified(ctx.Message.CorrelationId, ctx.Message.Nin));
        else
            await ctx.Publish(new NinRejected(ctx.Message.CorrelationId, "InvalidOrServiceDown"));
    }
}