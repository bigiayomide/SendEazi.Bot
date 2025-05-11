using Bot.Infrastructure.Data;
using Bot.Shared.Models;
using MassTransit;

namespace Bot.Core.StateMachine;

/// <summary>
///     Custom endpoint tuning for the saga: queue name, prefetch, outbox.
/// </summary>
public class DirectDebitMandateMachineDefinition
    : SagaDefinition<DirectDebitMandateState>
{
    public DirectDebitMandateMachineDefinition()
    {
        EndpointName = "direct-debit-saga";
        ConcurrentMessageLimit = 8;
    }

    protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator,
        ISagaConfigurator<DirectDebitMandateState> sagaConfigurator,
        IRegistrationContext context)
    {
        base.ConfigureSaga(endpointConfigurator, sagaConfigurator, context);
        endpointConfigurator.UseEntityFrameworkOutbox<ApplicationDbContext>(context);
        endpointConfigurator.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(2)));
    }
}