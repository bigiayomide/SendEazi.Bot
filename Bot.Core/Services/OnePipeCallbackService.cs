// Bot.Core.Services/OnePipeCallbackService.cs

using Bot.Infrastructure.Data;
using Bot.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bot.Core.Services;

public interface IOnePipeCallbackService
{
    Task HandleCallbackAsync(string requestRef, OnePipeResp resp);
}

public class OnePipeCallbackService(
    ApplicationDbContext db,
    ILogger<OnePipeCallbackService> log)
    : IOnePipeCallbackService
{
    public async Task HandleCallbackAsync(string requestRef, OnePipeResp resp)
    {
        var tx = await db.Transactions
            .FirstOrDefaultAsync(t => t.Reference == requestRef);

        if (tx == null)
        {
            log.LogWarning("No Tx for OnePipe request_ref {Ref}", requestRef);
            return;
        }

        tx.CompletedAt = DateTime.UtcNow;

        if (resp.Status.Equals("Successful", StringComparison.OrdinalIgnoreCase))
        {
            tx.Status = TransactionStatus.Success;
            tx.Amount = resp.Data.Amount;
        }
        else if (resp.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
        {
            tx.Status = TransactionStatus.Failed;
        }
        else
        {
            tx.Status = TransactionStatus.Pending; // or keep previous
        }

        await db.SaveChangesAsync();

        log.LogInformation("OnePipe txn {TxId} marked {Status}", tx.Id, tx.Status);
    }
}

/* OnePipe webhook DTOs */
public record OnePipeResp(string Status, string Message, OnePipeData Data);

public record OnePipeData(decimal Amount, string TransactionRef);