// Bot.WebApi/Endpoints/HealthEndpoint.cs

using FastEndpoints;

namespace Bot.Host.Endpoints;

public class HealthResponse
{
    public string Status { get; set; } = "Healthy";
}

/// <summary>
///     Simple liveness/readiness probe.
/// </summary>
public class HealthEndpoint : EndpointWithoutRequest<HealthResponse>
{
    public override void Configure()
    {
        Verbs(Http.GET);
        Routes("/health", "/healthz");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendOkAsync(new HealthResponse { Status = "Healthy" }, ct);
    }
}