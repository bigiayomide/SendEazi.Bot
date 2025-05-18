using Bot.Infrastructure.Data;
using Bot.Shared;
using Bot.Shared.Models;

namespace Bot.Core.Services;

public interface INudgeService
{
    Task<Nudge> RequestNudgeAsync(Guid userId, string content, DateTime scheduledAt);
    IQueryable<Nudge> GetDueNudgesAsync(DateTime asOfUtc);
    Task MarkAsSentAsync(Nudge nudge);
    string SelectAsset(NudgeType type);
}

public class NudgeService(ApplicationDbContext db) : INudgeService
{
    private static readonly Dictionary<NudgeType, string> _assets = new()
    {
        [NudgeType.InvalidNin] = "https://cdn.bot.ng/nudges/nin-invalid.gif",
        [NudgeType.InvalidBvn] = "https://cdn.bot.ng/nudges/bvn-invalid.gif",
        [NudgeType.ServiceDown] = "https://cdn.bot.ng/nudges/service-down.png",
        [NudgeType.BadPin] = "https://cdn.bot.ng/nudges/wrong-pin.png",
        [NudgeType.TransferFail] = "https://cdn.bot.ng/nudges/tx-failed.gif",
        [NudgeType.WaitingOnMandate] = "https://cdn.bot.ng/nudges/mandate-wait.gif",
        [NudgeType.BudgetAlert] = "https://cdn.bot.ng/nudges/limit-near.png",
        [NudgeType.SignupRequired] = "https://cdn.bot.ng/nudges/signup-required.png"
    };

    private readonly Random _rng = new();

    public Task<Nudge> RequestNudgeAsync(Guid userId, string content, DateTime scheduledAt)
    {
        var n = new Nudge
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Content = content,
            ScheduledAt = scheduledAt,
            IsSent = false,
            CreatedAt = DateTime.UtcNow
        };

        db.Nudges.Add(n);
        db.SaveChanges();
        return Task.FromResult(n);
    }

    public IQueryable<Nudge> GetDueNudgesAsync(DateTime asOfUtc)
    {
        return db.Nudges.Where(n => !n.IsSent && n.ScheduledAt <= asOfUtc);
    }

    public Task MarkAsSentAsync(Nudge nudge)
    {
        nudge.IsSent = true;
        nudge.SentAt = DateTime.UtcNow;
        return db.SaveChangesAsync();
    }

    public string SelectAsset(NudgeType type)
    {
        return _assets.TryGetValue(type, out var url)
            ? url
            : "https://cdn.bot.ng/nudges/default.png";
    }
}