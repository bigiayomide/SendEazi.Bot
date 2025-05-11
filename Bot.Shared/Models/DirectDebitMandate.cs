namespace Bot.Shared.Models;

public class DirectDebitMandate
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = null!;
    public string MandateReference { get; set; } = null!;
    public string? MandateId { get; set; }
    public string? CustomerId { get; set; }
    public decimal MaxAmount { get; set; }
    public string MandateType { get; set; } = "variable";
    public string AuthMethod { get; set; } = "emandate";
    public string Status { get; set; } = "pending";
    public string? TransferDestinationBank { get; set; }
    public bool IsRevoked { get; set; }

    public string? TransferDestinationAccount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}