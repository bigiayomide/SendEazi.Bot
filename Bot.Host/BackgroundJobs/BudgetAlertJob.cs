using Bot.Core.Models;
using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Bot.Host.BackgroundJobs;

[DisallowConcurrentExecution]
public class BudgetAlertJob(
    ApplicationDbContext db,
    IBudgetService budgetService,
    INotificationService notification,
    ILogger<BudgetAlertJob> log)
    : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        log.LogInformation("📊 BudgetAlertJob started at {Time}", context.FireTimeUtc);

        var userIds = await db.Users
            .AsNoTracking()
            .Where(u => u.BudgetGoals.Any())
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in userIds)
            try
            {
                var alerts = await budgetService.GetTriggeredBudgetAlertsAsync(userId);

                foreach (var (goal, spent) in alerts)
                {
                    await notification.SendBudgetAlertAsync(goal.UserId, new BudgetAlert
                    {
                        Category = goal.Category,
                        Limit = goal.MonthlyLimit,
                        Spent = spent
                    });

                    log.LogInformation("⚠️ Alert sent: {UserId} - {Category}: ₦{Spent:N0} / ₦{Limit:N0}",
                        goal.UserId, goal.Category, spent, goal.MonthlyLimit);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "❌ Failed processing alerts for user {UserId}", userId);
            }

        log.LogInformation("✅ BudgetAlertJob finished at {Time}", DateTime.UtcNow);
    }
}