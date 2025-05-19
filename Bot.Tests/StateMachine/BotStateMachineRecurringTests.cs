using System.Text.Json;
using System.Text.Json.Serialization;
using Bot.Core.Services;
using Bot.Core.StateMachine;
using Bot.Shared;
using Bot.Shared.DTOs;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Bot.Tests.StateMachine;

public class BotStateMachineRecurringTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions DedupeJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Mock<IReferenceGenerator> _referenceGeneratorMock = new();
    private readonly Mock<IConversationStateService> _stateServiceMock = new();
    private ITestHarness _harness = null!;
    private ServiceProvider _provider = null!;
    private ISagaStateMachineTestHarness<BotStateMachine, BotState> _sagaHarness = null!;

    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddSingleton<IConversationStateService>(_stateServiceMock.Object)
            .AddSingleton<IReferenceGenerator>(_referenceGeneratorMock.Object)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<BotStateMachine, BotState>()
                    .InMemoryRepository();
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

        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Transfer, payload));

        var saga = _sagaHarness.Sagas.Contains(id);
        saga.PendingIntentType = IntentType.Transfer;
        saga.PendingIntentPayload =
            JsonSerializer.Serialize(new UserIntentDetected(id, IntentType.Transfer, payload), DedupeJsonOptions);

        await _harness.Bus.Publish(new RecurringExecuted(id, recurringId));
        await _harness.InactivityTask;

        var cmd = _harness.Published.Select<TransferCmd>().FirstOrDefault(x => x.Context.Message.CorrelationId == id);
        Assert.NotNull(cmd);
        Assert.Equal("REC-REF-123", cmd.Context.Message.Reference);
    }

    [Fact]
    public async Task REC_02_Should_Stay_In_Current_State_On_RecurringFailed()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Transfer));

        await _harness.Bus.Publish(new RecurringFailed(id, "failed"));
        await _harness.InactivityTask;

        var saga = _sagaHarness.Sagas.Contains(id);
        Assert.NotNull(saga);
        Assert.Equal("AwaitingPinValidate", saga.CurrentState);
    }

    [Fact]
    public async Task REC_03_Should_Clear_Pending_Intent_On_RecurringCancel()
    {
        var id = NewId.NextGuid();
        var recurringId = Guid.NewGuid();
        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Transfer));
        var payload = new TransferPayload("111111", "001", 12345, "Test");
        var saga = _sagaHarness.Sagas.Contains(id);
        saga.PendingIntentType = IntentType.Transfer;
        saga.PendingIntentPayload =
            JsonSerializer.Serialize(new UserIntentDetected(id, IntentType.Transfer, payload), DedupeJsonOptions);

        await _harness.Bus.Publish(new RecurringCancelled(id, recurringId));
        await _harness.InactivityTask;

        saga = _sagaHarness.Sagas.Contains(id);
        Assert.Null(saga?.PendingIntentType);
        Assert.Null(saga?.PendingIntentPayload);
    }

    [Fact]
    public async Task REC_04_Should_Not_Crash_On_Missing_Payload()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new RecurringExecuted(id, Guid.NewGuid()));
        await _harness.InactivityTask;

        var transferCmd = _harness.Published.Select<TransferCmd>()
            .FirstOrDefault(x => x.Context.Message.CorrelationId == id);
        Assert.Null(transferCmd);
    }
}