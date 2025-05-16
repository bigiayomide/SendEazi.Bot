// Bot.Core.Services/NotificationService.cs

using Bot.Core.Models;
using Microsoft.Extensions.Logging;

namespace Bot.Core.Services;

public interface INotificationService
{
    /// <summary>
    ///     Sends a budget alert to the given user, using the channel(s)
    ///     configured in your app (WhatsApp, SMS, email, etc.).
    /// </summary>
    Task SendBudgetAlertAsync(Guid userId, BudgetAlert alert);
}

public class NotificationService(
    IUserService userService,
    ITemplateRenderingService templateService,
    IWhatsAppService whatsApp,
    ISmsBackupService smsBackup,
    ILogger<NotificationService> logger)
    : INotificationService
{
    public async Task SendBudgetAlertAsync(Guid userId, BudgetAlert alert)
    {
        // 1) Lookup user to get phone number
        var user = await userService.GetByIdAsync(userId);
        if (user == null)
        {
            logger.LogWarning("Cannot send alert: user {UserId} not found", userId);
            return;
        }

        var phone = user.PhoneNumber;

        // 2) Render the message via your templating service
        var message = await templateService.RenderAsync("BudgetAlert", new
        {
            alert.Category,
            alert.Spent,
            alert.Limit
        });

        // 3) Try sending over WhatsApp
        try
        {
            await whatsApp.SendTextMessageAsync(phone, message);
            logger.LogInformation("Sent budget alert via WhatsApp to {Phone}", phone);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WhatsApp send failed for {Phone}, falling back to SMS", phone);

            // 4) Fallback to SMS
            try
            {
                await smsBackup.SendSmsAsync(phone, message);
                logger.LogInformation("Sent budget alert via SMS to {Phone}", phone);
            }
            catch (Exception smsEx)
            {
                logger.LogError(smsEx, "SMS send also failed for {Phone}", phone);
            }
        }
    }
}