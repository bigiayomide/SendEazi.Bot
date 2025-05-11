// Bot.Shared/Models/Nudge.cs

namespace Bot.Shared.Models;

public class Nudge
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Content { get; set; } = null!;
    public DateTime ScheduledAt { get; set; }
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
}