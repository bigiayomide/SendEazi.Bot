// File: Bot.Host.BackgroundJobs/BudgetAlertScheduler.cs

using Quartz;
using Quartz.Spi;

namespace Bot.Host.BackgroundJobs;

public class BudgetAlertScheduler(
    IServiceProvider sp,
    ILogger<BudgetAlertScheduler> logger)
    : IHostedService
{
    private IScheduler _scheduler = null!;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = sp.GetRequiredService<ISchedulerFactory>();
        _scheduler = await factory.GetScheduler(cancellationToken);
        _scheduler.JobFactory = sp.GetRequiredService<IJobFactory>();

        var job = JobBuilder.Create<BudgetAlertJob>()
            .WithIdentity("BudgetAlertJob", "Goals")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("BudgetAlertTrigger", "Goals")
            .WithCronSchedule("0 0 * * * ?") // every hour
            .Build();

        await _scheduler.ScheduleJob(job, trigger, cancellationToken);
        await _scheduler.Start(cancellationToken);

        logger.LogInformation("📊 BudgetAlertJob scheduled (every hour)");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(cancellationToken);
            logger.LogInformation("⏹️ BudgetAlertScheduler stopped");
        }
    }
}