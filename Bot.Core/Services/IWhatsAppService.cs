namespace Bot.Core.Services;

public interface IWhatsAppService
{
    Task<string> SendTextMessageAsync(string toPhoneNumber, string message);
    Task<string> SendMediaAsync(string toPhoneNumber, string mediaUrl, string caption = "");
    Task<string> SendQuickReplyAsync(string toPhoneNumber, string header, string body, string[] buttonLabels);
    Task<string> SendTemplateAsync(string toPhoneNumber, object template);
    Task<string> SendVoiceAsync(string toPhoneNumber, Stream audio);
    Task DeleteMessageAsync(string messageId);
}
