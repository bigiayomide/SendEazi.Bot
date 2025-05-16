// File: Bot.Core.Consumers.Payments/TransferCmdConsumer.cs

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
        var reference = ctx.Message.Reference;

        // 🔒 Prevent duplicate transactions
        var exists = await db.Transactions.AnyAsync(t => t.Reference == reference);
        if (exists)
        {
            log.LogWarning("Duplicate transfer attempt for reference {Ref}", reference);
            return;
        }

        var user = await db.Users.FindAsync(userId);
        if (user == null)
        {
            await ctx.Publish(new TransferFailed(userId, "User not found", reference));
            return;
        }

        var mandate = await db.DirectDebitMandates
            .Where(x => x.UserId == userId && x.Status == "ready" && !x.IsRevoked)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (mandate == null)
        {
            await ctx.Publish(new TransferFailed(userId, "No active mandate", reference));
            return;
        }

        var provider = await factory.GetProviderAsync(userId, ctx.Message.BankAccountId);

        var tx = new Transaction
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

        db.Transactions.Add(tx);
        await db.SaveChangesAsync();

        try
        {
            var providerTxnId = await provider.InitiateDebitAsync(
                mandateId: mandate.MandateId!,
                amount: tx.Amount,
                reference: reference,
                narration: tx.RecipientName
            );

            log.LogInformation("Transfer initiated: {Ref} (ProviderTxnId: {Id})", reference, providerTxnId);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to initiate transfer for ref {Ref}", reference);
            await ctx.Publish(new TransferFailed(userId, "Provider error", reference));
        }
    }
}
