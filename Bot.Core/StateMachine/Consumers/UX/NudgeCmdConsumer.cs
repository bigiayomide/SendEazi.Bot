using Bot.Core.Services;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.UX;

public class NudgeCmdConsumer(IWhatsAppService wa, INudgeService nudges, IUserService users) : IConsumer<NudgeCmd>
{
    public async Task Consume(ConsumeContext<NudgeCmd> ctx)
    {
        var user = await users.GetByIdAsync(ctx.Message.CorrelationId);
        var media = nudges.SelectAsset(ctx.Message.NudgeType);
        await wa.SendMediaAsync(user!.PhoneNumber, media);

        await ctx.Publish(new NudgeSent(user.Id, ctx.Message.NudgeType));
    }
}