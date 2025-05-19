using System.Text.Json;
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
using Xunit.Abstractions;

namespace Bot.Tests.StateMachine;

public class BotStateMachinePinAndIntentTests(ITestOutputHelper testOutputHelper) : IAsyncLifetime
{
    private readonly Mock<IConversationStateService> _stateServiceMock = new();
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;
    private ITestHarness _harness = null!;
    private ServiceProvider _provider = null!;
    private ISagaStateMachineTestHarness<BotStateMachine, BotState> _sagaHarness = null!;

    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddSingleton<IConversationStateService>(_stateServiceMock.Object)
            .AddSingleton<IReferenceGenerator>(sp =>
            {
                var mock = new Mock<IReferenceGenerator>();
                mock.Setup(x => x.GenerateTransferRef(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns("TEST-REF-123");
                return mock.Object;
            })
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<BotStateMachine, BotState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        _sagaHarness = _provider.GetRequiredService<ISagaStateMachineTestHarness<BotStateMachine, BotState>>();

        _stateServiceMock
            .Setup(x => x.SetStateAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _stateServiceMock
            .Setup(x => x.SetUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
        _harness.TestTimeout = TimeSpan.FromSeconds(5);
        _harness.TestInactivityTimeout = TimeSpan.FromSeconds(1);

        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
    }


    private async Task<Guid> SeedUserToReadyAsync()
    {
        var sagaId = NewId.NextGuid();

        // Signup starts
        await _harness.Bus.Publish(new UserIntentDetected(sagaId, IntentType.Signup,
            SignupPayload: new SignupPayload("Ayomide Fajobi", "+2349043844316", "12345678901", "12345678901")));
        await _sagaHarness.Exists(sagaId, x => x.NinValidating, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new NinVerified(sagaId, "12345678901"));
        await _sagaHarness.Exists(sagaId, x => x.AskBvn, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BvnProvided(sagaId, "12345678901"));
        await _sagaHarness.Exists(sagaId, x => x.BvnValidating, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BvnVerified(sagaId, "12345678901"));
        await _sagaHarness.Exists(sagaId, x => x.AwaitingKyc, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new SignupSucceeded(sagaId, Guid.NewGuid()));
        await _sagaHarness.Exists(sagaId, x => x.AwaitingBankLink, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new MandateReadyToDebit(sagaId, "mandate-123", "test-provider"));
        await _sagaHarness.Exists(sagaId, x => x.AwaitingPinSetup, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new BankLinkSucceeded(sagaId));
        await _sagaHarness.Exists(sagaId, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new PinSet(sagaId));
        await _sagaHarness.Exists(sagaId, x => x.Ready, TimeSpan.FromSeconds(5));

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
        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Transfer, payload));

        var exists = await _sagaHarness.Exists(id, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));
        Assert.NotNull(exists);

        var saga = _sagaHarness.Sagas.Contains(exists.Value);
        Assert.Equal(IntentType.Transfer, saga?.PendingIntentType);
        Assert.NotNull(saga?.PendingIntentPayload);
    }

