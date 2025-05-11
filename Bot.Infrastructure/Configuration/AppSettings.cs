namespace Bot.Infrastructure.Configuration;

public class AppSettings
{
    public WhatsAppSettings WhatsApp { get; set; } = new();
    public SmsSettings Sms { get; set; } = new();
    public bool EphemeralModeEnabled { get; set; }
    public string RedisConnectionString { get; set; } = null!;
    public BillPaySettings BillPay { get; set; } = new();
}

public class WhatsAppSettings
{
    public string ApiUrl { get; set; } = null!;
    public string Token { get; set; } = null!;
}

public class SmsSettings
{
    public string ProviderUrl { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
    public string SenderId { get; set; } = null!;
}

public class BillPaySettings
{
    public string ProviderUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}