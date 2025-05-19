using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Bot.Shared;
using Bot.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Bot.Tests.Services;

public class FeedbackServiceTests
{
    private static ApplicationDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    [Fact]
    public async Task StoreAsync_Should_Return_New_Id()
    {
        await using var db = CreateDb("feedback-store");
        var service = new FeedbackService(db);
        var userId = Guid.NewGuid();
        var payload = new FeedbackPayload(5, "great");

        var resultId = await service.StoreAsync(userId, payload);

        resultId.Should().NotBeEmpty();
        var saved = await db.Feedbacks.FindAsync(resultId);
        saved.Should().NotBeNull();
        saved!.UserId.Should().Be(userId);
        saved.Rating.Should().Be(5);
        saved.Comment.Should().Be("great");
    }

    [Fact]
    public async Task GetAverageRatingAsync_Should_Return_Mean_Rating()
    {
        await using var db = CreateDb("feedback-avg");
        var userId = Guid.NewGuid();
        db.Feedbacks.AddRange(
            new Feedback { Id = Guid.NewGuid(), UserId = userId, Rating = 4, CreatedAt = DateTime.UtcNow },
            new Feedback { Id = Guid.NewGuid(), UserId = userId, Rating = 2, CreatedAt = DateTime.UtcNow },
            new Feedback { Id = Guid.NewGuid(), UserId = userId, Rating = 5, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var service = new FeedbackService(db);
        var average = await service.GetAverageRatingAsync(userId);

        average.Should().BeApproximately((4 + 2 + 5) / 3.0, 0.0001);
    }
}
