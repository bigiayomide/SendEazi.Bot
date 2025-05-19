using Bot.Core.Models;
using Bot.Core.Services;
using Bot.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Bot.Tests.Services;

public class NotificationServiceTests : IDisposable
{
    private readonly string _templatesDir;
    private readonly ITemplateRenderingService _templateService;

    public NotificationServiceTests()
    {
        _templatesDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_templatesDir);
        File.WriteAllText(Path.Combine(_templatesDir, "BudgetAlert.scriban"),
            "Alert: {{Category}} spent {{Spent}} of {{Limit}}.");

        var opts = Options.Create(new TemplateSettings { TemplatesPath = _templatesDir });
        var logger = new LoggerFactory().CreateLogger<TemplateRenderingService>();
        _templateService = new TemplateRenderingService(opts, logger);
    }

    public void Dispose()
    {
        if (Directory.Exists(_templatesDir))
            Directory.Delete(_templatesDir, true);
    }

    private NotificationService CreateService(Mock<IUserService> userSvc,
        Mock<IWhatsAppService> wa,
        Mock<ISmsBackupService> sms)
    {
        var logger = new LoggerFactory().CreateLogger<NotificationService>();
        return new NotificationService(userSvc.Object, _templateService, wa.Object, sms.Object, logger);
    }

    [Fact]
    public async Task Should_Send_Via_WhatsApp_When_No_Error()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, PhoneNumber = "+2349000000000" };
        var userSvc = new Mock<IUserService>();
        userSvc.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(user);

        var wa = new Mock<IWhatsAppService>();
        var sms = new Mock<ISmsBackupService>();

        var service = CreateService(userSvc, wa, sms);

        var alert = new BudgetAlert
        {
            UserId = userId,
            Category = "rent",
            Spent = 5000,
            Limit = 10000
        };

        var expected = await _templateService.RenderAsync("BudgetAlert", new
        {
            alert.Category,
            alert.Spent,
            alert.Limit
        });

        await service.SendBudgetAlertAsync(userId, alert);

        wa.Verify(w => w.SendTextMessageAsync(user.PhoneNumber, expected), Times.Once);
        sms.Verify(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Should_Fallback_To_Sms_When_WhatsApp_Fails()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, PhoneNumber = "+2348111111111" };
        var userSvc = new Mock<IUserService>();
        userSvc.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(user);

        var wa = new Mock<IWhatsAppService>();
        wa.Setup(w => w.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("wa failure"));
        var sms = new Mock<ISmsBackupService>();

        var service = CreateService(userSvc, wa, sms);

        var alert = new BudgetAlert
        {
            UserId = userId,
            Category = "food",
            Spent = 3000,
            Limit = 5000
        };

        var expected = await _templateService.RenderAsync("BudgetAlert", new
        {
            alert.Category,
            alert.Spent,
            alert.Limit
        });

        await service.SendBudgetAlertAsync(userId, alert);

        wa.Verify(w => w.SendTextMessageAsync(user.PhoneNumber, expected), Times.Once);
        sms.Verify(s => s.SendSmsAsync(user.PhoneNumber, expected), Times.Once);
    }
}