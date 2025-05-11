using Bot.Shared.Enums;

namespace Bot.Shared.Models;

public class DirectDebitTransaction
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public Guid MandateId { get; set; }

    public decimal Amount { get; set; }
    public string Reference { get; set; } = null!;
    public string? ProviderTransactionId { get; set; }
    public string Narration { get; set; } = null!;

    public DirectDebitStatus Status { get; set; } = DirectDebitStatus.Pending;
    public string? FailureReason { get; set; }

    public int RetryCount { get; set; } = 0;
    public string? ExternalReference { get; set; } // from webhook or provider
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
    public virtual DirectDebitMandate Mandate { get; set; } = null!;
}