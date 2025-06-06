using Quartz;
using Quartz.Spi;

namespace Bot.Host.BackgroundJobs;

public class FeeSweepScheduler(
    IServiceProvider sp,
    ILogger<FeeSweepScheduler> logger)
    : IHostedService
{
    private IScheduler _scheduler = null!;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = sp.GetRequiredService<ISchedulerFactory>();
        _scheduler = await factory.GetScheduler(cancellationToken);
        _scheduler.JobFactory = sp.GetRequiredService<IJobFactory>();

        var job = JobBuilder.Create<FeeSweepJob>()
            .WithIdentity("FeeSweepJob", "Billing")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("FeeSweepTrigger", "Billing")
            .WithCronSchedule("0 0 0 * * ?") // daily at midnight UTC
            .Build();

        await _scheduler.ScheduleJob(job, trigger, cancellationToken);
        await _scheduler.Start(cancellationToken);

        logger.LogInformation("💸 FeeSweepJob scheduled (daily at midnight)");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(cancellationToken);
            logger.LogInformation("⏹️ FeeSweepScheduler stopped");
        }
    }
}