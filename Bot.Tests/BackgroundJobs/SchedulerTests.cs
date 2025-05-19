using Bot.Host.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Quartz.Spi;
using Assert = Xunit.Assert;

namespace Bot.Tests.BackgroundJobs;

public class SchedulerTests
{
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

    private static Mock<IScheduler> SetupScheduler(out IJobDetail? job, out ITrigger? trigger)
    {
        var scheduler = new Mock<IScheduler>();
        job = null;
        trigger = null;
        IJobDetail? capturedJob = null;
        ITrigger? capturedTrigger = null;
        scheduler.Setup(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .Callback<IJobDetail, ITrigger, CancellationToken>((j, t, _) =>
            {
                capturedJob = j;
                capturedTrigger = t;
            })
            .ReturnsAsync(DateTimeOffset.UtcNow);
        scheduler.Setup(s => s.Start(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        scheduler.Setup(s => s.Shutdown(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        job = capturedJob;
        trigger = capturedTrigger;
        return scheduler;
    }

    [Fact]
    public async Task BillPayScheduler_Starts_With_Correct_Cron()
    {
        var schedulerMock = SetupScheduler(out var job, out var trigger);
        using var provider = BuildProvider(schedulerMock.Object);
        var logger = new Mock<ILogger<BillPayScheduler>>();
        var svc = new BillPayScheduler(provider, logger.Object);

        await svc.StartAsync(CancellationToken.None);

        Assert.NotNull(job);
        Assert.Equal("BillPayJob", job!.Key.Name);
        Assert.Equal("Billing", job!.Key.Group);
        Assert.NotNull(trigger);
        Assert.Equal("BillPayTrigger", trigger!.Key.Name);
        Assert.Equal("Billing", trigger!.Key.Group);
        var cron = Assert.IsAssignableFrom<ICronTrigger>(trigger);
        Assert.Equal("0 */10 * * * ?", cron.CronExpressionString);
    }

    [Fact]
    public async Task BillPayScheduler_Stop_Shuts_Down()
    {
        var schedulerMock = SetupScheduler(out _, out _);
        using var provider = BuildProvider(schedulerMock.Object);
        var logger = new Mock<ILogger<BillPayScheduler>>();
        var svc = new BillPayScheduler(provider, logger.Object);
        await svc.StartAsync(CancellationToken.None);

        await svc.StopAsync(CancellationToken.None);

        schedulerMock.Verify(s => s.Shutdown(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FeeSweepScheduler_Starts_With_Correct_Cron()
    {
        var schedulerMock = SetupScheduler(out var job, out var trigger);
        using var provider = BuildProvider(schedulerMock.Object);
        var logger = new Mock<ILogger<FeeSweepScheduler>>();
        var svc = new FeeSweepScheduler(provider, logger.Object);

        await svc.StartAsync(CancellationToken.None);

        Assert.NotNull(job);
        Assert.Equal("FeeSweepJob", job!.Key.Name);
        Assert.Equal("Billing", job!.Key.Group);
        Assert.NotNull(trigger);
        Assert.Equal("FeeSweepTrigger", trigger!.Key.Name);
        Assert.Equal("Billing", trigger!.Key.Group);
        var cron = Assert.IsAssignableFrom<ICronTrigger>(trigger);
        Assert.Equal("0 0 0 * * ?", cron.CronExpressionString);
    }

    [Fact]
    public async Task FeeSweepScheduler_Stop_Shuts_Down()
    {
        var schedulerMock = SetupScheduler(out _, out _);
        using var provider = BuildProvider(schedulerMock.Object);
        var logger = new Mock<ILogger<FeeSweepScheduler>>();
        var svc = new FeeSweepScheduler(provider, logger.Object);
        await svc.StartAsync(CancellationToken.None);

        await svc.StopAsync(CancellationToken.None);

        schedulerMock.Verify(s => s.Shutdown(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecurringTransferScheduler_Starts_With_Correct_Cron()
    {
        var schedulerMock = SetupScheduler(out var job, out var trigger);
        using var provider = BuildProvider(schedulerMock.Object);
        var logger = new Mock<ILogger<RecurringTransferScheduler>>();
        var svc = new RecurringTransferScheduler(provider, logger.Object);

        await svc.StartAsync(CancellationToken.None);

        Assert.NotNull(job);
        Assert.Equal("RecurringTransferJob", job!.Key.Name);
        Assert.Equal("Transfers", job!.Key.Group);
        Assert.NotNull(trigger);
        Assert.Equal("RecurringTrigger", trigger!.Key.Name);
        Assert.Equal("Transfers", trigger!.Key.Group);
        var cron = Assert.IsAssignableFrom<ICronTrigger>(trigger);
        Assert.Equal("0 */5 * * * ?", cron.CronExpressionString);
    }

    [Fact]
    public async Task RecurringTransferScheduler_Stop_Shuts_Down()
    {
        var schedulerMock = SetupScheduler(out _, out _);
        using var provider = BuildProvider(schedulerMock.Object);
        var logger = new Mock<ILogger<RecurringTransferScheduler>>();
        var svc = new RecurringTransferScheduler(provider, logger.Object);
        await svc.StartAsync(CancellationToken.None);

        await svc.StopAsync(CancellationToken.None);

        schedulerMock.Verify(s => s.Shutdown(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BudgetAlertScheduler_Starts_With_Correct_Cron()
    {
        var schedulerMock = SetupScheduler(out var job, out var trigger);
        using var provider = BuildProvider(schedulerMock.Object);
        var logger = new Mock<ILogger<BudgetAlertScheduler>>();
        var svc = new BudgetAlertScheduler(provider, logger.Object);

        await svc.StartAsync(CancellationToken.None);

        Assert.NotNull(job);
        Assert.Equal("BudgetAlertJob", job!.Key.Name);
        Assert.Equal("Goals", job!.Key.Group);
        Assert.NotNull(trigger);
        Assert.Equal("BudgetAlertTrigger", trigger!.Key.Name);
        Assert.Equal("Goals", trigger!.Key.Group);
        var cron = Assert.IsAssignableFrom<ICronTrigger>(trigger);
        Assert.Equal("0 0 * * * ?", cron.CronExpressionString);
    }

    [Fact]
    public async Task BudgetAlertScheduler_Stop_Shuts_Down()
    {
        var schedulerMock = SetupScheduler(out _, out _);
        using var provider = BuildProvider(schedulerMock.Object);
        var logger = new Mock<ILogger<BudgetAlertScheduler>>();
        var svc = new BudgetAlertScheduler(provider, logger.Object);
        await svc.StartAsync(CancellationToken.None);

        await svc.StopAsync(CancellationToken.None);

        schedulerMock.Verify(s => s.Shutdown(It.IsAny<CancellationToken>()), Times.Once);
    }
}