using System.Text.Json;
using Bot.Core.Models;
using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Bot.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Bot.Tests.Services;

public class QuickReplyServiceTests
{
    private static ApplicationDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    private static IDistributedCache CreateCache() =>
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    private static QuickReplyService CreateService(ApplicationDbContext db, IDistributedCache cache) =>
        new(cache, Options.Create(new QuickReplyOptions()), db);

    [Fact]
    public async Task GetQuickReplies_Should_Return_Defaults_When_No_Favorites()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDb("qr-no-fav");
        var service = CreateService(db, CreateCache());

        var results = await service.GetQuickRepliesAsync(userId, max: 2);

        results.Should().ContainInOrder("Check balance", "Send money");
    }

    [Fact]
    public async Task RecordPayeeUse_Should_Update_Cache_Order()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDb("qr-order");
        var cache = CreateCache();
        var service = CreateService(db, cache);

        var p1 = new Payee
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountNumber = "1111222233",
            BankCode = "001",
            Nickname = "Alpha"
        };
        var p2 = new Payee
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountNumber = "2222333344",
            BankCode = "002"
        };

        db.Payees.AddRange(p1, p2);
        await db.SaveChangesAsync();

        await service.RecordPayeeUseAsync(userId, p1.Id);
        await service.RecordPayeeUseAsync(userId, p2.Id);
        await service.RecordPayeeUseAsync(userId, p1.Id);

        var replies = await service.GetQuickRepliesAsync(userId, max: 2);

        replies.Should().ContainInOrder("Alpha", "Acct:3344");

        var key = $"qr:{userId}";
        var data = await cache.GetStringAsync(key);
        var ids = JsonSerializer.Deserialize<List<Guid>>(data!);
        ids.Should().BeEquivalentTo(new[] { p1.Id, p2.Id }, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task GetQuickReplies_Should_Fill_With_Defaults()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDb("qr-fill");
        var cache = CreateCache();
        var service = CreateService(db, cache);

        var p1 = new Payee
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountNumber = "5555666677",
            BankCode = "003",
            Nickname = "Beta"
        };

        db.Payees.Add(p1);
        await db.SaveChangesAsync();

        await service.RecordPayeeUseAsync(userId, p1.Id);

        var results = await service.GetQuickRepliesAsync(userId, max: 4);

        results.Should().ContainInOrder("Beta", "Check balance", "Send money", "Recent transactions");
    }

    [Fact]
    public async Task BuildBalanceCardAsync_Should_Create_Correct_Structure()
    {
        await using var db = CreateDb("qr-balance");
        var service = CreateService(db, CreateCache());

        var card = await service.BuildBalanceCardAsync(2500.5m);
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(card));
        var root = doc.RootElement;
        root.GetProperty("title").GetString().Should().Be("\ud83d\udcb0 Account Balance");
        root.GetProperty("body").GetString().Should().Be("You have \u20a62,500.50 available.");
        root.GetProperty("actions").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task BuildGoalReminderAsync_Should_Create_Correct_Structure()
    {
        await using var db = CreateDb("qr-goal");
        var service = CreateService(db, CreateCache());

        var card = await service.BuildGoalReminderAsync("Food", 10000);
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(card));
        var root = doc.RootElement;
        root.GetProperty("title").GetString().Should().Be("\ud83d\udcca Food Budget Alert");
        root.GetProperty("body").GetString().Should().Be("You’re nearing your \u20a610,000 monthly limit.");
        root.GetProperty("actions").GetArrayLength().Should().Be(2);
    }
}
