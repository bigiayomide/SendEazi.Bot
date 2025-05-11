namespace Bot.Shared.Enums;

public enum TransactionStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2,
    Cancelled = 3
}


public enum BillerEnum
{
    DSTV,
    Electricity,
    Insurance,
    Water
}

public enum RewardTypeEnum
{
    Badge,
    Discount,
    Cashback,
    Streak, RecurringStreak, GoalAchieved
}

public enum PersonalityEnum
{
    Formal,
    Casual,
    Fun,
    Friendly
}

public enum DirectDebitStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2,
    Cancelled = 3,
    Retrying = 4
}