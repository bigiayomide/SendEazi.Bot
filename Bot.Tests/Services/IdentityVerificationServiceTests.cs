using Bot.Core.Services;
using FluentAssertions;

namespace Bot.Tests.Services;

public class IdentityVerificationServiceTests
{
    private readonly IdentityVerificationService _service = new();

    [Theory]
    [InlineData("12345678901")]
    [InlineData("00000000000")]
    public async Task VerifyNinAsync_Should_ReturnTrue_For_11_Digits(string nin)
    {
        var result = await _service.VerifyNinAsync(nin);
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("123456789012")]
    [InlineData("")]
    public async Task VerifyNinAsync_Should_ReturnFalse_For_Invalid_Length(string nin)
    {
        var result = await _service.VerifyNinAsync(nin);
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("12345678901")]
    [InlineData("11111111111")]
    public async Task VerifyBvnAsync_Should_ReturnTrue_For_11_Digits(string bvn)
    {
        var result = await _service.VerifyBvnAsync(bvn);
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("123456789012")]
    [InlineData("abc")]
    public async Task VerifyBvnAsync_Should_ReturnFalse_For_Invalid_Length(string bvn)
    {
        var result = await _service.VerifyBvnAsync(bvn);
        result.Should().BeFalse();
    }
}
