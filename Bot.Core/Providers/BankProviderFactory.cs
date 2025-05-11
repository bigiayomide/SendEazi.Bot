using Bot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bot.Core.Providers;

public class BankProviderFactory(IServiceProvider sp, ApplicationDbContext db) : IBankProviderFactory
{
    public async Task<IBankProvider> GetProviderAsync(Guid userId, Guid? selectedBankId = null)
    {
        var query = db.LinkedBankAccounts.AsQueryable();

        var account = selectedBankId.HasValue
            ? await query.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == selectedBankId)
            : await query.FirstOrDefaultAsync(x => x.UserId == userId && x.IsDefault);

        if (account == null)
            throw new InvalidOperationException("No linked bank account found");

        return account.Provider switch
        {
            "Mono" => sp.GetRequiredService<MonoBankProvider>(),
            "OnePipe" => sp.GetRequiredService<OnePipeBankProvider>(),
            _ => throw new NotSupportedException("Unsupported provider")
        };
    }
}