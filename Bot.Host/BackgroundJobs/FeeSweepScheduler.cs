// Bot.BillingWorker/Services/FeeSweepScheduler.cs

using Quartz;
using Quartz.Spi;

namespace Bot.Host.BackgroundJobs;

/// <summary>
///     Spins up Quartz and schedules the FeeSweepJob to run daily at midnight UTC.
/// </summary>
public class FeeSweepScheduler(
    IServiceProvider serviceProvider,
    ILogger<FeeSweepScheduler> logger)
    : IHostedService
{
    private IScheduler _scheduler;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = serviceProvider.GetRequiredService<ISchedulerFactory>();
        _scheduler = await factory.GetScheduler(cancellationToken);
        _scheduler.JobFactory = serviceProvider.GetRequiredService<IJobFactory>();

        var jobDetail = JobBuilder.Create<FeeSweepJob>()
            .WithIdentity("FeeSweepJob", "Billing")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("FeeSweepTrigger", "Billing")
            .WithCronSchedule("0 0 0 * * ?") // every day at 00:00 UTC
            .Build();

        await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
        await _scheduler.Start(cancellationToken);

        logger.LogInformation("FeeSweepJob scheduled (daily at midnight UTC)");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(cancellationToken);
            logger.LogInformation("FeeSweepScheduler stopped");
        }
    }
}