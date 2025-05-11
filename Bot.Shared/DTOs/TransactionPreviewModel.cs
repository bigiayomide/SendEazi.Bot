namespace Bot.Shared.DTOs;

public class TransactionPreviewModel
{
    public string PayeeName { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public decimal NewBalance { get; set; }
    public DateTime Timestamp { get; set; }
}