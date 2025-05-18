using Bot.Core.Services;
using Bot.Shared.DTOs;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.UX;

public class PromptBvnCmdConsumer(IWhatsAppService wa, IUserService users) : IConsumer<PromptBvnCmd>
{
    public async Task Consume(ConsumeContext<PromptBvnCmd> ctx)
    {
        var user = await users.GetByIdAsync(ctx.Message.CorrelationId);
        if (user is null)
            return;

        await wa.SendTextMessageAsync(user.PhoneNumber, "\uD83D\uDD10 Please provide your 11-digit BVN.");
    }
}
