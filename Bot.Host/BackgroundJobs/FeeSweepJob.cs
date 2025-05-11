using Bot.Core.Services;
using Quartz;

namespace Bot.Host.BackgroundJobs;

[DisallowConcurrentExecution]
public class FeeSweepJob(
    IBillingService billingService,
    ILogger<FeeSweepJob> logger)
    : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("💸 FeeSweepJob started at {Time}", context.FireTimeUtc);

        try
        {
            await billingService.SweepFeesAsync();
            logger.LogInformation("✅ FeeSweepJob completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ FeeSweepJob failed.");
        }
    }
}