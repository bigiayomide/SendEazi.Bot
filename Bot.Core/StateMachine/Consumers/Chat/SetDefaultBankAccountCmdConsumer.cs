using Bot.Infrastructure.Data;
using Bot.Shared.DTOs;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Bot.Core.StateMachine.Consumers.Chat;

public class SetDefaultBankAccountCmdConsumer(ApplicationDbContext db)
    : IConsumer<SetDefaultBankAccountCmd>
{
    public async Task Consume(ConsumeContext<SetDefaultBankAccountCmd> ctx)
    {
        var accounts = await db.LinkedBankAccounts
            .Where(a => a.UserId == ctx.Message.UserId)
            .ToListAsync();

        foreach (var acc in accounts)
            acc.IsDefault = acc.Id == ctx.Message.BankAccountId;

        await db.SaveChangesAsync();
    }
}