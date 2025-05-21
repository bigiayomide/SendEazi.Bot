// Bot.Messages.Shared/Payloads.cs

namespace Bot.Shared;

public record TransferPayload(
    string ToAccount,
    string BankCode,
    decimal Amount,
    string? Description);

public record BillPayload(
    string BillerCode,
    string CustomerRef,
    decimal Amount,
    string? BillerName);

public record GoalPayload(
    Guid GoalId,
    decimal TargetAmount,
    DateOnly Start,
    DateOnly End);

public record RecurringPayload(
    Guid RecurringId,
    TransferPayload Transfer,
    string Cron); // e.g. "0 9 * * *"

public record MemoPayload(Guid TransactionId, string MemoText, string? ReceiptUrl);

public record FeedbackPayload(int Rating, string Comment);

public record SignupPayload(string FullName, string Phone, string NIN, string BVN);

public record GreetingPayload(string Message);

public record UnknownPayload(string Message);
