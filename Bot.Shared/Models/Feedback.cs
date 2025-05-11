namespace Bot.Shared.Models;

public class Feedback
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public int Rating { get; set; }
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
}