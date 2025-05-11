using Bot.Core.Providers;
using Bot.Infrastructure.Data;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bot.Core.StateMachine.Consumers.Payments;

public class TransferCmdConsumer(
    IBankProviderFactory factory,
    ApplicationDbContext db,
    ILogger<TransferCmdConsumer> log)
    : IConsumer<TransferCmd>
{
    public async Task Consume(ConsumeContext<TransferCmd> ctx)
    {
        var user = await db.Users.FindAsync(ctx.Message.CorrelationId);
        if (user is null)
        {
            log.LogWarning("Transfer failed: user not found.");
            await ctx.Publish(new TransferFailed(ctx.Message.CorrelationId, "User not found"));
            return;
        }

        var provider = await factory.GetProviderAsync(user.Id);
        var mandate = await db.DirectDebitMandates
            .Where(x => x.UserId == user.Id && x.Status == "ready")
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (mandate is null)
        {
            log.LogWarning("No active mandate for user.");
            await ctx.Publish(new TransferFailed(ctx.Message.CorrelationId, "No active mandate"));
            return;
        }

        var reference = $"txn:{Guid.NewGuid()}";

        var txn = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TenantId = user.TenantId,
            Amount = ctx.Message.Payload.Amount,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Reference = reference,
            RecipientName = ctx.Message.Payload.Description ?? "Beneficiary"
        };

        db.Transactions.Add(txn);

        var raw = new DirectDebitTransaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            MandateId = mandate.Id,
            Amount = txn.Amount,
            Reference = reference,
            Narration = ctx.Message.Payload.Description ?? "Auto debit",
            Status = DirectDebitStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        db.Add(raw);
        await db.SaveChangesAsync();

        var providerTxnId = await provider.InitiateDebitAsync(
            mandateId: mandate.MandateId!,
            amount: txn.Amount,
            reference: reference,
            narration: raw.Narration
        );

        raw.ProviderTransactionId = providerTxnId;
        await db.SaveChangesAsync();

        // webhook will update success/failure
    }
}
