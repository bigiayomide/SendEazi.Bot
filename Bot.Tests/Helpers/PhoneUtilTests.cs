using Bot.Core.StateMachine.Helpers;
using FluentAssertions;

namespace Bot.Tests.Helpers;

public class PhoneUtilTests
{
    [Fact]
    public void Normalize_Should_Handle_Plus234_Prefix()
    {
        var result = PhoneUtil.Normalize("+2348012345678");
        result.Should().Be("+2348012345678");
    }

    [Fact]
    public void Normalize_Should_Add_Plus_For_234_Prefix()
    {
        var result = PhoneUtil.Normalize("2348012345678");
        result.Should().Be("+2348012345678");
    }

    [Fact]
    public void Normalize_Should_Convert_Local_Number()
    {
        var result = PhoneUtil.Normalize("0801 234-5678");
        result.Should().Be("+2348012345678");
    }

    [Fact]
    public void Normalize_Should_Return_Raw_For_Unknown_Format()
    {
        var result = PhoneUtil.Normalize("123456789");
        result.Should().Be("123456789");
    }
}