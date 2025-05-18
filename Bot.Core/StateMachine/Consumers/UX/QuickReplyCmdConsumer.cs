using Bot.Core.Services;
using Bot.Shared.DTOs;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.UX;

public class QuickReplyCmdConsumer(IWhatsAppService wa, IUserService users, IQuickReplyService replies)
    : IConsumer<QuickReplyCmd>
{
    public async Task Consume(ConsumeContext<QuickReplyCmd> ctx)
    {
        var user = await users.GetByIdAsync(ctx.Message.CorrelationId);
        var labels = await replies.GetQuickRepliesAsync(user!.Id);

        await wa.SendQuickReplyAsync(
            user.PhoneNumber,
            "Your top payees",
            "Tap to pay someone quickly",
            labels.ToArray()
        );

        await ctx.Publish(new QuickReplySent(user.Id, ctx.Message.TemplateName));
    }
}