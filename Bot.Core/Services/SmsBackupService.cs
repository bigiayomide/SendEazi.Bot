// Bot.WebApi/Services/SmsBackupService.cs

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;

namespace Bot.Core.Services;

public interface ISmsBackupService
{
    /// <summary>
    ///     Sends a fallback SMS (e.g. Twilio) to the given phone number.
    /// </summary>
    Task SendSmsAsync(string toPhoneNumber, string message);
}

public class SmsOptions
{
    public string AccountSid { get; set; } = null!;
    public string AuthToken { get; set; } = null!;
    public string FromNumber { get; set; } = null!; // e.g. "+1XXX..."
}

public class SmsBackupService : ISmsBackupService
{
    private readonly ILogger<SmsBackupService> _logger;
    private readonly SmsOptions _opts;
    private readonly ITwilioMessageSender _twilio;

    public SmsBackupService(
        IOptions<SmsOptions> opts,
        ILogger<SmsBackupService> logger,
        ITwilioMessageSender twilio)
    {
        _opts = opts.Value;
        _logger = logger;
        _twilio = twilio;

        TwilioClient.Init(_opts.AccountSid, _opts.AuthToken);
    }

    public async Task SendSmsAsync(string toPhoneNumber, string message)
    {
        try
        {
            var sid = await _twilio.SendAsync(_opts.FromNumber, toPhoneNumber, message);
            _logger.LogInformation("SMS sent: SID={Sid}", sid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {Phone}", toPhoneNumber);
            throw;
        }
    }
}