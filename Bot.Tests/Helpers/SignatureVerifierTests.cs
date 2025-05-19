using Bot.Core.Helpers;
using FluentAssertions;

namespace Bot.Tests.Helpers;

public class SignatureVerifierTests
{
    [Fact]
    public void HmacIsValid_Should_Return_True_For_Known_Payload_And_Secret()
    {
        const string secret = "secret123";
        const string payload = "hello world";
        const string expectedSignature = "57938295649097379cddb382dd6c82d5e0460645a8fd01674a48a76de6142646";

        var result = SignatureVerifier.HmacIsValid(payload, secret, expectedSignature);

        result.Should().BeTrue();
    }

    [Fact]
    public void HmacIsValid_Should_Return_False_For_Incorrect_Signature()
    {
        const string secret = "secret123";
        const string payload = "hello world";

        var result = SignatureVerifier.HmacIsValid(payload, secret, "wrong");

        result.Should().BeFalse();
    }

    [Fact]
    public void MetaSecretVerifier_Should_Return_True_For_Valid_Header()
    {
        const string secret = "mysecret";
        const string payload = "payload";
        const string signature = "ef5ac2c4e7b82aa1a2f4fad2152102830f55af3ad4ebc2652411874f0283130a";
        var headerValue = $"sha256={signature}";

        var result = SignatureVerifier.MetaSecretVerifier(headerValue, payload, secret);

        result.Should().BeTrue();
    }

    [Fact]
    public void MetaSecretVerifier_Should_Return_False_For_Wrong_Signature()
    {
        const string secret = "mysecret";
        const string payload = "payload";
        var headerValue = "sha256=wrong";

        var result = SignatureVerifier.MetaSecretVerifier(headerValue, payload, secret);

        result.Should().BeFalse();
    }

    [Fact]
    public void MetaSecretVerifier_Should_Return_False_For_Missing_Prefix()
    {
        const string secret = "mysecret";
        const string payload = "payload";
        var headerValue = "ef5ac2c4e7b82aa1a2f4fad2152102830f55af3ad4ebc2652411874f0283130a";

        var result = SignatureVerifier.MetaSecretVerifier(headerValue, payload, secret);

        result.Should().BeFalse();
    }
}