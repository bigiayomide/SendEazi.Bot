using Bot.Core.Providers;
using Bot.Core.Services;
using Bot.Core.StateMachine;
using Bot.Shared.DTOs;
using Bot.Shared.Models;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Assert = Xunit.Assert;

namespace Bot.Tests.StateMachine;

public class DirectDebitMandateStateMachineTests : IAsyncLifetime
{
    private readonly Mock<IBankProvider> _bankProvider = new();
    private readonly Mock<IUserService> _userService = new();
    private ITestHarness _harness = null!;
    private ServiceProvider _provider = null!;
    private ISagaStateMachineTestHarness<DirectDebitMandateStateMachine, DirectDebitMandateState> _sagaHarness = null!;

    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddSingleton(_bankProvider.Object)
            .AddSingleton(_userService.Object)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<DirectDebitMandateStateMachine, DirectDebitMandateState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        _sagaHarness = _provider
            .GetRequiredService<
                ISagaStateMachineTestHarness<DirectDebitMandateStateMachine, DirectDebitMandateState>>();

        _bankProvider.Setup(x => x.CreateCustomerAsync(It.IsAny<User>())).ReturnsAsync("cust");
        _bankProvider.Setup(x =>
                x.CreateMandateAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync("mandate-123");

        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        if (_harness != null) await _harness.Stop();
        if (_provider is IAsyncDisposable disp) await disp.DisposeAsync();
    }

    [Fact]
    public async Task DDM_01_Should_Move_To_AwaitingApproval_On_StartCmd()
    {
        var id = NewId.NextGuid();
        _userService.Setup(u => u.GetByIdAsync(id)).ReturnsAsync(new User { Id = id });

        await _harness.Bus.Publish(new StartMandateSetupCmd(id, "Jane", "+234", "12345678901"));

        var instance = await _sagaHarness.Exists(id, x => x.AwaitingApproval, TimeSpan.FromSeconds(5));
        Assert.NotNull(instance);
    }

    [Fact]
    public async Task DDM_02_Should_Publish_Event_And_Move_To_Ready_On_MandateReady()
    {
        var id = NewId.NextGuid();
        _userService.Setup(u => u.GetByIdAsync(id)).ReturnsAsync(new User { Id = id });

        await _harness.Bus.Publish(new StartMandateSetupCmd(id, "Jane", "+234", "12345678901"));
        await _sagaHarness.Exists(id, x => x.AwaitingApproval, TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new MandateReadyToDebit(id, "mandate-123", "Mono"));
        var ready = await _sagaHarness.Exists(id, x => x.Ready, TimeSpan.FromSeconds(5));
        Assert.NotNull(ready);

        var count = _harness.Published.Select<MandateReadyToDebit>().Count(x => x.Context.Message.CorrelationId == id);
        Assert.Equal(2, count);
    }
}