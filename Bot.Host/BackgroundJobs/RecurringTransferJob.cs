using Bot.Core.Services;
using Quartz;

namespace Bot.Host.BackgroundJobs;

[DisallowConcurrentExecution]
public class RecurringTransferJob(
    IRecurringTransferService recurringService,
    ILogger<RecurringTransferJob> log)
    : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        log.LogInformation("RecurringTransferJob started at {Time}", context.FireTimeUtc);

        try
        {
            await recurringService.ProcessDueTransfersAsync();
            log.LogInformation("RecurringTransferJob completed at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "RecurringTransferJob failed.");
        }
    }
}