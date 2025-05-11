namespace Bot.Infrastructure.Configuration;

public class PromptSettings
{
    public string IntentExtractionPath { get; set; } = null!;
    public string ConfirmationPath { get; set; } = null!;
    public string FallbackPath { get; set; } = null!;
    public string DeploymentName { get; set; }
}