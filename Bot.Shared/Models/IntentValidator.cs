using System.Text.RegularExpressions;
using Bot.Shared.DTOs;
using Bot.Shared.Enums;

namespace Bot.Shared.Models;

public static class IntentValidator
{
    public static ValidationResult ValidateIntent(UserIntentDetected intent)
    {
        return intent.Intent switch
        {
            IntentType.Transfer => ValidateTransfer(intent.TransferPayload!),
            IntentType.Memo => ValidateMemo(intent.MemoPayload!),
            IntentType.BillPay => ValidateBill(intent.BillPayload!),
            IntentType.Signup => ValidateSignup(intent.SignupPayload!),
            IntentType.SetGoal => ValidateGoal(intent.GoalPayload!),
            IntentType.ScheduleRecurring => ValidateRecurring(intent.RecurringPayload!),
            IntentType.Feedback => ValidateFeedback(intent.FeedbackPayload!),
            IntentType.Greeting => ValidateGreeting(),
            IntentType.Unknown => ValidateUnknown(),
            _ => ValidationResult.Fail("❌ Unknown intent provided.")
        };
    }

    public static ValidationResult ValidateGreeting()
    {
        return ValidationResult.Fail(
            "👋 Hello! How can I assist you today? You can say things like 'check my balance', 'send money', or 'pay for electricity.'");
    }

    public static ValidationResult ValidateUnknown()
    {
        return ValidationResult.Fail(
            "❓ I'm not sure what you want to do. Please say something like 'check my balance' or 'send ₦5,000 to John.'");
    }

    public static ValidationResult ValidateMemo(MemoPayload payload)
    {
        var result = new ValidationResult();
        if (payload.TransactionId == Guid.Empty)
            result.AddError("🔍 Please specify the transaction you want to add a memo to.");
        if (string.IsNullOrWhiteSpace(payload.MemoText))
            result.AddError("📝 Please provide the memo text.");
        return result;
    }

    public static ValidationResult ValidateTransfer(TransferPayload payload)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(payload.ToAccount) || !Regex.IsMatch(payload.ToAccount, "^\\d{10}$"))
            result.AddError("🏦 Please provide a valid 10-digit recipient account number.");
        if (string.IsNullOrWhiteSpace(payload.BankCode) || !Regex.IsMatch(payload.BankCode, "^\\d{3,6}$"))
            result.AddError("🏦 Please provide a valid bank code (3 to 6 digits).");
        if (payload.Amount <= 0)
            result.AddError("💰 Please enter a valid amount to transfer.");
        return result;
    }

    public static ValidationResult ValidateBill(BillPayload payload)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(payload.BillerCode))
            result.AddError("📡 Please specify the biller code.");
        if (string.IsNullOrWhiteSpace(payload.CustomerRef))
            result.AddError("🔢 Please enter your customer reference number.");
        if (payload.Amount <= 0)
            result.AddError("💰 Please enter a valid amount to pay.");
        return result;
    }

    public static ValidationResult ValidateSignup(SignupPayload payload)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(payload.FullName))
            result.AddError("👤 What's your full name?");
        if (string.IsNullOrWhiteSpace(payload.Phone) || !Regex.IsMatch(payload.Phone, "^\\d{11}$"))
            result.AddError("📱 Please enter a valid 11-digit Nigerian phone number.");
        if (string.IsNullOrWhiteSpace(payload.NIN) || !Regex.IsMatch(payload.NIN, "^\\d{11}$"))
            result.AddError("🆔 Please provide a valid 11-digit NIN.");
        if (string.IsNullOrWhiteSpace(payload.BVN) || !Regex.IsMatch(payload.BVN, "^\\d{11}$"))
            result.AddError("🔐 Please provide a valid 11-digit BVN.");
        return result;
    }

    public static ValidationResult ValidateGoal(GoalPayload payload)
    {
        var result = new ValidationResult();
        if (payload.TargetAmount <= 0)
            result.AddError("🎯 Please set a valid target amount.");
        if (payload.Start >= payload.End)
            result.AddError("📅 End date must be after start date.");
        return result;
    }

    public static ValidationResult ValidateRecurring(RecurringPayload payload)
    {
        var result = new ValidationResult();
        if (payload.Transfer == null)
            result.AddError("📋 Please provide transfer details for the recurring schedule.");
        else
            result = ValidateTransfer(payload.Transfer);

        if (string.IsNullOrWhiteSpace(payload.Cron) || !Regex.IsMatch(payload.Cron,
                @"^([0-5]?\d)\s([0-5]?\d)\s([0-2]?\d|\*)\s([1-9]|1[0-2]|\*)\s([0-6]|\*)$"))
            result.AddError("⏱️ Please provide a valid CRON schedule in 5-field format (min hour day month week).");

        return result;
    }

    public static ValidationResult ValidateFeedback(FeedbackPayload payload)
    {
        var result = new ValidationResult();
        if (payload.Rating is < 1 or > 5)
            result.AddError("⭐ Rating must be between 1 and 5.");
        if (string.IsNullOrWhiteSpace(payload.Comment))
            result.AddError("💬 Please include a comment with your feedback.");
        return result;
    }
}

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; set; } = new();
    public string? FirstError => Errors.FirstOrDefault();

    public static ValidationResult Success()
    {
        return new ValidationResult();
    }

    public static ValidationResult Fail(params string[] errors)
    {
        return new ValidationResult { Errors = errors.ToList() };
    }

    public void AddError(string message)
    {
        Errors.Add(message);
    }
}