namespace Bot.Shared.Models;

public class Payee
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string AccountNumber { get; set; } = null!;
    public string BankCode { get; set; } = null!;
    public string? Nickname { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
}