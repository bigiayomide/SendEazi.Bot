using Bot.Core.Models;
using Bot.Core.Services;
using Bot.Host.BackgroundJobs;
using Bot.Shared.Models;
using Bot.Tests.TestUtilities;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Assert = Xunit.Assert;

namespace Bot.Tests.BackgroundJobs;

public class BudgetAlertJobTests
{
    [Fact]
    public async Task Execute_Dispatches_Notifications_For_Each_Alert()
    {
        var db = await TestContextHelper.SetupInMemoryDb("budget-alerts");
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        await db.SeedUserAsync(user1);
        await db.SeedUserAsync(user2);

        var goal1 = new BudgetGoal
        {
            Id = Guid.NewGuid(),
            UserId = user1,
            Category = "food",
            MonthlyLimit = 1000,
            CreatedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow.AddDays(-30)
        };
        var goal2 = new BudgetGoal
        {
            Id = Guid.NewGuid(),
            UserId = user2,
            Category = "travel",
            MonthlyLimit = 2000,
            CreatedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow.AddDays(-30)
        };
        db.BudgetGoals.AddRange(goal1, goal2);
        await db.SaveChangesAsync();

        var alerts1 = new List<(BudgetGoal, decimal)> { (goal1, 900m) };
        var alerts2 = new List<(BudgetGoal, decimal)> { (goal2, 1500m) };
        var budgetSvc = new Mock<IBudgetService>();
        budgetSvc.Setup(s => s.GetTriggeredBudgetAlertsAsync(user1)).ReturnsAsync(alerts1);
        budgetSvc.Setup(s => s.GetTriggeredBudgetAlertsAsync(user2)).ReturnsAsync(alerts2);

        var sent = new List<(Guid, BudgetAlert)>();
        var notification = new Mock<INotificationService>();
        notification
            .Setup(n => n.SendBudgetAlertAsync(It.IsAny<Guid>(), It.IsAny<BudgetAlert>()))
            .Callback<Guid, BudgetAlert>((uid, alert) => sent.Add((uid, alert)))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger<BudgetAlertJob>>();
        var job = new BudgetAlertJob(db, budgetSvc.Object, notification.Object, logger.Object);
        var context = new Mock<IJobExecutionContext>();
        context.SetupGet(c => c.FireTimeUtc).Returns(DateTimeOffset.UtcNow);

        await job.Execute(context.Object);

        Assert.Equal(2, sent.Count);
        notification.Verify(n => n.SendBudgetAlertAsync(user1, It.IsAny<BudgetAlert>()), Times.Once);
        notification.Verify(n => n.SendBudgetAlertAsync(user2, It.IsAny<BudgetAlert>()), Times.Once);
    }

    [Fact]
    public async Task Execute_Logs_Errors_And_Continues()
    {
        var db = await TestContextHelper.SetupInMemoryDb("budget-alert-errors");
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        await db.SeedUserAsync(user1);
        await db.SeedUserAsync(user2);

        var goal1 = new BudgetGoal
        {
            Id = Guid.NewGuid(),
            UserId = user1,
            Category = "food",
            MonthlyLimit = 1000,
            CreatedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow.AddDays(-30)
        };
        var goal2 = new BudgetGoal
        {
            Id = Guid.NewGuid(),
            UserId = user2,
            Category = "travel",
            MonthlyLimit = 2000,
            CreatedAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow.AddDays(-30)
        };
        db.BudgetGoals.AddRange(goal1, goal2);
        await db.SaveChangesAsync();

        var budgetSvc = new Mock<IBudgetService>();
        budgetSvc.Setup(s => s.GetTriggeredBudgetAlertsAsync(user1))
            .ThrowsAsync(new Exception("fail"));
        var alerts2 = new List<(BudgetGoal, decimal)> { (goal2, 1500m) };
        budgetSvc.Setup(s => s.GetTriggeredBudgetAlertsAsync(user2))
            .ReturnsAsync(alerts2);

        var sent = new List<(Guid, BudgetAlert)>();
        var notification = new Mock<INotificationService>();
        notification
            .Setup(n => n.SendBudgetAlertAsync(It.IsAny<Guid>(), It.IsAny<BudgetAlert>()))
            .Callback<Guid, BudgetAlert>((uid, alert) => sent.Add((uid, alert)))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger<BudgetAlertJob>>();
        var job = new BudgetAlertJob(db, budgetSvc.Object, notification.Object, logger.Object);
        var context = new Mock<IJobExecutionContext>();
        context.SetupGet(c => c.FireTimeUtc).Returns(DateTimeOffset.UtcNow);

        await job.Execute(context.Object);

        Assert.Single(sent);
        notification.Verify(n => n.SendBudgetAlertAsync(user2, It.IsAny<BudgetAlert>()), Times.Once);
        logger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}