using Bot.Core.Providers;
using Bot.Core.Services;
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

public class PaymentConsumersTests
{
    [Fact]
    public async Task BalanceCmd_Should_Publish_BalanceSent()
    {
        var userId = Guid.NewGuid();
        var provider = new Mock<IBankProvider>();
        provider.Setup(p => p.GetBalanceAsync(It.IsAny<string>())).ReturnsAsync(500m);

        var userSvc = new Mock<IUserService>();
        userSvc.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(new User { Id = userId, PhoneNumber = "234" });

        var harness = await TestContextHelper.BuildTestHarness<BalanceCmdConsumer>(services =>
        {
            services.AddMockBankFactory(userId, provider.Object);
            services.AddSingleton(userSvc.Object);
        });

        await harness.Bus.Publish(new BalanceCmd(userId));

        (await harness.Published.Any<BalanceSent>()).Should().BeTrue();
        var sent = (await harness.Published.SelectAsync<BalanceSent>().First()).Context.Message;
        sent.CorrelationId.Should().Be(userId);
        sent.Amount.Should().Be(500m);

        await harness.Stop();
    }

    [Fact]
    public async Task BillPayCmd_Should_Publish_BillPaid_When_Successful()
    {
        var userId = Guid.NewGuid();
        var billSvc = new Mock<IBillPayService>();
        var billId = Guid.NewGuid();
        billSvc.Setup(b => b.PayBillAsync(userId, It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new BillPayment
                { Id = billId, UserId = userId, IsPaid = true, Biller = BillerEnum.Electricity });

        var userSvc = new Mock<IUserService>();
        userSvc.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(new User { Id = userId });

        var harness = await TestContextHelper.BuildTestHarness<BillPayCmdConsumer>(services =>
        {
            services.AddSingleton(billSvc.Object);
            services.AddSingleton(userSvc.Object);
        });

        await harness.Bus.Publish(new BillPayCmd(userId, new BillPayload("b", "ref", 100m, null)));

        (await harness.Published.Any<BillPaid>()).Should().BeTrue();
        var evt = (await harness.Published.SelectAsync<BillPaid>().First()).Context.Message;
        evt.CorrelationId.Should().Be(userId);
        evt.BillId.Should().Be(billId);

        await harness.Stop();
    }

    [Fact]
    public async Task RecurringCmd_Should_Create_Record_And_Publish()
    {
        var userId = Guid.NewGuid();

        var harness = await TestContextHelper.BuildTestHarness<RecurringCmdConsumer>();
        var db = harness.Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.SeedUserAsync(userId);

        var payload = new RecurringPayload(Guid.NewGuid(), new TransferPayload("123", "001", 50m, null), "*");
        await harness.Bus.Publish(new RecurringCmd(userId, payload));

        (await harness.Published.Any<RecurringExecuted>()).Should().BeTrue();
        (await db.RecurringTransfers.CountAsync()).Should().Be(1);
        await harness.Stop();
    }

    [Fact]
    public async Task RecurringCancelCmd_Should_Set_Inactive()
    {
        var userId = Guid.NewGuid();
        var recId = Guid.NewGuid();

        var harness = await TestContextHelper.BuildTestHarness<RecurringCancelCmdConsumer>();

        // Create a scoped db context for seeding
        using var seedScope = harness.Scope.ServiceProvider.CreateScope();
        var seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await seedDb.RecurringTransfers.AddAsync(new RecurringTransfer
        {
            Id = recId,
            UserId = userId,
            IsActive = true,
            CronExpression = "0 */5 * * * ?",
            CreatedAt = DateTime.UtcNow,
            NextRun = DateTime.UtcNow.AddMinutes(5)
        });

        await seedDb.SaveChangesAsync();

        await harness.Bus.Publish(new RecurringCancelCmd(userId, recId));

        // ✅ Ensure the message was consumed
        Assert.True(await harness.Consumed.Any<RecurringCancelCmd>(), "Message was not consumed");

        // 🔁 Use fresh scope to verify update
        using var verifyScope = harness.Scope.ServiceProvider.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        (await verifyDb.RecurringTransfers.FindAsync(recId))!.IsActive.Should().BeFalse();
        (await harness.Published.Any<RecurringCancelled>()).Should().BeTrue();

        await harness.Stop();
    }


    [Fact]
    public async Task RewardCmd_Should_Call_Service_And_Publish()
    {
        var userId = Guid.NewGuid();
        var rewardServiceMock = new Mock<IRewardService>();

        var harness = await TestContextHelper.BuildTestHarness<RewardCmdConsumer>(services =>
        {
            services.AddSingleton(rewardServiceMock.Object);
        });

        await harness.Bus.Publish(new RewardCmd(userId, RewardTypeEnum.Signup));

        // ✅ Ensure the consumer actually handled the message
        Assert.True(await harness.Consumed.Any<RewardCmd>(), "RewardCmd was not consumed");

        // ✅ Verify service call
        rewardServiceMock.Verify(s => s.GrantAsync(userId, RewardTypeEnum.Signup), Times.Once);

        // ✅ Ensure event was published
        (await harness.Published.Any<RewardIssued>()).Should().BeTrue();

        await harness.Stop();
    }

}