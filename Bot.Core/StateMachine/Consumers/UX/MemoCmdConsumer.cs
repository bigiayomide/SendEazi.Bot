using Bot.Core.Services;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.UX;

public class MemoCmdConsumer(IMemoService memos) : IConsumer<MemoCmd>
{
    public async Task Consume(ConsumeContext<MemoCmd> ctx)
    {
        var memoId = await memos.SaveAsync(ctx.Message.CorrelationId, ctx.Message.Payload);

        if (memoId != Guid.Empty)
            await ctx.Publish(new MemoSaved(ctx.Message.CorrelationId, memoId));
        else
            await ctx.Publish(new MemoSaveFailed(ctx.Message.CorrelationId, "Unable to save memo"));
    }
}