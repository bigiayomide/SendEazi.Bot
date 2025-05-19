using Bot.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;

namespace Bot.Tests.Services;

public class ConversationStateServiceTests
{
    private class RedisMock
    {
        public Mock<IConnectionMultiplexer> Connection { get; } = new();
        public Mock<IDatabase> Database { get; } = new();
        public Mock<ITransaction> Transaction { get; } = new();
        public Dictionary<string, RedisValue> Strings { get; } = new();
        public Dictionary<string, Dictionary<string, RedisValue>> Hashes { get; } = new();
        public Dictionary<string, TimeSpan?> Expirations { get; } = new();

        public RedisMock()
        {
            Connection.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
                .Returns(Database.Object);
            Database.Setup(d => d.CreateTransaction(It.IsAny<object?>()))
                .Returns(Transaction.Object);

            Database.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .Returns((RedisKey k, CommandFlags _) =>
                {
                    Strings.TryGetValue(k, out var v);
                    return Task.FromResult(v);
                });

            Database.Setup(d => d.HashGetAllAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .Returns((RedisKey k, CommandFlags _) =>
                {
                    if (Hashes.TryGetValue(k, out var dict))
                        return Task.FromResult(dict.Select(e => new HashEntry(e.Key, e.Value)).ToArray());
                    return Task.FromResult(Array.Empty<HashEntry>());
                });

            Database.Setup(d => d.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .Callback((RedisKey key, RedisValue field, RedisValue val, When w, CommandFlags f) =>
                {
                    if (!Hashes.ContainsKey(key)) Hashes[key] = new();
                    Hashes[key][field] = val;
                })
                .ReturnsAsync(true);

            Database.Setup(d => d.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
                .Returns((RedisKey key, RedisValue field, CommandFlags f) =>
                {
                    if (Hashes.TryGetValue(key, out var dict) && dict.TryGetValue(field, out var val))
                        return Task.FromResult(val);
                    return Task.FromResult(RedisValue.Null);
                });

            Database.Setup(d => d.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<CommandFlags>()))
                .Callback((RedisKey key, TimeSpan? exp, CommandFlags f) => Expirations[key] = exp)
                .ReturnsAsync(true);

            Database.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .Callback((RedisKey key, RedisValue val, TimeSpan? exp, When w, CommandFlags f) =>
                {
                    Strings[key] = val;
                    Expirations[key] = exp;
                })
                .ReturnsAsync(true);

            Transaction.Setup(t => t.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), It.IsAny<CommandFlags>()))
                .Callback((RedisKey key, HashEntry[] entries, CommandFlags f) =>
                {
                    if (!Hashes.ContainsKey(key)) Hashes[key] = new();
                    foreach (var e in entries) Hashes[key][e.Name] = e.Value;
                })
                .ReturnsAsync(true);

            Transaction.Setup(t => t.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<CommandFlags>()))
                .Callback((RedisKey key, TimeSpan? exp, CommandFlags f) => Expirations[key] = exp)
                .ReturnsAsync(true);

            Transaction.Setup(t => t.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .Callback((RedisKey key, RedisValue val, TimeSpan? exp, When w, CommandFlags f) =>
                {
                    Strings[key] = val;
                    Expirations[key] = exp;
                })
                .ReturnsAsync(true);

            Transaction.Setup(t => t.ExecuteAsync(It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
        }
    }

    private static ConversationStateService CreateService(RedisMock redis, out ConversationStateOptions opts)
    {
        opts = new ConversationStateOptions { RedisKeyPrefix = "session:", SessionTtl = TimeSpan.FromMinutes(5) };
        var logger = new Mock<ILogger<ConversationStateService>>();
        return new ConversationStateService(redis.Connection.Object, Options.Create(opts), logger.Object);
    }

    [Fact]
    public async Task GetOrCreateSessionAsync_Should_Create_And_Cache_New_Session()
    {
        var redis = new RedisMock();
        var service = CreateService(redis, out var opts);
        var phone = "+123";

        var session = await service.GetOrCreateSessionAsync(phone);

        session.PhoneNumber.Should().Be(phone);
        session.SessionId.Should().NotBeEmpty();

        var idxKey = $"{opts.RedisKeyPrefix}index:{phone}";
        var sessKey = $"{opts.RedisKeyPrefix}{session.SessionId}";

        redis.Strings[idxKey].Should().Be(session.SessionId.ToString());
        redis.Hashes[sessKey][nameof(ConversationSession.PhoneNumber)].Should().Be(phone);
        redis.Expirations[idxKey].Should().Be(opts.SessionTtl);
        redis.Expirations[sessKey].Should().Be(opts.SessionTtl);
    }

    [Fact]
    public async Task SetUserStateAndMessage_Should_Update_Stored_Hash_And_Refresh_Ttl()
    {
        var redis = new RedisMock();
        var service = CreateService(redis, out var opts);
        var phone = "+234";
        var session = await service.GetOrCreateSessionAsync(phone);
        var sessKey = $"{opts.RedisKeyPrefix}{session.SessionId}";
        var idxKey = $"{opts.RedisKeyPrefix}index:{phone}";

        var userId = Guid.NewGuid();
        await service.SetUserAsync(session.SessionId, userId);
        redis.Hashes[sessKey][nameof(ConversationSession.UserId)].Should().Be(userId.ToString());
        var userIdxKey = $"{opts.RedisKeyPrefix}user:{userId}";
        redis.Strings[userIdxKey].Should().Be(session.SessionId.ToString());
        redis.Expirations[userIdxKey].Should().Be(opts.SessionTtl);
        redis.Expirations[sessKey].Should().Be(opts.SessionTtl);
        redis.Expirations[idxKey].Should().Be(opts.SessionTtl);

        var state = "Ready";
        await service.SetStateAsync(session.SessionId, state);
        redis.Hashes[sessKey][nameof(ConversationSession.State)].Should().Be(state);
        redis.Expirations[sessKey].Should().Be(opts.SessionTtl);

        var msg = "hi";
        await service.UpdateLastMessageAsync(session.SessionId, msg);
        redis.Hashes[sessKey][nameof(ConversationSession.LastMessage)].Should().Be(msg);
        redis.Expirations[sessKey].Should().Be(opts.SessionTtl);
    }

    [Fact]
    public async Task GetSessionByUserAsync_Should_Return_Mapped_Session()
    {
        var redis = new RedisMock();
        var service = CreateService(redis, out var opts);
        var phone = "+111";
        var session = await service.GetOrCreateSessionAsync(phone);
        var userId = Guid.NewGuid();
        await service.SetUserAsync(session.SessionId, userId);

        var result = await service.GetSessionByUserAsync(userId);

        result.Should().NotBeNull();
        result!.SessionId.Should().Be(session.SessionId);
        result.PhoneNumber.Should().Be(phone);
        result.UserId.Should().Be(userId);
    }
}
