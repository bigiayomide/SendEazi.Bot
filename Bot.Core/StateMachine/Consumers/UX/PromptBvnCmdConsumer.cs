using Bot.Core.Services;
using Bot.Shared.DTOs;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.UX;

public class PromptBvnCmdConsumer(IWhatsAppService wa, IConversationStateService sessions) : IConsumer<PromptBvnCmd>
{
    public async Task Consume(ConsumeContext<PromptBvnCmd> ctx)
    {
        var session = await sessions.GetSessionByUserAsync(ctx.Message.CorrelationId);
        if (session is null) return;

        await wa.SendTextMessageAsync(session.PhoneNumber, "\uD83D\uDD10 Please provide your 11-digit BVN.");
    }
}
