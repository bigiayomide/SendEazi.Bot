using Bot.Host.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Quartz;
using Quartz.Spi;

namespace Bot.Tests.BackgroundJobs;

public class SingletonJobFactoryTests
{
    [Fact]
    public void NewJob_Returns_Registered_Service_And_ReturnJob_Does_Nothing()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<FakeJob>();
        using var provider = services.BuildServiceProvider();
        var factory = new SingletonJobFactory(provider);
        var jobDetail = JobBuilder.Create<FakeJob>().Build();
        var trigger = (IOperableTrigger)TriggerBuilder.Create().Build();
        var bundle = new TriggerFiredBundle(jobDetail, trigger, null, false, DateTimeOffset.MinValue,
            DateTimeOffset.MinValue, DateTimeOffset.MinValue, DateTimeOffset.MinValue);

        // Act
        var job = factory.NewJob(bundle, new Mock<IScheduler>().Object);

        // Assert
        Assert.IsType<FakeJob>(job);
        var ex = Record.Exception(() => factory.ReturnJob(job));
        Assert.Null(ex);
    }

    private class FakeJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            return Task.CompletedTask;
        }
    }
}