using Bot.Core.Helpers;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using FluentAssertions;

namespace Bot.Tests.Helpers;

public class PreviewCardBuilderTests
{
    [Fact]
    public void BuildTransactionCard_Should_Set_Title()
    {
        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 1000,
            Reference = "txn:test",
            RecipientName = "Jane Doe",
            CompletedAt = DateTime.UtcNow
        };

        var card = PreviewCardBuilder.BuildTransactionCard(tx);

        card.GetType().GetProperty("title")!.GetValue(card)
            .Should().Be("✅ Transfer Successful");
    }

    [Fact]
    public void BuildBillCard_Should_Set_Title()
    {
        var bill = new BillPayment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 2000,
            Biller = BillerEnum.Electricity,
            IsPaid = true,
            CreatedAt = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow
        };

        var card = PreviewCardBuilder.BuildBillCard(bill);

        card.GetType().GetProperty("title")!.GetValue(card)
            .Should().Be("✅ Bill Payment Successful");
    }

    [Fact]
    public void BuildNoPreviewCard_Should_Set_Title_And_Body()
    {
        var card = PreviewCardBuilder.BuildNoPreviewCard();

        card.GetType().GetProperty("title")!.GetValue(card)
            .Should().Be("Info");

        card.GetType().GetProperty("body")!.GetValue(card)
            .Should().Be("No preview available.");
    }
}
