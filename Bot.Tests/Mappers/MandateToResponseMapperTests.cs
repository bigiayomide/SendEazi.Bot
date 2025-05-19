using Bot.Core.StateMachine.Mappers;
using Bot.Shared.Models;
using FluentAssertions;
using Xunit;

namespace Bot.Tests.Mappers;

public class MandateToResponseMapperTests
{
    [Fact]
    public void ToUserSummary_Should_Map_All_Fields()
    {
        var mandate = new DirectDebitMandate
        {
            Id = Guid.NewGuid(),
            Provider = "Mono",
            Status = "active",
            MaxAmount = 2000m,
            ExpiresAt = new DateTime(2024, 6, 1),
            TransferDestinationAccount = "0123456789"
        };

        var result = MandateToResponseMapper.ToUserSummary(mandate);

        result.Should().BeEquivalentTo(new
        {
            mandate.Id,
            mandate.Provider,
            mandate.Status,
            MaxLimit = mandate.MaxAmount,
            Expires = "2024-06-01",
            LinkedTo = mandate.TransferDestinationAccount
        });
    }
}
