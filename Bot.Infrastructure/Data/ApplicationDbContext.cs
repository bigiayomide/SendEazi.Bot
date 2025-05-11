using Bot.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bot.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<FeeRecord> FeeRecords { get; set; }
    public DbSet<Payee> Payees { get; set; }
    public DbSet<BillPayment> BillPayments { get; set; }
    public DbSet<BudgetGoal> BudgetGoals { get; set; }
    public DbSet<RecurringTransfer> RecurringTransfers { get; set; }
    public DbSet<Reward> Rewards { get; set; }
    public DbSet<PersonalitySetting> PersonalitySettings { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<TransactionMemo> TransactionMemos { get; set; }
    public DbSet<Nudge> Nudges { get; set; }
    public DbSet<LinkedBankAccount> LinkedBankAccounts { get; set; }
    public DbSet<DirectDebitMandate>  DirectDebitMandates { get; set; }
}