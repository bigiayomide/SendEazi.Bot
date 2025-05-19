using Bot.Core.Services;
using Bot.Core.StateMachine;
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

namespace Bot.Tests.Integration;

public class OnboardingFlowTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<BotStateMachine, BotState> _sagaHarness = null!;
    private readonly Mock<IConversationStateService> _stateSvc = new();

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddInMemoryDb("onboarding-flow");
        services.AddMassTransitTestHarness(cfg =>
        {
            cfg.AddSagaStateMachine<BotStateMachine, BotState>()
               .InMemoryRepository();
        });

        services.AddSingleton<IConversationStateService>(_stateSvc.Object);

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        _sagaHarness = _provider.GetRequiredService<ISagaStateMachineTestHarness<BotStateMachine, BotState>>();

        _stateSvc.Setup(x => x.SetStateAsync(It.IsAny<Guid>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _stateSvc.Setup(x => x.SetUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.CompletedTask);

        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        if (_harness != null) await _harness.Stop();
        if (_provider is IAsyncDisposable disp) await disp.DisposeAsync();
    }

    [Fact]
    public async Task Should_Reaching_Ready_State_On_Happy_Path()
    {
        var id = NewId.NextGuid();
        var userId = Guid.NewGuid();

        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Signup,
            SignupPayload: new SignupPayload("Jane Doe", "+2348110000000", "12345678901", "12345678901")));
        await _sagaHarness.Exists(id, x => x.NinValidating, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new NinVerified(id, "12345678901"));
        await _sagaHarness.Exists(id, x => x.AskBvn, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BvnProvided(id, "12345678901"));
        await _sagaHarness.Exists(id, x => x.BvnValidating, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BvnVerified(id, "12345678901"));
        await _sagaHarness.Exists(id, x => x.AwaitingKyc, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new SignupSucceeded(id, userId));
        await _sagaHarness.Exists(id, x => x.AwaitingBankLink, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new MandateReadyToDebit(id, "mandate", "Mono"));
        await _sagaHarness.Exists(id, x => x.AwaitingPinSetup, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BankLinkSucceeded(id));
        await _sagaHarness.Exists(id, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new PinSet(id));
        var final = await _sagaHarness.Exists(id, x => x.Ready, TimeSpan.FromSeconds(5));

        Assert.NotNull(final);
    }

    [Fact]
    public async Task Should_Return_To_BankLink_On_Failure()
    {
        var id = NewId.NextGuid();
        var userId = Guid.NewGuid();

        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Signup,
            SignupPayload: new SignupPayload("Jane Doe", "+2348110000000", "12345678901", "12345678901")));
        await _sagaHarness.Exists(id, x => x.NinValidating, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new NinVerified(id, "12345678901"));
        await _sagaHarness.Exists(id, x => x.AskBvn, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BvnProvided(id, "12345678901"));
        await _sagaHarness.Exists(id, x => x.BvnValidating, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BvnVerified(id, "12345678901"));
        await _sagaHarness.Exists(id, x => x.AwaitingKyc, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new SignupSucceeded(id, userId));
        await _sagaHarness.Exists(id, x => x.AwaitingBankLink, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new MandateReadyToDebit(id, "mandate", "Mono"));
        await _sagaHarness.Exists(id, x => x.AwaitingPinSetup, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BankLinkFailed(id, "denied"));
        await _sagaHarness.Exists(id, x => x.AwaitingBankLink, TimeSpan.FromSeconds(5));

        var saga = _sagaHarness.Sagas.Contains(id);
        Assert.Equal("AwaitingBankLink", saga?.CurrentState);
        Assert.Equal("BankLinkFailed", saga?.LastFailureReason);
    }
}