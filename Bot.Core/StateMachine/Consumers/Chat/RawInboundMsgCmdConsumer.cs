using Bot.Core.Services;
using Bot.Shared.DTOs;
using Bot.Shared.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Bot.Core.StateMachine.Consumers.Chat;

public class RawInboundMsgCmdConsumer(IConversationStateService session, ILogger<RawInboundMsgCmdConsumer> log)
    : IConsumer<RawInboundMsgCmd>
{
    public async Task Consume(ConsumeContext<RawInboundMsgCmd> ctx)
    {
        var s = await session.GetOrCreateSessionAsync(ctx.Message.Phone);
        var state = await session.GetStateAsync(s.SessionId);
        var text = ctx.Message.Text;
        var correlationId = s.UserId != Guid.Empty ? s.UserId : ctx.Message.CorrelationId;

        switch (state)
        {
            case ConversationState.AskFullName:
                await ctx.Publish(new FullNameProvided(correlationId, text));
                break;
            case ConversationState.AskNin:
                await ctx.Publish(new NinProvided(correlationId, text));
                break;
            case ConversationState.AskBvn:
                await ctx.Publish(new BvnProvided(correlationId, text));
                break;
            case ConversationState.AwaitingPinSetup:
                await ctx.Publish(new PinSetupCmd(correlationId, text, ctx.Message.MessageId));
                break;
            default:
                await ctx.Publish(new VoiceMessageTranscribed(correlationId, text, "auto", ctx.Message.Phone));
                break;
        }

        await session.UpdateLastMessageAsync(s.SessionId, text);
    }
}