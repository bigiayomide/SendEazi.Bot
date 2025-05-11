using Bot.Infrastructure.Data;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bot.Core.Services;

public interface IPersonalityService
{
    Task SetAsync(Guid userId, PersonalityEnum personality);
    Task<PersonalityEnum?> GetAsync(Guid userId);
}

public class PersonalityService(ApplicationDbContext db) : IPersonalityService
{
    public async Task SetAsync(Guid userId, PersonalityEnum p)
    {
        db.PersonalitySettings.Add(new PersonalitySetting
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Personality = p,
            SetAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public async Task<PersonalityEnum?> GetAsync(Guid userId)
    {
        return await db.PersonalitySettings
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.SetAt)
            .Select(p => (PersonalityEnum?)p.Personality)
            .FirstOrDefaultAsync();
    }
}