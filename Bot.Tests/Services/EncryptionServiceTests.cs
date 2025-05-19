using Bot.Core.Services;
using Bot.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Bot.Tests.Services;

public class EncryptionServiceTests
{
    private static EncryptionService CreateService()
    {
        const string key = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="; // 32 zero bytes
        var opts = Options.Create(new AppSettings { EncryptionBase64Key = key });
        return new EncryptionService(opts);
    }

    [Fact]
    public void EncryptDecrypt_Should_RestoreOriginalValue()
    {
        var service = CreateService();
        const string plaintext = "hello world";

        var cipher = service.Encrypt(plaintext);
        var result = service.Decrypt(cipher);

        result.Should().Be(plaintext);
    }

    [Fact]
    public void Decrypt_Should_Throw_When_Input_IsInvalid()
    {
        var service = CreateService();

        var act = () => service.Decrypt("invalid");

        act.Should().Throw<FormatException>();
    }
}