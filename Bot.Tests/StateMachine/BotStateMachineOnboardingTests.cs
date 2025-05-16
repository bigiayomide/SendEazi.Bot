using Bot.Core.Services;
using Bot.Core.StateMachine;
using Bot.Shared;
using Bot.Shared.Models;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Bot.Tests.StateMachine;

public class BotStateMachineOnboardingTests(ITestOutputHelper testOutputHelper) : IAsyncLifetime
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
        if (_harness != null)
            await _harness.Stop();
        if (_provider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
    }

    [Fact]
    public async Task ONB_01_Should_Transition_To_AskFullName_On_Signup_Intent()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new UserIntentDetected(id, "signup"));

        var instance = await _sagaHarness.Exists(id, x => x.AskFullName, TimeSpan.FromSeconds(5));

        Assert.NotNull(instance);
    }

    [Fact]
    public async Task ONB_02_Should_Transition_To_AskNin_On_FullNameProvided()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new UserIntentDetected(id, "signup"));
        await _harness.Bus.Publish(new FullNameProvided(id, "Jane Doe"));

        var instance = await _sagaHarness.Exists(id, x => x.AskNin, TimeSpan.FromSeconds(5));
        var saga = _sagaHarness.Sagas.Contains(instance.Value);

        Assert.Equal("Jane Doe", saga?.TempName);
    }

    [Fact]
    public async Task ONB_03_Should_Transition_To_AskBvn_On_NinVerified()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new UserIntentDetected(id, "signup"));
        await _harness.Bus.Publish(new FullNameProvided(id, "John Smith"));
        await _harness.Bus.Publish(new NinProvided(id, "12345678901"));
        await _harness.Bus.Publish(new NinVerified(id, "12345678901"));

        var instance = await _sagaHarness.Exists(id, x => x.AskBvn, TimeSpan.FromSeconds(5));
        var saga = _sagaHarness.Sagas.Contains(instance.Value);

        Assert.Equal("12345678901", saga?.TempNIN);
    }

    [Fact]
    public async Task ONB_04_Should_Return_To_AskNin_On_NinRejected()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new UserIntentDetected(id, "signup"));
        await _harness.Bus.Publish(new FullNameProvided(id, "Test User"));
        await _harness.Bus.Publish(new NinProvided(id, "invalid"));
        await _harness.Bus.Publish(new NinRejected(id, "NIN is invalid"));

        var instance = await _sagaHarness.Exists(id, x => x.AskNin, TimeSpan.FromSeconds(5));
        Assert.NotNull(instance);
    }

    [Fact]
    public async Task ONB_05_Should_Transition_To_AwaitingKyc_On_BvnVerified()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new UserIntentDetected(id, "signup"));
        await _harness.Bus.Publish(new FullNameProvided(id, "Test User"));
        await _harness.Bus.Publish(new NinProvided(id, "12345678901"));
        await _harness.Bus.Publish(new NinVerified(id, "12345678901"));
        await _harness.Bus.Publish(new BvnProvided(id, "12345678901"));
        await _harness.Bus.Publish(new BvnVerified(id, "12345678901"));

        var instance = await _sagaHarness.Exists(id, x => x.AwaitingKyc, TimeSpan.FromSeconds(5));
        var saga = _sagaHarness.Sagas.Contains(instance.Value);

        Assert.Equal("12345678901", saga?.TempBVN);
    }

    [Fact]
    public async Task ONB_06_Should_Finalize_On_SignupFailed()
    {
        var id = NewId.NextGuid();
        await _harness.Bus.Publish(new UserIntentDetected(id, "signup"));
        await _harness.Bus.Publish(new FullNameProvided(id, "Test User"));
        await _harness.Bus.Publish(new NinProvided(id, "12345678901"));
        await _harness.Bus.Publish(new NinVerified(id, "12345678901"));
        await _harness.Bus.Publish(new BvnProvided(id, "12345678901"));
        await _harness.Bus.Publish(new BvnVerified(id, "12345678901"));
        await _harness.Bus.Publish(new SignupFailed(id, "Invalid data"));

        var instance = await _sagaHarness.Exists(id, x => x.Final, TimeSpan.FromSeconds(5));
        Assert.Null(instance);
    }
    [Fact]
    public async Task ONB_07_Should_Return_To_AskBvn_On_BvnRejected()
    {
        var id = NewId.NextGuid();

        await _harness.Bus.Publish(new UserIntentDetected(id, "signup"));
        await _harness.Bus.Publish(new FullNameProvided(id, "Test User"));
        await _harness.Bus.Publish(new NinProvided(id, "12345678901"));
        await _harness.Bus.Publish(new NinVerified(id, "12345678901"));
        await _harness.Bus.Publish(new BvnProvided(id, "12345678901"));
        await _harness.Bus.Publish(new BvnRejected(id, "BVN mismatch"));

        var instance = await _sagaHarness.Exists(id, x => x.AskBvn, TimeSpan.FromSeconds(5));
        Assert.NotNull(instance);

        var nudge = _harness.Published.Select<NudgeCmd>().FirstOrDefault(x => x.Context.Message.CorrelationId == id);
        Assert.NotNull(nudge);
        Assert.Equal(NudgeType.InvalidBvn, nudge.Context.Message.NudgeType);
    }
    
    [Fact]
    public async Task ONB_08_Should_Transition_To_AwaitingBankLink_On_SignupSucceeded()
    {
        var id = NewId.NextGuid();

        await _harness.Bus.Publish(new UserIntentDetected(id, "signup"));
        await _harness.Bus.Publish(new FullNameProvided(id, "Test User"));
        await _harness.Bus.Publish(new NinProvided(id, "12345678901"));
        await _harness.Bus.Publish(new NinVerified(id, "12345678901"));
        await _harness.Bus.Publish(new BvnProvided(id, "12345678901"));
        await _harness.Bus.Publish(new BvnVerified(id, "12345678901"));
        await _harness.Bus.Publish(new SignupSucceeded(id));

        var instance = await _sagaHarness.Exists(id, x => x.AwaitingBankLink, TimeSpan.FromSeconds(5));
        Assert.NotNull(instance);

        var kyc = _harness.Published.Select<KycCmd>().FirstOrDefault(x => x.Context.Message.CorrelationId == id);
        Assert.NotNull(kyc);
    }
    [Fact]
    public async Task ONB_09_Should_Finalize_On_KycRejected()
    {
        var id = NewId.NextGuid();

        await _harness.Bus.Publish(new UserIntentDetected(id, "signup"));
        await _harness.Bus.Publish(new FullNameProvided(id, "Test User"));
        await _harness.Bus.Publish(new NinProvided(id, "12345678901"));
        await _harness.Bus.Publish(new NinVerified(id, "12345678901"));
        await _harness.Bus.Publish(new BvnProvided(id, "12345678901"));
        await _harness.Bus.Publish(new BvnVerified(id, "12345678901"));
        await _harness.Bus.Publish(new SignupSucceeded(id));
        await _harness.Bus.Publish(new KycRejected(id, "Failed validation"));

        var instance = await _sagaHarness.Exists(id, x => x.Final, TimeSpan.FromSeconds(5));
        Assert.Null(instance);
    }

    [Fact]
    public async Task ONB_10_Should_Persist_Temp_Fields_Throughout_Onboarding()
    {
        var id = NewId.NextGuid();

        await _harness.Bus.Publish(new UserIntentDetected(id, "signup"));
        await _harness.Bus.Publish(new FullNameProvided(id, "Ayomide Fajobi"));
        await _harness.Bus.Publish(new NinProvided(id, "11223344556"));
        await _harness.Bus.Publish(new NinVerified(id, "11223344556"));
        await _harness.Bus.Publish(new BvnProvided(id, "99887766554"));
        await _harness.Bus.Publish(new BvnVerified(id, "99887766554"));

        var instance = await _sagaHarness.Exists(id, x => x.AwaitingKyc, TimeSpan.FromSeconds(5));
        var saga = _sagaHarness.Sagas.Contains(instance.Value);

        Assert.Equal("Ayomide Fajobi", saga?.TempName);
        Assert.Equal("11223344556", saga?.TempNIN);
        Assert.Equal("99887766554", saga?.TempBVN);
    }
    
    [Fact]
    public async Task ONB_11_Should_Transition_To_AwaitingBankLink_On_KycApproved()
    {
        var id = NewId.NextGuid();

        await _harness.Bus.Publish(new UserIntentDetected(id, "signup"));
        await _harness.Bus.Publish(new FullNameProvided(id, "Test User"));
        await _harness.Bus.Publish(new NinProvided(id, "12345678901"));
        await _harness.Bus.Publish(new NinVerified(id, "12345678901"));
        await _harness.Bus.Publish(new BvnProvided(id, "12345678901"));
        await _harness.Bus.Publish(new BvnVerified(id, "12345678901"));
        await _harness.Bus.Publish(new SignupSucceeded(id));
        await _harness.Bus.Publish(new KycApproved(id));

        var instance = await _sagaHarness.Exists(id, x => x.AwaitingBankLink, TimeSpan.FromSeconds(5));
        Assert.NotNull(instance);
    }
    [Fact]
    public async Task ONB_12_Should_Ignore_Second_Signup_Intent()
    {
        var id = NewId.NextGuid();

        await _harness.Bus.Publish(new UserIntentDetected(id, "signup"));
        await _harness.Bus.Publish(new UserIntentDetected(id, "signup"));

        var count = _sagaHarness.Sagas.Count();
        testOutputHelper.WriteLine(count.ToString());
        Assert.Equal(1, count);
    }
    [Fact]
    public async Task ONB_13_Should_Ignore_BvnProvided_If_Nin_Not_Set()
    {
        var id = NewId.NextGuid();

        await _harness.Bus.Publish(new UserIntentDetected(id, "signup"));
        await _harness.Bus.Publish(new FullNameProvided(id, "Test User"));

        // Wait for AskNin state to be set
        var step1 = await _sagaHarness.Exists(id, x => x.AskNin, TimeSpan.FromSeconds(5));
        Assert.NotNull(step1); // make sure saga progressed

        // Send BvnProvided before NinProvided
        await _harness.Bus.Publish(new BvnProvided(id, "12345678901"));

        var saga = _sagaHarness.Sagas.Contains(id);
        Assert.Equal("AskNin", saga?.CurrentState); // should remain in AskNin
        Assert.Null(saga?.TempNIN); // nin was never set
        Assert.Null(saga?.TempBVN); // bvn should not stick
    }

    [Fact]
    public async Task ONB_14_Should_Not_Throw_On_Malformed_PendingIntentPayload()
    {
        var id = NewId.NextGuid();

        // Complete onboarding
        await _harness.Bus.Publish(new UserIntentDetected(id, "signup"));
        await _harness.Bus.Publish(new FullNameProvided(id, "Ayomide"));
        await _harness.Bus.Publish(new NinProvided(id, "12345678901"));
        await _harness.Bus.Publish(new NinVerified(id, "12345678901"));
        await _harness.Bus.Publish(new BvnProvided(id, "12345678901"));
        await _harness.Bus.Publish(new BvnVerified(id, "12345678901"));
        await _harness.Bus.Publish(new SignupSucceeded(id));
        await _harness.Bus.Publish(new KycApproved(id));
        await _harness.Bus.Publish(new MandateReadyToDebit(id, "mid", "Mono"));
        await _harness.Bus.Publish(new BankLinkSucceeded(id));
        await _harness.Bus.Publish(new PinSet(id));

        // Confirm we're in Ready state
        var ready = await _sagaHarness.Exists(id, x => x.Ready, TimeSpan.FromSeconds(5));
        Assert.NotNull(ready);

        // Send a sensitive intent (transfer) now that we're in Ready
        var payload = new TransferPayload("1234567890", "001", 5000, "Test");
        await _harness.Bus.Publish(new UserIntentDetected(id, "transfer", TransferPayload: payload));

        // Saga should transition to AwaitingPinValidate
        var pinPrompt = await _sagaHarness.Exists(id, x => x.AwaitingPinValidate, TimeSpan.FromSeconds(5));
        Assert.NotNull(pinPrompt);

        // Corrupt the payload manually
        var saga = _sagaHarness.Sagas.Contains(id);
        Assert.NotNull(saga);
        saga!.PendingIntentPayload = "<<<INVALID_JSON>>>";
        saga.PendingIntentType = "transfer";

        // Publish PIN validated event
        var ex = await Record.ExceptionAsync(() => _harness.Bus.Publish(new PinValidated(id)));

        // Should not throw
        Assert.Null(ex);

        // Should remain in AwaitingPinValidate (not proceed to Ready)
        var after = _sagaHarness.Sagas.Contains(id);
        Assert.Equal("AwaitingPinValidate", after?.CurrentState);
    }

    [Fact]
    public async Task ONB_15_Should_Ignore_Unknown_Intent_Gracefully()
    {
        var id = NewId.NextGuid();

        await _harness.Bus.Publish(new UserIntentDetected(id, "signup"));

        var askFullName = await _sagaHarness.Exists(id, x => x.AskFullName, TimeSpan.FromSeconds(5));
        Assert.True(askFullName.HasValue, "Saga should be created in AskFullName");

        await _harness.Bus.Publish(new FullNameProvided(id, "Test User"));

        var askNin = await _sagaHarness.Exists(id, x => x.AskNin, TimeSpan.FromSeconds(5));
        Assert.True(askNin.HasValue, "Saga should be in AskNin");

        await _harness.Bus.Publish(new UserIntentDetected(id, "alien_command"));

        var saga = _sagaHarness.Sagas.Contains(id);
        Assert.NotNull(saga);
        Assert.Contains(saga.CurrentState, new[] { "AskNin", "AskFullName" }); // state should remain stable
    }

}
