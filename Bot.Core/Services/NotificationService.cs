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

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly ISmsBackupService _smsBackup;
    private readonly ITemplateRenderingService _templateService;
    private readonly IUserService _userService;
    private readonly IWhatsAppService _whatsApp;

    public NotificationService(
        IUserService userService,
        ITemplateRenderingService templateService,
        IWhatsAppService whatsApp,
        ISmsBackupService smsBackup,
        ILogger<NotificationService> logger)
    {
        _userService = userService;
        _templateService = templateService;
        _whatsApp = whatsApp;
        _smsBackup = smsBackup;
        _logger = logger;
    }

    public async Task SendBudgetAlertAsync(Guid userId, BudgetAlert alert)
    {
        // 1) Lookup user to get phone number
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Cannot send alert: user {UserId} not found", userId);
            return;
        }

        var phone = user.PhoneNumber;

        // 2) Render the message via your templating service
        var message = await _templateService.RenderAsync("BudgetAlert", new
        {
            alert.Category,
            alert.Spent,
            alert.Limit
        });

        // 3) Try sending over WhatsApp
        try
        {
            await _whatsApp.SendTextMessageAsync(phone, message);
            _logger.LogInformation("Sent budget alert via WhatsApp to {Phone}", phone);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp send failed for {Phone}, falling back to SMS", phone);

            // 4) Fallback to SMS
            try
            {
                await _smsBackup.SendSmsAsync(phone, message);
                _logger.LogInformation("Sent budget alert via SMS to {Phone}", phone);
            }
            catch (Exception smsEx)
            {
                _logger.LogError(smsEx, "SMS send also failed for {Phone}", phone);
            }
        }
    }
}