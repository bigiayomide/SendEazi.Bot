using Bot.Core.Providers;
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
    private IServiceScope _scope = null!;
    private ApplicationDbContext _db = null!;
    private IBillPayService _billPayService = null!;
    private ITestHarness _harness = null!;
    private ServiceProvider _provider = null!;
    private readonly Mock<IBankProvider> _bank = new();
    private ISagaStateMachineTestHarness<BotStateMachine, BotState> _sagaHarness = null!;
    private readonly Mock<IConversationStateService> _stateSvc = new();

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddInMemoryDb("recurring-flow");
        services.AddMassTransitTestHarness(cfg =>
        {
            cfg.AddSagaStateMachine<BotStateMachine, BotState>()
                .InMemoryRepository();
            cfg.AddConsumer<RecurringCmdConsumer>();
            cfg.AddConsumer<TransferCmdConsumer>();
        });

        services.AddScoped<RecurringCmdConsumer>();
        services.AddScoped<TransferCmdConsumer>();

        services.AddSingleton<IConversationStateService>(_stateSvc.Object);
        services.AddSingleton<IReferenceGenerator>(sp =>
        {
            var m = new Mock<IReferenceGenerator>();
            m.Setup(r => r.GenerateTransferRef(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("REC-REF");
            return m.Object;
        });

        services.AddSingleton<IBankProviderFactory>(sp =>
        {
            var mock = new Mock<IBankProviderFactory>();
            mock.Setup(f => f.GetProviderAsync(It.IsAny<Guid>(), It.IsAny<Guid?>()))
                .ReturnsAsync(_bank.Object);
            return mock.Object;
        });

        _provider = services.BuildServiceProvider(validateScopes: true);
        _scope = _provider.CreateScope();

        var scoped = _scope.ServiceProvider;

        _db = scoped.GetRequiredService<ApplicationDbContext>();
        _harness = scoped.GetRequiredService<ITestHarness>();
        _sagaHarness = scoped.GetRequiredService<ISagaStateMachineTestHarness<BotStateMachine, BotState>>();

        _stateSvc.Setup(x => x.SetStateAsync(It.IsAny<Guid>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _stateSvc.Setup(x => x.SetUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.CompletedTask);

        await _harness.Start();
    }


    public async Task DisposeAsync()
    {
        if (_harness != null) await _harness.Stop();
        _scope?.Dispose();
        if (_provider is IAsyncDisposable disp) await disp.DisposeAsync();
    }

    private async Task<Guid> SeedReadyAsync(Guid userId)
    {
        await _db.SeedUserAsync(userId);

        await _harness.Bus.Publish(new UserIntentDetected(userId, IntentType.Signup,
            SignupPayload: new SignupPayload("User", "+2348000000000", "12345678901", "12345678901")));
        await _sagaHarness.Exists(userId, x => x.NinValidating, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new NinVerified(userId, "12345678901"));
        await _sagaHarness.Exists(userId, x => x.AskBvn, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BvnProvided(userId, "12345678901"));
        await _sagaHarness.Exists(userId, x => x.BvnValidating, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BvnVerified(userId, "12345678901"));
        await _sagaHarness.Exists(userId, x => x.AwaitingKyc, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new SignupSucceeded(userId, userId));
        await _sagaHarness.Exists(userId, x => x.AwaitingBankLink, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new MandateReadyToDebit(userId, "mandate", "Mono"));
        await _sagaHarness.Exists(userId, x => x.AwaitingPinSetup, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BankLinkSucceeded(userId));
        await _sagaHarness.Exists(userId, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new PinSet(userId));
        await _sagaHarness.Exists(userId, x => x.Ready, TimeSpan.FromSeconds(5));
        return userId;
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
        await _harness.InactivityTask;

        // ✅ Wait until the command is consumed
        Assert.True(await _harness.Consumed.Any<BillPayCmd>(x => x.Context.Message.CorrelationId == sid));

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

        // Ensure you're modifying the correct service instance
        var svc = (FakeBillPayService)_billPayService;
        svc.ShouldFail = true;

        var payload = new BillPayload("Water", "222", 2500, "Water");
        await _harness.Bus.Publish(new UserIntentDetected(sid, IntentType.BillPay, BillPayload: payload));
        await _sagaHarness.Exists(sid, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new PinValidated(sid));
        await _harness.InactivityTask;

        Assert.True(await _harness.Consumed.Any<BillPayCmd>(x => x.Context.Message.CorrelationId == sid));

        var bill = svc.LastBill;
        Assert.NotNull(bill);
        Assert.False(bill!.IsPaid);
        Assert.True(await _harness.Published.Any<BillPayFailed>(x => x.Context.Message.CorrelationId == sid));
    }



    private class FakeBillPayService(ApplicationDbContext db) : IBillPayService
    {
        public bool ShouldFail { get; set; }
        public BillPayment? LastBill { get; private set; }

        public Task<IReadOnlyList<BillPayment>> ProcessDueBillPaymentsAsync()
        {
            return Task.FromResult<IReadOnlyList<BillPayment>>(Array.Empty<BillPayment>());
        }

        public async Task<BillPayment> PayBillAsync(Guid userId, string billerCode, decimal amount, DateTime dueDate)
        {
            Console.WriteLine($"PayBillAsync called. ShouldFail: {ShouldFail}");

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

            LastBill = bill;

            db.BillPayments.Add(bill);
            await db.SaveChangesAsync();
            return bill;
        }
    }


}
