using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Bot.Shared.DTOs;
using Bot.Shared.Models;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NCrontab;

namespace Bot.Tests.Services;

public class RecurringTransferServiceTests
{
    private ApplicationDbContext CreateDb(string name)
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options);
    }

    [Fact]
    public async Task Should_Publish_TransferCmd_For_Due_Transfer()
    {
        var userId = Guid.NewGuid();
        var payee = new Payee
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountNumber = "1234567890",
            BankCode = "058",
            CreatedAt = DateTime.UtcNow
        };

        var recurring = new RecurringTransfer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PayeeId = payee.Id,
            Amount = 1500,
            CronExpression = "* * * * *", // runs every minute
            NextRun = DateTime.UtcNow.AddMinutes(-1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var db = CreateDb("due-transfer");
        db.Payees.Add(payee);
        db.RecurringTransfers.Add(recurring);
        await db.SaveChangesAsync();

        var published = new List<TransferCmd>();
        var bus = new Mock<IPublishEndpoint>();
        bus.Setup(b => b.Publish(It.IsAny<TransferCmd>(), default))
            .Callback<object, CancellationToken>((m, _) => published.Add((TransferCmd)m))
            .Returns(Task.CompletedTask);

        var refGen = new ReferenceGenerator();
        var service =
            new RecurringTransferService(db, Mock.Of<ILogger<RecurringTransferService>>(), refGen, bus.Object);

        // Act
        await service.ProcessDueTransfersAsync();

        // Assert
        published.Should().HaveCount(1);
        published[0].CorrelationId.Should().Be(userId);
        published[0].Payload.Amount.Should().Be(1500);

        var updated = await db.RecurringTransfers.FindAsync(recurring.Id);
        updated!.NextRun.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Should_Skip_If_Not_Due_Yet()
    {
        var userId = Guid.NewGuid();
        var db = CreateDb("skip-not-due");

        var payeeId = Guid.NewGuid();
        db.Payees.Add(new Payee
        {
            Id = payeeId,
            UserId = userId,
            AccountNumber = "1",
            BankCode = "058"
        });

        db.RecurringTransfers.Add(new RecurringTransfer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = 999,
            PayeeId = payeeId,
            CronExpression = "* * * * *",
            NextRun = DateTime.UtcNow.AddHours(1),
            IsActive = true
        });

        await db.SaveChangesAsync();

        var bus = new Mock<IPublishEndpoint>();
        var refGen = new ReferenceGenerator();

        var service =
            new RecurringTransferService(db, Mock.Of<ILogger<RecurringTransferService>>(), refGen, bus.Object);
        await service.ProcessDueTransfersAsync();

        bus.Verify(x => x.Publish(It.IsAny<TransferCmd>(), default), Times.Never);
    }

    [Fact]
    public async Task Should_Skip_If_Payee_Missing()
    {
        var userId = Guid.NewGuid();
        var db = CreateDb("missing-payee");

        db.RecurringTransfers.Add(new RecurringTransfer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PayeeId = Guid.NewGuid(), // invalid
            Amount = 500,
            CronExpression = "* * * * *",
            NextRun = DateTime.UtcNow.AddMinutes(-1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var bus = new Mock<IPublishEndpoint>();
        var refGen = new ReferenceGenerator();
        var service =
            new RecurringTransferService(db, Mock.Of<ILogger<RecurringTransferService>>(), refGen, bus.Object);

        await service.ProcessDueTransfersAsync();

        bus.Verify(x => x.Publish(It.IsAny<TransferCmd>(), default), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Fail_If_CronExpression_Is_Invalid()
    {
        var userId = Guid.NewGuid();
        var payee = new Payee
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountNumber = "invalid",
            BankCode = "000"
        };

        var db = CreateDb("invalid-cron");
        db.Payees.Add(payee);
        db.RecurringTransfers.Add(new RecurringTransfer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PayeeId = payee.Id,
            Amount = 100,
            CronExpression = "", // simulate invalid input
            NextRun = DateTime.UtcNow.AddMinutes(-1),
            IsActive = true
        });

        await db.SaveChangesAsync();

        var bus = new Mock<IPublishEndpoint>();
        var refGen = new ReferenceGenerator();
        var service =
            new RecurringTransferService(db, Mock.Of<ILogger<RecurringTransferService>>(), refGen, bus.Object);

        var act = async () => await service.ProcessDueTransfersAsync();

        await act.Should().NotThrowAsync(); // handles it gracefully
    }

    [Fact]
    public async Task ScheduleAsync_InvalidCron_Should_Throw()
    {
        var db = CreateDb("schedule-invalid");

        var service = new RecurringTransferService(
            db,
            Mock.Of<ILogger<RecurringTransferService>>(),
            new ReferenceGenerator(),
            new Mock<IPublishEndpoint>().Object);

        var payload = new RecurringPayload(
            Guid.NewGuid(),
            new TransferPayload("1234567890", "058", 50m, null),
            "invalid cron");

        var act = () => service.ScheduleAsync(Guid.NewGuid(), payload);

        await act.Should().ThrowAsync<CrontabException>();
    }

    [Fact]
    public async Task Schedule_And_Cancel_Should_Work()
    {
        var db = CreateDb("schedule-cancel");
        var bus = new Mock<IPublishEndpoint>();
        var refGen = new ReferenceGenerator();
        var service =
            new RecurringTransferService(db, Mock.Of<ILogger<RecurringTransferService>>(), refGen, bus.Object);

        var userId = Guid.NewGuid();
        var payload = new RecurringPayload(
            Guid.NewGuid(),
            new TransferPayload("1111111111", "058", 500, null),
            "* * * * *");

        var now = DateTime.UtcNow;
        var expectedNext = CrontabSchedule.Parse(payload.Cron).GetNextOccurrence(now);

        var recId = await service.ScheduleAsync(userId, payload);

        var rec = await db.RecurringTransfers.Include(r => r.Payee).FirstAsync(r => r.Id == recId);
        db.Payees.Should().HaveCount(1);
        rec.Payee.AccountNumber.Should().Be("1111111111");
        rec.NextRun.Should().BeCloseTo(expectedNext, TimeSpan.FromSeconds(1));

        await service.CancelRecurringAsync(recId);
        (await db.RecurringTransfers.FindAsync(recId))!.IsActive.Should().BeFalse();
    }
}