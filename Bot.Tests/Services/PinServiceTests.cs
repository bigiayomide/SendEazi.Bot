using Bot.Core.Services;
using Bot.Infrastructure.Data;
using Bot.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bot.Tests.Services;

public class PinServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<User> _hasher = new PasswordHasher<User>();
    private readonly PinService _service;

    public PinServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _service = new PinService(_db, _hasher);
    }

    private User CreateTestUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            PhoneNumber = "+2348000000000",
            FullName = "Test User",
            CreatedAt = DateTime.UtcNow,
            BVNEnc = "bvn123",
            BVNHash = "hash1",
            NINEnc = "nin123",
            NINHash = "hash2",
            SignupSource = "test",
            BankAccessToken = "mock"
        };
    }

    [Fact]
    public async Task SetAsync_Should_SaveHashedPin()
    {
        var user = CreateTestUser();
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _service.SetAsync(user.Id, "1234");
        var saved = await _db.Users.FindAsync(user.Id);

        saved!.PinHash.Should().NotBeNullOrWhiteSpace();
        _hasher.VerifyHashedPassword(saved, saved.PinHash, "1234")
            .Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnTrue_WhenPinIsCorrect()
    {
        var user = CreateTestUser();
        var pin = "5678";
        user.PinHash = _hasher.HashPassword(user, pin);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var isValid = await _service.ValidateAsync(user.Id, pin);

        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnFalse_WhenPinIsWrong()
    {
        var user = CreateTestUser();
        user.PinHash = _hasher.HashPassword(user, "9999");

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var isValid = await _service.ValidateAsync(user.Id, "0000");

        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnFalse_WhenUserNotFound()
    {
        var result = await _service.ValidateAsync(Guid.NewGuid(), "1234");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnFalse_WhenPinHashIsNull()
    {
        var user = CreateTestUser();
        user.PinHash = null!;

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var isValid = await _service.ValidateAsync(user.Id, "0000");

        isValid.Should().BeFalse();
    }
}