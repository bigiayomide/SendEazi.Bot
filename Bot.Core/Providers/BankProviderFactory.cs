// Bot.Core.Providers/BankProviderFactory.cs
using System;
using System.Threading.Tasks;
using Bot.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Bot.Core.Providers
{
    public class BankProviderFactory(IServiceProvider sp, ApplicationDbContext db) : IBankProviderFactory
    {
        public async Task<IBankProvider> GetProviderAsync(Guid userId)
        {
            var account = await db.LinkedBankAccounts.FirstOrDefaultAsync(x => x.UserId == userId);

            if (account == null)
                throw new InvalidOperationException("No linked bank for user");

            return account.Provider switch
            {
                "Mono" => sp.GetRequiredService<MonoBankProvider>(),
                "OnePipe" => sp.GetRequiredService<OnePipeBankProvider>(),
                _ => throw new NotSupportedException("Unsupported provider: " + account.Provider)
            };
        }
    }
}