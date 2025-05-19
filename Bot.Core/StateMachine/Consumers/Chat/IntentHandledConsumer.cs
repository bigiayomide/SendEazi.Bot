using Bot.Shared.DTOs;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Bot.Core.StateMachine.Consumers.Chat;

public class IntentHandledConsumer(ILogger<IntentHandledConsumer> logger) : IConsumer<IntentHandledEvent>
{
    public Task Consume(ConsumeContext<IntentHandledEvent> context)
    {
        var e = context.Message;
        logger.LogInformation("Audit: Intent '{Intent}' from {Phone} at {Time}", e.Intent, e.PhoneNumber, e.Timestamp);
        // You can persist this to DB if desired
        return Task.CompletedTask;
    }
}