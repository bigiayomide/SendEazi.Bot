using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bot.Core.Services;
using Bot.Tests.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bot.Tests.Services;

public class WhatsAppServiceTests
{
    private static (WhatsAppService svc, MockHttpMessageHandler handler) CreateService(
        Func<HttpRequestMessage, HttpResponseMessage> handlerFunc, TimeSpan? ttl = null)
    {
        var handler = new MockHttpMessageHandler(handlerFunc);
        var client = new HttpClient(handler);
        var opts = Options.Create(new WhatsAppOptions
        {
            BaseUrl = "https://wa.test",
            PhoneNumberId = "123",
            AccessToken = "token",
            EphemeralTtl = ttl ?? TimeSpan.Zero
        });
        var logger = new LoggerFactory().CreateLogger<WhatsAppService>();
        var svc = new WhatsAppService(client, opts, logger);
        return (svc, handler);
    }

    [Fact]
    public async Task SendTextMessageAsync_Posts_Correct_Payload()
    {
        HttpRequestMessage? req = null;
        var (svc, _) = CreateService(r =>
        {
            req = r;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { messages = new[] { new { id = "msg1" } } })
            };
        });

        var id = await svc.SendTextMessageAsync("234", "hello");

        id.Should().Be("msg1");
        req.Should().NotBeNull();
        req!.Method.Should().Be(HttpMethod.Post);
        req.RequestUri!.PathAndQuery.Should().Be("/123/messages");
        var json = JsonDocument.Parse(await req.Content!.ReadAsStringAsync()).RootElement;
        json.GetProperty("type").GetString().Should().Be("text");
        json.GetProperty("to").GetString().Should().Be("234");
        json.GetProperty("text").GetProperty("body").GetString().Should().Be("hello");
    }

    [Fact]
    public async Task SendMediaAsync_Posts_Correct_Payload()
    {
        HttpRequestMessage? req = null;
        var (svc, _) = CreateService(r =>
        {
            req = r;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { messages = new[] { new { id = "msg2" } } })
            };
        });

        var id = await svc.SendMediaAsync("111", "http://img", "cap");

        id.Should().Be("msg2");
        req.Should().NotBeNull();
        req!.RequestUri!.PathAndQuery.Should().Be("/123/messages");
        var json = JsonDocument.Parse(await req.Content!.ReadAsStringAsync()).RootElement;
        json.GetProperty("type").GetString().Should().Be("image");
        var img = json.GetProperty("image");
        img.GetProperty("link").GetString().Should().Be("http://img");
        img.GetProperty("caption").GetString().Should().Be("cap");
    }

    [Fact]
    public async Task SendQuickReplyAsync_Posts_Correct_Payload()
    {
        HttpRequestMessage? req = null;
        var (svc, _) = CreateService(r =>
        {
            req = r;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { messages = new[] { new { id = "msg3" } } })
            };
        });

        var id = await svc.SendQuickReplyAsync("234", "hdr", "body", new[] { "A", "B" });

        id.Should().Be("msg3");
        req.Should().NotBeNull();
        var json = JsonDocument.Parse(await req!.Content!.ReadAsStringAsync()).RootElement;
        json.GetProperty("type").GetString().Should().Be("interactive");
        var buttons = json.GetProperty("interactive").GetProperty("action").GetProperty("buttons");
        buttons.GetArrayLength().Should().Be(2);
        buttons[0].GetProperty("reply").GetProperty("id").GetString().Should().Be("btn_1");
        buttons[0].GetProperty("reply").GetProperty("title").GetString().Should().Be("A");
    }

    [Fact]
    public async Task SendTemplateAsync_Posts_Correct_Payload()
    {
        HttpRequestMessage? req = null;
        var (svc, _) = CreateService(r =>
        {
            req = r;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { messages = new[] { new { id = "msg4" } } })
            };
        });

        var id = await svc.SendTemplateAsync("345", new { name = "temp" });

        id.Should().Be("msg4");
        req.Should().NotBeNull();
        var json = JsonDocument.Parse(await req!.Content!.ReadAsStringAsync()).RootElement;
        json.GetProperty("type").GetString().Should().Be("template");
        json.GetProperty("template").GetProperty("name").GetString().Should().Be("temp");
    }

    [Fact]
    public async Task SendVoiceAsync_Uploads_Media_And_Posts_Message()
    {
        HttpRequestMessage? first = null;
        HttpRequestMessage? second = null;
        var count = 0;
        var (svc, _) = CreateService(r =>
        {
            count++;
            if (count == 1)
            {
                first = r;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { id = "med" })
                };
            }

            second = r;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { messages = new[] { new { id = "msg5" } } })
            };
        });

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var id = await svc.SendVoiceAsync("900", stream);

        id.Should().Be("msg5");
        first.Should().NotBeNull();
        first!.RequestUri!.PathAndQuery.Should().Be("/123/media");
        second.Should().NotBeNull();
        second!.RequestUri!.PathAndQuery.Should().Be("/123/messages");
        var json = JsonDocument.Parse(await second.Content!.ReadAsStringAsync()).RootElement;
        json.GetProperty("type").GetString().Should().Be("audio");
        json.GetProperty("audio").GetProperty("id").GetString().Should().Be("med");
    }

    [Fact]
    public async Task EphemeralDeletion_Triggers_DeleteMessage()
    {
        HttpRequestMessage? deleteReq = null;
        var count = 0;
        var (svc, _) = CreateService(r =>
        {
            count++;
            if (count == 1)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { messages = new[] { new { id = "msgX" } } })
                };

            deleteReq = r;
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, TimeSpan.FromMilliseconds(20));

        await svc.SendTextMessageAsync("555", "bye");
        await Task.Delay(50);

        deleteReq.Should().NotBeNull();
        deleteReq!.Method.Should().Be(HttpMethod.Delete);
        deleteReq.RequestUri!.PathAndQuery.Should().Be("/123/messages/msgX");
    }
}