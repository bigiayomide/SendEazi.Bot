// Bot.Core.Services/BillPayService.cs

using System.Text;
using System.Text.Json;
using Bot.Infrastructure.Data;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bot.Core.Services;

public interface IBillPayService
{
    Task<BillPayment> PayBillAsync(Guid userId, string billerCode, decimal amount, DateTime dueDate);
    Task<IReadOnlyList<BillPayment>> ProcessDueBillPaymentsAsync();
}

public class BillPayOptions
{
    public string BaseUrl { get; set; } = null!;

    public string ApiKey { get; set; } = null!;
    // Map biller codes to endpoints, etc., if needed
}

public class BillPayService : IBillPayService
{
    private readonly ApplicationDbContext _db;
    private readonly HttpClient _http;
    private readonly ILogger<BillPayService> _logger;
    private readonly BillPayOptions _opts;

    public BillPayService(
        ApplicationDbContext db,
        IHttpClientFactory httpFactory,
        IOptions<BillPayOptions> opts,
        ILogger<BillPayService> logger)
    {
        _db = db;
        _http = httpFactory.CreateClient(nameof(BillPayService));
        _opts = opts.Value;
        _logger = logger;

        _http.BaseAddress = new Uri(_opts.BaseUrl);
        _http.DefaultRequestHeaders.Add("x-api-key", _opts.ApiKey);
    }

    public async Task<BillPayment> PayBillAsync(Guid userId, string billerCode, decimal amount, DateTime dueDate)
    {
        // 1) Create local record
        var bill = new BillPayment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Biller = Enum.Parse<BillerEnum>(billerCode, true),
            Amount = amount,
            DueDate = dueDate,
            IsPaid = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.BillPayments.Add(bill);
        await _db.SaveChangesAsync();

        // 2) Call external API
        var payload = new
        {
            billerCode,
            amount,
            reference = bill.Id.ToString(),
            dueDate = dueDate.ToString("o")
        };
        var resp = await _http.PostAsync("/v1/payments",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("Bill pay API failed ({Status}): {Body}",
                resp.StatusCode, await resp.Content.ReadAsStringAsync());
            return bill; // leave IsPaid = false
        }

        // 3) Mark as paid
        bill.IsPaid = true;
        bill.PaidAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Bill {BillId} paid successfully for user {UserId}", bill.Id, userId);
        return bill;
    }

    public async Task<IReadOnlyList<BillPayment>> ProcessDueBillPaymentsAsync()
    {
        var now = DateTime.UtcNow;
        // fetch all unpaid, due bills
        var due = await _db.BillPayments
            .Where(b => !b.IsPaid && b.DueDate <= now)
            .ToListAsync();

        foreach (var bill in due)
        {
            // send reminder (your NotificationService could be injected if you prefer)
            _logger.LogInformation("Reminder: biller {Biller} for user {User} is due on {Due}",
                bill.Biller, bill.UserId, bill.DueDate);

            bill.LastReminderSentAt = now;
        }

        await _db.SaveChangesAsync();
        return due;
    }
}