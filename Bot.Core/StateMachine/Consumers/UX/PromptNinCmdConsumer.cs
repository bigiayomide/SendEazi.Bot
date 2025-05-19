using Bot.Core.Services;
using Bot.Shared.DTOs;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.UX;

public class PromptNinCmdConsumer(IWhatsAppService wa, IConversationStateService sessions) : IConsumer<PromptNinCmd>
{
    public async Task Consume(ConsumeContext<PromptNinCmd> ctx)
    {
        var session = await sessions.GetSessionByUserAsync(ctx.Message.CorrelationId);
        if (session is null) return;

        await wa.SendTextMessageAsync(session.PhoneNumber, "\uD83C\uDD94 Please provide your 11-digit NIN.");
    }
}
