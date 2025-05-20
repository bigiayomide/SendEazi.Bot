using Polly;
using Polly.Timeout;

namespace Bot.Core.StateMachine.Helpers;

public static class PollyPolicies
{
    public static readonly IAsyncPolicy<HttpResponseMessage> TransientHttp =
        Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(resp => !resp.IsSuccessStatusCode)
            .WaitAndRetryAsync(3,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    public static readonly IAsyncPolicy TimeoutWithToken =
        Policy.TimeoutAsync(TimeSpan.FromSeconds(10), TimeoutStrategy.Optimistic);
}