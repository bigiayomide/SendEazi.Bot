using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Bot.Core.Services;

public interface ITwilioMessageSender
{
    Task<string> SendAsync(string from, string to, string body);
}

public class TwilioMessageSender : ITwilioMessageSender
{
    public async Task<string> SendAsync(string from, string to, string body)
    {
        var msg = await MessageResource.CreateAsync(body: body,
            from: new PhoneNumber(from),
            to: new PhoneNumber(to));
        return msg.Sid;
    }
}


