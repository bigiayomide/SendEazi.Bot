// Bot.Core.Services/MonoCallbackService.cs

using System.Threading.Tasks;
using Bot.Infrastructure.Data;
using Bot.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bot.Core.Services;

public interface IMonoCallbackService
{
    Task HandleCallbackAsync(string evt, MonoData data);
}

public class MonoCallbackService : IMonoCallbackService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<MonoCallbackService> _log;

    public MonoCallbackService(
        ApplicationDbContext db,
        ILogger<MonoCallbackService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task HandleCallbackAsync(string evt, MonoData data)
    {
        switch (evt)
        {
            /* --- Account Link --- */
            case "mono.events.account_connected":
                await SaveLinkedAccountAsync(data.Id);
                break;

            /* --- One-time Debit Events --- */
            case "direct_debit.payment_successful":
                await UpdateTransactionAsync(
                    data.Object?.Reference,
                    TransactionStatus.Success,
                    data.Object?.Amount ?? 0);
                break;

            case "direct_debit.payment_failed":
            case "direct_debit.payment_abandoned":
            case "direct_debit.payment_cancelled":
                await UpdateTransactionAsync(
                    data.Object?.Reference,
                    TransactionStatus.Failed,
                    0);
                break;

            default:
                _log.LogWarning("Unhandled Mono event: {Evt}", evt);
                break;
        }
    }

    private async Task SaveLinkedAccountAsync(string monoAccountId)
    {
        // Persist monoAccountId against the user profile if not already saved.
        // demo: just log
        _log.LogInformation("Mono account linked: {AccId}", monoAccountId);
        await Task.CompletedTask;
    }

    private async Task UpdateTransactionAsync(string? reference, TransactionStatus status, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(reference)) return;

        var tx = await _db.Transactions
            .Where(t => t.Reference == reference)
            .FirstOrDefaultAsync();
        if (tx == null)
        {
            _log.LogWarning("Transaction not found for Mono reference {Ref}", reference);
            return;
        }

        tx.Status = status;
        tx.CompletedAt = DateTime.UtcNow;
        if (amount > 0) tx.Amount = amount;

        await _db.SaveChangesAsync();

        _log.LogInformation("Transaction {TxId} updated to {Status}", tx.Id, status);
    }
}

/* Mono webhook DTOs */
public record MonoData(
    string Id,
    string? Type,
    MonoTxn? Object);

public record MonoTxn(
    string Id,
    string Status,
    string Reference,
    decimal Amount);