using Bot.Core.Services;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.UX;

public class RespondWithVoiceCmdConsumer(ITextToSpeechService tts) : IConsumer<RespondWithVoiceCmd>
{
    public async Task Consume(ConsumeContext<RespondWithVoiceCmd> ctx)
    {
        var stream = await tts.SynthesizeAsync(ctx.Message.Text, ctx.Message.Language ?? "en");
        await ctx.Publish(new VoiceReplyReady(ctx.Message.CorrelationId, stream));
    }
}