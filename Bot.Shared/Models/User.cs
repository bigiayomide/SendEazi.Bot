namespace Bot.Shared.Models;

public class User
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string? FullName { get; set; }

    public string PhoneNumber { get; set; } = null!;
    public string? PinHash { get; set; }
    
    public string NINEnc { get; set; }
    public string BVNEnc { get; set; }
    public string NINHash { get; set; }
    public string BVNHash { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string BankAccessToken { get; set; }
    public string SignupSource { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<Payee> Payees { get; set; } = new List<Payee>();
    public virtual ICollection<BillPayment> BillPayments { get; set; } = new List<BillPayment>();
    public virtual ICollection<BudgetGoal> BudgetGoals { get; set; } = new List<BudgetGoal>();
    public virtual ICollection<RecurringTransfer> RecurringTransfers { get; set; } = new List<RecurringTransfer>();
    public virtual ICollection<Reward> Rewards { get; set; } = new List<Reward>();
    public virtual ICollection<PersonalitySetting> PersonalitySettings { get; set; } = new List<PersonalitySetting>();
    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    public virtual ICollection<Nudge> Nudges { get; set; } = new List<Nudge>();
    public List<LinkedBankAccount> LinkedAccounts { get; set; }
}