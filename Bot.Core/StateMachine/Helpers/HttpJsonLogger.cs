using Microsoft.Extensions.Logging;

namespace Bot.Core.StateMachine.Helpers;

public static class HttpJsonLogger
{
    public static async Task LogRequest(HttpRequestMessage req, ILogger log)
    {
        var json = req.Content != null
            ? await req.Content.ReadAsStringAsync()
            : "<empty>";
        log.LogDebug("➡️ {Method} {Uri} \n{Json}", req.Method, req.RequestUri, json);
    }

    public static async Task LogResponse(HttpResponseMessage res, ILogger log)
    {
        var body = await res.Content.ReadAsStringAsync();
        log.LogDebug("⬅️ {Status} {Reason}\n{Body}", (int)res.StatusCode, res.ReasonPhrase, body);
    }
}