namespace Bot.Core.Models;

public class WebhookRequest
{
    public Guid UserId { get; set; }
    public string From { get; set; }
    public string Text { get; set; }
}