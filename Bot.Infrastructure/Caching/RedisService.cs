using StackExchange.Redis;

namespace Bot.Infrastructure.Caching;

public class RedisService(IConnectionMultiplexer connection)
{
    private readonly IDatabase _db = connection.GetDatabase();

    public RedisService(RedisConfiguration config)
        : this(ConnectionMultiplexer.Connect(config.ConnectionString))
    {
    }

    public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
    {
        await _db.StringSetAsync(key, value, expiry);
    }

    public async Task<string?> GetAsync(string key)
    {
        return await _db.StringGetAsync(key);
    }

    public async Task DeleteAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }
}