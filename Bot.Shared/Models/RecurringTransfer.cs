// Bot.Shared/Models/RecurringTransfer.cs

using System.ComponentModel.DataAnnotations.Schema;

namespace Bot.Shared.Models;

public class RecurringTransfer
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PayeeId { get; set; }

    public decimal Amount { get; set; }
    public string CronExpression { get; set; } = null!;
    public DateTime NextRun { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    [ForeignKey(nameof(DirectDebitMandate))]
    public Guid? MandateId { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
    public virtual Payee Payee { get; set; } = null!;
    public virtual DirectDebitMandate DirectDebitMandate { get; set; } = null!;
}