using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Bot.Tests.Services;

public class PersonalityServiceTests
{
    private static ApplicationDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    [Fact]
    public async Task SetAsync_Should_Persist_New_PersonalitySetting()
    {
        await using var db = CreateDb("persist-setting");
        var service = new PersonalityService(db);
        var userId = Guid.NewGuid();

        await service.SetAsync(userId, PersonalityEnum.Casual);

        var setting = await db.PersonalitySettings.FirstOrDefaultAsync(p => p.UserId == userId);
        setting.Should().NotBeNull();
        setting!.Personality.Should().Be(PersonalityEnum.Casual);
    }

    [Fact]
    public async Task GetAsync_Should_Return_Latest_Personality()
    {
        await using var db = CreateDb("latest-setting");
        var service = new PersonalityService(db);
        var userId = Guid.NewGuid();

        db.PersonalitySettings.Add(new PersonalitySetting
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Personality = PersonalityEnum.Formal,
            SetAt = DateTime.UtcNow.AddMinutes(-5)
        });
        await db.SaveChangesAsync();

        await service.SetAsync(userId, PersonalityEnum.Fun);
        var result = await service.GetAsync(userId);

        result.Should().Be(PersonalityEnum.Fun);
    }
}
