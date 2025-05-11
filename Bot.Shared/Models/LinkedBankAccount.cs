// Bot.Shared.Models/LinkedBankAccount.cs

namespace Bot.Shared.Models;

/// <summary>
///     One row per user ↔︎ external-bank link (Mono, OnePipe, etc.).
/// </summary>
// Bot.Shared.Models/LinkedBankAccount.cs
public class LinkedBankAccount
{
    public Guid   Id        { get; set; }
    public Guid   UserId    { get; set; }
    public string Provider  { get; set; } = null!; // "Mono" or "OnePipe"
    public string AccountName { get; set; } = null!;
    public string AccountNumberEnc { get; set; } = null!; // encrypted
    public string AccountHash      { get; set; } = null!; // SHA-256
    public string BankCode         { get; set; } = null!;
    public string? ProviderCustomerId { get; set; } // e.g. Mono customer ID
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}
