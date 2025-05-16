using System.Text.Json;
using Bot.Core.Services;
using Bot.Core.StateMachine;
using Bot.Shared;
using Bot.Shared.Models;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Bot.Tests.StateMachine;

public class BotStateMachineRecurringTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<BotStateMachine, BotState> _sagaHarness = null!;
    private readonly Mock<IConversationStateService> _stateServiceMock = new();
    private readonly Mock<IReferenceGenerator> _referenceGeneratorMock = new();

    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<BotStateMachine, BotState>().InMemoryRepository();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        _sagaHarness = _provider.GetRequiredService<ISagaStateMachineTestHarness<BotStateMachine, BotState>>();
        

        _referenceGeneratorMock
            .Setup(x => x.GenerateTransferRef(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("REC-REF-123");

        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        if (_harness != null) await _harness.Stop();
        if (_provider is IAsyncDisposable asyncDisposable) await asyncDisposable.DisposeAsync();
    }

    [Fact]
    public async Task REC_01_Should_Trigger_TransferCmd_On_RecurringExecuted()
    {
        var id = NewId.NextGuid();
        var recurringId = Guid.NewGuid();
        var payload = new TransferPayload("111111", "001", 12345, "Test");

        await _harness.Bus.Publish(new UserIntentDetected(id, "transfer", TransferPayload: payload));

        var saga = _sagaHarness.Sagas.Contains(id);
        saga.PendingIntentType = "transfer";
        saga.PendingIntentPayload = JsonSerializer.Serialize(new UserIntentDetected(id, "transfer", TransferPayload: payload));

        await _harness.Bus.Publish(new RecurringExecuted(id, recurringId));

        var cmd = _harness.Published.Select<TransferCmd>().FirstOrDefault(x => x.Context.Message.CorrelationId == id);
        Assert.NotNull(cmd);
        Assert.Equal("REC-REF-123", cmd.Context.Message.Reference);
    }

    [Fact]
    public async Task REC_02_Should_Stay_In_Current_State_On_RecurringFailed()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new UserIntentDetected(id, "transfer"));

        await _harness.Bus.Publish(new RecurringFailed(id, "failed"));

        var saga = _sagaHarness.Sagas.Contains(id);
        Assert.NotNull(saga);
        Assert.Equal("AwaitingPinValidate", saga.CurrentState);
    }

    [Fact]
    public async Task REC_03_Should_Clear_Pending_Intent_On_RecurringCancel()
    {
        var id = NewId.NextGuid();
        var recurringId = Guid.NewGuid();
        await _harness.Bus.Publish(new UserIntentDetected(id, "transfer"));
        var payload = new TransferPayload("111111", "001", 12345, "Test");
        var saga = _sagaHarness.Sagas.Contains(id);
        saga.PendingIntentType = "transfer";
        saga.PendingIntentPayload = JsonSerializer.Serialize(new UserIntentDetected(id, "transfer", payload));

        await _harness.Bus.Publish(new RecurringCancelled(id, recurringId));

        saga = _sagaHarness.Sagas.Contains(id);
        Assert.Null(saga?.PendingIntentType);
        Assert.Null(saga?.PendingIntentPayload);
    }

    [Fact]
    public async Task REC_04_Should_Not_Crash_On_Missing_Payload()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new RecurringExecuted(id, Guid.NewGuid()));

        var transferCmd = _harness.Published.Select<TransferCmd>().FirstOrDefault(x => x.Context.Message.CorrelationId == id);
        Assert.Null(transferCmd);
    }

    [Fact]
    public async Task REC_05_Should_Handle_Multiple_RecurringExecutions()
    {
        var id = NewId.NextGuid();
        
        var payload = new TransferPayload("111111", "001", 12345, "Test");

        await _harness.Bus.Publish(new UserIntentDetected(id, "transfer", TransferPayload: payload));
        var saga = _sagaHarness.Sagas.Contains(id);

        saga.PendingIntentType = "transfer";
        saga.PendingIntentPayload = JsonSerializer.Serialize(new UserIntentDetected(id, "transfer", TransferPayload: payload));

        await _harness.Bus.Publish(new RecurringExecuted(id, Guid.NewGuid()));
        await _harness.Bus.Publish(new RecurringExecuted(id, Guid.NewGuid()));

        var transfers = _harness.Published.Select<TransferCmd>().Count(x => x.Context.Message.CorrelationId == id);
        Assert.Equal(2, transfers);
    }
}
