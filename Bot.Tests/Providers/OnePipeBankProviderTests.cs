using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bot.Core.Providers;
using Bot.Core.Services;
using Bot.Infrastructure.Configuration;
using Bot.Shared.Models;
using Bot.Tests.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Bot.Tests.Providers;

public class OnePipeBankProviderTests
{
    [Fact]
    public async Task CreateMandateAsync_Should_EncryptBvn_And_ReturnRef()
    {
        var user = new User
        {
            FullName = "Jane Doe",
            PhoneNumber = "+2347012345678",
            BVNEnc = "enc-bvn"
        };

        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Decrypt("enc-bvn")).Returns("22222222222");

        var secret = "0123456789abcdef01234567"; // 24 chars
        var opts = Options.Create(new OnePipeOptions
        {
            BaseUrl = "https://pipe.test",
            ApiKey = "api-key",
            MerchantId = "m",
            SecretKey = secret
        });

        string Encrypt(string val)
        {
            var key = Encoding.UTF8.GetBytes(secret[..24]);
            using var tdes = TripleDES.Create();
            tdes.Key = key;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;
            var enc = tdes.CreateEncryptor();
            var input = Encoding.UTF8.GetBytes(val);
            var output = enc.TransformFinalBlock(input, 0, input.Length);
            return Convert.ToBase64String(output);
        }

        var expectedEnc = Encrypt("22222222222");

        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":{\"transactionRef\":\"ok\"}}")
        });
        var client = new HttpClient(handler);

        var provider = new OnePipeBankProvider(client, opts, encryption.Object);

        var result = await provider.CreateMandateAsync(user, "cust", 100m, "ref-1");

        result.Should().Be("ok");
        encryption.Verify(e => e.Decrypt("enc-bvn"), Times.Exactly(2));

        handler.LastRequest.Should().NotBeNull();
        var req = handler.LastRequest!;
        req.Method.Should().Be(HttpMethod.Post);
        req.RequestUri!.PathAndQuery.Should().Be("/transact");
        req.Headers.GetValues("Authorization").Single().Should().Be("Bearer api-key");

        var body = JsonDocument.Parse(await req.Content!.ReadAsStringAsync()).RootElement;
        body.GetProperty("request_ref").GetString().Should().Be("ref-1");
        body.GetProperty("auth").GetProperty("secure").GetString().Should().Be(expectedEnc);
        var trans = body.GetProperty("transaction");
        trans.GetProperty("amount").GetInt32().Should().Be(10000);
        trans.GetProperty("customer").GetProperty("bvn").GetString().Should().Be(expectedEnc);
    }

    [Fact]
    public async Task InitiateDebitAsync_Should_HitEndpoint_And_ReturnRef()
    {
        var secret = "0123456789abcdef01234567"; // 24 chars
        var opts = Options.Create(new OnePipeOptions
        {
            BaseUrl = "https://pipe.test",
            ApiKey = "api-key",
            MerchantId = "m",
            SecretKey = secret
        });

        string Encrypt(string val)
        {
            var key = Encoding.UTF8.GetBytes(secret[..24]);
            using var tdes = TripleDES.Create();
            tdes.Key = key;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;
            var enc = tdes.CreateEncryptor();
            var input = Encoding.UTF8.GetBytes(val);
            var output = enc.TransformFinalBlock(input, 0, input.Length);
            return Convert.ToBase64String(output);
        }

        var expectedEnc = Encrypt("mand1");

        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":{\"transactionRef\":\"tx123\"}}")
        });
        var client = new HttpClient(handler);

        var provider = new OnePipeBankProvider(client, opts, Mock.Of<IEncryptionService>());

        var result = await provider.InitiateDebitAsync("mand1", 75m, "ref-1", "pay");

        result.Should().Be("tx123");

        handler.LastRequest.Should().NotBeNull();
        var req = handler.LastRequest!;
        req.Method.Should().Be(HttpMethod.Post);
        req.RequestUri!.PathAndQuery.Should().Be("/transact");

        var body = JsonDocument.Parse(await req.Content!.ReadAsStringAsync()).RootElement;
        body.GetProperty("request_ref").GetString().Should().Be("ref-1");
        body.GetProperty("auth").GetProperty("secure").GetString().Should().Be(expectedEnc);
        body.GetProperty("transaction").GetProperty("amount").GetInt32().Should().Be(7500);
    }

    [Fact]
    public async Task GetBalanceAsync_Should_HitEndpoint_And_ReturnBalance()
    {
        var opts = Options.Create(new OnePipeOptions
        {
            BaseUrl = "https://pipe.test",
            ApiKey = "api-key",
            MerchantId = "m",
            SecretKey = "0123456789abcdef01234567"
        });

        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":{\"available_balance\":250}}")
        });
        var client = new HttpClient(handler);

        var provider = new OnePipeBankProvider(client, opts, Mock.Of<IEncryptionService>());

        var result = await provider.GetBalanceAsync("acc123");

        result.Should().Be(250m);

        handler.LastRequest.Should().NotBeNull();
        var req = handler.LastRequest!;
        req.Method.Should().Be(HttpMethod.Post);
        req.RequestUri!.PathAndQuery.Should().Be("/v1/balance");

        var body = JsonDocument.Parse(await req.Content!.ReadAsStringAsync()).RootElement;
        body.GetProperty("account_ref").GetString().Should().Be("acc123");
    }
}