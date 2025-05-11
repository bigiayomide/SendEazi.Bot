// Bot.Core/Orchestrators/BotStateMachineDefinition.cs

using Bot.Infrastructure.Data;
using Bot.Shared.Models;
using MassTransit;

namespace Bot.Core.StateMachine;

/// <summary>
///     Custom endpoint tuning for the saga: queue name, prefetch, outbox.
/// </summary>
public class BotStateMachineDefinition
    : SagaDefinition<BotState>
{
    public BotStateMachineDefinition()
    {
        EndpointName = "bot-saga";
        ConcurrentMessageLimit = 8;
    }

    protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator,
        ISagaConfigurator<BotState> sagaConfigurator,
        IRegistrationContext context)
    {
        base.ConfigureSaga(endpointConfigurator, sagaConfigurator, context);
        endpointConfigurator.UseEntityFrameworkOutbox<ApplicationDbContext>(context);
        endpointConfigurator.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(2)));
    }
}