using Bot.Core.Services;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.UX;

public class QuickReplyCmdConsumer(IWhatsAppService wa, IUserService users) : IConsumer<QuickReplyCmd>
{
    public async Task Consume(ConsumeContext<QuickReplyCmd> ctx)
    {
        var user = await users.GetByIdAsync(ctx.Message.CorrelationId);
        await wa.SendQuickReplyAsync(user!.PhoneNumber, ctx.Message.TemplateName, "Quick options", ctx.Message.Args);

        await ctx.Publish(new QuickReplySent(user.Id, ctx.Message.TemplateName));
    }
}
