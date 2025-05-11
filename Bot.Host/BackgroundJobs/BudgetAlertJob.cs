// // Bot.GoalsWorker/Services/BudgetAlertJob.cs
//
// using Bot.Core.Services;
// using Quartz;
//
// namespace Bot.Host.BackgroundJobs;
//
// [DisallowConcurrentExecution]
// public class BudgetAlertJob : IJob
// {
//     private readonly IBudgetService _budgetService;
//     private readonly ILogger<BudgetAlertJob> _logger;
//     private readonly INotificationService _notificationService;
//
//     public BudgetAlertJob(
//         IBudgetService budgetService,
//         INotificationService notificationService,
//         ILogger<BudgetAlertJob> logger)
//     {
//         _budgetService = budgetService;
//         _notificationService = notificationService;
//         _logger = logger;
//     }
//
//     public async Task Execute(IJobExecutionContext context)
//     {
//         _logger.LogInformation("BudgetAlertJob started at {Time}", context.FireTimeUtc);
//
//         // Retrieve any budget goals that have been exceeded or are nearing their limit
//         var alerts = await _budgetService.GetTriggeredBudgetAlertsAsync(Guid.Empty);
//
//         foreach (var alert in alerts)
//         {
//             // Send an alert (WhatsApp, SMS, email, etc.)
//             await _notificationService.SendBudgetAlertAsync(alert.Goal.UserId, alert.Spent.Scale);
//             _logger.LogInformation("Sent budget alert to user {UserId}", alert.UserId);
//         }
//
//         _logger.LogInformation("BudgetAlertJob completed at {Time}", DateTime.UtcNow);
//     }
// }