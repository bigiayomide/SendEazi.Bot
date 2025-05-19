using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bot.Tests.Services;

public class MonoCallbackServiceTests
{
    private static ApplicationDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    [Fact]
    public async Task Should_Update_Transaction_On_Success_Event()
    {
        var db = CreateDb("mono-success");
        var txId = Guid.NewGuid();
        db.Transactions.Add(new Transaction
        {
            Id = txId,
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Reference = "mono-ref-1",
            Amount = 0,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var logger = new LoggerFactory().CreateLogger<MonoCallbackService>();
        var service = new MonoCallbackService(db, logger);

        var data = new MonoData("evt1", "", new MonoTxn("t1", "success", "mono-ref-1", 1500));
        await service.HandleCallbackAsync("direct_debit.payment_successful", data);

        var updated = await db.Transactions.FindAsync(txId);
        updated!.Status.Should().Be(TransactionStatus.Success);
        updated.Amount.Should().Be(1500);
        updated.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Mark_Failed_On_Failure_Event()
    {
        var db = CreateDb("mono-fail");
        var txId = Guid.NewGuid();
        db.Transactions.Add(new Transaction
        {
            Id = txId,
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Reference = "mono-ref-2",
            Amount = 1000,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var logger = new LoggerFactory().CreateLogger<MonoCallbackService>();
        var service = new MonoCallbackService(db, logger);

        var data = new MonoData("evt2", "", new MonoTxn("t2", "failed", "mono-ref-2", 0));
        await service.HandleCallbackAsync("direct_debit.payment_failed", data);

        var updated = await db.Transactions.FindAsync(txId);
        updated!.Status.Should().Be(TransactionStatus.Failed);
        updated.Amount.Should().Be(1000); // unchanged
        updated.CompletedAt.Should().NotBeNull();
    }
}
