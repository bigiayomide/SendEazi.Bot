using Bot.Core.Services;
using Bot.Host.BackgroundJobs;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;

namespace Bot.Tests.BackgroundJobs;

public class FeeSweepJobTests
{
    [Fact]
    public async Task Execute_Calls_SweepFeesAsync()
    {
        var service = new Mock<IBillingService>();
        var logger = new Mock<ILogger<FeeSweepJob>>();
        var job = new FeeSweepJob(service.Object, logger.Object);
        var context = new Mock<IJobExecutionContext>();
        context.SetupGet(c => c.FireTimeUtc).Returns(DateTimeOffset.UtcNow);

        await job.Execute(context.Object);

        service.Verify(s => s.SweepFeesAsync(), Times.Once);
    }

    [Fact]
    public async Task Execute_Logs_Error_On_Exception()
    {
        var service = new Mock<IBillingService>();
        service.Setup(s => s.SweepFeesAsync()).ThrowsAsync(new Exception("fail"));
        var logger = new Mock<ILogger<FeeSweepJob>>();
        var job = new FeeSweepJob(service.Object, logger.Object);
        var context = new Mock<IJobExecutionContext>();
        context.SetupGet(c => c.FireTimeUtc).Returns(DateTimeOffset.UtcNow);

        await job.Execute(context.Object);

        logger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}