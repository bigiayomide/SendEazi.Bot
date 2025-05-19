using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Linq;
using Bot.Core.Providers;
using Bot.Core.Services;
using Bot.Infrastructure.Configuration;
using Bot.Shared.Models;
using Bot.Tests.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Bot.Tests.Providers;

public class MonoBankProviderTests
{
    [Fact]
    public async Task CreateCustomerAsync_Should_SendCorrectRequest_And_ReturnId()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            PhoneNumber = "+2347000000000",
            NINEnc = "enc-nin"
        };

        var crypto = new Mock<IEncryptionService>();
        crypto.Setup(c => c.Decrypt("enc-nin")).Returns("12345678901");

        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":{\"id\":\"cust123\"}}")
        });
        var client = new HttpClient(handler);

        var opts = Options.Create(new MonoOptions
        {
            BaseUrl = "https://mono.test",
            SecretKey = "sec",
            BusinessSubAccountId = "sub"
        });

        var provider = new MonoBankProvider(client, opts, Mock.Of<ILogger<MonoBankProvider>>(), crypto.Object);

        var result = await provider.CreateCustomerAsync(user);

        result.Should().Be("cust123");
        crypto.Verify(c => c.Decrypt("enc-nin"), Times.Once);

        handler.LastRequest.Should().NotBeNull();
        var req = handler.LastRequest!;
        req.Method.Should().Be(HttpMethod.Post);
        req.RequestUri!.PathAndQuery.Should().Be("/v2/customers");
        req.Headers.GetValues("mono-sec-key").Single().Should().Be("sec");

        var body = JsonDocument.Parse(await req.Content!.ReadAsStringAsync()).RootElement;
        body.GetProperty("first_name").GetString().Should().Be("John");
        body.GetProperty("last_name").GetString().Should().Be("Doe");
        body.GetProperty("phone").GetString().Should().Be(user.PhoneNumber);
        body.GetProperty("bvn").GetString().Should().Be("12345678901");
    }
}
