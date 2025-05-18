using Bot.Core.Services;
using Bot.Shared.DTOs;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.UX;

public class FeedbackCmdConsumer(IFeedbackService feedback) : IConsumer<FeedbackCmd>
{
    public async Task Consume(ConsumeContext<FeedbackCmd> ctx)
    {
        var feedbackId = await feedback.StoreAsync(ctx.Message.CorrelationId, ctx.Message.Payload);
        await ctx.Publish(new FeedbackLogged(ctx.Message.CorrelationId, feedbackId));
    }
}