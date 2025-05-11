// Bot.Infrastructure.Configuration/AzureOpenAIOptions.cs

namespace Bot.Infrastructure.Configuration;

public class AzureOpenAiOptions
{
    /// <summary>Base endpoint, e.g. "https://my-resource.openai.azure.com/".</summary>
    public string Endpoint { get; set; } = null!;

    /// <summary>Name of your deployed model, e.g. "gpt-4".</summary>
    public string DeploymentName { get; set; } = "gpt-4";

    /// <summary>Temperature default for completions (0-1).</summary>
    public float Temperature { get; set; } = 0.2f;

    public string ApiKey { get; set; }
}