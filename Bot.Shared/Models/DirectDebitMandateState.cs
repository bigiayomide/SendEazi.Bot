using MassTransit;

namespace Bot.Shared.Models;

public class DirectDebitMandateState : SagaStateMachineInstance
{
    public string CurrentState { get; set; } = default!;
    public string? MandateId { get; set; }
    public string? Provider { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CorrelationId { get; set; }
}