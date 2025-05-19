using Bot.Core.Services;
using Bot.Core.StateMachine;
using Bot.Core.StateMachine.Consumers.Payments;
using Bot.Infrastructure.Data;
using Bot.Shared;
using Bot.Shared.DTOs;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using Bot.Tests.TestUtilities;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Assert = Xunit.Assert;

namespace Bot.Tests.Integration;

public class BillPayFlowTests : IAsyncLifetime
{
    private readonly Mock<IConversationStateService> _stateSvc = new();
    private ApplicationDbContext _db = null!;
    private ITestHarness _harness = null!;
    private ServiceProvider _provider = null!;
    private ISagaStateMachineTestHarness<BotStateMachine, BotState> _sagaHarness = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddInMemoryDb("billpay-flow");
        services.AddMassTransitTestHarness(cfg =>
        {
            cfg.AddSagaStateMachine<BotStateMachine, BotState>()
                .InMemoryRepository();
            cfg.AddConsumer<BillPayCmdConsumer>();
        });

        services.AddScoped<BillPayCmdConsumer>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<IBillPayService, FakeBillPayService>();
        services.AddSingleton<IConversationStateService>(_stateSvc.Object);

        _provider = services.BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        _sagaHarness = _provider.GetRequiredService<ISagaStateMachineTestHarness<BotStateMachine, BotState>>();
        _db = _provider.GetRequiredService<ApplicationDbContext>();

        _stateSvc.Setup(x => x.SetStateAsync(It.IsAny<Guid>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _stateSvc.Setup(x => x.SetUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.CompletedTask);

        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        if (_harness != null) await _harness.Stop();
        if (_provider is IAsyncDisposable disp) await disp.DisposeAsync();
    }

    private async Task<Guid> SeedReadyAsync(Guid userId)
    {
        await _db.SeedUserAsync(userId);
        var sid = NewId.NextGuid();

        await _harness.Bus.Publish(new UserIntentDetected(sid, IntentType.Signup,
            SignupPayload: new SignupPayload("User", "+2348000000000", "12345678901", "12345678901")));
        await _sagaHarness.Exists(sid, x => x.NinValidating, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new NinVerified(sid, "12345678901"));
        await _sagaHarness.Exists(sid, x => x.AskBvn, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BvnProvided(sid, "12345678901"));
        await _sagaHarness.Exists(sid, x => x.BvnValidating, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BvnVerified(sid, "12345678901"));
        await _sagaHarness.Exists(sid, x => x.AwaitingKyc, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new SignupSucceeded(sid, userId));
        await _sagaHarness.Exists(sid, x => x.AwaitingBankLink, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new MandateReadyToDebit(sid, "mandate", "Mono"));
        await _sagaHarness.Exists(sid, x => x.AwaitingPinSetup, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BankLinkSucceeded(sid));
        await _sagaHarness.Exists(sid, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new PinSet(sid));
        await _sagaHarness.Exists(sid, x => x.Ready, TimeSpan.FromSeconds(5));
        return sid;
    }

    [Fact]
    public async Task Should_Pay_Bill_And_Record()
    {
        var userId = Guid.NewGuid();
        var sid = await SeedReadyAsync(userId);

        var payload = new BillPayload("Electricity", "12345", 4500, "Electricity");
        await _harness.Bus.Publish(new UserIntentDetected(sid, IntentType.BillPay, BillPayload: payload));
        await _sagaHarness.Exists(sid, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new PinValidated(sid));
        await _sagaHarness.Exists(sid, x => x.Ready, TimeSpan.FromSeconds(5));

        Assert.True(await _harness.Published.Any<BillPayCmd>(x => x.Context.Message.CorrelationId == sid));
        var bill = await _db.BillPayments.FirstOrDefaultAsync();
        Assert.NotNull(bill);
        Assert.True(bill!.IsPaid);
    }

    [Fact]
    public async Task Should_Handle_BillPay_Failure()
    {
        var userId = Guid.NewGuid();
        var sid = await SeedReadyAsync(userId);

        var svc = (FakeBillPayService)_provider.GetRequiredService<IBillPayService>();
        svc.ShouldFail = true;

        var payload = new BillPayload("Water", "222", 2500, "Water");
        await _harness.Bus.Publish(new UserIntentDetected(sid, IntentType.BillPay, BillPayload: payload));
        await _sagaHarness.Exists(sid, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new PinValidated(sid));
        await _harness.InactivityTask;

        var bill = await _db.BillPayments.FirstOrDefaultAsync();
        Assert.NotNull(bill);
        Assert.False(bill!.IsPaid);
        Assert.True(await _harness.Published.Any<BillPayFailed>(x => x.Context.Message.CorrelationId == sid));
    }

    private class FakeBillPayService : IBillPayService
    {
        private readonly ApplicationDbContext _db;

        public FakeBillPayService(ApplicationDbContext db)
        {
            _db = db;
        }

        public bool ShouldFail { get; set; }

        public Task<IReadOnlyList<BillPayment>> ProcessDueBillPaymentsAsync()
        {
            return Task.FromResult<IReadOnlyList<BillPayment>>(Array.Empty<BillPayment>());
        }

        public async Task<BillPayment> PayBillAsync(Guid userId, string billerCode, decimal amount, DateTime dueDate)
        {
            var bill = new BillPayment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Biller = Enum.Parse<BillerEnum>(billerCode),
                Amount = amount,
                DueDate = dueDate,
                IsPaid = !ShouldFail,
                CreatedAt = DateTime.UtcNow,
                PaidAt = ShouldFail ? null : DateTime.UtcNow
            };
            _db.BillPayments.Add(bill);
            await _db.SaveChangesAsync();
            return bill;
        }
    }
}