namespace Bot.Shared.Models;

public class PinRecord
{
    public Guid UserId { get; set; }
    public string PinHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}