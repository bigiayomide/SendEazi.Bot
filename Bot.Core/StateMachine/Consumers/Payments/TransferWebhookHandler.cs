using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Bot.Shared.DTOs;
using Bot.Shared.Enums;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bot.Core.StateMachine.Consumers.Payments;

public class TransferWebhookHandler(
    ApplicationDbContext db,
    IWhatsAppService wa,
    ILogger<TransferWebhookHandler> log)
    : IConsumer<TransferCompleted>, IConsumer<TransferFailed>
{
    public async Task Consume(ConsumeContext<TransferCompleted> ctx)
    {
        var tx = await db.Transactions.FirstOrDefaultAsync(t => t.Reference == ctx.Message.Reference);
        if (tx == null)
        {
            log.LogWarning("TransferCompleted: no transaction found for ref {Ref}", ctx.Message.Reference);
            return;
        }

        tx.Status = TransactionStatus.Success;
        tx.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await ctx.Publish(new PreviewCmd(ctx.Message.CorrelationId, tx.Id));

        log.LogInformation("✅ Transfer marked successful and preview published for {Ref}", ctx.Message.Reference);
    }


    public async Task Consume(ConsumeContext<TransferFailed> ctx)
    {
        var tx = await db.Transactions.FirstOrDefaultAsync(t => t.Reference == ctx.Message.Reference);
        if (tx == null)
        {
            log.LogWarning("TransferFailed: no transaction found for ref {Ref}", ctx.Message.Reference);
            return;
        }

        tx.Status = TransactionStatus.Failed;
        tx.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var user = await db.Users.FindAsync(tx.UserId);
        if (user != null)
        {
            var msg = $"❌ Your transfer of ₦{tx.Amount:N0} failed.\nReason: {ctx.Message.Reason}";
            await wa.SendTextMessageAsync(user.PhoneNumber, msg);
        }

        log.LogWarning("❌ Transfer marked failed for {Ref} — Reason: {Reason}", ctx.Message.Reference,
            ctx.Message.Reason);
    }
}