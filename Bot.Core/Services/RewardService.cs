// Bot.Core.Services/RewardService.cs

using Bot.Infrastructure.Data;
using Bot.Shared.Enums;
using Bot.Shared.Models;

namespace Bot.Core.Services;

public interface IRewardService
{
    Task GrantAsync(Guid userId, RewardTypeEnum type);
}

public class RewardService(ApplicationDbContext db) : IRewardService
{
    public async Task GrantAsync(Guid userId, RewardTypeEnum type)
    {
        db.Rewards.Add(new Reward
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Details = $"Granted {type} at {DateTime.UtcNow}",
            GrantedAt = DateTime.UtcNow,
            IsRedeemed = false
        });

        await db.SaveChangesAsync();
    }
}