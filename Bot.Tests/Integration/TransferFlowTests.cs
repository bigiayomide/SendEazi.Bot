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

namespace Bot.Tests.Integration;

public class TransferFlowTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<BotStateMachine, BotState> _sagaHarness = null!;
    private ApplicationDbContext _db = null!;
    private readonly Mock<IConversationStateService> _stateSvc = new();
    private readonly Mock<IBankProvider> _bank = new();

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddInMemoryDb("transfer-flow");
        services.AddMassTransitTestHarness(cfg =>
        {
            cfg.AddSagaStateMachine<BotStateMachine, BotState>()
               .InMemoryRepository();
            cfg.AddConsumer<TransferCmdConsumer>();
            cfg.AddConsumer<TransferWebhookHandler>();
        });

        services.AddScoped<TransferCmdConsumer>();
        services.AddScoped<TransferWebhookHandler>();

        services.AddSingleton<IConversationStateService>(_stateSvc.Object);
        services.AddSingleton<IWhatsAppService>(new Mock<IWhatsAppService>().Object);
        services.AddSingleton<IReferenceGenerator>(sp =>
        {
            var m = new Mock<IReferenceGenerator>();
            m.Setup(r => r.GenerateTransferRef(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("TX-REF");
            return m.Object;
        });
        services.AddSingleton<IBankProviderFactory>(sp =>
        {
            var mock = new Mock<IBankProviderFactory>();
            mock.Setup(f => f.GetProviderAsync(It.IsAny<Guid>(), It.IsAny<Guid?>()))
                .ReturnsAsync(_bank.Object);
            return mock.Object;
        });

        _provider = services.BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        _sagaHarness = _provider.GetRequiredService<ISagaStateMachineTestHarness<BotStateMachine, BotState>>();
        _db = _provider.GetRequiredService<ApplicationDbContext>();

        _stateSvc.Setup(s => s.SetStateAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _stateSvc.Setup(s => s.SetUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

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
        await _db.SeedMandateAsync(userId, "mandate-1");
        var sid = NewId.NextGuid();

        await _harness.Bus.Publish(new UserIntentDetected(sid, IntentType.Signup,
            SignupPayload: new SignupPayload("Test", "+2348000000000", "12345678901", "12345678901")));
        await _sagaHarness.Exists(sid, x => x.NinValidating, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new NinVerified(sid, "12345678901"));
        await _sagaHarness.Exists(sid, x => x.AskBvn, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BvnProvided(sid, "12345678901"));
        await _sagaHarness.Exists(sid, x => x.BvnValidating, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BvnVerified(sid, "12345678901"));
        await _sagaHarness.Exists(sid, x => x.AwaitingKyc, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new SignupSucceeded(sid, userId));
        await _sagaHarness.Exists(sid, x => x.AwaitingBankLink, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new MandateReadyToDebit(sid, "mandate-1", "Mono"));
        await _sagaHarness.Exists(sid, x => x.AwaitingPinSetup, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BankLinkSucceeded(sid));
        await _sagaHarness.Exists(sid, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new PinSet(sid));
        await _sagaHarness.Exists(sid, x => x.Ready, TimeSpan.FromSeconds(5));
        return sid;
    }

    [Fact]
    public async Task Should_Complete_Transfer_Flow()
    {
        var userId = Guid.NewGuid();
        var sid = await SeedReadyAsync(userId);

        var payload = new TransferPayload("111111", "001", 5000, "Test");
        await _harness.Bus.Publish(new UserIntentDetected(sid, IntentType.Transfer, TransferPayload: payload));
        await _sagaHarness.Exists(sid, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new PinValidated(sid));
        await _sagaHarness.Exists(sid, x => x.Ready, TimeSpan.FromSeconds(5));

        var cmd = _harness.Published.Select<TransferCmd>()
            .FirstOrDefault(x => x.Context.Message.CorrelationId == sid);
        Assert.NotNull(cmd);

        var tx = await _db.Transactions.FirstOrDefaultAsync(t => t.Reference == "TX-REF");
        Assert.NotNull(tx);
        Assert.Equal(TransactionStatus.Pending, tx!.Status);

        await _harness.Bus.Publish(new TransferCompleted(sid, "TX-REF"));
        await _harness.InactivityTask;

        tx = await _db.Transactions.FirstAsync(t => t.Reference == "TX-REF");
        Assert.Equal(TransactionStatus.Success, tx.Status);
        Assert.True(await _harness.Published.Any<PreviewCmd>(x => x.Context.Message.CorrelationId == sid));
    }

    [Fact]
    public async Task Should_Handle_Transfer_Failure()
    {
        var userId = Guid.NewGuid();
        var sid = await SeedReadyAsync(userId);

        _bank.Setup(b => b.InitiateDebitAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("fail"));

        var payload = new TransferPayload("2222", "999", 7000, "FailTest");
        await _harness.Bus.Publish(new UserIntentDetected(sid, IntentType.Transfer, TransferPayload: payload));
        await _sagaHarness.Exists(sid, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new PinValidated(sid));
        await _harness.InactivityTask;

        var tx = await _db.Transactions.FirstOrDefaultAsync(t => t.Reference == "TX-REF");
        Assert.NotNull(tx);
        Assert.Equal(TransactionStatus.Failed, tx!.Status);
        Assert.True(await _harness.Published.Any<TransferFailed>(x => x.Context.Message.CorrelationId == sid));
        Assert.True(await _harness.Published.Any<NudgeCmd>(x => x.Context.Message.CorrelationId == sid && x.Context.Message.NudgeType == NudgeType.TransferFail));
    }
}