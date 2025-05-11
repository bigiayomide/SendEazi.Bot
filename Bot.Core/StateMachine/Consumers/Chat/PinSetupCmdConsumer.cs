using Bot.Core.Services;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.Chat;

public class PinSetupCmdConsumer(IPinService pins) : IConsumer<PinSetupCmd>
{
    public async Task Consume(ConsumeContext<PinSetupCmd> ctx)
    {
        try
        {
            await pins.SetAsync(ctx.Message.CorrelationId, ctx.Message.PinHash);
            await ctx.Publish(new PinSet(ctx.Message.CorrelationId));
        }
        catch (Exception e)
        {
            await ctx.Publish(new PinSetupFailed(ctx.Message.CorrelationId, "Could not set Pin"));
            throw;
        }
    }
}