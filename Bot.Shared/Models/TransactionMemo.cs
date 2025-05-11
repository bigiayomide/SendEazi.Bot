namespace Bot.Shared.Models;

public class TransactionMemo
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }

    public string MemoText { get; set; } = null!;
    public string? ReceiptUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual Transaction Transaction { get; set; } = null!;
}