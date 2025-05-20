using Bot.Infrastructure.Caching;
using FluentAssertions;
using Moq;
using StackExchange.Redis;

namespace Bot.Tests.Caching;

public class RedisServiceTests
{
    private static RedisService CreateService(RedisMock mock)
    {
        return new RedisService(mock.Connection.Object);
    }

    [Fact]
    public async Task SetAsync_Should_Store_Value_With_Optional_Expiry()
    {
        var store = new Dictionary<string, RedisValue>();
        var mockDb = new Mock<IDatabase>();
        mockDb.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), 
                It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .Callback<RedisKey, RedisValue, TimeSpan?, bool, When, CommandFlags>((key, val, _, _, _, _) =>
            {
                store[key] = val;
            })
            .ReturnsAsync(true);

        var mockConnection = new Mock<IConnectionMultiplexer>();
        mockConnection.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDb.Object);

        var service = new RedisService(mockConnection.Object);

        await service.SetAsync("a", "val");

        store["a"].ToString().Should().Be("val");
    }

    [Fact]
    public async Task GetAsync_Should_Retrieve_Stored_Value()
    {
        var store = new Dictionary<string, RedisValue>
        {
            ["a"] = "val"
        };

        var mockDb = new Mock<IDatabase>();
        mockDb.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns<RedisKey, CommandFlags>((key, _) =>
            {
                return Task.FromResult(store.TryGetValue(key, out var value) ? value : RedisValue.Null);
            });

        var mockConnection = new Mock<IConnectionMultiplexer>();
        mockConnection.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(mockDb.Object);

        var service = new RedisService(mockConnection.Object);

        var result = await service.GetAsync("a");

        result.Should().Be("val");
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_Key()
    {
        var redis = new RedisMock();
        var service = CreateService(redis);

        await service.SetAsync("k", "v");
        await service.DeleteAsync("k");

        redis.Strings.ContainsKey("k").Should().BeFalse();
        redis.Expirations.ContainsKey("k").Should().BeFalse();
    }

    private class RedisMock
    {
        public RedisMock()
        {
            Connection.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
                .Returns(Database.Object);

            Database.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
                    It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .Callback((RedisKey key, RedisValue val, TimeSpan? exp, When _, CommandFlags __) =>
                {
                    Strings[key] = val;
                    if (exp != null)
                        Expirations[key] = exp;
                    else
                        Expirations.Remove(key);
                })
                .ReturnsAsync(true);

            Database.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, CommandFlags _) =>
                {
                    return Task.FromResult(Strings.TryGetValue(key, out var val) ? val : RedisValue.Null);
                });

            Database.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .Callback((RedisKey key, CommandFlags _) =>
                {
                    Strings.Remove(key);
                    Expirations.Remove(key);
                })
                .ReturnsAsync(true);
        }

        public Mock<IConnectionMultiplexer> Connection { get; } = new();
        public Mock<IDatabase> Database { get; } = new();
        public Dictionary<string, RedisValue> Strings { get; } = new();
        public Dictionary<string, TimeSpan?> Expirations { get; } = new();
    }
}