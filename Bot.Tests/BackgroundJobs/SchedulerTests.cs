using Bot.Host.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Quartz.Spi;
using Xunit;

namespace Bot.Tests.BackgroundJobs;

public class SchedulerTests
{
    private class Capture
    {
        public IJobDetail? Job { get; set; }
        public ITrigger? Trigger { get; set; }
    }

    private static ServiceProvider BuildProvider(IScheduler scheduler)
    {
        var factory = new Mock<ISchedulerFactory>();
        factory.Setup(f => f.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduler);

        var services = new ServiceCollection();
        services.AddSingleton(factory.Object);
        services.AddSingleton<IJobFactory>(new Mock<IJobFactory>().Object);
        return services.BuildServiceProvider();
    }

    private static (Mock<IScheduler> SchedulerMock, Capture Captured) SetupScheduler()
    {
        var scheduler = new Mock<IScheduler>();
        var captured = new Capture();

        scheduler.Setup(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .Callback<IJobDetail, ITrigger, CancellationToken>((j, t, _) =>
            {
                captured.Job = j;
                captured.Trigger = t;
            })
            .ReturnsAsync(DateTimeOffset.UtcNow);

        scheduler.Setup(s => s.Start(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        scheduler.Setup(s => s.Shutdown(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        return (scheduler, captured);
    }

    [Fact]
    public async Task BillPayScheduler_Starts_With_Correct_Cron()
    {
        var (schedulerMock, captured) = SetupScheduler();
        await using var provider = BuildProvider(schedulerMock.Object);
        var logger = new Mock<ILogger<BillPayScheduler>>();
        var svc = new BillPayScheduler(provider, logger.Object);

        await svc.StartAsync(CancellationToken.None);

        Assert.NotNull(captured.Job);
        Assert.Equal("BillPayJob", captured.Job!.Key.Name);
        Assert.Equal("Billing", captured.Job!.Key.Group);
        Assert.NotNull(captured.Trigger);
        Assert.Equal("BillPayTrigger", captured.Trigger!.Key.Name);
        Assert.Equal("Billing", captured.Trigger!.Key.Group);
        var cron = Assert.IsAssignableFrom<ICronTrigger>(captured.Trigger);
        Assert.Equal("0 */10 * * * ?", cron.CronExpressionString);
    }

    [Fact]
    public async Task BillPayScheduler_Stop_Shuts_Down()
    {
        var (schedulerMock, _) = SetupScheduler();
        await using var provider = BuildProvider(schedulerMock.Object);
        var logger = new Mock<ILogger<BillPayScheduler>>();
        var svc = new BillPayScheduler(provider, logger.Object);
        await svc.StartAsync(CancellationToken.None);

        await svc.StopAsync(CancellationToken.None);

        schedulerMock.Verify(s => s.Shutdown(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FeeSweepScheduler_Starts_With_Correct_Cron()
    {
        var (schedulerMock, captured) = SetupScheduler();
        await using var provider = BuildProvider(schedulerMock.Object);
        var logger = new Mock<ILogger<FeeSweepScheduler>>();
        var svc = new FeeSweepScheduler(provider, logger.Object);

        await svc.StartAsync(CancellationToken.None);

        Assert.NotNull(captured.Job);
        Assert.Equal("FeeSweepJob", captured.Job!.Key.Name);
        Assert.Equal("Billing", captured.Job!.Key.Group);
        Assert.NotNull(captured.Trigger);
        Assert.Equal("FeeSweepTrigger", captured.Trigger!.Key.Name);
        Assert.Equal("Billing", captured.Trigger!.Key.Group);
        var cron = Assert.IsAssignableFrom<ICronTrigger>(captured.Trigger);
        Assert.Equal("0 0 0 * * ?", cron.CronExpressionString);
    }

    [Fact]
    public async Task FeeSweepScheduler_Stop_Shuts_Down()
    {
        var (schedulerMock, _) = SetupScheduler();
        await using var provider = BuildProvider(schedulerMock.Object);
        var logger = new Mock<ILogger<FeeSweepScheduler>>();
        var svc = new FeeSweepScheduler(provider, logger.Object);
        await svc.StartAsync(CancellationToken.None);

        await svc.StopAsync(CancellationToken.None);

        schedulerMock.Verify(s => s.Shutdown(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecurringTransferScheduler_Starts_With_Correct_Cron()
    {
        var (schedulerMock, captured) = SetupScheduler();
        await using var provider = BuildProvider(schedulerMock.Object);
        var logger = new Mock<ILogger<RecurringTransferScheduler>>();
        var svc = new RecurringTransferScheduler(provider, logger.Object);

        await svc.StartAsync(CancellationToken.None);

        Assert.NotNull(captured.Job);
        Assert.Equal("RecurringTransferJob", captured.Job!.Key.Name);
        Assert.Equal("Transfers", captured.Job!.Key.Group);
        Assert.NotNull(captured.Trigger);
        Assert.Equal("RecurringTrigger", captured.Trigger!.Key.Name);
        Assert.Equal("Transfers", captured.Trigger!.Key.Group);
        var cron = Assert.IsAssignableFrom<ICronTrigger>(captured.Trigger);
        Assert.Equal("0 */5 * * * ?", cron.CronExpressionString);
    }

    [Fact]
    public async Task RecurringTransferScheduler_Stop_Shuts_Down()
    {
        var (schedulerMock, _) = SetupScheduler();
        await using var provider = BuildProvider(schedulerMock.Object);
        var logger = new Mock<ILogger<RecurringTransferScheduler>>();
        var svc = new RecurringTransferScheduler(provider, logger.Object);
        await svc.StartAsync(CancellationToken.None);

        await svc.StopAsync(CancellationToken.None);

        schedulerMock.Verify(s => s.Shutdown(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BudgetAlertScheduler_Starts_With_Correct_Cron()
    {
        var (schedulerMock, captured) = SetupScheduler();
        await using var provider = BuildProvider(schedulerMock.Object);
        var logger = new Mock<ILogger<BudgetAlertScheduler>>();
        var svc = new BudgetAlertScheduler(provider, logger.Object);

        await svc.StartAsync(CancellationToken.None);

        Assert.NotNull(captured.Job);
        Assert.Equal("BudgetAlertJob", captured.Job!.Key.Name);
        Assert.Equal("Goals", captured.Job!.Key.Group);
        Assert.NotNull(captured.Trigger);
        Assert.Equal("BudgetAlertTrigger", captured.Trigger!.Key.Name);
        Assert.Equal("Goals", captured.Trigger!.Key.Group);
        var cron = Assert.IsAssignableFrom<ICronTrigger>(captured.Trigger);
        Assert.Equal("0 0 * * * ?", cron.CronExpressionString);
    }

    [Fact]
    public async Task BudgetAlertScheduler_Stop_Shuts_Down()
    {
        var (schedulerMock, _) = SetupScheduler();
        await using var provider = BuildProvider(schedulerMock.Object);
        var logger = new Mock<ILogger<BudgetAlertScheduler>>();
        var svc = new BudgetAlertScheduler(provider, logger.Object);
        await svc.StartAsync(CancellationToken.None);

        await svc.StopAsync(CancellationToken.None);

        schedulerMock.Verify(s => s.Shutdown(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Scheduler_Should_Set_JobFactory_From_DI()
    {
        var schedulerMock = new Mock<IScheduler>();
        var factoryMock = new Mock<ISchedulerFactory>();
        var jobFactoryMock = new Mock<IJobFactory>();

        factoryMock.Setup(f => f.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedulerMock.Object);

        var services = new ServiceCollection();
        services.AddSingleton(factoryMock.Object);
        services.AddSingleton(jobFactoryMock.Object);
        var provider = services.BuildServiceProvider();

        var logger = new Mock<ILogger<BillPayScheduler>>();
        var svc = new BillPayScheduler(provider, logger.Object);

        await svc.StartAsync(CancellationToken.None);

        schedulerMock.VerifySet(s => s.JobFactory = jobFactoryMock.Object, Times.Once);
    }

    [Fact]
    public async Task StopAsync_Should_Handle_Uninitialized_Scheduler()
    {
        var logger = new Mock<ILogger<BillPayScheduler>>();
        var provider = BuildProvider(Mock.Of<IScheduler>());
        var svc = new BillPayScheduler(provider, logger.Object);

        await svc.StopAsync(CancellationToken.None); // should not throw even if not started
    }

}
