namespace Bot.Infrastructure.Configuration;

public class OnePipeOptions
{
    public string BaseUrl { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
    public string MerchantId { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
}