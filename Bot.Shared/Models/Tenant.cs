namespace Bot.Shared.Models;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}