using Bot.Core.Services;
using Bot.Shared.DTOs;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.UX;

public class PromptFullNameCmdConsumer(IWhatsAppService wa, IConversationStateService sessions) : IConsumer<PromptFullNameCmd>
{
    public async Task Consume(ConsumeContext<PromptFullNameCmd> ctx)
    {
        var session = await sessions.GetSessionByUserAsync(ctx.Message.CorrelationId);
        if (session is null) return;

        await wa.SendTextMessageAsync(session.PhoneNumber, "\uD83D\uDC64 What's your full name?");
    }
}
