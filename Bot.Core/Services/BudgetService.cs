// Bot.Core.Services/BudgetService.cs

using Bot.Infrastructure.Data;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using Microsoft.EntityFrameworkCore;

// for date math if needed

namespace Bot.Core.Services;

public interface IBudgetService
{
    Task<BudgetGoal> SetBudgetGoalAsync(Guid userId, string category, decimal limit, DateTime start, DateTime? end);
    Task<IReadOnlyList<(BudgetGoal Goal, decimal Spent)>> GetTriggeredBudgetAlertsAsync(Guid userId);
}

public class BudgetService(ApplicationDbContext db) : IBudgetService
{
    public async Task<BudgetGoal> SetBudgetGoalAsync(Guid userId, string category, decimal limit, DateTime start, DateTime? end)
    {
        var goal = await db.BudgetGoals
            .FirstOrDefaultAsync(g => g.UserId == userId && g.Category == category);

        if (goal == null)
        {
            goal = new BudgetGoal
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Category = category,
                MonthlyLimit = limit,
                StartDate = start,
                EndDate = end,
                CreatedAt = DateTime.UtcNow
            };
            db.BudgetGoals.Add(goal);
        }
        else
        {
            goal.MonthlyLimit = limit;
            goal.StartDate = start;
            goal.EndDate = end;
            db.BudgetGoals.Update(goal);
        }

        await db.SaveChangesAsync();
        return goal;
    }

    public async Task<IReadOnlyList<(BudgetGoal, decimal)>> GetTriggeredBudgetAlertsAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var goals = await db.BudgetGoals
            .Where(g => g.UserId == userId && g.StartDate <= now && (g.EndDate == null || g.EndDate >= now))
            .ToListAsync();

        var txns = await db.Transactions
            .Where(t => t.UserId == userId && t.CreatedAt >= monthStart && t.Status == TransactionStatus.Success)
            .ToListAsync();

        var alerts = new List<(BudgetGoal, decimal)>();

        foreach (var g in goals)
        {
            var spent = txns
                .Where(t => t.RecipientName?.ToLower().Contains(g.Category.ToLower()) == true)
                .Sum(t => t.Amount);

            if (spent >= g.MonthlyLimit * 0.9m)
                alerts.Add((g, spent));
        }

        return alerts;
    }
}
