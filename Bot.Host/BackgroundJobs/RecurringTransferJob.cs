// Bot.RecurringWorker/Services/RecurringTransferJob.cs

using Bot.Core.Services;
using Quartz;

namespace Bot.Host.BackgroundJobs;

[DisallowConcurrentExecution]
public class RecurringTransferJob(
    IRecurringTransferService recurringService,
    ILogger<RecurringTransferJob> logger)
    : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("RecurringTransferJob started at {Time}", context.FireTimeUtc);

        // Process all transfers that are due now
        await recurringService.ProcessDueTransfersAsync();

        logger.LogInformation("RecurringTransferJob completed at {Time}", DateTime.UtcNow);
    }
}