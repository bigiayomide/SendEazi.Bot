namespace Bot.Core.Models;

public class QuickReplyOptions
{
    public int MaxFavorites { get; set; } = 5;
    public string RedisKeyPrefix { get; set; } = "qr:";

    public List<string> DefaultTemplates { get; set; } = new()
    {
        "Check balance",
        "Send money",
        "Recent transactions"
    };
}