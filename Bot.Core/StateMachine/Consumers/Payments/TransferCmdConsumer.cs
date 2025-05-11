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
        var userId = ctx.Message.CorrelationId;
        var user = await db.Users.FindAsync(userId);
        if (user is null)
        {
            log.LogWarning("Transfer failed: user not found.");
            await ctx.Publish(new TransferFailed(userId, "User not found"));
            return;
        }

        var reference = ctx.Message.Reference;

        var exists = await db.Transactions.AnyAsync(t => t.Reference == reference);
        if (exists)
        {
            log.LogWarning("Duplicate transfer for reference {Ref}", reference);
            return;
        }

        var existing = await db.Transactions.AnyAsync(t => t.Reference == reference);
        if (existing)
        {
            log.LogWarning("Duplicate transfer attempt");
            return;
        }

        // ✅ Get provider based on optional bank account ID
        var provider = await factory.GetProviderAsync(user.Id, ctx.Message.BankAccountId);

        // ✅ Validate mandate
        var mandate = await db.DirectDebitMandates
            .Where(x => x.UserId == user.Id && x.Status == "ready" && !x.IsRevoked)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (mandate is null)
        {
            await ctx.Publish(new TransferFailed(userId, "No active mandate available."));
            return;
        }

        // ✅ Save Transaction
        var payload = ctx.Message.Payload;
        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TenantId = user.TenantId,
            Amount = payload.Amount,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Reference = reference,
            RecipientName = payload.Description ?? "Beneficiary"
        };

        db.Transactions.Add(tx);

        var raw = new DirectDebitTransaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            MandateId = mandate.Id,
            Amount = tx.Amount,
            Reference = reference,
            Narration = payload.Description ?? "Auto debit",
            Status = DirectDebitStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        db.Add(raw);
        await db.SaveChangesAsync();

        try
        {
            var providerTxnId = await provider.InitiateDebitAsync(
                mandate.MandateId!,
                tx.Amount,
                reference,
                raw.Narration
            );

            raw.ProviderTransactionId = providerTxnId;
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Transfer initiation failed");
            await ctx.Publish(new TransferFailed(userId, "Provider error"));
        }

        // Note: we wait for webhook to confirm success/failure
    }
}