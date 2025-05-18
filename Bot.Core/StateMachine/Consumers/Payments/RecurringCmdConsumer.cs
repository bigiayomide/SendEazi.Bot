using Bot.Infrastructure.Data;
using Bot.Shared.DTOs;
using Bot.Shared.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Bot.Core.StateMachine.Consumers.Payments;

public class RecurringCmdConsumer : IConsumer<RecurringCmd>
{
    private readonly ApplicationDbContext _db;

    public RecurringCmdConsumer(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<RecurringCmd> ctx)
    {
        var p = ctx.Message.Payload.Transfer;

        var payee = await _db.Payees
            .FirstOrDefaultAsync(x =>
                x.UserId == ctx.Message.CorrelationId &&
                x.AccountNumber == p.ToAccount &&
                x.BankCode == p.BankCode);

        if (payee == null)
        {
            payee = new Payee
            {
                Id = Guid.NewGuid(),
                UserId = ctx.Message.CorrelationId,
                AccountNumber = p.ToAccount,
                BankCode = p.BankCode,
                Nickname = null,
                CreatedAt = DateTime.UtcNow
            };
            _db.Payees.Add(payee);
        }

        var recurring = new RecurringTransfer
        {
            Id = Guid.NewGuid(),
            UserId = ctx.Message.CorrelationId,
            PayeeId = payee.Id,
            Amount = p.Amount,
            CronExpression = ctx.Message.Payload.Cron,
            IsActive = true,
            NextRun = DateTime.UtcNow.AddMinutes(1),
            CreatedAt = DateTime.UtcNow
        };

        _db.RecurringTransfers.Add(recurring);
        await _db.SaveChangesAsync();

        await ctx.Publish(new RecurringExecuted(ctx.Message.CorrelationId, recurring.Id));
    }
}