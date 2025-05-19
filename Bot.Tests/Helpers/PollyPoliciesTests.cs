using System.Net;
using Bot.Core.StateMachine.Helpers;
using Bot.Tests.TestUtilities;
using FluentAssertions;
using Polly.Timeout;

namespace Bot.Tests.Helpers;

public class PollyPoliciesTests
{
    [Fact]
    public async Task TransientHttp_Should_Invoke_Delegate_Three_Times()
    {
        var attempts = 0;
        var handler = new MockHttpMessageHandler(_ =>
        {
            attempts++;
            return attempts < 3
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : new HttpResponseMessage(HttpStatusCode.OK);
        });
        var client = new HttpClient(handler);

        var response = await PollyPolicies.TransientHttp.ExecuteAsync(() => client.GetAsync("https://example.com"));

        attempts.Should().Be(3);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ExternalTimeout_Should_Throw_When_Exceeded()
    {
        var act = () => PollyPolicies.ExternalTimeout.ExecuteAsync(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(20));
        });

        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }
}