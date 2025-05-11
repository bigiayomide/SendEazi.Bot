using StackExchange.Redis;

namespace Bot.Infrastructure.Caching;

public class RedisService
{
    private readonly IDatabase _db;

    public RedisService(RedisConfiguration config)
    {
        var mux = ConnectionMultiplexer.Connect(config.ConnectionString);
        _db = mux.GetDatabase();
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