using Bot.Core.Services;
using Bot.Shared.DTOs;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.UX;

public class PromptFullNameCmdConsumer(IWhatsAppService wa, IUserService users) : IConsumer<PromptFullNameCmd>
{
    public async Task Consume(ConsumeContext<PromptFullNameCmd> ctx)
    {
        var user = await users.GetByIdAsync(ctx.Message.CorrelationId);
        if (user is null)
            return;

        await wa.SendTextMessageAsync(user.PhoneNumber, "\uD83D\uDC64 What's your full name?");
    }
}
