using System.Security.Cryptography;
using System.Text;
using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Bot.Shared;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Bot.Tests.Services;

public class UserServiceTests
{
    private static ApplicationDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    [Fact]
    public async Task CreateAsync_Should_Encrypt_And_Hash_Identifiers()
    {
        var db = CreateDb("user-create");
        var service = new UserService(db);
        var userId = Guid.NewGuid();
        var payload = new SignupPayload("Test User", "+2348111111111", "12345678901", "10987654321");

        var user = await service.CreateAsync(userId, payload);

        user.NINEnc.Should().Be(Convert.ToBase64String(Encoding.UTF8.GetBytes(payload.NIN)));
        user.BVNEnc.Should().Be(Convert.ToBase64String(Encoding.UTF8.GetBytes(payload.BVN)));
        user.NINHash.Should().Be(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload.NIN))));
        user.BVNHash.Should().Be(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload.BVN))));
    }

    [Fact]
    public async Task GetByIdAsync_Should_Include_PersonalitySettings()
    {
        var db = CreateDb("user-personality");
        var service = new UserService(db);
        var userId = Guid.NewGuid();
        var payload = new SignupPayload("Jane", "+2348222222222", "11111111111", "22222222222");

        await service.CreateAsync(userId, payload);
        await service.SetPersonalityAsync(userId, PersonalityEnum.Casual);

        var result = await service.GetByIdAsync(userId);

        result.Should().NotBeNull();
        result!.PersonalitySettings.Should().ContainSingle(p => p.Personality == PersonalityEnum.Casual);
    }
}

