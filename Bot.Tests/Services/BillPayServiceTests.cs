using System.Net;
using System.Net.Http;
using System.Text.Json;
using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Bot.Shared.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Bot.Tests.TestUtilities;

namespace Bot.Tests.Services;

public class BillPayServiceTests
{
    private static ApplicationDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    private static (BillPayService svc, MockHttpMessageHandler handler, Mock<ILogger<BillPayService>> logger) CreateService(
        ApplicationDbContext db,
        Func<HttpRequestMessage, HttpResponseMessage> handlerFunc)
    {
        var handler = new MockHttpMessageHandler(handlerFunc);
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(nameof(BillPayService))).Returns(client);
        var opts = Options.Create(new BillPayOptions
        {
            BaseUrl = "https://bill.test",
            ApiKey = "key"
        });
        var logger = new Mock<ILogger<BillPayService>>();
        var svc = new BillPayService(db, factory.Object, opts, logger.Object);
        return (svc, handler, logger);
    }

    [Fact]
    public async Task PayBillAsync_Should_Post_And_Mark_Paid_On_Success()
    {
        var db = CreateDb("bill-success");
        HttpRequestMessage? req = null;
        var (svc, handler, logger) = CreateService(db, r =>
        {
            req = r;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
        });

        var userId = Guid.NewGuid();
        var due = DateTime.UtcNow;
        var bill = await svc.PayBillAsync(userId, "Electricity", 1200m, due);

        bill.IsPaid.Should().BeTrue();
        bill.PaidAt.Should().NotBeNull();

        req.Should().NotBeNull();
        req!.RequestUri!.PathAndQuery.Should().Be("/v1/payments");
        var json = JsonDocument.Parse(await req.Content!.ReadAsStringAsync()).RootElement;
        json.GetProperty("billerCode").GetString().Should().Be("Electricity");
        json.GetProperty("amount").GetDecimal().Should().Be(1200m);
        json.GetProperty("reference").GetString().Should().Be(bill.Id.ToString());
        json.GetProperty("dueDate").GetString().Should().Be(due.ToString("o"));

        logger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("paid successfully")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task PayBillAsync_Should_Log_Error_On_Failure()
    {
        var db = CreateDb("bill-fail");
        var (svc, _, logger) = CreateService(db, _ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("fail")
            });

        var bill = await svc.PayBillAsync(Guid.NewGuid(), "Water", 500m, DateTime.UtcNow);

        bill.IsPaid.Should().BeFalse();
        bill.PaidAt.Should().BeNull();

        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Bill pay API failed")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessDueBillPaymentsAsync_Should_Update_Reminder_Timestamps()
    {
        var db = CreateDb("due-bills");
        var now = DateTime.UtcNow.AddHours(-1);
        db.BillPayments.Add(new Bot.Shared.Models.BillPayment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Biller = BillerEnum.DSTV,
            Amount = 100,
            DueDate = now,
            IsPaid = false,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        });
        db.BillPayments.Add(new Bot.Shared.Models.BillPayment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Biller = BillerEnum.Electricity,
            Amount = 200,
            DueDate = now.AddMinutes(-10),
            IsPaid = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        db.BillPayments.Add(new Bot.Shared.Models.BillPayment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Biller = BillerEnum.Water,
            Amount = 300,
            DueDate = DateTime.UtcNow.AddDays(1),
            IsPaid = false,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var (svc, _, _) = CreateService(db, _ => new HttpResponseMessage(HttpStatusCode.OK));

        var due = await svc.ProcessDueBillPaymentsAsync();

        due.Should().HaveCount(2);
        foreach (var bill in due)
        {
            bill.LastReminderSentAt.Should().NotBeNull();
            var refreshed = await db.BillPayments.FindAsync(bill.Id);
            refreshed!.LastReminderSentAt.Should().NotBeNull();
        }
    }
}

