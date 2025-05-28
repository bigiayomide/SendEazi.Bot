namespace Bot.Core.Services;

public class WhatsAppOptions
{
    public string BaseUrl { get; set; } = "https://graph.facebook.com/v18.0";
    public string PhoneNumberId { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public TimeSpan EphemeralTtl { get; set; } = TimeSpan.FromMinutes(5);
}
