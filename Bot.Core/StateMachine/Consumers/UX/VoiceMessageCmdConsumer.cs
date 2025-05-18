using Bot.Core.Services;
using Bot.Shared.DTOs;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.UX;

public class VoiceMessageCmdConsumer(ISpeechService speech, IHttpClientFactory http) : IConsumer<VoiceMessageCmd>
{
    public async Task Consume(ConsumeContext<VoiceMessageCmd> ctx)
    {
        using var client = http.CreateClient();
        await using var stream = await client.GetStreamAsync(ctx.Message.FileUrl);

        var (text, lang) = await speech.TranscribeAsync(stream);

        await ctx.Publish(new VoiceMessageTranscribed(ctx.Message.CorrelationId, text, lang, ctx.Message.PhoneNumber));
    }
}