using Bot.Core.Services;
using Bot.Core.StateMachine;
using Bot.Shared;
using Bot.Shared.DTOs;
using Bot.Shared.Models;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Bot.Tests.StateMachine;

public class BotStateMachineWebhookTests : IAsyncLifetime
{
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
        if (_harness != null) await _harness.Stop();
        if (_provider is IAsyncDisposable asyncDisposable) await asyncDisposable.DisposeAsync();
    }

    [Fact]
    public async Task WBK_01_Should_Trigger_PreviewCmd_On_TransferCompleted()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new TransferCompleted(id, "REF-123"));

        var preview = _harness.Published
            .Select<PreviewCmd>()
            .FirstOrDefault(x => x.Context.Message.CorrelationId == id);

        Assert.NotNull(preview); // Fails
    }


    [Fact]
    public async Task WBK_02_Should_Send_Text_On_TransferFailed()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new TransferFailed(id, "Insufficient funds", "REF-456"));

        var nudge = _harness.Published
            .Select<NudgeCmd>()
            .FirstOrDefault(x => x.Context.Message.CorrelationId == id);

        Assert.NotNull(nudge); // Fails
    }


    [Fact]
    public async Task WBK_03_Should_Not_Republish_On_Duplicate_TransferCompleted()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new TransferCompleted(id, "REF-789"));
        await _harness.Bus.Publish(new TransferCompleted(id, "REF-789")); // duplicate

        var count = _harness.Published
            .Select<PreviewCmd>()
            .Count(x => x.Context.Message.CorrelationId == id);

        Assert.Equal(1, count); // but we get 0
    }

}
