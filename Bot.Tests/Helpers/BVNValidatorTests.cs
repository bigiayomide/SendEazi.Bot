using Bot.Core.StateMachine.Helpers;
using FluentAssertions;

namespace Bot.Tests.Helpers;

public class BVNValidatorTests
{
    [Theory]
    [InlineData("12345678901", true)]
    [InlineData("1234567890", false)]
    [InlineData("1234567890a", false)]
    public void IsValid_Should_Validate_Bvn_Format(string input, bool expected)
    {
        BvnValidator.IsValid(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("12345678901", true)]
    [InlineData("1234567890", false)]
    [InlineData("1234567890a", false)]
    public void IsNinValid_Should_Validate_Nin_Format(string input, bool expected)
    {
        BvnValidator.IsNinValid(input).Should().Be(expected);
    }
}