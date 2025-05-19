using Bot.Core.Services;
using FluentAssertions;

namespace Bot.Tests.Services;

public class ReferenceGeneratorTests
{
    private readonly ReferenceGenerator _generator = new();

    [Fact]
    public void GenerateTransferRef_Should_ReturnStableFormat()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string account = "0123456789";
        const string bankCode = "044";

        // Act
        var reference = _generator.GenerateTransferRef(userId, account, bankCode);

        // Assert
        reference.Should().StartWith($"txn:{userId}:");
        reference.Length.Should().Be($"txn:{userId}:".Length + 12);
        reference.Should().MatchRegex($"^txn:{userId}:[a-z0-9]{12}$");
    }

    [Fact]
    public void GenerateTransferRef_Should_HandleEmptyInputs()
    {
        var reference = _generator.GenerateTransferRef(Guid.Empty, "", "");

        reference.Should().StartWith($"txn:{Guid.Empty}:");
        reference.Length.Should().Be($"txn:{Guid.Empty}:".Length + 12);
    }

    [Fact]
    public void GenerateTransferRef_Should_ProduceUniqueValues()
    {
        var userId = Guid.NewGuid();
        const string acct = "9876543210";
        const string bank = "058";

        var ref1 = _generator.GenerateTransferRef(userId, acct, bank);
        Thread.Sleep(1000);
        var ref2 = _generator.GenerateTransferRef(userId, acct, bank);

        ref1.Should().NotBe(ref2);
    }

    [Fact]
    public void GenerateRecurringRef_Should_UseRecurringId()
    {
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        var result = _generator.GenerateRecurringRef(id);

        result.Should().StartWith("rec:");
        result.Length.Should().Be(16);
    }

    [Fact]
    public void GenerateMandateRef_Should_EmbedUserId()
    {
        var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var result = _generator.GenerateMandateRef(userId);

        result.Should().Be($"mandate:{userId}");
    }
}