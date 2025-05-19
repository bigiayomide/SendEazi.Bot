using Bot.Core.StateMachine.Mappers;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using FluentAssertions;

namespace Bot.Tests.Mappers;

public class TransactionToPreviewMapperTests
{
    [Fact]
    public void ToCardTemplate_Should_Format_Transaction_Properties()
    {
        var tx = new Transaction
        {
            Amount = 1234.5m,
            CreatedAt = new DateTime(2024, 1, 2, 15, 30, 0, DateTimeKind.Utc),
            Status = TransactionStatus.Success,
            Reference = "ref-123",
            RecipientName = "Jane Doe"
        };

        dynamic result = TransactionToPreviewMapper.ToCardTemplate(tx);

        var expectedDate = tx.CreatedAt.ToLocalTime().ToString("dd MMM, HH:mm");

        ((string)result.Amount).Should().Be("1,234.50 NGN");
        ((string)result.Date).Should().Be(expectedDate);
        ((string)result.Status).Should().Be("Success");
        ((string)result.Ref).Should().Be("ref-123");
        ((string)result.Summary).Should().Be("You sent 1,234.50 to Jane Doe");
    }
}