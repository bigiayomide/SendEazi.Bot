using Bot.Infrastructure.Data;
using Bot.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bot.Core.Services;

public interface IBeneficiaryService
{
    Task<Payee> SavePayeeAsync(
        Guid userId, string accountNumber, string bankCode, string? nickname);

    Task<IReadOnlyList<Payee>> GetPayeesAsync(Guid userId);
}

public class BeneficiaryService : IBeneficiaryService
{
    private readonly ApplicationDbContext _db;

    public BeneficiaryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Payee> SavePayeeAsync(
        Guid userId, string accountNumber, string bankCode, string? nickname)
    {
        // Upsert: if account exists, update nickname; else create new
        var payee = await _db.Payees
            .FirstOrDefaultAsync(p =>
                p.UserId == userId &&
                p.AccountNumber == accountNumber &&
                p.BankCode == bankCode);

        if (payee is null)
        {
            payee = new Payee
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountNumber = accountNumber,
                BankCode = bankCode,
                Nickname = nickname,
                CreatedAt = DateTime.UtcNow
            };
            _db.Payees.Add(payee);
        }
        else
        {
            payee.Nickname = nickname;
            _db.Payees.Update(payee);
        }

        await _db.SaveChangesAsync();
        return payee;
    }

    public async Task<IReadOnlyList<Payee>> GetPayeesAsync(Guid userId)
    {
        return await _db.Payees
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync();
    }
}