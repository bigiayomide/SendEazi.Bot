using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Bot.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Bot.Tests.Services;

public class BeneficiaryServiceTests
{
    private static ApplicationDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    [Fact]
    public async Task SavePayeeAsync_Should_Insert_New_Payee()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDb("payee-insert");
        var service = new BeneficiaryService(db);

        var result = await service.SavePayeeAsync(userId, "1111222233", "001", "Home");

        result.Id.Should().NotBeEmpty();
        var saved = await db.Payees.SingleAsync();
        saved.UserId.Should().Be(userId);
        saved.AccountNumber.Should().Be("1111222233");
        saved.BankCode.Should().Be("001");
        saved.Nickname.Should().Be("Home");
    }

    [Fact]
    public async Task SavePayeeAsync_Should_Update_Existing_Payee_Nickname()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDb("payee-update");
        var existing = new Payee
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountNumber = "4444555566",
            BankCode = "002",
            Nickname = "Old"
        };
        db.Payees.Add(existing);
        await db.SaveChangesAsync();
        var service = new BeneficiaryService(db);

        var result = await service.SavePayeeAsync(userId, "4444555566", "002", "New");

        result.Id.Should().Be(existing.Id);
        var payees = await db.Payees.ToListAsync();
        payees.Should().HaveCount(1);
        payees[0].Nickname.Should().Be("New");
    }

    [Fact]
    public async Task GetPayeesAsync_Should_Return_Payees_For_Specified_User()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await using var db = CreateDb("payee-filter");
        db.Payees.Add(new Payee
        {
            Id = Guid.NewGuid(),
            UserId = userA,
            AccountNumber = "123",
            BankCode = "001"
        });
        db.Payees.Add(new Payee
        {
            Id = Guid.NewGuid(),
            UserId = userB,
            AccountNumber = "321",
            BankCode = "002"
        });
        await db.SaveChangesAsync();
        var service = new BeneficiaryService(db);

        var results = await service.GetPayeesAsync(userA);

        results.Should().ContainSingle();
        results[0].UserId.Should().Be(userA);
        results[0].AccountNumber.Should().Be("123");
    }
}
