using Bot.Core.Services;
using Bot.Shared.DTOs;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.Chat;

public class PinValidationCmdConsumer(IPinService pins) : IConsumer<PinValidationCmd>
{
    public async Task Consume(ConsumeContext<PinValidationCmd> ctx)
    {
        var ok = await pins.ValidateAsync(ctx.Message.CorrelationId, ctx.Message.Pin);
        if (ok)
            await ctx.Publish(new PinValidated(ctx.Message.CorrelationId));
        else
            await ctx.Publish(new PinInvalid(ctx.Message.CorrelationId, "Wrong PIN"));
    }
}