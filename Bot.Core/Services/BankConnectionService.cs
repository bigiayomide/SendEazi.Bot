using Bot.Core.Providers;
using Bot.Infrastructure.Data;

namespace Bot.Core.Services;

public class BankConnectionService(ApplicationDbContext db, IBankProvider provider)
{
    public async Task ConnectUserBankAsync(Guid userId, string authCode)
    {
        var user = await db.Users.FindAsync(userId) ?? throw new InvalidOperationException();
        //TODO: check for correctness
        // user.BankAccessToken = await provider.ConnectAsync(authCode, userId);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}