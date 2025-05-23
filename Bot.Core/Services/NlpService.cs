using System.Text.Json;
using Azure.AI.OpenAI;
using Bot.Infrastructure.Configuration;
using Bot.Shared;
using Bot.Shared.DTOs;
using Bot.Shared.Enums;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Bot.Core.Services;

public interface INlpService
{
    Task<UserIntentDetected> DetectIntentAsync(Guid correlationId, string rawText, string phoneNumber);
}

public interface IChatClientWrapper
{
    Task<string> CompleteChatAsync(List<ChatMessage> messages, ChatCompletionOptions options);
}

public class ChatClientWrapper(AzureOpenAIClient chatClient, IOptions<AzureOpenAiOptions> openAiOptions)
    : IChatClientWrapper
{
    public async Task<string> CompleteChatAsync(List<ChatMessage> messages, ChatCompletionOptions options)
    {
        var response = await chatClient.GetChatClient(openAiOptions.Value.DeploymentName)
            .CompleteChatAsync(messages, options);
        return response.Value.Content.First().Text;
    }
}

public class NlpService(
    IChatClientWrapper chatClient,
    IOptions<PromptSettings> opts,
    IReferenceGenerator referenceGenerator)
    : INlpService
{
    private readonly PromptSettings _prompts = opts.Value;

    public async Task<UserIntentDetected> DetectIntentAsync(Guid correlationId, string rawText, string phoneNumber)
    {
        try
        {
            var promptTemplate = await File.ReadAllTextAsync(_prompts.IntentExtractionPath);
            var fullPrompt = promptTemplate.Replace("{message}", rawText);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(promptTemplate),
                new UserChatMessage(fullPrompt)
            };

            var options = new ChatCompletionOptions { MaxOutputTokenCount = 300 };
            var response = await chatClient.CompleteChatAsync(messages, options);

            var json = response.Trim();
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var intentStr = root.TryGetProperty("intent", out var p) ? p.GetString() : "unknown";
            var intent = intentStr switch
            {
                "transfer" => IntentType.Transfer,
                "billpay" => IntentType.BillPay,
                "set_goal" => IntentType.SetGoal,
                "schedule_recurring" => IntentType.ScheduleRecurring,
                "memo" => IntentType.Memo,
                "feedback" => IntentType.Feedback,
                "signup" => IntentType.Signup,
                "greeting" => IntentType.Greeting,
                _ => IntentType.Unknown
            };

            var result = new UserIntentDetected(
                correlationId,
                intent,
                intent == IntentType.Transfer && root.TryGetProperty("toAccount", out _)
                    ? new TransferPayload(
                        root.GetProperty("toAccount").GetString()!,
                        root.GetProperty("bankCode").GetString()!,
                        root.GetProperty("amount").GetDecimal(),
                        root.TryGetProperty("description", out var d) ? d.GetString() : null)
                    : null,
                intent == IntentType.BillPay && root.TryGetProperty("billerCode", out _)
                    ? new BillPayload(
                        root.GetProperty("billerCode").GetString()!,
                        root.GetProperty("customerRef").GetString()!,
                        root.GetProperty("amount").GetDecimal(),
                        root.TryGetProperty("billerName", out var b) ? b.GetString() : null)
                    : null,
                intent == IntentType.SetGoal && root.TryGetProperty("monthlyLimit", out _)
                    ? new GoalPayload(
                        Guid.Empty,
                        root.GetProperty("monthlyLimit").GetDecimal(),
                        DateOnly.FromDateTime(DateTime.UtcNow),
                        DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)))
                    : null,
                intent == IntentType.ScheduleRecurring && root.TryGetProperty("toAccount", out _)
                    ? new RecurringPayload(
                        Guid.NewGuid(),
                        new TransferPayload(
                            root.GetProperty("toAccount").GetString()!,
                            root.GetProperty("bankCode").GetString()!,
                            root.GetProperty("amount").GetDecimal(),
                            root.TryGetProperty("description", out var r) ? r.GetString() : null),
                        root.GetProperty("cron").GetString()!)
                    : null,
                intent == IntentType.Memo && root.TryGetProperty("memoText", out _)
                    ? new MemoPayload(
                        root.GetProperty("transactionId").GetGuid(),
                        root.GetProperty("memoText").GetString()!,
                        root.TryGetProperty("receiptUrl", out var u) ? u.GetString() : null)
                    : null,
                intent == IntentType.Feedback && root.TryGetProperty("rating", out _)
                    ? new FeedbackPayload(
                        root.GetProperty("rating").GetInt32(),
                        root.GetProperty("comment").GetString()!)
                    : null,
                intent == IntentType.Signup && root.TryGetProperty("fullName", out _)
                    ? new SignupPayload(
                        root.GetProperty("fullName").GetString()!,
                        root.GetProperty("phone").GetString()!,
                        root.GetProperty("nin").GetString()!,
                        root.GetProperty("bvn").GetString()!)
                    : null,
                intent == IntentType.Greeting ? null : null,
                intent == IntentType.Unknown ? null : null,
                phoneNumber
            );

            return result;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return new UserIntentDetected(correlationId, IntentType.Unknown);
        }
    }
}