using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Bot.Core.Services;

public interface ITwilioMessageSender
{
    Task<string> SendAsync(string from, string to, string body);
}

public class TwilioMessageSender : ITwilioMessageSender
{
    private readonly ITwilioRestClient _client;

    public TwilioMessageSender(ITwilioRestClient client)
    {
        _client = client;
    }

    public async Task<string> SendAsync(string from, string to, string body)
    {
        var options = new CreateMessageOptions(new PhoneNumber(to))
        {
            From = new PhoneNumber(from),
            Body = body
        };

        var msg = await MessageResource.CreateAsync(options, _client);
        return msg.Sid;
    }
}