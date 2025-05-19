using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Bot.Host.Endpoints;
using Bot.Shared.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bot.Tests.Endpoints;

public class OnePipeWebhookEventConsumerTests
{
    private static string CreateSignature(string secret, string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        return Convert.ToHexString(hash).ToLower();
    }

    [Fact]
    public async Task Valid_Request_Should_Publish_Event()
    {
        var secret = "test-secret";
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["OnePipe:WebhookSecret"] = secret
            })
            .Build();

        var bus = new Mock<MassTransit.IPublishEndpoint>();
        var logger = Mock.Of<ILogger<OnePipeWebhookEventConsumer>>();
        var endpoint = new OnePipeWebhookEventConsumer(bus.Object, cfg, logger);
        var userId = Guid.NewGuid();
        var payload = JsonSerializer.Serialize(new
        {
            event_type = "mandate.approved",
            data = new
            {
                mandate_id = "mandate",
                transaction_ref = $"txn:{userId}"
            }
        });
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.Headers["Signature"] = CreateSignature(secret, payload);
        endpoint.HttpContext = context;

        await endpoint.HandleAsync(default);

        bus.Verify(b => b.Publish(It.Is<MandateReadyToDebit>(m => m.CorrelationId == userId), default), Times.Once);
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Invalid_Signature_Should_Return_401()
    {
        var secret = "test-secret";
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["OnePipe:WebhookSecret"] = secret
            })
            .Build();

        var bus = new Mock<MassTransit.IPublishEndpoint>();
        var logger = Mock.Of<ILogger<OnePipeWebhookEventConsumer>>();
        var endpoint = new OnePipeWebhookEventConsumer(bus.Object, cfg, logger);
        var payload = "{}";
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.Headers["Signature"] = "bad";
        endpoint.HttpContext = context;

        await endpoint.HandleAsync(default);

        context.Response.StatusCode.Should().Be(401);
        bus.Verify(b => b.Publish(It.IsAny<object>(), default), Times.Never);
    }
}
