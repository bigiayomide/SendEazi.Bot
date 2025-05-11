using Bot.Infrastructure.Data;
using Bot.Shared;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bot.Core.Services;

public interface IUserService
{
    Task<User> CreateAsync(SignupPayload payload);
    Task<User?> GetByIdAsync(Guid userId);
    Task SetPersonalityAsync(Guid userId, PersonalityEnum personality);
    Task<bool> RunKycAsync(Guid userId);
}

public class UserService(ApplicationDbContext db) : IUserService
{
    public async Task<User> CreateAsync(SignupPayload p)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = p.FullName,
            PhoneNumber = p.Phone,
            NINEnc = Encrypt(p.NIN),
            BVNEnc = Encrypt(p.BVN),
            NINHash = Hash(p.NIN),
            BVNHash = Hash(p.BVN),
            CreatedAt = DateTime.UtcNow,
            SignupSource = "chatbot",
            BankAccessToken = "pending"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public Task<User?> GetByIdAsync(Guid userId) =>
        db.Users.Include(x => x.PersonalitySettings).FirstOrDefaultAsync(x => x.Id == userId);

    public async Task SetPersonalityAsync(Guid userId, PersonalityEnum p)
    {
        var setting = new PersonalitySetting
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Personality = p,
            SetAt = DateTime.UtcNow
        };

        db.PersonalitySettings.Add(setting);
        await db.SaveChangesAsync();
    }

    public Task<bool> RunKycAsync(Guid userId) => Task.FromResult(true);

    private static string Encrypt(string val) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(val));
    private static string Hash(string val) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(val)));
}