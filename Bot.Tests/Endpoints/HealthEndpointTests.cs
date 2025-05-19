using System.Net;
using System.Net.Http.Json;
using Bot.Host.Endpoints;
using FastEndpoints.Testing;
using FluentAssertions;
using Xunit;

namespace Bot.Tests.Endpoints;

public class HealthEndpointTests : IClassFixture<TestApp<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(TestApp<Program> app)
    {
        _client = app.Client;
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/healthz")]
    public async Task Health_Endpoints_Return_Healthy(string url)
    {
        var response = await _client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Healthy");
    }
}
