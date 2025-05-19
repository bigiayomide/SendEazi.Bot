using System.Net;
using Bot.Core.Services;
using FluentAssertions;
using Moq;
using Twilio.Clients;
using Twilio.Http;
using Xunit;

namespace Bot.Tests.Services;

public class TwilioMessageSenderTests
{
    [Fact]
    public async Task SendAsync_Should_Use_RestClient_And_Return_Sid()
    {
        // Arrange
        var captured = default(Request);
        var sid = "SM123";
        var client = new Mock<ITwilioRestClient>();
        client.Setup(c => c.RequestAsync(It.IsAny<Request>()))
            .Callback<Request>(r => captured = r)
            .ReturnsAsync(new Response(HttpStatusCode.Created, $"{{\"sid\":\"{sid}\"}}"));

        var sender = new TwilioMessageSender(client.Object);

        // Act
        var result = await sender.SendAsync("+1111", "+2222", "hello");

        // Assert
        result.Should().Be(sid);
        captured.Should().NotBeNull();
        captured.PostParams["From"].Should().Be("+1111");
        captured.PostParams["To"].Should().Be("+2222");
        captured.PostParams["Body"].Should().Be("hello");
    }

    [Fact]
    public async Task SendAsync_Should_Propagate_Exception()
    {
        var client = new Mock<ITwilioRestClient>();
        client.Setup(c => c.RequestAsync(It.IsAny<Request>()))
            .ThrowsAsync(new InvalidOperationException("fail"));

        var sender = new TwilioMessageSender(client.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync("+1", "+2", "msg"));
    }
}
