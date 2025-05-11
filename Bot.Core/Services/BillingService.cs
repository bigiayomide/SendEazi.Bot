using Bot.Infrastructure.Data;
using Bot.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bot.Core.Services;

public interface IBillingService
{
    Task HandleTransferCompletedAsync(Guid transactionId);
    Task SweepFeesAsync();
}

public class BillingService(ApplicationDbContext db, ILogger<BillingService> logger) : IBillingService
{
    private const decimal FlatFee = 50m;
    private const decimal PercentFee = 0.01m;

    public async Task HandleTransferCompletedAsync(Guid transactionId)
    {
        var tx = await db.Transactions.FindAsync(transactionId);
        if (tx == null)
        {
            logger.LogWarning("Transaction {TxId} not found", transactionId);
            return;
        }

        var feeAmt = FlatFee + tx.Amount * PercentFee;
        var fee = new FeeRecord
        {
            TransactionId = transactionId,
            Amount = feeAmt,
            CreatedAt = DateTime.UtcNow,
            Swept = false
        };

        db.FeeRecords.Add(fee);
        await db.SaveChangesAsync();
        logger.LogInformation("Recorded fee of {Fee} for transaction {TxId}", feeAmt, transactionId);
    }

    public async Task SweepFeesAsync()
    {
        var unswept = await db.FeeRecords.Where(f => !f.Swept).ToListAsync();
        var total = unswept.Sum(f => f.Amount);

        if (total <= 0)
        {
            logger.LogInformation("No fees to sweep");
            return;
        }

        // TODO: Transfer to master account using provider here.
        var success = true;
        if (success)
        {
            unswept.ForEach(f => f.Swept = true);
            await db.SaveChangesAsync();
            logger.LogInformation("Swept total fees {Total} to master account", total);
        }
        else
        {
            logger.LogError("Fee sweep of {Total} failed", total);
        }
    }
}