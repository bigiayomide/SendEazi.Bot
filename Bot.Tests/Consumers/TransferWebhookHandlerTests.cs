using Bot.Core.Services;
using Bot.Core.StateMachine;
using Bot.Core.StateMachine.Consumers.Payments;
using Bot.Infrastructure.Data;
using Bot.Shared.DTOs;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Bot.Tests.Consumers;

public class TransferWebhookHandlerTests
{
    private ApplicationDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    [Fact]
    public async Task Should_Update_Transaction_And_Publish_PreviewCmd_On_Success()
    {
        var userId = Guid.NewGuid();
        var reference = "txn:abc";
        var db = CreateDb("webhook-success");

        var txId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            PhoneNumber = "+2348100000000",
            FullName = "Test User",
            TenantId = Guid.NewGuid(),
            BVNEnc = "x", BVNHash = "x", NINEnc = "x", NINHash = "x", SignupSource = "test", BankAccessToken = "x"
        });

        db.Transactions.Add(new Transaction
        {
            Id = txId,
            UserId = userId,
            TenantId = Guid.NewGuid(),
            Reference = reference,
            Amount = 1000,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var contextMock = new Mock<ConsumeContext<TransferCompleted>>();
        contextMock.Setup(c => c.Message).Returns(new TransferCompleted(userId, reference));
        contextMock.Setup(c => c.Publish(It.IsAny<PreviewCmd>(), default)).Returns(Task.CompletedTask);

        var handler = new TransferWebhookHandler(db, Mock.Of<IWhatsAppService>(), Mock.Of<ILogger<TransferWebhookHandler>>());

        await handler.Consume(contextMock.Object);

        var updated = await db.Transactions.FirstOrDefaultAsync(t => t.Reference == reference);
        updated!.Status.Should().Be(TransactionStatus.Success);
        updated.CompletedAt.Should().NotBeNull();

        contextMock.Verify(c =>
            c.Publish(It.Is<PreviewCmd>(cmd => cmd.TransactionId == txId), default),
            Times.Once);
    }

    [Fact]
    public async Task Should_Update_Transaction_And_Publish_Preview_On_Failure()
    {
        var userId = Guid.NewGuid();
        var reference = "txn:fail";
        var db = CreateDb("webhook-fail");

        var txId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            PhoneNumber = "+2348111111111",
            FullName = "Fail User",
            TenantId = Guid.NewGuid(),
            BVNEnc = "x", BVNHash = "x", NINEnc = "x", NINHash = "x", SignupSource = "test", BankAccessToken = "x"
        });

        db.Transactions.Add(new Transaction
        {
            Id = txId,
            UserId = userId,
            TenantId = Guid.NewGuid(),
            Reference = reference,
            Amount = 2000,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var contextMock = new Mock<ConsumeContext<TransferFailed>>();
        contextMock.Setup(c => c.Message).Returns(new TransferFailed(userId, "Insufficient funds", reference));
        contextMock.Setup(c => c.Publish(It.IsAny<PreviewCmd>(), default)).Returns(Task.CompletedTask);

        var handler = new TransferWebhookHandler(db, Mock.Of<IWhatsAppService>(), Mock.Of<ILogger<TransferWebhookHandler>>());

        await handler.Consume(contextMock.Object);

        var updated = await db.Transactions.FirstOrDefaultAsync(t => t.Reference == reference);
        updated!.Status.Should().Be(TransactionStatus.Failed);
        updated.CompletedAt.Should().NotBeNull();

        contextMock.Verify(c =>
            c.Publish(It.Is<PreviewCmd>(cmd => cmd.TransactionId == txId), default),
            Times.Never);
    }

    [Fact]
    public async Task Should_Skip_When_Transaction_Not_Found()
    {
        var db = CreateDb("webhook-notfound");
        var context = Mock.Of<ConsumeContext<TransferCompleted>>(c =>
            c.Message == new TransferCompleted(Guid.NewGuid(), "txn:missing"));

        var handler = new TransferWebhookHandler(db, Mock.Of<IWhatsAppService>(), Mock.Of<ILogger<TransferWebhookHandler>>());

        var act = async () => await handler.Consume(context);

        await act.Should().NotThrowAsync(); // Gracefully skip
    }
}
