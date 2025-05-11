using Bot.Infrastructure.Data;
using Bot.Shared;
using Bot.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NCrontab;

namespace Bot.Core.Services;

public interface IRecurringTransferService
{
    Task ProcessDueTransfersAsync();
    Task CancelRecurringAsync(Guid recurringId);
    Task<Guid> ScheduleAsync(Guid userId, RecurringPayload payload);
}



public class RecurringTransferService(ApplicationDbContext db, ILogger<RecurringTransferService> log)
    : IRecurringTransferService
{
    private readonly ILogger<RecurringTransferService> _log = log;

    public async Task ProcessDueTransfersAsync()
    {
        var now = DateTime.UtcNow;
        var due = await db.RecurringTransfers
            .Where(r => r.IsActive && r.NextRun <= now)
            .Include(r => r.Payee)
            .ToListAsync();

        foreach (var r in due)
        {
            // TODO: Dispatch transfer command here
            r.NextRun = CrontabSchedule.Parse(r.CronExpression).GetNextOccurrence(now);
        }

        await db.SaveChangesAsync();
    }

    public async Task CancelRecurringAsync(Guid recurringId)
    {
        var rec = await db.RecurringTransfers.FindAsync(recurringId);
        if (rec != null)
        {
            rec.IsActive = false;
            await db.SaveChangesAsync();
        }
    }

    public async Task<Guid> ScheduleAsync(Guid userId, RecurringPayload payload)
    {
        var payee = await db.Payees.FirstOrDefaultAsync(p =>
            p.UserId == userId &&
            p.AccountNumber == payload.Transfer.ToAccount &&
            p.BankCode == payload.Transfer.BankCode);

        if (payee == null)
        {
            payee = new Payee
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountNumber = payload.Transfer.ToAccount,
                BankCode = payload.Transfer.BankCode,
                CreatedAt = DateTime.UtcNow
            };
            db.Payees.Add(payee);
        }

        var rec = new RecurringTransfer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PayeeId = payee.Id,
            Amount = payload.Transfer.Amount,
            CronExpression = payload.Cron,
            NextRun = CrontabSchedule.Parse(payload.Cron).GetNextOccurrence(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        db.RecurringTransfers.Add(rec);
        await db.SaveChangesAsync();
        return rec.Id;
    }
}