    [Fact]
    public async Task PIN_02_Should_Publish_TransferCmd_On_Valid_Pin()
    {
        // 1) Start from a known "Ready" user
        var id = await SeedUserToReadyAsync();

        // 2) Publish a unique Transfer intent
        var payload = new TransferPayload(Guid.NewGuid().ToString(), "001", 12345, $"Test-{Guid.NewGuid()}");
        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Transfer, payload));

        // 3) Wait for the saga to reach "AwaitingPinValidate"
        var pinValidateState = await _sagaHarness.Exists(id, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));
        Assert.NotNull(pinValidateState);

        // 4) Publish PinValidated
        await _harness.Bus.Publish(new PinValidated(id));

        // 5) Wait until saga returns to "Ready"
        var readyState = await _sagaHarness.Exists(id, x => x.Ready, TimeSpan.FromSeconds(5));
        Assert.NotNull(readyState);

        // 6) Confirm that TransferCmd was published
        Assert.True(await _harness.Published.Any<TransferCmd>(m => m.Context.Message.CorrelationId == id));
    }


    [Fact]
    public async Task PIN_03_Should_Stay_In_AwaitingPinValidate_On_PinInvalid()
    {
        var id = await SeedUserToReadyAsync();
        var payload = new TransferPayload("111111", "001", 12345, "Test");

        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Transfer, payload));
        await _harness.InactivityTask;
        await _harness.Bus.Publish(new PinInvalid(id, "wrong"));
        await _harness.InactivityTask;

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

        // 1) Publish BillPay intent
        var payload = new BillPayload("DSTV", "123456", 5000, "DSTV");
        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.BillPay, BillPayload: payload));

        // 2) Wait for saga to become AwaitingPinValidate
        var pinValidate = await _sagaHarness.Exists(id, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));
        Assert.NotNull(pinValidate);

        // 3) Publish PinValidated
        await _harness.Bus.Publish(new PinValidated(id));

        // 4) Wait for saga to become Ready
        var readyState = await _sagaHarness.Exists(id, x => x.Ready, TimeSpan.FromSeconds(5));
        Assert.NotNull(readyState);

        // 5) Confirm BillPayCmd was published
        Assert.True(
            await _harness.Published.Any<BillPayCmd>(x => x.Context.Message.CorrelationId == id),
            "Expected BillPayCmd to be published but it wasn't."
        );
    }


    [Fact]
    public async Task PIN_05_Should_Ignore_Unknown_IntentType_On_PinValidated()
    {
        var id = await SeedUserToReadyAsync();

        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Transfer,
            new TransferPayload("111111", "001", 12345, "Test")));
        await _harness.InactivityTask;
        var saga = _sagaHarness.Sagas.Contains(id);
        saga.PendingIntentType = IntentType.Unknown;
        saga.PendingIntentPayload = JsonSerializer.Serialize(new UserIntentDetected(id, IntentType.Transfer));

        await _harness.Bus.Publish(new PinValidated(id));
        await _harness.InactivityTask;
        saga = _sagaHarness.Sagas.Contains(id);
        Assert.Equal("AwaitingPinValidate", saga?.CurrentState);
    }

    [Fact]
    public async Task PIN_06_Should_Handle_Invalid_Json_Payload_Gracefully()
    {
        var id = await SeedUserToReadyAsync();

        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Transfer,
            new TransferPayload("111111", "001", 12345, "Test")));
        await _harness.InactivityTask;
        var saga = _sagaHarness.Sagas.Contains(id);
        saga.PendingIntentPayload = "not valid json";
        saga.PendingIntentType = IntentType.Transfer;

        await _harness.Bus.Publish(new PinValidated(id));
        await _harness.InactivityTask;

        var after = _sagaHarness.Sagas.Contains(id);
        Assert.Equal("AwaitingPinValidate", after?.CurrentState);
    }

    [Fact]
    public async Task PIN_07_Should_Not_Publish_If_PendingIntent_Null()
    {
        var id = await SeedUserToReadyAsync();

        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Transfer,
            new TransferPayload("111111", "001", 12345, "Test")));
        await _harness.InactivityTask;
        var saga = _sagaHarness.Sagas.Contains(id);
        saga.PendingIntentType = IntentType.Transfer;
        saga.PendingIntentPayload = null;

        await _harness.Bus.Publish(new PinValidated(id));
        await _harness.InactivityTask;

        var result = _sagaHarness.Sagas.Contains(id);
        Assert.Equal("AwaitingPinValidate", result?.CurrentState);
    }

    [Fact]
    public async Task PIN_08_Should_Generate_Reference_And_Publish_TransferCmd()
    {
        var id = await SeedUserToReadyAsync();
        var payload = new TransferPayload("111111", "001", 12345, "Test");

        // 1) Publish Transfer Intent
        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Transfer, payload));

        // 2) Wait for saga => AwaitingPinValidate
        var pinValidateState = await _sagaHarness.Exists(id, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));
        Assert.NotNull(pinValidateState);

        // 3) Publish PinValidated
        await _harness.Bus.Publish(new PinValidated(id));

        // 4) Wait for saga => Ready
        var readyState = await _sagaHarness.Exists(id, x => x.Ready, TimeSpan.FromSeconds(5));
        Assert.NotNull(readyState);

        // 5) Confirm TransferCmd was published
        var cmd = _harness.Published
            .Select<TransferCmd>()
            .FirstOrDefault(x => x.Context.Message.CorrelationId == id);

        Assert.NotNull(cmd);
        Assert.NotNull(cmd.Context.Message.Reference);
    }


    [Fact]
    public async Task PIN_09_Should_Allow_PinRetry_And_Then_Proceed_On_Valid_Pin()
    {
        // 1) Ensure the saga is in "Ready"
        var id = await SeedUserToReadyAsync();

        // 2) Publish a Transfer intent
        var transferPayload = new TransferPayload("111111", "001", 12345, "Test");
        await _harness.Bus.Publish(new UserIntentDetected(
            id,
            IntentType.Transfer,
            transferPayload
        ));

        // 3) Wait for saga to reach "AwaitingPinValidate"
        var awaitingPinState = await _sagaHarness.Exists(id, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));
        Assert.NotNull(awaitingPinState);

        // 4) Publish PinInvalid
        await _harness.Bus.Publish(new PinInvalid(id, "wrong"));

        // 5) Confirm we still remain in "AwaitingPinValidate"
        var stillAwaitingPin = await _sagaHarness.Exists(id, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));
        Assert.NotNull(stillAwaitingPin);

        // 6) Publish PinValidated
        await _harness.Bus.Publish(new PinValidated(id));

        // 7) Wait for saga to return to "Ready"
        var readyState = await _sagaHarness.Exists(id, x => x.Ready, TimeSpan.FromSeconds(5));
        Assert.NotNull(readyState);

        // 8) Confirm TransferCmd was published
        var transferCmd = _harness.Published
            .Select<TransferCmd>()
            .FirstOrDefault(x => x.Context.Message.CorrelationId == id);

        Assert.NotNull(transferCmd);
        // Optionally confirm payload
        Assert.Equal("111111", transferCmd.Context.Message.Payload.ToAccount);
    }

    [Fact]
    public async Task PIN_10_Should_Execute_Only_Latest_Intent_Before_PinValidated()
    {
        // 1) Start from a known "Ready" user
        var id = await SeedUserToReadyAsync();

        // 2) Publish a Transfer intent
        var transfer = new TransferPayload("111111", "001", 12345, "Test");
        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Transfer, transfer));

        // 3) Wait for the saga to become AwaitingPinValidate
        var pinState1 = await _sagaHarness.Exists(id, s => s.AwaitingPinValidate, TimeSpan.FromSeconds(5));
        Assert.NotNull(pinState1);

        // 4) Publish a BillPay intent (overriding the older pending Transfer)
        var bill = new BillPayload("DSTV", "123456", 5000, "DSTV");
        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.BillPay, BillPayload: bill));

        // 5) Confirm we remain in AwaitingPinValidate
        var pinState2 = await _sagaHarness.Exists(id, s => s.AwaitingPinValidate, TimeSpan.FromSeconds(5));
        Assert.NotNull(pinState2);

        // 6) Publish PinValidated
        await _harness.Bus.Publish(new PinValidated(id));

        // 7) Wait for the saga to become Ready
        var readyState = await _sagaHarness.Exists(id, s => s.Ready, TimeSpan.FromSeconds(5));
        Assert.NotNull(readyState);

        // 8) Confirm BillPayCmd was published, and TransferCmd was not
        var billCmd = _harness.Published
            .Select<BillPayCmd>()
            .FirstOrDefault(x => x.Context.Message.CorrelationId == id);

        Assert.NotNull(billCmd); // <-- no more NullRef
        Assert.Equal("DSTV", billCmd.Context.Message.Payload.BillerCode);

        var transferCmd = _harness.Published
            .Select<TransferCmd>()
            .FirstOrDefault(x => x.Context.Message.CorrelationId == id);

        Assert.Null(transferCmd);
    }

    [Fact]
    public async Task PIN_11_Should_Allow_Intent_Change_While_Awaiting_Pin()
    {
        // 1) Start from "Ready"
        var id = await SeedUserToReadyAsync();

        // 2) Publish Transfer intent
        var transfer = new TransferPayload("111111", "001", 12345, "Test");
        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Transfer, transfer));

        // 3) Wait for saga => AwaitingPinValidate
        var pinState1 = await _sagaHarness.Exists(id, s => s.AwaitingPinValidate, TimeSpan.FromSeconds(5));
        Assert.NotNull(pinState1);

        // 4) Publish a BillPay intent while in AwaitingPinValidate
        var bill = new BillPayload("DSTV", "123456", 5000, "DSTV");
        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.BillPay, BillPayload: bill));

        // 5) Confirm saga is still in AwaitingPinValidate (intent changed but state is same)
        var pinState2 = await _sagaHarness.Exists(id, s => s.AwaitingPinValidate, TimeSpan.FromSeconds(5));
        Assert.NotNull(pinState2);

        // 6) Publish PinValidated
        await _harness.Bus.Publish(new PinValidated(id));

        // 7) Wait for saga => Ready
        var readyState = await _sagaHarness.Exists(id, s => s.Ready, TimeSpan.FromSeconds(5));
        Assert.NotNull(readyState);

        // 8) Confirm BillPayCmd was published
        var billCmd = _harness.Published
            .Select<BillPayCmd>()
            .FirstOrDefault(x => x.Context.Message.CorrelationId == id);

        Assert.NotNull(billCmd); // <-- ensures no NullRef
        Assert.Equal("DSTV", billCmd.Context.Message.Payload.BillerCode);

        // 9) Confirm TransferCmd was *not* published
        var transferCmd = _harness.Published
            .Select<TransferCmd>()
            .FirstOrDefault(x => x.Context.Message.CorrelationId == id);

        Assert.Null(transferCmd);
    }

    [Fact]
    public async Task PIN_12_Should_Finalize_On_AwaitingPinValidate_Timeout()
    {
        var id = await SeedUserToReadyAsync();

        var payload = new TransferPayload("111111", "001", 12345, "Test");
        await _harness.Bus.Publish(new UserIntentDetected(id, IntentType.Transfer, payload));

        await _sagaHarness.Exists(id, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new TimeoutExpired { CorrelationId = id });

        var final = await _sagaHarness.Exists(id, x => x.Final, TimeSpan.FromSeconds(5));
        Assert.NotNull(final);

        var nudge = _harness.Published.Select<NudgeCmd>()
            .FirstOrDefault(x => x.Context.Message.CorrelationId == id);
        Assert.NotNull(nudge);
        Assert.Equal(NudgeType.TimedOut, nudge.Context.Message.NudgeType);
    }
}