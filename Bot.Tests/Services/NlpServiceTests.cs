using System.Text.Json;
using Bot.Core.Services;
using Bot.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using OpenAI.Chat;

namespace Bot.Tests.Services;

public class NlpServiceTests : IDisposable
{
    private readonly string _promptFilePath;

    public NlpServiceTests()
    {
        _promptFilePath = Path.GetTempFileName();
        File.WriteAllText(_promptFilePath, "Prompt: {message}");
    }

    public void Dispose()
    {
        if (File.Exists(_promptFilePath))
            File.Delete(_promptFilePath);
    }

    private NlpService CreateService(string jsonResponse)
    {
        var mockWrapper = new Mock<IChatClientWrapper>();
        mockWrapper
            .Setup(x => x.CompleteChatAsync(It.IsAny<List<ChatMessage>>(), It.IsAny<ChatCompletionOptions>()))
            .ReturnsAsync(jsonResponse);

        var opts = Options.Create(new PromptSettings
        {
            DeploymentName = "gpt-mock",
            IntentExtractionPath = _promptFilePath
        });

        return new NlpService(mockWrapper.Object, opts, new ReferenceGenerator());
    }

    [Fact]
    public async Task Should_Parse_Transfer_Intent()
    {
        var json = """
                   {
                       "intent": "transfer",
                       "toAccount": "1234567890",
                       "bankCode": "058",
                       "amount": 1000,
                       "description": "Rent"
                   }
                   """;

        var service = CreateService(json);
        var result = await service.DetectIntentAsync(Guid.NewGuid(), "transfer rent", phoneNumber:"+2349043844315");

        result.Intent.Should().Be(Shared.Enums.IntentType.Transfer);
        result.TransferPayload!.ToAccount.Should().Be("1234567890");
        result.TransferPayload.BankCode.Should().Be("058");
        result.TransferPayload.Amount.Should().Be(1000);
        result.TransferPayload.Description.Should().Be("Rent");
    }

    [Fact]
    public async Task Should_Parse_BillPay_Intent()
    {
        var json = """
                   {
                       "intent": "billpay",
                       "billerCode": "DSTV",
                       "customerRef": "00112233",
                       "amount": 5500
                   }
                   """;

        var service = CreateService(json);
        var result = await service.DetectIntentAsync(Guid.NewGuid(), "pay DSTV", "+2349043844315");

        result.Intent.Should().Be(Shared.Enums.IntentType.BillPay);
        result.BillPayload!.BillerCode.Should().Be("DSTV");
        result.BillPayload.CustomerRef.Should().Be("00112233");
        result.BillPayload.Amount.Should().Be(5500);
    }

    [Fact]
    public async Task Should_Parse_Signup_Intent()
    {
        var json = """
                   {
                       "intent": "signup",
                       "fullName": "Jane Doe",
                       "phone": "+2348123456789",
                       "nin": "12345678901",
                       "bvn": "22222222222"
                   }
                   """;

        var service = CreateService(json);
        var result = await service.DetectIntentAsync(Guid.NewGuid(), "sign me up", "+2349043844315");

        result.Intent.Should().Be(Shared.Enums.IntentType.Signup);
        result.SignupPayload!.FullName.Should().Be("Jane Doe");
        result.SignupPayload.Phone.Should().Be("+2348123456789");
        result.SignupPayload.NIN.Should().Be("12345678901");
        result.SignupPayload.BVN.Should().Be("22222222222");
    }

    [Fact]
    public async Task Should_Parse_Feedback_Intent()
    {
        var json = """
                   {
                       "intent": "feedback",
                       "rating": 5,
                       "comment": "Great job"
                   }
                   """;

        var service = CreateService(json);
        var result = await service.DetectIntentAsync(Guid.NewGuid(), "feedback", "+2349043844315");

        result.Intent.Should().Be(Shared.Enums.IntentType.Feedback);
        result.FeedbackPayload!.Rating.Should().Be(5);
        result.FeedbackPayload.Comment.Should().Be("Great job");
    }

    [Fact]
    public async Task Should_Parse_Memo_Intent()
    {
        var txId = Guid.NewGuid();
        var json = $$"""
                     {
                         "intent": "memo",
                         "transactionId": "{{txId}}",
                         "memoText": "January rent"
                     }
                     """;

        var service = CreateService(json);
        var result = await service.DetectIntentAsync(Guid.NewGuid(), "add memo", "+2349043844315");

        result.Intent.Should().Be(Shared.Enums.IntentType.Memo);
        result.MemoPayload!.TransactionId.Should().Be(txId);
        result.MemoPayload.MemoText.Should().Be("January rent");
    }

    [Fact]
    public async Task Should_Handle_Unknown_Intent()
    {
        var json = "{}";

        var service = CreateService(json);
        var result = await service.DetectIntentAsync(Guid.NewGuid(), "bla bla", "+2349043844315");

        result.Intent.Should().Be(Shared.Enums.IntentType.Unknown);
    }

    [Fact]
    public async Task Should_Throw_On_Bad_Json()
    {
        const string badJson = "not-a-json";

        var mockWrapper = new Mock<IChatClientWrapper>();
        mockWrapper
            .Setup(x => x.CompleteChatAsync(It.IsAny<List<ChatMessage>>(), It.IsAny<ChatCompletionOptions>()))
            .ReturnsAsync(badJson);

        var opts = Options.Create(new PromptSettings
        {
            DeploymentName = "gpt-mock",
            IntentExtractionPath = _promptFilePath
        });

        var service = new NlpService(mockWrapper.Object, opts, new ReferenceGenerator());

        Func<Task> act = async () => await service.DetectIntentAsync(Guid.NewGuid(), "bad", "+2349043844315");

        await act.Should().ThrowAsync<JsonException>();
    }
}
