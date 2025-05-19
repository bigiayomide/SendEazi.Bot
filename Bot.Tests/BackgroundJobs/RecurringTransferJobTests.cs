using Bot.Host.BackgroundJobs;
using Bot.Core.Services;
using Moq;
using Quartz;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bot.Tests.BackgroundJobs;

public class RecurringTransferJobTests
{
    [Fact]
    public async Task Execute_Calls_ProcessDueTransfersAsync()
    {
        var service = new Mock<IRecurringTransferService>();
        var logger = new Mock<ILogger<RecurringTransferJob>>();
        var job = new RecurringTransferJob(service.Object, logger.Object);
        var context = new Mock<IJobExecutionContext>();
        context.SetupGet(c => c.FireTimeUtc).Returns(DateTimeOffset.UtcNow);

        await job.Execute(context.Object);

        service.Verify(s => s.ProcessDueTransfersAsync(), Times.Once);
    }

    [Fact]
    public async Task Execute_Logs_Error_On_Exception()
    {
        var service = new Mock<IRecurringTransferService>();
        service.Setup(s => s.ProcessDueTransfersAsync()).ThrowsAsync(new Exception("fail"));
        var logger = new Mock<ILogger<RecurringTransferJob>>();
        var job = new RecurringTransferJob(service.Object, logger.Object);
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
