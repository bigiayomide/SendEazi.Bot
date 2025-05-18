using Bot.Core.Services;
using Bot.Shared.DTOs;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.UX;

public class PromptNinCmdConsumer(IWhatsAppService wa, IUserService users) : IConsumer<PromptNinCmd>
{
    public async Task Consume(ConsumeContext<PromptNinCmd> ctx)
    {
        var user = await users.GetByIdAsync(ctx.Message.CorrelationId);
        if (user is null)
            return;

        await wa.SendTextMessageAsync(user.PhoneNumber, "\uD83C\uDD94 Please provide your 11-digit NIN.");
    }
}
