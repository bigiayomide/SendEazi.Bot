using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Bot.Shared;
using Bot.Shared.Models;
using Bot.Shared.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Bot.Tests.Services;

public class NudgeServiceTests
{
    private static ApplicationDbContext CreateDb(string name)
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options);
    }

    [Fact]
    public async Task RequestNudgeAsync_Should_Persist_Nudge()
    {
        var db = CreateDb("request-nudge");
        var service = new NudgeService(db);
        var userId = Guid.NewGuid();
        var content = "hello";
        var scheduled = DateTime.UtcNow.AddMinutes(10);

        var result = await service.RequestNudgeAsync(userId, content, scheduled);
        var saved = await db.Nudges.FindAsync(result.Id);

        saved.Should().NotBeNull();
        saved!.UserId.Should().Be(userId);
        saved.Content.Should().Be(content);
        saved.ScheduledAt.Should().Be(scheduled);
        saved.IsSent.Should().BeFalse();
    }

    [Fact]
    public async Task GetDueNudgesAsync_Should_Return_Only_Due_Unsent()
    {
        var db = CreateDb("due-nudges");
        var service = new NudgeService(db);
        var now = DateTime.UtcNow;

        db.Nudges.AddRange(
            new Nudge
            {
                Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Content = "a", ScheduledAt = now.AddMinutes(-1),
                IsSent = false, CreatedAt = now
            },
            new Nudge
            {
                Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Content = "b", ScheduledAt = now.AddMinutes(5),
                IsSent = false, CreatedAt = now
            },
            new Nudge
            {
                Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Content = "c", ScheduledAt = now.AddMinutes(-2),
                IsSent = true, CreatedAt = now
            });
        await db.SaveChangesAsync();

        var results = service.GetDueNudgesAsync(now).ToList();

        results.Should().HaveCount(1);
        results[0].Content.Should().Be("a");
    }

    [Fact]
    public async Task MarkAsSentAsync_Should_Update_Status()
    {
        var db = CreateDb("mark-sent");
        var service = new NudgeService(db);
        var nudge = new Nudge
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Content = "x",
            ScheduledAt = DateTime.UtcNow,
            IsSent = false,
            CreatedAt = DateTime.UtcNow
        };
        db.Nudges.Add(nudge);
        await db.SaveChangesAsync();

        await service.MarkAsSentAsync(nudge);
        var updated = await db.Nudges.FindAsync(nudge.Id);

        updated!.IsSent.Should().BeTrue();
        updated.SentAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(NudgeType.InvalidNin, "https://cdn.bot.ng/nudges/nin-invalid.gif")]
    [InlineData(NudgeType.InvalidBvn, "https://cdn.bot.ng/nudges/bvn-invalid.gif")]
    [InlineData(NudgeType.ServiceDown, "https://cdn.bot.ng/nudges/service-down.png")]
    [InlineData(NudgeType.BadPin, "https://cdn.bot.ng/nudges/wrong-pin.png")]
    [InlineData(NudgeType.TransferFail, "https://cdn.bot.ng/nudges/tx-failed.gif")]
    [InlineData(NudgeType.WaitingOnMandate, "https://cdn.bot.ng/nudges/mandate-wait.gif")]
    [InlineData(NudgeType.BudgetAlert, "https://cdn.bot.ng/nudges/limit-near.png")]
    [InlineData(NudgeType.SignupRequired, "https://cdn.bot.ng/nudges/signup-required.png")]
    public void SelectAsset_Should_Return_Configured_Asset(NudgeType type, string expected)
    {
        var service = new NudgeService(CreateDb("assets"));

        var url = service.SelectAsset(type);

        url.Should().Be(expected);
    }

    [Fact]
    public void SelectAsset_Should_Return_Default_For_Unknown()
    {
        var service = new NudgeService(CreateDb("asset-default"));

        var url = service.SelectAsset(NudgeType.Unknown);

        url.Should().Be("https://cdn.bot.ng/nudges/default.png");
    }
}