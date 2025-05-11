using System.Text.Json;
using Bot.Core.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bot.Core.Services;

public interface IQuickReplyService
{
    Task<IReadOnlyList<string>> GetQuickRepliesAsync(Guid userId, int max = 5);
    Task RecordPayeeUseAsync(Guid userId, Guid payeeId);
    Task<object> BuildBalanceCardAsync(decimal amount);
    Task<object> BuildGoalReminderAsync(string category, decimal limit);
}

public class QuickReplyService(IDistributedCache cache, IOptions<QuickReplyOptions> opts) : IQuickReplyService
{
    private readonly QuickReplyOptions _opts = opts.Value;

    public async Task<IReadOnlyList<string>> GetQuickRepliesAsync(Guid userId, int max = 5)
    {
        var key = $"{_opts.RedisKeyPrefix}{userId}";
        var data = await cache.GetStringAsync(key);
        var favorites = !string.IsNullOrEmpty(data)
            ? JsonSerializer.Deserialize<List<Guid>>(data)!
            : [];

        var favLabels = favorites
            .Take(max)
            .Select(id => $"Payee:{id}") // TODO: resolve via IBeneficiaryService
            .ToList();

        favLabels.AddRange(_opts.DefaultTemplates.Take(max - favLabels.Count));
        return favLabels;
    }

    public async Task RecordPayeeUseAsync(Guid userId, Guid payeeId)
    {
        var key = $"{_opts.RedisKeyPrefix}{userId}";
        var data = await cache.GetStringAsync(key);
        var favorites = string.IsNullOrEmpty(data)
            ? []
            : JsonSerializer.Deserialize<List<Guid>>(data)!;

        favorites.Remove(payeeId);
        favorites.Insert(0, payeeId);

        if (favorites.Count > _opts.MaxFavorites)
            favorites = favorites.Take(_opts.MaxFavorites).ToList();

        await cache.SetStringAsync(key, JsonSerializer.Serialize(favorites),
            new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromDays(7)
            });
    }

    public Task<object> BuildBalanceCardAsync(decimal amount) =>
        Task.FromResult<object>(new
        {
            title = "💰 Account Balance",
            body = $"You have ₦{amount:N2} available.",
            actions = new[]
            {
                new { type = "reply", label = "Send Money", value = "send money" },
                new { type = "reply", label = "Recent Activity", value = "recent transactions" }
            }
        });

    public Task<object> BuildGoalReminderAsync(string category, decimal limit) =>
        Task.FromResult<object>(new
        {
            title = $"📊 {category} Budget Alert",
            body = $"You’re nearing your ₦{limit:N0} monthly limit.",
            actions = new[]
            {
                new { type = "reply", label = "Adjust Goal", value = "update goal" },
                new { type = "reply", label = "Ignore", value = "dismiss" }
            }
        });
}
