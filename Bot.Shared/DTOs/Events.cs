using Bot.Shared.Enums;

namespace Bot.Shared.DTOs;

/* onboarding */
public record SignupSucceeded(Guid CorrelationId, Guid UserId);

public record SignupFailed(Guid CorrelationId, string Reason);

public record KycApproved(Guid CorrelationId);

public record KycRejected(Guid CorrelationId, string Reason);

public record BankLinkSucceeded(Guid CorrelationId);

public record BankLinkFailed(Guid CorrelationId, string Reason);

public record PinSet(Guid CorrelationId);

public record PinSetupFailed(Guid CorrelationId, string Reason);

public record PinValidated(Guid CorrelationId);

public record PinInvalid(Guid CorrelationId, string Reason);

/* intent */
public record UserIntentDetected(
    Guid CorrelationId,
    Bot.Shared.Enums.IntentType Intent,
    TransferPayload? TransferPayload = null,
    BillPayload? BillPayload = null,
    GoalPayload? GoalPayload = null,
    RecurringPayload? RecurringPayload = null,
    MemoPayload? MemoPayload = null,
    FeedbackPayload? FeedbackPayload = null,
    SignupPayload? SignupPayload = null,
    GreetingPayload? GreetingPayload = null,
    UnknownPayload UnknownPayload = null,
    string? PhoneNumber = null);

/* money */
public record TransferCompleted(Guid CorrelationId, string Reference);

public record TransferRequested(Guid CorrelationId, TransferPayload Payload, Guid? BankAccountId = null);

public record TransferFailed(Guid CorrelationId, string Reason, string Reference);


public record BillPaid(
    Guid CorrelationId,
    Guid BillId,
    decimal Amount,
    string BillerName);

public record BillPayFailed(Guid CorrelationId, string Reason);

/* balance */
public record BalanceSent(Guid CorrelationId, decimal Amount);

/* recurring */
public record RecurringExecuted(Guid CorrelationId, Guid RecurringId);

public record RecurringFailed(Guid CorrelationId, string Reason);

public record RecurringCancelled(Guid CorrelationId, Guid RecurringId);

/* goals */
public record BudgetAlertTriggered(
    Guid CorrelationId,
    Guid GoalId,
    decimal PercentSpent);

/* ux */
public record RewardIssued(Guid CorrelationId, RewardTypeEnum RewardType);

public record NudgeSent(Guid CorrelationId, NudgeType NudgeType);

public record QuickReplySent(Guid CorrelationId, string TemplateName);

public record PreviewSent(Guid CorrelationId);

/* misc */
public record MemoSaved(Guid CorrelationId, Guid MemoId);

public record MemoSaveFailed(Guid CorrelationId, string Reason);

public record FeedbackLogged(Guid CorrelationId, Guid FeedbackId);

public record FullNameProvided(Guid CorrelationId, string FullName);

public record NinProvided(Guid CorrelationId, string NIN);

public record BvnProvided(Guid CorrelationId, string BVN);

public record SignupAborted(Guid CorrelationId, string Reason);

public record NinVerified(Guid CorrelationId, string Nin);

public record NinRejected(Guid CorrelationId, string Reason);

public record BvnVerified(Guid CorrelationId, string Bvn);

public record BvnRejected(Guid CorrelationId, string Reason);

public record MandateReadyToDebit(Guid CorrelationId, string MandateId, string Provider);

public record VoiceMessageTranscribed(Guid CorrelationId, string Text, string Language, string PhoneNumber);

public record OcrResultAvailable(Guid CorrelationId, string ExtractedText, string PhoneNumber);

public record VoiceReplyReady(Guid CorrelationId, Stream AudioStream);