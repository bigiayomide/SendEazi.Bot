// Bot.BillPayWorker/Services/BillPayJob.cs

using Bot.Core.Services;
using Quartz;

// IBillPayService

namespace Bot.Host.BackgroundJobs;

[DisallowConcurrentExecution]
public class BillPayJob : IJob
{
    private readonly IBillPayService _billPayService;
    private readonly ILogger<BillPayJob> _logger;

    public BillPayJob(
        IBillPayService billPayService,
        ILogger<BillPayJob> logger)
    {
        _billPayService = billPayService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("BillPayJob started at {Time}", context.FireTimeUtc);

        // Send reminders or execute any bill payments due
        await _billPayService.ProcessDueBillPaymentsAsync();

        _logger.LogInformation("BillPayJob completed");
    }
}