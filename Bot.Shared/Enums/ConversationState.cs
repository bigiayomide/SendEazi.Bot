namespace Bot.Shared.Enums;

public enum ConversationState
{
    None,
    AskFullName,
    AskNin,
    NinValidating,
    AskBvn,
    BvnValidating,
    AwaitingKyc,
    AwaitingBankLink,
    AwaitingPinSetup,
    AwaitingPinValidate,
    Ready
}
