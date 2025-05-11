namespace Bot.Shared.Models;

public class FeeRecord
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Swept { get; set; }

    // Navigation
    public virtual Transaction Transaction { get; set; } = null!;
}