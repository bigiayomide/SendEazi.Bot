using Quartz;
using Quartz.Spi;

namespace Bot.Host.BackgroundJobs;

public class RecurringTransferScheduler(
    IServiceProvider sp,
    ILogger<RecurringTransferScheduler> logger)
    : IHostedService
{
    private IScheduler _scheduler = null!;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = sp.GetRequiredService<ISchedulerFactory>();
        _scheduler = await factory.GetScheduler(cancellationToken);
        _scheduler.JobFactory = sp.GetRequiredService<IJobFactory>();

        var job = JobBuilder.Create<RecurringTransferJob>()
            .WithIdentity("RecurringTransferJob", "Transfers")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("RecurringTrigger", "Transfers")
            .WithCronSchedule("0 */5 * * * ?") // Every 5 minutes
            .Build();

        await _scheduler.ScheduleJob(job, trigger, cancellationToken);
        await _scheduler.Start(cancellationToken);

        logger.LogInformation("📆 RecurringTransferJob scheduled every 5 minutes");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(cancellationToken);
            logger.LogInformation("⏹️ RecurringTransferScheduler stopped");
        }
    }
}