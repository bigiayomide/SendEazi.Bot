// Bot.Core.Services/ConversationStateService.cs

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Bot.Shared.Enums;

namespace Bot.Core.Services;

public interface IConversationStateService
{
    Task<ConversationSession> GetOrCreateSessionAsync(string phoneNumber);
    Task<ConversationSession?> GetSessionByUserAsync(Guid userId);
    Task UpdateLastMessageAsync(Guid sessionId, string message);
    Task SetUserAsync(Guid sessionId, Guid userId);
    Task SetStateAsync(Guid sessionId, ConversationState state);
    Task<ConversationState> GetStateAsync(Guid sessionId);
}

public class ConversationSession
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string PhoneNumber { get; set; } = null!;
    public ConversationState State { get; set; } = ConversationState.None;
    public string? LastMessage { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}

public class ConversationStateOptions
{
    public string RedisKeyPrefix { get; set; } = "session:";
    public TimeSpan SessionTtl { get; set; } = TimeSpan.FromHours(24);
}

public class ConversationStateService(
    IConnectionMultiplexer redis,
    IOptions<ConversationStateOptions> opts,
    ILogger<ConversationStateService> logger)
    : IConversationStateService
{
    private readonly ConversationStateOptions _opts = opts.Value;
    private readonly IDatabase _redis = redis.GetDatabase();

    public async Task<ConversationSession> GetOrCreateSessionAsync(string phoneNumber)
    {
        var idxKey = IndexKey(phoneNumber);
        var sessionIdStr = await _redis.StringGetAsync(idxKey);

        if (!sessionIdStr.IsNullOrEmpty)
        {
            var sid = Guid.Parse(sessionIdStr!);
            var hash = await _redis.HashGetAllAsync(SessionKey(sid));
            if (hash.Length > 0)
                return Map(hash);
        }

        var newId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var sessHash = new HashEntry[]
        {
            new(nameof(ConversationSession.SessionId), newId.ToString()),
            new(nameof(ConversationSession.UserId), Guid.Empty.ToString()),
            new(nameof(ConversationSession.PhoneNumber), phoneNumber),
            new(nameof(ConversationSession.State), ConversationState.None.ToString()),
            new(nameof(ConversationSession.LastUpdatedUtc), now.ToString("o"))
        };

        var tran = _redis.CreateTransaction();
        tran.HashSetAsync(SessionKey(newId), sessHash);
        tran.KeyExpireAsync(SessionKey(newId), _opts.SessionTtl);
        tran.StringSetAsync(idxKey, newId.ToString(), _opts.SessionTtl);
        await tran.ExecuteAsync();

        logger.LogDebug("Created session {Sid} for phone {Phone}", newId, phoneNumber);

        return new ConversationSession
        {
            SessionId = newId,
            PhoneNumber = phoneNumber,
            LastUpdatedUtc = now
        };
    }

    public async Task UpdateLastMessageAsync(Guid sessionId, string message)
    {
        var key = SessionKey(sessionId);
        await _redis.HashSetAsync(key, nameof(ConversationSession.LastMessage), message);
        await TouchAsync(sessionId);
    }

    public async Task<ConversationSession?> GetSessionByUserAsync(Guid userId)
    {
        var sessionIdStr = await _redis.StringGetAsync(UserIndexKey(userId));
        if (sessionIdStr.IsNullOrEmpty)
            return null;

        var sid = Guid.Parse(sessionIdStr!);
        var hash = await _redis.HashGetAllAsync(SessionKey(sid));
        return hash.Length == 0 ? null : Map(hash);
    }

    public async Task SetUserAsync(Guid sessionId, Guid userId)
    {
        var key = SessionKey(sessionId);
        var tran = _redis.CreateTransaction();
        tran.HashSetAsync(key, nameof(ConversationSession.UserId), userId.ToString());
        tran.StringSetAsync(UserIndexKey(userId), sessionId.ToString(), _opts.SessionTtl);
        await tran.ExecuteAsync();
        await TouchAsync(sessionId);
    }

    public async Task SetStateAsync(Guid sessionId, ConversationState state)
    {
        var key = SessionKey(sessionId);
        await _redis.HashSetAsync(key, nameof(ConversationSession.State), state.ToString());
        await TouchAsync(sessionId);
    }

    public async Task<ConversationState> GetStateAsync(Guid sessionId)
    {
        var key = SessionKey(sessionId);
        var state = await _redis.HashGetAsync(key, nameof(ConversationSession.State));
        if (state.IsNullOrEmpty)
            return ConversationState.None;
        return Enum.TryParse<ConversationState>(state!, out var result) ? result : ConversationState.None;
    }

    private string UserIndexKey(Guid userId)
    {
        return $"{_opts.RedisKeyPrefix}user:{userId}";
    }

    private string SessionKey(Guid id)
    {
        return $"{_opts.RedisKeyPrefix}{id}";
    }

    private string IndexKey(string phone)
    {
        return $"{_opts.RedisKeyPrefix}index:{phone}";
    }

    private ConversationSession Map(HashEntry[] hash)
    {
        var dict = hash.ToStringDictionary();
        var stateStr = dict[nameof(ConversationSession.State)];
        var state = Enum.TryParse<ConversationState>(stateStr, out var parsed)
            ? parsed
            : ConversationState.None;
        return new ConversationSession
        {
            SessionId = Guid.Parse(dict[nameof(ConversationSession.SessionId)]),
            UserId = Guid.Parse(dict[nameof(ConversationSession.UserId)]),
            PhoneNumber = dict[nameof(ConversationSession.PhoneNumber)],
            State = state,
            LastMessage = dict.GetValueOrDefault(nameof(ConversationSession.LastMessage)),
            LastUpdatedUtc = DateTime.Parse(dict[nameof(ConversationSession.LastUpdatedUtc)])
        };
    }

    private async Task TouchAsync(Guid sessionId)
    {
        var key = SessionKey(sessionId);
        await _redis.KeyExpireAsync(key, _opts.SessionTtl);

        var phone = await _redis.HashGetAsync(key, nameof(ConversationSession.PhoneNumber));
        if (!phone.IsNullOrEmpty)
            await _redis.KeyExpireAsync(IndexKey(phone!), _opts.SessionTtl);

        var user = await _redis.HashGetAsync(key, nameof(ConversationSession.UserId));
        if (!user.IsNullOrEmpty && Guid.TryParse(user!, out var uid))
            await _redis.KeyExpireAsync(UserIndexKey(uid), _opts.SessionTtl);
    }
}