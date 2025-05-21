using Bot.Core.Services;
using Bot.Core.StateMachine;
using Bot.Shared.DTOs;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Assert = Xunit.Assert;

namespace Bot.Tests.StateMachine;

public class BotStateMachineWebhookTests : IAsyncLifetime
{
    private readonly Mock<IConversationStateService> _stateServiceMock = new();
    private ITestHarness _harness = null!;
    private ServiceProvider _provider = null!;
    private ISagaStateMachineTestHarness<BotStateMachine, BotState> _sagaHarness = null!;

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

        _stateServiceMock
            .Setup(x => x.SetStateAsync(It.IsAny<Guid>(), It.IsAny<ConversationState>()))
            .Returns(Task.CompletedTask);

        _stateServiceMock
            .Setup(x => x.SetUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        if (_harness != null) await _harness.Stop();
        if (_provider is IAsyncDisposable asyncDisposable) await asyncDisposable.DisposeAsync();
    }

    [Fact]
    public async Task WBK_01_Should_Trigger_PreviewCmd_On_TransferCompleted()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Greeting));
        await _harness.InactivityTask;

        await _harness.Bus.Publish(new TransferCompleted(id, "REF-123"));

        var preview = _harness.Published
            .Select<PreviewCmd>()
            .FirstOrDefault(x => x.Context.Message.CorrelationId == id);

        Assert.NotNull(preview);
    }


    [Fact]
    public async Task WBK_02_Should_Send_Text_On_TransferFailed()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Greeting));
        await _harness.InactivityTask;

        await _harness.Bus.Publish(new TransferFailed(id, "Insufficient funds", "REF-456"));

        var nudge = _harness.Published
            .Select<NudgeCmd>()
            .FirstOrDefault(x => x.Context.Message.CorrelationId == id);

        Assert.NotNull(nudge);
    }


    [Fact]
    public async Task WBK_03_Should_Not_Republish_On_Duplicate_TransferCompleted()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Greeting));
        await _harness.InactivityTask;

        await _harness.Bus.Publish(new TransferCompleted(id, "REF-789"));
        await _harness.Bus.Publish(new TransferCompleted(id, "REF-789")); // duplicate

        var count = _harness.Published
            .Select<PreviewCmd>()
            .Count(x => x.Context.Message.CorrelationId == id);

        Assert.Equal(1, count);
    }
}