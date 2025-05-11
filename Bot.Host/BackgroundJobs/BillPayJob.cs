// Bot.BillPayWorker/Services/BillPayJob.cs

using Bot.Core.Services;
using Quartz;

// IBillPayService

namespace Bot.Host.BackgroundJobs;

[DisallowConcurrentExecution]
public class BillPayJob(
    IBillPayService billPayService,
    ILogger<BillPayJob> logger)
    : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("📡 BillPayJob started at {Time}", context.FireTimeUtc);

        try
        {
            var dueBills = await billPayService.ProcessDueBillPaymentsAsync();
            logger.LogInformation("✅ BillPayJob completed. {Count} due bills processed.", dueBills.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ BillPayJob failed.");
        }
    }
}