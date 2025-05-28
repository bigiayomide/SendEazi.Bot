namespace Bot.Core.Services;

public class ConversationStateOptions
{
    public string RedisKeyPrefix { get; set; } = "session:";
    public TimeSpan SessionTtl { get; set; } = TimeSpan.FromHours(24);
}
