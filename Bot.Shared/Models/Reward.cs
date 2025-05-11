// Bot.Shared/Models/Reward.cs

using Bot.Shared.Enums;

namespace Bot.Shared.Models;

public class Reward
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public RewardTypeEnum Type { get; set; }
    public string? Details { get; set; }

    public DateTime GrantedAt { get; set; }
    public bool IsRedeemed { get; set; }
    public DateTime? RedeemedAt { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
}