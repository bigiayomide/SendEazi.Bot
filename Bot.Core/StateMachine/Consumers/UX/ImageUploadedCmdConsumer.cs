using Bot.Core.Services;
using Bot.Shared.DTOs;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.UX;

public class ImageUploadedCmdConsumer(IOcrService ocr, IHttpClientFactory http) : IConsumer<ImageUploadedCmd>
{
    public async Task Consume(ConsumeContext<ImageUploadedCmd> ctx)
    {
        using var client = http.CreateClient();
        await using var stream = await client.GetStreamAsync(ctx.Message.FileUrl);

        var result = await ocr.ExtractTextAsync(stream);
        await ctx.Publish(new OcrResultAvailable(ctx.Message.CorrelationId, result, ctx.Message.PhoneNumber));
    }
}