namespace Bot.Core.Models;

public class BudgetAlert
{
    public Guid UserId { get; set; }
    public string Category { get; set; } = null!;
    public decimal Spent { get; set; }
    public decimal Limit { get; set; }
}
