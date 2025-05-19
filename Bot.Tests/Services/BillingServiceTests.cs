using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Bot.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Bot.Tests.Services;

public class BillingServiceTests
{
    private ApplicationDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    [Fact]
    public async Task HandleTransferCompletedAsync_Should_Create_Fee_Record()
    {
        var db = CreateDb("fee-create");
        var txId = Guid.NewGuid();
        db.Transactions.Add(new Transaction
        {
            Id = txId,
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Amount = 1000m,
            Reference = "ref",
            CreatedAt = DateTime.UtcNow,
            Status = 0
        });
        await db.SaveChangesAsync();

        var service = new BillingService(db, Mock.Of<ILogger<BillingService>>());
        await service.HandleTransferCompletedAsync(txId);

        var record = await db.FeeRecords.FirstOrDefaultAsync(f => f.TransactionId == txId);
        record.Should().NotBeNull();
        record!.Amount.Should().Be(50m + 1000m * 0.01m);
        record.Swept.Should().BeFalse();
    }

    [Fact]
    public async Task SweepFeesAsync_Should_Mark_Fees_As_Swept_And_Log()
    {
        var db = CreateDb("fee-sweep");
        db.FeeRecords.Add(new FeeRecord { Id = Guid.NewGuid(), TransactionId = Guid.NewGuid(), Amount = 10m, CreatedAt = DateTime.UtcNow, Swept = false });
        db.FeeRecords.Add(new FeeRecord { Id = Guid.NewGuid(), TransactionId = Guid.NewGuid(), Amount = 20m, CreatedAt = DateTime.UtcNow, Swept = false });
        await db.SaveChangesAsync();

        var logger = new Mock<ILogger<BillingService>>();
        var service = new BillingService(db, logger.Object);

        await service.SweepFeesAsync();

        var remaining = await db.FeeRecords.CountAsync(f => !f.Swept);
        remaining.Should().Be(0);

        logger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Swept")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }
}
