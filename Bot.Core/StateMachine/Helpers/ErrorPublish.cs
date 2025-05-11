using MassTransit;

namespace Bot.Core.StateMachine.Consumers.Helpers;

internal static class ErrorPublish
{
    public static Task PublishFail<TFailed>(this ConsumeContext ctx, string reason)
        where TFailed : class
        => ctx.Publish<TFailed>(new { ctx.CorrelationId, Reason = reason });
}