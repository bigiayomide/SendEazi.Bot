using Bot.Core.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Bot.Core.StateMachine.Consumers.Chat;

public abstract class RawInboundMsgCmdConsumer(IConversationStateService session, ILogger<RawInboundMsgCmdConsumer> log)
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
            case "AskFullName":
                await ctx.Publish(new FullNameProvided(correlationId, text));
                break;
            case "AskNin":
                await ctx.Publish(new NinProvided(correlationId, text));
                break;
            case "AskBvn":
                await ctx.Publish(new BvnProvided(correlationId, text));
                break;
            default:
                await ctx.Publish(new VoiceMessageTranscribed(correlationId, text, "auto"));
                break;
        }

        await session.UpdateLastMessageAsync(s.SessionId, text);
    }
}