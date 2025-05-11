using Quartz;
using Quartz.Spi;

namespace Bot.Host.BackgroundJobs;

public class SingletonJobFactory(IServiceProvider provider) : IJobFactory
{
    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        return (IJob)provider.GetRequiredService(bundle.JobDetail.JobType);
    }

    public void ReturnJob(IJob job)
    {
    } // No cleanup needed
}