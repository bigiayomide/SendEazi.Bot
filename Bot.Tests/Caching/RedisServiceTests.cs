using Bot.Infrastructure.Caching;
using FluentAssertions;
using Moq;
using StackExchange.Redis;

namespace Bot.Tests.Caching;

public class RedisServiceTests
{
    private class RedisMock
    {
        public Mock<IConnectionMultiplexer> Connection { get; } = new();
        public Mock<IDatabase> Database { get; } = new();
        public Dictionary<string, RedisValue> Strings { get; } = new();
        public Dictionary<string, TimeSpan?> Expirations { get; } = new();

        public RedisMock()
        {
            Connection.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
                .Returns(Database.Object);

            Database.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
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
    }

    private static RedisService CreateService(RedisMock mock)
    {
        return new RedisService(mock.Connection.Object);
    }

    [Fact]
    public async Task SetAsync_Should_Store_Value_With_Optional_Expiry()
    {
        var redis = new RedisMock();
        var service = CreateService(redis);

        var ttl = TimeSpan.FromSeconds(30);
        await service.SetAsync("a", "1", ttl);
        redis.Strings["a"].Should().Be("1");
        redis.Expirations["a"].Should().Be(ttl);

        await service.SetAsync("b", "2");
        redis.Strings["b"].Should().Be("2");
        redis.Expirations.ContainsKey("b").Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_Should_Retrieve_Stored_Value()
    {
        var redis = new RedisMock();
        var service = CreateService(redis);

        await service.SetAsync("key", "val");
        var result = await service.GetAsync("key");

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
}
