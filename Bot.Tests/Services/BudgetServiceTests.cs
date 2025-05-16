using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Bot.Tests.Services;

public class BudgetServiceTests
{
    private static ApplicationDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    [Fact]
    public async Task Should_Trigger_Alert_If_90Percent_Spent()
    {
        var userId = Guid.NewGuid();
        var db = CreateDb("goal-hit");

        db.BudgetGoals.Add(new BudgetGoal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Category = "rent",
            MonthlyLimit = 10000,
            StartDate = new DateTimeOffset(DateTime.UtcNow.AddDays(-5)),
            EndDate = DateTime.UtcNow.AddDays(30)
        });

        db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = 9500,
            Status = TransactionStatus.Success,
            CreatedAt = DateTime.UtcNow,
            RecipientName = "rent monthly",
            Reference = "Amazon"
        });

        await db.SaveChangesAsync();

        var service = new BudgetService(db);
        var results = await service.GetTriggeredBudgetAlertsAsync(userId);

        results.Should().ContainSingle();
        results[0].Item2.Should().Be(9500);
        results[0].Item1.MonthlyLimit.Should().Be(10000);
    }

    [Fact]
    public async Task Should_Not_Trigger_If_Spent_Is_Low()
    {
        var userId = Guid.NewGuid();
        var db = CreateDb("goal-low");

        db.BudgetGoals.Add(new BudgetGoal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Category = "travel",
            MonthlyLimit = 5000,
            StartDate = DateTime.UtcNow.AddDays(-5),
            EndDate = DateTime.UtcNow.AddDays(30)
        });

        db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = 1000,
            Status = TransactionStatus.Success,
            CreatedAt = DateTime.UtcNow,
            RecipientName = "Uber travel",
            Reference = "Amazon"
        });

        await db.SaveChangesAsync();

        var service = new BudgetService(db);
        var results = await service.GetTriggeredBudgetAlertsAsync(userId);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Handle_No_Goals_Gracefully()
    {
        var db = CreateDb("no-goals");

        var service = new BudgetService(db);
        var results = await service.GetTriggeredBudgetAlertsAsync(Guid.NewGuid());

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Handle_No_Matching_Transactions()
    {
        var userId = Guid.NewGuid();
        var db = CreateDb("no-match");

        db.BudgetGoals.Add(new BudgetGoal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Category = "groceries",
            MonthlyLimit = 8000,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(10)
        });

        // Transaction doesn't match category text
        db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = 5000,
            Status = TransactionStatus.Success,
            CreatedAt = DateTime.UtcNow,
            RecipientName = "Amazon",
            Reference = "Amazon"
        });

        await db.SaveChangesAsync();

        var service = new BudgetService(db);
        var results = await service.GetTriggeredBudgetAlertsAsync(userId);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Ignore_Non_Success_Transactions()
    {
        var userId = Guid.NewGuid();
        var db = CreateDb("non-success");

        db.BudgetGoals.Add(new BudgetGoal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Category = "rent",
            MonthlyLimit = 6000,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30)
        });

        db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = 6000,
            Status = TransactionStatus.Failed,
            CreatedAt = DateTime.UtcNow,
            RecipientName = "rent payment",
            Reference = "Amazon"
        });

        await db.SaveChangesAsync();

        var service = new BudgetService(db);
        var results = await service.GetTriggeredBudgetAlertsAsync(userId);

        results.Should().BeEmpty();
    }
}
