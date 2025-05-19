// using System.Text;
// using System.Text.Json;
// using System.Security.Cryptography;
// using Bot.Host.Endpoints;
// using Bot.Infrastructure.Data;
// using Bot.Shared.DTOs;
// using Bot.Tests.TestUtilities;
// using FluentAssertions;
// using Microsoft.AspNetCore.Http;
// using Microsoft.EntityFrameworkCore;
// using MassTransit;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.Logging;
// using Moq;
// using Xunit;
//
// namespace Bot.Tests.Endpoints;
//
// public class WhatsAppWebhookEndpointTests
// {
//     private static string CreateSignature(string secret, string body)
//     {
//         using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
//         var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
//         return "sha256=" + Convert.ToHexString(hash).ToLower();
//     }
//
//     private static WhatsAppWebhookEndpoint CreateEndpoint(string secret, ApplicationDbContext db, Mock<MassTransit.IBusControl> bus)
//     {
//         var cfg = new ConfigurationBuilder()
//             .AddInMemoryCollection(new Dictionary<string, string> { ["WhatsApp:VerifyToken"] = secret })
//             .Build();
//         var logger = Mock.Of<ILogger<WhatsAppWebhookEndpoint>>();
//         var state = new FakeConversationStateService();
//         return new WhatsAppWebhookEndpoint(cfg, state, bus.Object, db, logger);
//     }
//
//     [Fact]
//     public async Task Valid_Request_Should_Publish_RawInboundMsgCmd()
//     {
//         var secret = "tok";
//         var options = new DbContextOptionsBuilder<ApplicationDbContext>()
//             .UseInMemoryDatabase("wa-valid")
//             .Options;
//         await using var db = new ApplicationDbContext(options);
//         var bus = new Mock<MassTransit.IBusControl>();
//         var endpoint = CreateEndpoint(secret, db, bus);
//
//         var payload = JsonSerializer.Serialize(new
//         {
//             entry = new[]
//             {
//                 new { changes = new[] { new { value = new { messages = new[] { new { from = "8000000000", id = "1", type = "text", text = new { body = "hi" } } } } } } }
//             }
//         });
//
//         var context = new DefaultHttpContext();
//         context.Request.Method = "POST";
//         context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
//         context.Request.Headers["X-Hub-Signature-256"] = CreateSignature(secret, payload);
//         endpoint.HttpContext = context;
//
//         await endpoint.HandleAsync(default);
//
//         bus.Verify(b => b.Publish(It.IsAny<RawInboundMsgCmd>(), default), Times.Once);
//         context.Response.StatusCode.Should().Be(200);
//     }
//
//     [Fact]
//     public async Task Invalid_Signature_Should_Return_401()
//     {
//         var secret = "tok";
//         var options = new DbContextOptionsBuilder<ApplicationDbContext>()
//             .UseInMemoryDatabase("wa-bad")
//             .Options;
//         await using var db = new ApplicationDbContext(options);
//         var bus = new Mock<MassTransit.IBusControl>();
//         var endpoint = CreateEndpoint(secret, db, bus);
//
//         var payload = "{}";
//         var context = new DefaultHttpContext();
//         context.Request.Method = "POST";
//         context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
//         context.Request.Headers["X-Hub-Signature-256"] = "bad";
//         endpoint.HttpContext = context;
//
//         await endpoint.HandleAsync(default);
//
//         context.Response.StatusCode.Should().Be(401);
//         bus.Verify(b => b.Publish(It.IsAny<object>(), default), Times.Never);
//     }
// }

