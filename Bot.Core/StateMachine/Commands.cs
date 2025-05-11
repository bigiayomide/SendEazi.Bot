using Bot.Shared;
using Bot.Shared.Enums;

namespace Bot.Core.StateMachine;

// inbound & NLP
public record RawInboundMsgCmd(Guid CorrelationId, string Phone, string Text, string MessageId);

// onboarding
// Bot.Messages.Commands/SignupCmd.cs
public record SignupCmd(Guid CorrelationId, SignupPayload Payload);
public record KycCmd(Guid CorrelationId);
public record BankLinkCmd(Guid CorrelationId);
public record PinSetupCmd(Guid CorrelationId, string PinHash);
public record PinValidationCmd(Guid CorrelationId, string Pin);

// money
public record TransferCmd(Guid CorrelationId, TransferPayload Payload);
public record BillPayCmd(Guid CorrelationId, BillPayload Payload);
public record BalanceCmd(Guid CorrelationId);

// goals / recurring
public record GoalsCmd(Guid CorrelationId, GoalPayload Payload);
public record RecurringCmd(Guid CorrelationId, RecurringPayload Payload);
public record RecurringCancelCmd(Guid CorrelationId, Guid RecurringId);

// ux / extras
public record RewardCmd(Guid CorrelationId, RewardTypeEnum RewardType);
public record NudgeCmd(Guid CorrelationId, NudgeType NudgeType, string? Text = null);
public record QuickReplyCmd(Guid CorrelationId, string TemplateName, string[] Args);
public record PreviewCmd(Guid CorrelationId, Guid? TransactionId = null, Guid? BillId = null);

// misc
public record MemoCmd(Guid CorrelationId, MemoPayload Payload);
public record FeedbackCmd(Guid CorrelationId, FeedbackPayload Payload);

public record PromptFullNameCmd(Guid CorrelationId);
public record PromptNinCmd     (Guid CorrelationId);
public record PromptBvnCmd     (Guid CorrelationId);
public record ValidateNinCmd(Guid CorrelationId, string Nin);
public record ValidateBvnCmd(Guid CorrelationId, string Bvn);
public record StartMandateSetupCmd( Guid CorrelationId, string FullName, string Phone, string BVN);
    
public record VoiceMessageCmd(Guid CorrelationId, string FileUrl, string LanguageHint = "auto");
public record ImageUploadedCmd(Guid CorrelationId, string FileUrl, string? Hint = null);
public record RespondWithVoiceCmd(Guid CorrelationId, string Text, string? Language = "en");
