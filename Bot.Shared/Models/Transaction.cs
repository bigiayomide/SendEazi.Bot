using Bot.Shared.Enums;

namespace Bot.Shared.Models;

public sealed class Transaction
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid? PayeeId { get; set; }

    public decimal Amount { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Reference { get; set; }
    public string? RecipientName { get; set; } 
    // Navigation
    public User User { get; set; } = null!;
    public Payee? Payee { get; set; }
    public ICollection<FeeRecord> FeeRecords { get; set; } = new List<FeeRecord>();
    public ICollection<TransactionMemo> Memos { get; set; } = new List<TransactionMemo>();
}