using System.Text.Json;
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
public class BotStateMachinePinAndIntentTests(ITestOutputHelper testOutputHelper) : IAsyncLifetime
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
                cfg.AddSagaStateMachine<BotStateMachine, BotState>().InMemoryRepository();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        _sagaHarness = _provider.GetRequiredService<ISagaStateMachineTestHarness<BotStateMachine, BotState>>();

        await _harness.Start();
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

   
    private async Task<Guid> SeedUserToReadyAsync()
    {
        var sagaId = NewId.NextGuid();

        await _harness.Bus.Publish(new UserIntentDetected(
            sagaId,
            "signup"
        ));

        await _harness.Bus.Publish(new FullNameProvided(
            sagaId, 
            "Test User"
        ));

        await _harness.Bus.Publish(new NinProvided(
            sagaId,
            "12345678901"
        ));

        await _harness.Bus.Publish(new NinVerified(
            sagaId, "12345678901"
        ));

        await _harness.Bus.Publish(new BvnProvided(
            sagaId,
            "12345678901"
        ));

        await _harness.Bus.Publish(new BvnVerified(
            sagaId, "12345678901"
        ));

        await _harness.Bus.Publish(new SignupSucceeded(
            sagaId,
            Guid.NewGuid()
        ));

        await _harness.Bus.Publish(new MandateReadyToDebit(
            sagaId,
            "mandate-123",
            "test-provider"
        ));

        await _harness.Bus.Publish(new BankLinkSucceeded(
            sagaId
        ));

        await _harness.Bus.Publish(new PinSet(
            sagaId
        ));
        // Wait for the saga to process all events
        await _harness.InactivityTask;

        return sagaId;
    }



    [Fact]
    public async Task PIN_01_Should_Transition_To_AwaitingPinValidate_On_Transfer_Intent()
    {
        var id = await SeedUserToReadyAsync();
  
        var payload = new TransferPayload("111111", "001", 12345, "Test");

        var ready = await _sagaHarness.Exists(id, x => x.Ready, TimeSpan.FromSeconds(5));
        Assert.True(ready.HasValue, "User should be in Ready state");

        // Now send transfer intent
        await _harness.Bus.Publish(new UserIntentDetected(id, "transfer", TransferPayload: payload));

        var exists = await _sagaHarness.Exists(id, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));
        Assert.NotNull(exists);

        var saga = _sagaHarness.Sagas.Contains(exists.Value);
        Assert.Equal("transfer", saga?.PendingIntentType);
        Assert.NotNull(saga?.PendingIntentPayload);
    }

    
    [Fact]
    public async Task PIN_02_Should_Publish_TransferCmd_On_Valid_Pin()
    {
        var id = await SeedUserToReadyAsync();
        var payload = new TransferPayload("111111", "001", 12345, "Test");

        await _harness.Bus.Publish(new UserIntentDetected(id, "transfer", TransferPayload: payload));
        await _harness.InactivityTask; // wait for saga to move to AwaitingPinValidate

        await _harness.Bus.Publish(new PinValidated(id));
        await _harness.InactivityTask; // wait for saga to publish TransferCmd

        var saga = _sagaHarness.Sagas.Contains(id);
        Assert.Equal("Ready", saga?.CurrentState);

        // Now it's safe to assert intent cleared
        Assert.Null(saga?.PendingIntentType);
        Assert.Null(saga?.PendingIntentPayload);

        var published = _harness.Published.Select<TransferCmd>().Any(x => x.Context.Message.CorrelationId == id);
        Assert.True(published);
    }


    [Fact]
    public async Task PIN_03_Should_Stay_In_AwaitingPinValidate_On_PinInvalid()
    {
        var id = await SeedUserToReadyAsync();
        var payload = new TransferPayload("111111", "001", 12345, "Test");

        await _harness.Bus.Publish(new UserIntentDetected(id, "transfer", TransferPayload: payload));
        await _harness.Bus.Publish(new PinInvalid(id, "wrong"));

        var saga = _sagaHarness.Sagas.Contains(id);
        Assert.Equal("AwaitingPinValidate", saga?.CurrentState);

        var nudge = _harness.Published.Select<NudgeCmd>().FirstOrDefault(x => x.Context.Message.CorrelationId == id);
        Assert.NotNull(nudge);
        Assert.Equal(NudgeType.BadPin, nudge.Context.Message.NudgeType);
    }

    [Fact]
    public async Task PIN_04_Should_Publish_BillPayCmd_On_Valid_Pin()
    {
        var id = await SeedUserToReadyAsync();
        var payload = new BillPayload("DSTV", "123456", 5000, "DSTV");

        await _harness.Bus.Publish(new UserIntentDetected(id, "billpay", BillPayload: payload));
        await _harness.InactivityTask; // saga enters AwaitingPinValidate

        await _harness.Bus.Publish(new PinValidated(id));
        await _harness.InactivityTask; // process BillPayCmd

        var saga = _sagaHarness.Sagas.Contains(id);
        Assert.Equal("Ready", saga?.CurrentState);

        var published = _harness.Published.Select<BillPayCmd>().Any(x => x.Context.Message.CorrelationId == id);
        Assert.True(published);
    }


    [Fact]
    public async Task PIN_05_Should_Ignore_Unknown_IntentType_On_PinValidated()
    {
        var id = await SeedUserToReadyAsync();

        var saga = _sagaHarness.Sagas.Contains(id);
        saga.PendingIntentType = "unknown";
        saga.PendingIntentPayload = JsonSerializer.Serialize(new UserIntentDetected(id, "transfer"));

        await _harness.Bus.Publish(new PinValidated(id));
        saga = _sagaHarness.Sagas.Contains(id);

        Assert.Equal("AwaitingPinValidate", saga?.CurrentState);
    }

    [Fact]
    public async Task PIN_06_Should_Handle_Invalid_Json_Payload_Gracefully()
    {
        var id = await SeedUserToReadyAsync();

        var saga = _sagaHarness.Sagas.Contains(id);
        saga.PendingIntentPayload = "not valid json";
        saga.PendingIntentType = "transfer";

        await _harness.Bus.Publish(new PinValidated(id));

        var after = _sagaHarness.Sagas.Contains(id);
        Assert.Equal("AwaitingPinValidate", after?.CurrentState);
    }

    [Fact]
    public async Task PIN_07_Should_Not_Publish_If_PendingIntent_Null()
    {
        var id = await SeedUserToReadyAsync();

        var saga = _sagaHarness.Sagas.Contains(id);
        saga.PendingIntentType = "transfer";
        saga.PendingIntentPayload = null;

        await _harness.Bus.Publish(new PinValidated(id));

        var result = _sagaHarness.Sagas.Contains(id);
        Assert.Equal("AwaitingPinValidate", result?.CurrentState);
    }

    [Fact]
    public async Task PIN_08_Should_Generate_Reference_And_Publish_TransferCmd()
    {
        var id = await SeedUserToReadyAsync();
        var payload = new TransferPayload("111111", "001", 12345, "Test");

        await _harness.Bus.Publish(new UserIntentDetected(id, "transfer", TransferPayload: payload));
        await _harness.Bus.Publish(new PinValidated(id));

        var cmd = _harness.Published.Select<TransferCmd>().FirstOrDefault(x => x.Context.Message.CorrelationId == id);
        Assert.NotNull(cmd);
        Assert.NotNull(cmd.Context.Message.Reference);
    }

    [Fact]
    public async Task PIN_09_Should_Allow_PinRetry_And_Then_Proceed_On_Valid_Pin()
    {
        var id = await SeedUserToReadyAsync();
        var payload = new TransferPayload("111111", "001", 12345, "Test");

        await _harness.Bus.Publish(new UserIntentDetected(id, "transfer", TransferPayload: payload));
        await _harness.Bus.Publish(new PinInvalid(id, "wrong"));

        var saga = _sagaHarness.Sagas.Contains(id);
        Assert.Equal("AwaitingPinValidate", saga?.CurrentState);

        await _harness.Bus.Publish(new PinValidated(id));

        var cmd = _harness.Published.Select<TransferCmd>().FirstOrDefault(x => x.Context.Message.CorrelationId == id);
        Assert.NotNull(cmd);
    }

    [Fact]
    public async Task PIN_10_Should_Execute_Only_Latest_Intent_Before_PinValidated()
    {
        var id = await SeedUserToReadyAsync();

        var transfer = new TransferPayload("111111", "001", 12345, "Test");
        var bill = new BillPayload("DSTV", "123456", 5000, "DSTV");

        await _harness.Bus.Publish(new UserIntentDetected(id, "transfer", TransferPayload: transfer));
        await _harness.Bus.Publish(new UserIntentDetected(id, "billpay", BillPayload: bill));
        await _harness.Bus.Publish(new PinValidated(id));

        var billCmd = _harness.Published.Select<BillPayCmd>().FirstOrDefault(x => x.Context.Message.CorrelationId == id);
        Assert.NotNull(billCmd);
        Assert.Equal("DSTV", billCmd.Context.Message.Payload.BillerCode);

        var transferCmd = _harness.Published.Select<TransferCmd>().FirstOrDefault(x => x.Context.Message.CorrelationId == id);
        Assert.Null(transferCmd);
    }
}
