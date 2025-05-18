using Bot.Core.Services;
using Bot.Shared.DTOs;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.UX;

public class NudgeCmdConsumer(IWhatsAppService wa, INudgeService nudges, IUserService users) : IConsumer<NudgeCmd>
{
    public async Task Consume(ConsumeContext<NudgeCmd> ctx)
    {
        var media = nudges.SelectAsset(ctx.Message.NudgeType);
        await wa.SendTextMessageAsync(ctx.Message.PhoneNumber, ctx.Message.Text);

        await ctx.Publish(new NudgeSent(ctx.Message.CorrelationId, ctx.Message.NudgeType));
    }
}