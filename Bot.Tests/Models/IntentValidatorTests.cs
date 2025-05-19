using Bot.Shared.Models;
using Bot.Shared.Enums;
using Bot.Shared.DTOs;
using Bot.Shared;
using FluentAssertions;

namespace Bot.Tests.Models;

public class IntentValidatorTests
{
    [Fact]
    public void ValidateGreeting_Should_ReturnExpectedMessage()
    {
        var result = IntentValidator.ValidateGreeting();

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Be("\uD83D\uDC4B Hello! How can I assist you today? You can say things like 'check my balance', 'send money', or 'pay for electricity.'");
    }

    [Fact]
    public void ValidateUnknown_Should_ReturnExpectedMessage()
    {
        var result = IntentValidator.ValidateUnknown();

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Be("\u2753 I'm not sure what you want to do. Please say something like 'check my balance' or 'send \u20A65,000 to John.'");
    }

    [Fact]
    public void ValidateMemo_Should_ReturnValid_ForProperInput()
    {
        var payload = new MemoPayload(Guid.NewGuid(), "note", null);

        var result = IntentValidator.ValidateMemo(payload);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateMemo_Should_ReturnErrors_ForMissingFields()
    {
        var payload = new MemoPayload(Guid.Empty, "", null);

        var result = IntentValidator.ValidateMemo(payload);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("\uD83D\uDD0D Please specify the transaction you want to add a memo to.")
            .And.Contain("\uD83D\uDCDD Please provide the memo text.");
    }

    [Fact]
    public void ValidateTransfer_Should_ReturnValid_ForProperInput()
    {
        var payload = new TransferPayload("1234567890", "044", 1000, "test");

        var result = IntentValidator.ValidateTransfer(payload);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateTransfer_Should_ReturnErrors_ForInvalidFields()
    {
        var payload = new TransferPayload("", "", -5, null);

        var result = IntentValidator.ValidateTransfer(payload);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("\uD83C\uDFE6 Please provide a valid 10-digit recipient account number.")
            .And.Contain("\uD83C\uDFE6 Please provide a valid bank code (3 to 6 digits).")
            .And.Contain("\uD83D\uDCB0 Please enter a valid amount to transfer.");
    }

    [Fact]
    public void ValidateBill_Should_ReturnValid_ForProperInput()
    {
        var payload = new BillPayload("biller", "cust", 100, "name");

        var result = IntentValidator.ValidateBill(payload);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateBill_Should_ReturnErrors_ForInvalidFields()
    {
        var payload = new BillPayload("", "", 0, null);

        var result = IntentValidator.ValidateBill(payload);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("\uD83D\uDCF1 Please specify the biller code.")
            .And.Contain("\uD83D\uDD17 Please enter your customer reference number.")
            .And.Contain("\uD83D\uDCB0 Please enter a valid amount to pay.");
    }

    [Fact]
    public void ValidateSignup_Should_ReturnValid_ForProperInput()
    {
        var payload = new SignupPayload("John Doe", "08012345678", "12345678901", "10987654321");

        var result = IntentValidator.ValidateSignup(payload);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSignup_Should_ReturnErrors_ForInvalidFields()
    {
        var payload = new SignupPayload("", "", "", "");

        var result = IntentValidator.ValidateSignup(payload);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("\uD83D\uDC64 What's your full name?")
            .And.Contain("\uD83D\uDCF1 Please enter a valid 11-digit Nigerian phone number.")
            .And.Contain("\uD83C\uDD94 Please provide a valid 11-digit NIN.")
            .And.Contain("\uD83D\uDD10 Please provide a valid 11-digit BVN.");
    }

    [Fact]
    public void ValidateGoal_Should_ReturnValid_ForProperInput()
    {
        var start = DateOnly.FromDateTime(DateTime.Today);
        var payload = new GoalPayload(Guid.NewGuid(), 1000, start, start.AddDays(1));

        var result = IntentValidator.ValidateGoal(payload);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateGoal_Should_ReturnErrors_ForInvalidFields()
    {
        var start = DateOnly.FromDateTime(DateTime.Today);
        var payload = new GoalPayload(Guid.NewGuid(), -1, start.AddDays(1), start);

        var result = IntentValidator.ValidateGoal(payload);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("\uD83C\uDFAF Please set a valid target amount.")
            .And.Contain("\uD83D\uDCC5 End date must be after start date.");
    }

    [Fact]
    public void ValidateRecurring_Should_ReturnValid_ForProperInput()
    {
        var transfer = new TransferPayload("1234567890", "044", 1000, null);
        var payload = new RecurringPayload(Guid.NewGuid(), transfer, "0 9 * * *");

        var result = IntentValidator.ValidateRecurring(payload);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRecurring_Should_ReturnErrors_ForInvalidFields()
    {
        var payload = new RecurringPayload(Guid.NewGuid(), null!, "bad cron");

        var result = IntentValidator.ValidateRecurring(payload);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("\uD83D\uDCCB Please provide transfer details for the recurring schedule.")
            .And.Contain("\u23F1\uFE0F Please provide a valid CRON schedule in 5-field format (min hour day month week).");
    }

    [Fact]
    public void ValidateFeedback_Should_ReturnValid_ForProperInput()
    {
        var payload = new FeedbackPayload(5, "great");

        var result = IntentValidator.ValidateFeedback(payload);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateFeedback_Should_ReturnErrors_ForInvalidFields()
    {
        var payload = new FeedbackPayload(0, "");

        var result = IntentValidator.ValidateFeedback(payload);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("\u2B50 Rating must be between 1 and 5.")
            .And.Contain("\uD83D\uDCAC Please include a comment with your feedback.");
    }

    [Fact]
    public void ValidateIntent_Should_ReturnErrors_ForUnknownIntent()
    {
        var intent = new UserIntentDetected(Guid.NewGuid(), IntentType.Unknown);

        var result = IntentValidator.ValidateIntent(intent);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Be("\u2757 Unknown intent provided.");
    }

    [Fact]
    public void ValidateIntent_Should_Use_SpecificValidator()
    {
        var payload = new TransferPayload("1234567890", "044", 1000, null);
        var intent = new UserIntentDetected(Guid.NewGuid(), IntentType.Transfer, TransferPayload: payload);

        var result = IntentValidator.ValidateIntent(intent);

        result.IsValid.Should().BeTrue();
    }
}
