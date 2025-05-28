using Bot.Shared.Enums;

namespace Bot.Core.Models;

public class ConversationSession
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string PhoneNumber { get; set; } = null!;
    public ConversationState State { get; set; } = ConversationState.None;
    public string? LastMessage { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}
