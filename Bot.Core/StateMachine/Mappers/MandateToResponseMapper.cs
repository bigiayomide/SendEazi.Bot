using Bot.Shared.Models;

namespace Bot.Core.Mappers;

public static class MandateToResponseMapper
{
    public static object ToUserSummary(DirectDebitMandate m)
    {
        return new
        {
            Id       = m.Id,
            Provider = m.Provider,
            Status   = m.Status,
            MaxLimit = m.MaxAmount,
            Expires  = m.ExpiresAt?.ToString("yyyy-MM-dd"),
            LinkedTo = m.TransferDestinationAccount
        };
    }
}