using Bot.Shared.Enums;

namespace Bot.Shared.DTOs;

public class SignupRequestDto
{
    public string PhoneNumber { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string NIN { get; set; } = null!;
    public string BVN { get; set; } = null!;
}

public class SignupResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public Guid? UserId { get; set; }
}

public class TransferRequestDto
{
    public Guid UserId { get; set; }
    public string AccountNumber { get; set; } = null!;
    public string BankCode { get; set; } = null!;
    public decimal Amount { get; set; }
    public Guid PayeeId { get; set; }
    public string Description { get; set; }
    public string AccountIdentifier { get; set; }
}

public class TransferResponseDto
{
    public bool Success { get; set; }
    public decimal? NewBalance { get; set; }
    public string Message { get; set; } = null!;
    public Guid TransactionId { get; set; }
    public string Reference { get; set; }
}

public class BalanceResponseDto
{
    public bool Success { get; set; }
    public decimal Balance { get; set; }
    public string Message { get; set; } = null!;
}

public class PinSetupRequestDto
{
    public Guid UserId { get; set; }
    public string Pin { get; set; } = null!;
}

public class PinSetupResponseDto : MessageResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
}

public class PinValidationRequestDto
{
    public Guid UserId { get; set; }
    public string Pin { get; set; } = null!;
}

public class PinValidationResponseDto
{
    public string Message { get; set; } = null!;
    public bool IsValid { get; set; }
}

public class SaveBeneficiaryRequestDto
{
    public Guid UserId { get; set; }
    public string AccountNumber { get; set; } = null!;
    public string BankCode { get; set; } = null!;
    public string? Nickname { get; set; }
}

public class SaveBeneficiaryResponseDto
{
    public bool Success { get; set; }
    public Guid PayeeId { get; set; }
    public string Message { get; set; } = null!;
}

public class BillPayRequestDto
{
    public Guid UserId { get; set; }
    public string BillerCode { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
}

public class BillPayResponseDto
{
    public bool Success { get; set; }
    public Guid? PaymentId { get; set; }
    public string Message { get; set; } = null!;
}

public class GoalsSetupRequestDto
{
    public Guid UserId { get; set; }
    public string Category { get; set; } = null!;
    public decimal MonthlyLimit { get; set; }

    // New: when does this goal start and end?
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; } // null = indefinite
}

public class GoalsSetupResponseDto
{
    public bool Success { get; set; }
    public Guid GoalId { get; set; }
    public string Message { get; set; } = null!;
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
}

public class RecurringSetupRequestDto
{
    public Guid UserId { get; set; }
    public Guid PayeeId { get; set; }
    public decimal Amount { get; set; }
    public string CronExpression { get; set; } = null!;
}

public class RecurringSetupResponseDto
{
    public bool Success { get; set; }
    public Guid RecurringId { get; set; }
    public string Message { get; set; } = null!;

    public DateTime NextRun { get; set; }
}

public class RecurringCancelRequestDto
{
    public Guid UserId { get; set; }
    public Guid RecurringId { get; set; }
}

public class RecurringCancelResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
}

public class RewardRequestDto
{
    public Guid UserId { get; set; }
    public RewardTypeEnum Type { get; set; }
}

public class RewardResponseDto
{
    public bool Success { get; set; }
    public Guid? RewardId { get; set; }
    public string Message { get; set; } = null!;
}

public class PersonalitySettingRequestDto
{
    public Guid UserId { get; set; }
    public PersonalityEnum Personality { get; set; }
}

public class PersonalitySettingResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
}

public class VoiceMessageRequestDto
{
    public Guid UserId { get; set; }
    public string PhoneNumber { get; set; } = null!;
    public Stream AudioStream { get; set; } = null!;
}

public class VoiceMessageResponseDto
{
    public string Text { get; set; } = null!;
    public string Language { get; set; } = null!;
}

public class FeedbackRequestDto
{
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class FeedbackResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
}

public class NudgeRequestDto
{
    public Guid UserId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime? ScheduledAt { get; set; }
}

public class NudgeResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public Guid NudgeId { get; set; }
}

public class MemoRequestDto
{
    public Guid TransactionId { get; set; }
    public string MemoText { get; set; } = null!;
    public string? ReceiptUrl { get; set; }
}

public class MemoResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
}

public class MessageRequestDto
{
    public Guid UserId { get; set; }
    public string PhoneNumber { get; set; } = null!;
    public string Text { get; set; } = null!;
}

public class MessageResponseDto
{
    public string Text { get; set; } = null!;
}