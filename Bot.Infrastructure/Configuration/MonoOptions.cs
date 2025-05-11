namespace Bot.Infrastructure.Configuration;

public class MonoOptions
{
    public string BaseUrl { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    public string BusinessSubAccountId { get; set; } = null!;
}