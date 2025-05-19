using Bot.Core.Providers;
using Bot.Core.StateMachine.Consumers.Payments;
using Bot.Infrastructure.Data;
using Bot.Shared;
using Bot.Shared.DTOs;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using Bot.Tests.TestUtilities;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Bot.Tests.Consumers;

public class TransferCmdConsumerTests
{
    [Fact]
    public async Task Should_Create_Transaction_On_Valid_Transfer()
    {
        var userId = Guid.NewGuid();
        var mandateId = "mandate-ok";

        var providerMock = new Mock<IBankProvider>();
        providerMock
            .Setup(p => p.InitiateDebitAsync(mandateId, 1000, "txn:ok", "pay me"))
            .ReturnsAsync("provider-txn");

        var harness = await TestContextHelper.BuildTestHarness<TransferCmdConsumer>(services =>
        {
            services.AddMockBankFactory(userId, providerMock.Object);
        });

        var db = harness.Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.SeedUserAsync(userId);
        await db.SeedMandateAsync(userId, mandateId);

        var bus = harness.Bus;
        var cmd = new TransferCmd(userId, new TransferPayload("123", "058", 1000, "pay me"), "txn:ok");

        await bus.Publish(cmd);

        var consumerHarness = harness.GetConsumerHarness<TransferCmdConsumer>();
        (await consumerHarness.Consumed.Any<TransferCmd>()).Should().BeTrue();

        var tx = await db.Transactions.FirstOrDefaultAsync(x => x.Reference == "txn:ok");
        tx.Should().NotBeNull();
        tx!.Amount.Should().Be(1000);
        tx.Status.Should().Be(TransactionStatus.Pending);

        await harness.Stop();
    }

    [Fact]
    public async Task Should_Publish_TransferFailed_If_UserNotFound()
    {
        var harness = await TestContextHelper.BuildTestHarness<TransferCmdConsumer>(services =>
        {
            services.AddMockBankFactory(Guid.NewGuid(), new Mock<IBankProvider>().Object);
        });

        var cmd = new TransferCmd(Guid.NewGuid(), new TransferPayload("x", "y", 1000, null), "txn:user-missing");
        await harness.Bus.Publish(cmd);

        (await harness.Published.Any<TransferFailed>()).Should().BeTrue();
        var failed = (await harness.Published.SelectAsync<TransferFailed>().First()).Context.Message;

        failed.Reason.Should().Be("User not found");
        failed.Reference.Should().Be("txn:user-missing");

        await harness.Stop();
    }

    [Fact]
    public async Task Should_Publish_TransferFailed_If_MandateMissing()
    {
        var userId = Guid.NewGuid();
        var harness = await TestContextHelper.BuildTestHarness<TransferCmdConsumer>(services =>
        {
            services.AddMockBankFactory(userId, new Mock<IBankProvider>().Object);
        });

        var db = harness.Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.SeedUserAsync(userId);

        var cmd = new TransferCmd(userId, new TransferPayload("1", "044", 200, "test"), "txn:mandate-fail");
        await harness.Bus.Publish(cmd);

        (await harness.Published.Any<TransferFailed>()).Should().BeTrue();
        var failed = (await harness.Published.SelectAsync<TransferFailed>().First()).Context.Message;

        failed.Reason.Should().Be("No active mandate");
        failed.Reference.Should().Be("txn:mandate-fail");

        await harness.Stop();
    }

    [Fact]
    public async Task Should_Publish_TransferFailed_If_ProviderThrows()
    {
        var userId = Guid.NewGuid();
        var dbName = "provider-fail";

        var provider = new Mock<IBankProvider>();
        provider.Setup(p =>
                p.InitiateDebitAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("fail"));

        var harness =
            await TestContextHelper.BuildTestHarness<TransferCmdConsumer>(
                services => { services.AddMockBankFactory(userId, provider.Object); }, dbName);

        var db = harness.Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.SeedUserAsync(userId);
        await db.SeedMandateAsync(userId, "mandate-boom");

        var cmd = new TransferCmd(userId, new TransferPayload("to", "bank", 1000, "fail"), "txn:boom");
        await harness.Bus.Publish(cmd);

        (await harness.Published.Any<TransferFailed>()).Should().BeTrue();
        var failed = (await harness.Published.SelectAsync<TransferFailed>().First()).Context.Message;

        failed.Reference.Should().Be("txn:boom");
        failed.Reason.Should().Be("Provider error");

        await harness.Stop();
    }

    [Fact]
    public async Task Should_Skip_Duplicate_Reference()
    {
        var userId = Guid.NewGuid();
        var reference = "txn:dupe";

        var harness = await TestContextHelper.BuildTestHarness<TransferCmdConsumer>(
            services => { services.AddMockBankFactory(userId, new Mock<IBankProvider>().Object); }, "dupe-test");

        var db = harness.Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.SeedUserAsync(userId);
        db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = Guid.NewGuid(),
            Amount = 1000,
            Reference = reference,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var cmd = new TransferCmd(userId, new TransferPayload("x", "y", 1000, "dupe"), reference);
        await harness.Bus.Publish(cmd);

        var count = await db.Transactions.CountAsync(t => t.Reference == reference);
        count.Should().Be(1); // No duplicate added

        await harness.Stop();
    }
}