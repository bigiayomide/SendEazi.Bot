using Bot.Infrastructure.Data;
using Bot.Shared;
using Bot.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bot.Core.Services;

public interface IFeedbackService
{
    Task<Guid> StoreAsync(Guid userId, FeedbackPayload payload);
    Task<double> GetAverageRatingAsync(Guid userId);
}

public class FeedbackService(ApplicationDbContext db) : IFeedbackService
{
    public async Task<Guid> StoreAsync(Guid userId, FeedbackPayload p)
    {
        var f = new Feedback
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Rating = p.Rating,
            Comment = p.Comment,
            CreatedAt = DateTime.UtcNow
        };
        db.Feedbacks.Add(f);
        await db.SaveChangesAsync();
        return f.Id;
    }

    public async Task<double> GetAverageRatingAsync(Guid userId)
    {
        var ratings = await db.Feedbacks
            .Where(f => f.UserId == userId)
            .Select(f => f.Rating)
            .ToListAsync();

        return ratings.Any() ? ratings.Average() : 0.0;
    }
}