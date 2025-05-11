using Bot.Infrastructure.Data;
using Bot.Shared;
using Bot.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bot.Core.Services;

public interface IMemoService
{
    Task<Guid> SaveAsync(Guid userId, MemoPayload payload);
    Task AddOrUpdateMemoAsync(Guid transactionId, string memoText, string? receiptUrl);
}

public class MemoService(ApplicationDbContext db) : IMemoService
{
    public async Task<Guid> SaveAsync(Guid userId, MemoPayload p)
    {
        var existing = await db.TransactionMemos
            .FirstOrDefaultAsync(m => m.TransactionId == p.TransactionId);

        if (existing != null)
        {
            existing.MemoText = p.MemoText;
            existing.ReceiptUrl = p.ReceiptUrl;
            db.Update(existing);
        }
        else
        {
            existing = new TransactionMemo
            {
                Id = Guid.NewGuid(),
                TransactionId = p.TransactionId,
                MemoText = p.MemoText,
                ReceiptUrl = p.ReceiptUrl,
                CreatedAt = DateTime.UtcNow
            };
            db.TransactionMemos.Add(existing);
        }

        await db.SaveChangesAsync();
        return existing.Id;
    }

    public async Task AddOrUpdateMemoAsync(Guid transactionId, string memoText, string? receiptUrl)
    {
        await SaveAsync(Guid.Empty, new MemoPayload(transactionId, memoText, receiptUrl));
    }
}