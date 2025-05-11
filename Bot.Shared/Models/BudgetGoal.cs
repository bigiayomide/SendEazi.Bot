namespace Bot.Shared.Models;

public class BudgetGoal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Category { get; set; } = null!;
    public decimal MonthlyLimit { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastAlertSentAt { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
}