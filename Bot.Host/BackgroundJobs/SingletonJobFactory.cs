// Bot.Host/BackgroundJobs/SingletonJobFactory.cs

using Quartz;
using Quartz.Spi;

namespace Bot.Host.BackgroundJobs;

/// <summary>
///     Resolves every IJob once via the IServiceProvider and re-uses that
///     instance for all subsequent triggers (singleton per job type).
/// </summary>
public sealed class SingletonJobFactory(IServiceProvider provider) : IJobFactory
{
    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        return (IJob)provider.GetRequiredService(bundle.JobDetail.JobType);
    }

    public void ReturnJob(IJob job)
    {
        /* nothing to dispose – DI handles it */
    }
}