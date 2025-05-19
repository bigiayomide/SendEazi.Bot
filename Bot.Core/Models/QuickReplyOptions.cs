namespace Bot.Core.Models;

public class QuickReplyOptions
{
    // WhatsApp quick-reply buttons only allow up to three options. Limit the
    // stored favorites accordingly so that we never exceed that count when
    // generating suggestions for the user.
    public int MaxFavorites { get; set; } = 3;

    // Prefix used when storing favourite payees in Redis. Keeping it short
    // avoids wasting key space while still being descriptive.
    public string RedisKeyPrefix { get; set; } = "qr:";

    public List<string> DefaultTemplates { get; set; } = new()
    {
        // Fallback commands shown when the user has no recent payees.
        "Check balance",
        "Send money",
        "Help"
    };
}