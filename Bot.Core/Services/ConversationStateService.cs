// Bot.Core.Services/ConversationStateService.cs

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Bot.Core.Services;

public interface IConversationStateService
{
    Task<ConversationSession> GetOrCreateSessionAsync(string phoneNumber);
    Task UpdateLastMessageAsync(Guid sessionId, string message);
    Task SetStateAsync(Guid sessionId, string state);
    Task<string> GetStateAsync(Guid sessionId);
}

public class ConversationSession
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string PhoneNumber { get; set; } = null!;
    public string State { get; set; } = "None";
    public string? LastMessage { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}

public class ConversationStateOptions
{
    public string RedisKeyPrefix { get; set; } = "session:";
    public TimeSpan SessionTtl { get; set; } = TimeSpan.FromHours(24);
}

public class ConversationStateService : IConversationStateService
{
    private readonly ILogger<ConversationStateService> _logger;
    private readonly ConversationStateOptions _opts;
    private readonly IDatabase _redis;

    public ConversationStateService(
        IConnectionMultiplexer redis,
        IOptions<ConversationStateOptions> opts,
        ILogger<ConversationStateService> logger)
    {
        _redis = redis.GetDatabase();
        _opts = opts.Value;
        _logger = logger;
    }

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
            new(nameof(ConversationSession.State), "None"),
            new(nameof(ConversationSession.LastUpdatedUtc), now.ToString("o"))
        };

        var tran = _redis.CreateTransaction();
        tran.HashSetAsync(SessionKey(newId), sessHash);
        tran.KeyExpireAsync(SessionKey(newId), _opts.SessionTtl);
        tran.StringSetAsync(idxKey, newId.ToString(), _opts.SessionTtl);
        await tran.ExecuteAsync();

        _logger.LogDebug("Created session {Sid} for phone {Phone}", newId, phoneNumber);

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

    public async Task SetStateAsync(Guid sessionId, string state)
    {
        var key = SessionKey(sessionId);
        await _redis.HashSetAsync(key, nameof(ConversationSession.State), state);
        await TouchAsync(sessionId);
    }

    public async Task<string> GetStateAsync(Guid sessionId)
    {
        var key = SessionKey(sessionId);
        var state = await _redis.HashGetAsync(key, nameof(ConversationSession.State));
        return state.IsNullOrEmpty ? "None" : state!;
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
        return new ConversationSession
        {
            SessionId = Guid.Parse(dict[nameof(ConversationSession.SessionId)]),
            UserId = Guid.Parse(dict[nameof(ConversationSession.UserId)]),
            PhoneNumber = dict[nameof(ConversationSession.PhoneNumber)],
            State = dict[nameof(ConversationSession.State)],
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
    }
}