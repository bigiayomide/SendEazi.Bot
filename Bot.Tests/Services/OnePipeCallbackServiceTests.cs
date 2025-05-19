using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bot.Tests.Services;

public class OnePipeCallbackServiceTests
{
    private static ApplicationDbContext CreateDb(string name)
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options);
    }

    [Fact]
    public async Task Should_Update_Transaction_On_Success()
    {
        var db = CreateDb("onepipe-success");
        var txId = Guid.NewGuid();
        db.Transactions.Add(new Transaction
        {
            Id = txId,
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Reference = "op-ref-1",
            Amount = 0,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var logger = new LoggerFactory().CreateLogger<OnePipeCallbackService>();
        var service = new OnePipeCallbackService(db, logger);

        var resp = new OnePipeResp("Successful", "", new OnePipeData(3000, "tref"));
        await service.HandleCallbackAsync("op-ref-1", resp);

        var updated = await db.Transactions.FindAsync(txId);
        updated!.Status.Should().Be(TransactionStatus.Success);
        updated.Amount.Should().Be(3000);
        updated.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Update_Transaction_On_Failure()
    {
        var db = CreateDb("onepipe-fail");
        var txId = Guid.NewGuid();
        db.Transactions.Add(new Transaction
        {
            Id = txId,
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Reference = "op-ref-2",
            Amount = 500,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var logger = new LoggerFactory().CreateLogger<OnePipeCallbackService>();
        var service = new OnePipeCallbackService(db, logger);

        var resp = new OnePipeResp("Failed", "", new OnePipeData(0, "tref"));
        await service.HandleCallbackAsync("op-ref-2", resp);

        var updated = await db.Transactions.FindAsync(txId);
        updated!.Status.Should().Be(TransactionStatus.Failed);
        updated.Amount.Should().Be(500); // unchanged
        updated.CompletedAt.Should().NotBeNull();
    }
}