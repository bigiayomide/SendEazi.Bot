using Bot.Core.Providers;
using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Bot.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Bot.Tests.Services;

public class BankConnectionServiceTests
{
    private static ApplicationDbContext CreateDb(string name)
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options);
    }

    [Fact]
    public async Task ConnectUserBankAsync_Should_Update_Timestamp()
    {
        var db = CreateDb("bank-connect-update");
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            PhoneNumber = "+2348000000000",
            FullName = "Test User",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            BVNEnc = "x",
            BVNHash = "x",
            NINEnc = "x",
            NINHash = "x",
            SignupSource = "test",
            BankAccessToken = "old"
        });
        await db.SaveChangesAsync();
        var provider = new Mock<IBankProvider>();
        var service = new BankConnectionService(db, provider.Object);

        var before = (await db.Users.FindAsync(userId))!.UpdatedAt;
        await service.ConnectUserBankAsync(userId, "code");
        var updated = await db.Users.FindAsync(userId);

        updated!.UpdatedAt.Should().NotBe(before);
    }

    [Fact]
    public async Task ConnectUserBankAsync_Should_Throw_For_Missing_User()
    {
        var db = CreateDb("bank-connect-missing");
        var service = new BankConnectionService(db, new Mock<IBankProvider>().Object);

        var act = () => service.ConnectUserBankAsync(Guid.NewGuid(), "code");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}