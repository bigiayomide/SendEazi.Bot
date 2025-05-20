using Bot.Shared.Enums;
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
    public bool PreviewPublished { get; set; }

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

    public IntentType? PendingIntentType { get; set; }
    public string? PendingIntentPayload { get; set; }

    public byte[]? RowVersion { get; set; }

    // New fields for resiliency and diagnostics
    public string? PendingPayloadHash { get; set; } // To prevent duplication
    public string? SagaVersion { get; set; } = "v1"; // Version tracking for migrations
    public DateTime? LastIntentHandledAt { get; set; } // Helps with concurrency control
    public Guid? TimeoutTokenId { get; set; } // Token for scheduling inactivity timeouts
    public Guid CorrelationId { get; set; }
    public Guid UserId { get; set; }
}