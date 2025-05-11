using MassTransit;

namespace Bot.Shared.Models;

public class BotState : SagaStateMachineInstance
{
    public string CurrentState { get; set; } = default!;

    // Signup
    public bool KycApproved { get; set; }
    public bool BankLinked { get; set; }
    public bool PinSet { get; set; }
    public bool PinValidated { get; set; }

    public Guid? ActiveBillId { get; set; }
    public Guid? ActiveRecurringId { get; set; }
    public Guid? ActiveGoalId { get; set; }
    public Guid? LastTransactionId { get; set; }

    public string? LastFailureReason { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public string? TempName { get; set; }
    public string? TempNIN { get; set; }
    public string? TempBVN { get; set; }
    public Guid SessionId { get; set; }
    public string? PhoneNumber { get; set; }

    public string? PendingIntentType { get; set; } // "transfer", "billpay", etc.
    public string? PendingIntentPayload { get; set; } // JSON string of full UserIntentDetected

    public byte[]? RowVersion { get; set; }
    public Guid CorrelationId { get; set; }
}