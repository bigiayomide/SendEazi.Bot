using Bot.Infrastructure.Data;
using Bot.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bot.Core.Services;

public interface IPinService
{
    Task SetAsync(Guid userId, string pin);
    Task<bool> ValidateAsync(Guid userId, string pin);
}

public class PinService(ApplicationDbContext db, IPasswordHasher<User> hasher) : IPinService
{
    public async Task SetAsync(Guid userId, string pin)
    {
        var user = await db.Users.FindAsync(userId)
                   ?? throw new InvalidOperationException("User not found");

        user.PinHash = hasher.HashPassword(user, pin);
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }

    public async Task<bool> ValidateAsync(Guid userId, string pin)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || string.IsNullOrEmpty(user.PinHash))
            return false;

        var result = hasher.VerifyHashedPassword(user, user.PinHash, pin);
        return result == PasswordVerificationResult.Success;
    }
}