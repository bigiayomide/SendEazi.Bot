using Bot.Core.Services;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.Chat;

public class NlpFromTextConsumer(INlpService nlp) :
    IConsumer<VoiceMessageTranscribed>,
    IConsumer<OcrResultAvailable>
{
    public async Task Consume(ConsumeContext<VoiceMessageTranscribed> ctx)
    {
        var result = await nlp.DetectIntentAsync(ctx.Message.CorrelationId, ctx.Message.Text);
        await ctx.Publish(result);
    }

    public async Task Consume(ConsumeContext<OcrResultAvailable> ctx)
    {
        var result = await nlp.DetectIntentAsync(ctx.Message.CorrelationId, ctx.Message.ExtractedText);
        await ctx.Publish(result);
    }
}