using Quartz;
using Quartz.Spi;

namespace Bot.Host.BackgroundJobs;

public class BillPayScheduler(
    IServiceProvider sp,
    ILogger<BillPayScheduler> logger)
    : IHostedService
{
    private IScheduler _scheduler = null!;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = sp.GetRequiredService<ISchedulerFactory>();
        _scheduler = await factory.GetScheduler(cancellationToken);
        _scheduler.JobFactory = sp.GetRequiredService<IJobFactory>();

        var job = JobBuilder.Create<BillPayJob>()
            .WithIdentity("BillPayJob", "Billing")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("BillPayTrigger", "Billing")
            .WithCronSchedule("0 */10 * * * ?") // every 10 minutes
            .Build();

        await _scheduler.ScheduleJob(job, trigger, cancellationToken);
        await _scheduler.Start(cancellationToken);

        logger.LogInformation("✅ BillPayJob scheduled (every 10 minutes)");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(cancellationToken);
            logger.LogInformation("⏹️ BillPayScheduler stopped");
        }
    }
}