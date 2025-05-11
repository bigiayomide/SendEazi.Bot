// Bot.Shared/Models/BillPayment.cs

using Bot.Shared.Enums;

namespace Bot.Shared.Models;

public sealed class BillPayment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public BillerEnum Biller { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }

    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastReminderSentAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}