using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Bot.Shared;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Bot.Tests.Services;

public class MemoServiceTests
{
    private static ApplicationDbContext CreateDb(string name)
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options);
    }

    [Fact]
    public async Task SaveAsync_Should_Create_New_Memo()
    {
        var db = CreateDb("memo-create");
        var txId = Guid.NewGuid();
        db.Transactions.Add(new Transaction
        {
            Id = txId,
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Amount = 100,
            Status = TransactionStatus.Success,
            CreatedAt = DateTime.UtcNow,
            Reference = "ref-create"
        });
        await db.SaveChangesAsync();

        var service = new MemoService(db);
        var payload = new MemoPayload(txId, "first memo", "url1");

        var resultId = await service.SaveAsync(Guid.NewGuid(), payload);

        resultId.Should().NotBeEmpty();
        var saved = await db.TransactionMemos.FindAsync(resultId);
        saved.Should().NotBeNull();
        saved!.TransactionId.Should().Be(txId);
        saved.MemoText.Should().Be("first memo");
        saved.ReceiptUrl.Should().Be("url1");
    }

    [Fact]
    public async Task SaveAsync_Should_Update_Existing_Memo()
    {
        var db = CreateDb("memo-update");
        var txId = Guid.NewGuid();
        db.Transactions.Add(new Transaction
        {
            Id = txId,
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Amount = 200,
            Status = TransactionStatus.Success,
            CreatedAt = DateTime.UtcNow,
            Reference = "ref-update"
        });
        var existing = new TransactionMemo
        {
            Id = Guid.NewGuid(),
            TransactionId = txId,
            MemoText = "old memo",
            ReceiptUrl = "old-url",
            CreatedAt = DateTime.UtcNow
        };
        db.TransactionMemos.Add(existing);
        await db.SaveChangesAsync();

        var service = new MemoService(db);
        var payload = new MemoPayload(txId, "new memo", "new-url");

        var resultId = await service.SaveAsync(Guid.NewGuid(), payload);

        resultId.Should().Be(existing.Id);
        var count = await db.TransactionMemos.CountAsync(m => m.TransactionId == txId);
        count.Should().Be(1);
        var updated = await db.TransactionMemos.FindAsync(existing.Id);
        updated!.MemoText.Should().Be("new memo");
        updated.ReceiptUrl.Should().Be("new-url");
    }
}