// Bot.BillingWorker/Jobs/FeeSweepJob.cs

using Bot.Core.Services;
using Quartz;

// IBillingService

namespace Bot.Host.BackgroundJobs;

[DisallowConcurrentExecution]
public class FeeSweepJob : IJob
{
    private readonly IBillingService _billingService;
    private readonly ILogger<FeeSweepJob> _logger;

    public FeeSweepJob(
        IBillingService billingService,
        ILogger<FeeSweepJob> logger)
    {
        _billingService = billingService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("FeeSweepJob executing at {Time}", context.FireTimeUtc);
        await _billingService.SweepFeesAsync();
        _logger.LogInformation("FeeSweepJob completed");
    }
}