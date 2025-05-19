using Bot.Core.Services;
using MassTransit;

namespace Bot.Tests.TestUtilities;

public class ConversationStatePayloadFilter<T>(IConversationStateService service) : IFilter<ConsumeContext<T>>
    where T : class
{
    public Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        context.GetOrAddPayload(() => service);
        return next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
    }
}

public class ConversationStateConsumeFilter<T>(IConversationStateService conversationStateService)
    : IFilter<ConsumeContext<T>>
    where T : class
{
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        context.GetOrAddPayload(() => conversationStateService);
        await next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("ConversationStateConsumeFilter");
    }
}