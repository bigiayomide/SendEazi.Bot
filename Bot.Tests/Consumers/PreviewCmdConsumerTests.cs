using Bot.Core.Services;
using Bot.Core.StateMachine;
using Bot.Core.StateMachine.Consumers.UX;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using Bot.Tests.TestUtilities;
using FluentAssertions;
using MassTransit;
using Moq;

namespace Bot.Tests.Consumers;

public class PreviewCmdConsumerTests
{
    [Fact]
    public async Task Should_Send_Transaction_Preview_When_TransactionId_Is_Present()
    {
        var userId = Guid.NewGuid();
        var txId = Guid.NewGuid();
        var dbName = "preview-tx";

        var db = await TestContextHelper.SetupInMemoryDb(dbName);

        var tx = new Transaction
        {
            Id = txId,
            UserId = userId,
            Amount = 2500,
            Reference = "txn:abc",
            RecipientName = "John Doe",
            Status = TransactionStatus.Success,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
        db.Transactions.Add(tx);
        await db.SaveChangesAsync();

        var wa = new Mock<IWhatsAppService>();
        var userSvc = new Mock<IUserService>();
        userSvc.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync(new User
        {
            Id = userId,
            PhoneNumber = "2349000000000"
        });

        var cmd = new PreviewCmd(userId, TransactionId: txId);
        var ctx = Mock.Of<ConsumeContext<PreviewCmd>>(c => c.Message == cmd);

        var consumer = new PreviewCmdConsumer(wa.Object, db, userSvc.Object);

        await consumer.Consume(ctx);

        wa.Verify(w => w.SendTemplateAsync(
            "2349000000000",
            It.Is<object>(o => o.GetType().GetProperty("title").GetValue(o).ToString() == "✅ Transfer Successful")
        ), Times.Once);

    }

    [Fact]
    public async Task Should_Send_Bill_Preview_When_BillId_Is_Present()
    {
        var userId = Guid.NewGuid();
        var billId = Guid.NewGuid();
        var db = await TestContextHelper.SetupInMemoryDb("preview-bill");

        db.BillPayments.Add(new BillPayment
        {
            Id = billId,
            UserId = userId,
            Amount = 3000,
            Biller = BillerEnum.Electricity,
            IsPaid = true,
            CreatedAt = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var wa = new Mock<IWhatsAppService>();
        var userSvc = new Mock<IUserService>();
        userSvc.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync(new User
        {
            Id = userId,
            PhoneNumber = "+2348123456789"
        });

        var cmd = new PreviewCmd(userId, BillId: billId);
        var ctx = Mock.Of<ConsumeContext<PreviewCmd>>(c => c.Message == cmd);
        var consumer = new PreviewCmdConsumer(wa.Object, db, userSvc.Object);

        await consumer.Consume(ctx);

        wa.Verify(w => w.SendTemplateAsync(
            "+2348123456789",
            It.Is<object>(o => o.GetType().GetProperty("title").GetValue(o).ToString() == "✅ Bill Payment Successful")
        ), Times.Once);
    }

    [Fact]
    public async Task Should_Fallback_If_Neither_Id_Provided()
    {
        var userId = Guid.NewGuid();
        var db = await TestContextHelper.SetupInMemoryDb("preview-fallback");

        var wa = new Mock<IWhatsAppService>();
        var userSvc = new Mock<IUserService>();
        userSvc.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync(new User
        {
            Id = userId,
            PhoneNumber = "+2348111111111"
        });

        var cmd = new PreviewCmd(userId);
        var ctx = Mock.Of<ConsumeContext<PreviewCmd>>(c => c.Message == cmd);

        var consumer = new PreviewCmdConsumer(wa.Object, db, userSvc.Object);

        await consumer.Consume(ctx);

        wa.Verify(w => w.SendTemplateAsync("+2348111111111", It.Is<object>(o =>
            o.ToString()!.Contains("No preview available"))), Times.Once);
    }

    [Fact]
    public async Task Should_Gracefully_Handle_Missing_User()
    {
        var userId = Guid.NewGuid();
        var txId = Guid.NewGuid();
        var db = await TestContextHelper.SetupInMemoryDb("preview-missing-user");

        db.Transactions.Add(new Transaction
        {
            Id = txId,
            UserId = userId,
            Amount = 1000,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Reference = "txn:x",
            Status = TransactionStatus.Success
        });
        await db.SaveChangesAsync();

        var wa = new Mock<IWhatsAppService>();
        var userSvc = new Mock<IUserService>();
        userSvc.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync((User?)null); // simulate user not found

        var cmd = new PreviewCmd(userId, TransactionId: txId);
        var ctx = Mock.Of<ConsumeContext<PreviewCmd>>(c => c.Message == cmd);

        var consumer = new PreviewCmdConsumer(wa.Object, db, userSvc.Object);

        var act = async () => await consumer.Consume(ctx);

        await act.Should().NotThrowAsync(); // test passes if it fails gracefully
        wa.Verify(w => w.SendTemplateAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }
}
