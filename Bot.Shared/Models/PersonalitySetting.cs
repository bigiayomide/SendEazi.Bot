// Bot.Shared/Models/PersonalitySetting.cs

using Bot.Shared.Enums;

namespace Bot.Shared.Models;

public class PersonalitySetting
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public PersonalityEnum Personality { get; set; }
    public DateTime SetAt { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
}