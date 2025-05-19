using Bot.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Bot.Tests.Services;

public class SmsBackupServiceTests
{
    [Fact]
    public async Task SendSmsAsync_Should_Log_Sid_On_Success()
    {
        var opts = Options.Create(new SmsOptions
        {
            AccountSid = "sid",
            AuthToken = "token",
            FromNumber = "+1000"
        });

        var logger = new Mock<ILogger<SmsBackupService>>();
        var twilio = new Mock<ITwilioMessageSender>();
        twilio.Setup(t => t.SendAsync("+1000", "+2000", "hello"))
            .ReturnsAsync("SM123");

        var svc = new SmsBackupService(opts, logger.Object, twilio.Object);
        await svc.SendSmsAsync("+2000", "hello");

        logger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SMS sent")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task SendSmsAsync_Should_Log_And_Rethrow_On_Error()
    {
        var opts = Options.Create(new SmsOptions { AccountSid = "sid", AuthToken = "token", FromNumber = "+1000" });
        var logger = new Mock<ILogger<SmsBackupService>>();
        var twilio = new Mock<ITwilioMessageSender>();
        twilio.Setup(t => t.SendAsync("+1000", "+2000", "hello"))
            .ThrowsAsync(new Exception("fail"));

        var svc = new SmsBackupService(opts, logger.Object, twilio.Object);

        await Assert.ThrowsAsync<Exception>(() => svc.SendSmsAsync("+2000", "hello"));

        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Failed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}

