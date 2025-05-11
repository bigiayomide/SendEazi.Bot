using Bot.Shared.Models;

namespace Bot.Core.StateMachine.Mappers;

public static class MandateToResponseMapper
{
    public static object ToUserSummary(DirectDebitMandate m)
    {
        return new
        {
            m.Id,
            m.Provider,
            m.Status,
            MaxLimit = m.MaxAmount,
            Expires = m.ExpiresAt?.ToString("yyyy-MM-dd"),
            LinkedTo = m.TransferDestinationAccount
        };
    }
}