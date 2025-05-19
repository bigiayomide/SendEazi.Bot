using Bot.Core.Services;
using Bot.Core.StateMachine;
using Bot.Shared;
using Bot.Shared.DTOs;
using Bot.Shared.Models;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace Bot.Tests.StateMachine;

public class BotStateMachineSecurityAndEdgeTests(ITestOutputHelper testOutputHelper): IAsyncLifetime
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<BotStateMachine, BotState> _sagaHarness = null!;
    private readonly Mock<IConversationStateService> _stateServiceMock = new();

    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddSingleton(_stateServiceMock.Object)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<BotStateMachine, BotState>()
                   .InMemoryRepository();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        _sagaHarness = _provider.GetRequiredService<ISagaStateMachineTestHarness<BotStateMachine, BotState>>();

        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        if (_harness != null)
            await _harness.Stop();
        if (_provider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
    }

    [Fact]
    public async Task SEC_02_Should_Ignore_PinValidated_Without_Intent()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new PinValidated(id));

        var exists = _sagaHarness.Sagas.Contains(id);
        Assert.True(exists == null || exists.CurrentState == "Initial");
    }
    [Fact]
    public async Task EDG_03_Should_Not_Disrupt_Saga_On_IntentEvt_In_Invalid_State()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new UserIntentDetected(id, Shared.Enums.IntentType.Signup));
        await _harness.Bus.Publish(new FullNameProvided(id, "EdgeCase"));

        var exists = await _sagaHarness.Exists(id, x => x.AskNin, TimeSpan.FromSeconds(5));
        Assert.True(exists.HasValue, "Saga did not transition to AskNin");

        var saga = _sagaHarness.Sagas.Contains(id);
        Assert.Equal("AskNin", saga?.CurrentState);


        await _harness.Bus.Publish(new UserIntentDetected(id, Shared.Enums.IntentType.Transfer)); // invalid time

        saga = _sagaHarness.Sagas.Contains(id);
        Assert.Equal("AskNin", saga?.CurrentState); // remains safe
    }

    [Fact]
    public async Task SEC_03_Should_Ignore_BvnProvided_If_Nin_Was_Not_Set()
    {
        var id = NewId.NextGuid();

        await _harness.Bus.Publish(new UserIntentDetected(id, Shared.Enums.IntentType.Signup));
        await _harness.Bus.Publish(new FullNameProvided(id, "Test User"));

        var exists = await _sagaHarness.Exists(id, x => x.AskNin, TimeSpan.FromSeconds(5));
        Assert.True(exists.HasValue, "Saga did not reach AskNin");

        // Now send BvnProvided too early
        await _harness.Bus.Publish(new BvnProvided(id, "12345678901"));

        var saga = _sagaHarness.Sagas.Contains(id);
        Assert.Equal("AskNin", saga?.CurrentState); // should still be AskNin
        Assert.Null(saga?.TempNIN); // NIN was never set
    }

    [Fact]
    public async Task EVT_01_Should_Publish_NudgeCmd_On_NinRejected()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new UserIntentDetected(id, Shared.Enums.IntentType.Signup));
        await _harness.Bus.Publish(new FullNameProvided(id, "Nudge User"));
        await _harness.Bus.Publish(new NinProvided(id, "invalid"));
        await _harness.Bus.Publish(new NinRejected(id, "invalid"));

        var nudge = _harness.Published.Select<NudgeCmd>().FirstOrDefault(x => x.Context.Message.CorrelationId == id);
        Assert.NotNull(nudge);
        Assert.Equal(NudgeType.InvalidNin, nudge.Context.Message.NudgeType);
    }
    [Fact]
    public async Task EVT_02_Should_Publish_NudgeCmd_On_BvnRejected()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new UserIntentDetected(id, Shared.Enums.IntentType.Signup));
        await _harness.Bus.Publish(new FullNameProvided(id, "Nudge User"));
        await _harness.Bus.Publish(new NinProvided(id, "12345678901"));
        await _harness.Bus.Publish(new NinVerified(id, "12345678901"));
        await _harness.Bus.Publish(new BvnProvided(id, "98765432101"));
        await _harness.Bus.Publish(new BvnRejected(id, "BVN mismatch"));

        var nudge = _harness.Published.Select<NudgeCmd>().FirstOrDefault(x => x.Context.Message.CorrelationId == id);
        Assert.NotNull(nudge);
        Assert.Equal(NudgeType.InvalidBvn, nudge.Context.Message.NudgeType);
    }

}