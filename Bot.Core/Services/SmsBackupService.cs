// Bot.WebApi/Services/SmsBackupService.cs

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

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

    public SmsBackupService(
        IOptions<SmsOptions> opts,
        ILogger<SmsBackupService> logger)
    {
        _opts = opts.Value;
        _logger = logger;

        TwilioClient.Init(_opts.AccountSid, _opts.AuthToken);
    }

    public async Task SendSmsAsync(string toPhoneNumber, string message)
    {
        try
        {
            var msg = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(_opts.FromNumber),
                to: new PhoneNumber(toPhoneNumber)
            );
            _logger.LogInformation("SMS sent: SID={Sid}", msg.Sid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {Phone}", toPhoneNumber);
            throw;
        }
    }
}