using System.Text.Json;
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
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Assert = Xunit.Assert;

namespace Bot.Tests.Integration;

public class RecurringFlowTests : IAsyncLifetime
{
    private readonly Mock<IBankProvider> _bank = new();
    private readonly Mock<IConversationStateService> _stateSvc = new();
    private ApplicationDbContext _db = null!;
    private ITestHarness _harness = null!;
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private ISagaStateMachineTestHarness<BotStateMachine, BotState> _sagaHarness = null!;

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
        if (_provider is IAsyncDisposable disp) await disp.DisposeAsync();
    }

    private async Task<Guid> SeedReadyAsync(Guid userId)
    {
        await _db.SeedUserAsync(userId);
        await _db.SeedMandateAsync(userId);

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
    public async Task Should_Schedule_Recurring_And_Publish_Transfer()
    {
        var userId = Guid.NewGuid();
        var sid = await SeedReadyAsync(userId);

        var transfer = new TransferPayload("2222", "999", 100, "Recurring");

        var saga = _sagaHarness.Sagas.Contains(sid);
        saga.PendingIntentType = IntentType.Transfer;
        saga.PendingIntentPayload =
            JsonSerializer.Serialize(new UserIntentDetected(sid, IntentType.Transfer, transfer));

        await _harness.Bus.Publish(new RecurringCmd(sid,
            new RecurringPayload(Guid.NewGuid(), transfer, "* * * * *")));
        
        await _harness.InactivityTask;

        // ✅ Confirm consumer executed
        Assert.True(await _harness.Consumed.Any<RecurringCmd>(), "RecurringCmd was not consumed");

        // ✅ Confirm expected message was published
        Assert.True(await _harness.Published.Any<RecurringExecuted>(x => x.Context.Message.CorrelationId == sid),
            $"RecurringExecuted with CorrelationId {sid} was not published");

        Assert.True(await _harness.Published.Any<TransferCmd>(x => x.Context.Message.CorrelationId == sid));

        Assert.Single(_db.RecurringTransfers);
        Assert.Single(_db.Payees);
    }


    [Fact]
    public async Task Should_Handle_Recurring_Failure()
    {
        var userId = Guid.NewGuid();
        var sid = await SeedReadyAsync(userId);

        var transfer = new TransferPayload("3333", "555", 50, "RecurringFail");
        var saga = _sagaHarness.Sagas.Contains(sid);
        saga.PendingIntentType = IntentType.Transfer;
        saga.PendingIntentPayload =
            JsonSerializer.Serialize(new UserIntentDetected(sid, IntentType.Transfer, transfer));

        await _harness.Bus.Publish(new RecurringFailed(sid, "fail"));
        await _harness.InactivityTask;

        saga = _sagaHarness.Sagas.Contains(sid);
        Assert.Equal("AwaitingPinValidate", saga?.CurrentState);
        Assert.True(await _harness.Published.Any<NudgeCmd>(x =>
            x.Context.Message.CorrelationId == sid && x.Context.Message.NudgeType == NudgeType.RecurringFailed));
    }
}