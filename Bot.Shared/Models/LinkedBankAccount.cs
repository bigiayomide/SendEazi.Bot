namespace Bot.Shared.Models;

public class LinkedBankAccount
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = null!;
    public string AccountName { get; set; } = null!;
    public string AccountNumberEnc { get; set; } = null!;
    public string AccountHash { get; set; } = null!;
    public string BankCode { get; set; } = null!;
    public string? ProviderCustomerId { get; set; }
    public bool IsDefault { get; set; } = false; // NEW
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}