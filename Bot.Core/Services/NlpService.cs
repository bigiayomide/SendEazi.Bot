using System.Text.Json;
using Bot.Core.StateMachine;
using Bot.Infrastructure.Configuration;
using Bot.Shared;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace Bot.Core.Services;

public interface INlpService
{
    Task<UserIntentDetected> DetectIntentAsync(Guid correlationId, string rawText);
}

public class NlpService(OpenAIClient client, IOptions<PromptSettings> opts, IReferenceGenerator referenceGenerator)
    : INlpService
{
    private readonly PromptSettings _prompts = opts.Value;

    public async Task<UserIntentDetected> DetectIntentAsync(Guid correlationId, string rawText)
    {
        var prompt = await File.ReadAllTextAsync(_prompts.IntentExtractionPath);
        var fullPrompt = prompt.Replace("{message}", rawText);

        var chatClient = client.GetChatClient(_prompts.DeploymentName);
        var options = new ChatCompletionOptions { MaxOutputTokenCount = 300 };
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(prompt),
            new UserChatMessage(fullPrompt)
        };

        var response = await chatClient.CompleteChatAsync(messages, options);

        var json = response.Value.Content[0].Text.Trim();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var intent = root.GetProperty("intent").GetString() ?? "unknown";

        return new UserIntentDetected(
            correlationId,
            intent,
            intent == "transfer"
                ? new TransferPayload(
                    root.GetProperty("toAccount").GetString()!,
                    root.GetProperty("bankCode").GetString()!,
                    root.GetProperty("amount").GetDecimal(),
                    root.TryGetProperty("description", out var d) ? d.GetString() : null
                )
                : null,
            intent == "billpay"
                ? new BillPayload(
                    root.GetProperty("billerCode").GetString()!,
                    root.GetProperty("customerRef").GetString()!,
                    root.GetProperty("amount").GetDecimal(),
                    root.TryGetProperty("billerName", out var b) ? b.GetString() : null
                )
                : null,
            //TODO: its wrong
            intent == "set_goal"
                ? new GoalPayload(
                    Guid.Empty,
                    // root.GetProperty("category").GetString()!,
                    root.GetProperty("monthlyLimit").GetDecimal(),
                    DateOnly.FromDateTime(DateTime.UtcNow),
                    DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1))
                )
                : null,
            intent == "schedule_recurring"
                ? new RecurringPayload(
                    Guid.NewGuid(),
                    new TransferPayload(
                        root.GetProperty("toAccount").GetString()!,
                        root.GetProperty("bankCode").GetString()!,
                        root.GetProperty("amount").GetDecimal(),
                        root.TryGetProperty("description", out var r) ? r.GetString() : null
                    ),
                    root.GetProperty("cron").GetString()!
                )
                : null,
            intent == "memo"
                ? new MemoPayload(
                    root.GetProperty("transactionId").GetGuid(),
                    root.GetProperty("memoText").GetString()!,
                    root.TryGetProperty("receiptUrl", out var u) ? u.GetString() : null
                )
                : null,
            intent == "feedback"
                ? new FeedbackPayload(
                    root.GetProperty("rating").GetInt32(),
                    root.GetProperty("comment").GetString()!
                )
                : null,
            intent == "signup"
                ? new SignupPayload(
                    root.GetProperty("fullName").GetString()!,
                    root.GetProperty("phone").GetString()!,
                    root.GetProperty("nin").GetString()!,
                    root.GetProperty("bvn").GetString()!
                )
                : null
        );
    }
}