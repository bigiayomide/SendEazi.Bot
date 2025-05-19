using System.Net;
using System.Net.Http;
using Bot.Core.StateMachine.Helpers;
using Bot.Tests.TestUtilities;
using FluentAssertions;

namespace Bot.Tests.Helpers;

public class HttpJsonLoggerTests
{
    [Fact]
    public async Task LogRequest_Should_Log_Method_Uri_And_Body()
    {
        var logger = new ListLogger();
        var req = new HttpRequestMessage(HttpMethod.Post, "https://example.com/api")
        {
            Content = new StringContent("{\"x\":1}")
        };

        await HttpJsonLogger.LogRequest(req, logger);

        logger.Entries.Should().HaveCount(1);
        logger.Entries[0].Message.Should().Contain("POST").And.Contain("https://example.com/api").And.Contain("{\"x\":1}");
    }

    [Fact]
    public async Task LogResponse_Should_Log_Status_And_Body()
    {
        var logger = new ListLogger();
        var res = new HttpResponseMessage(HttpStatusCode.OK)
        {
            ReasonPhrase = "OK",
            Content = new StringContent("pong")
        };

        await HttpJsonLogger.LogResponse(res, logger);

        logger.Entries.Should().HaveCount(1);
        logger.Entries[0].Message.Should().Contain("200").And.Contain("OK").And.Contain("pong");
    }
}
