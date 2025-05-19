using Bot.Core.Providers;
using Bot.Infrastructure.Data;
using Bot.Shared.Models;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Bot.Tests.TestUtilities;

public static class TestContextHelper
{
    public static IServiceCollection AddInMemoryDb(this IServiceCollection services, string dbName)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        return services;
    }

    public static async Task SeedUserAsync(this ApplicationDbContext db, Guid userId)
    {
        db.Users.Add(new User
        {
            Id = userId,
            PhoneNumber = "+2348000000000",
            FullName = "Test User",
            TenantId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            BVNEnc = "x", BVNHash = "x",
            NINEnc = "x", NINHash = "x",
            SignupSource = "test",
            BankAccessToken = "mock"
        });

        await db.SaveChangesAsync();
    }

    public static async Task SeedMandateAsync(this ApplicationDbContext db, Guid userId,
        string mandateId = "mandate-test")
    {
        db.DirectDebitMandates.Add(new DirectDebitMandate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MandateId = mandateId,
            Status = "ready",
            CreatedAt = DateTime.UtcNow,
            MandateReference = "FG",
            Provider = "Mono"
        });

        await db.SaveChangesAsync();
    }

    public static async Task<ApplicationDbContext> SetupInMemoryDb(string name = "test-db")
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

        var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        return db;
    }

    public static IServiceCollection AddMockBankFactory(this IServiceCollection services, Guid userId,
        IBankProvider provider)
    {
        var mockFactory = new Mock<IBankProviderFactory>();
        mockFactory.Setup(f => f.GetProviderAsync(userId, null)).ReturnsAsync(provider);
        services.AddSingleton(mockFactory.Object);

        return services;
    }

    public static async Task<ITestHarness> BuildTestHarness<TConsumer>(
        Action<IServiceCollection>? configure = null,
        string dbName = "test-harness")
        where TConsumer : class, IConsumer
    {
        var services = new ServiceCollection();

        services.AddInMemoryDb(dbName);
        services.AddMassTransitTestHarness(cfg => { cfg.AddConsumer<TConsumer>(); });

        services.AddScoped<TConsumer>();

        configure?.Invoke(services);

        var provider = services.BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        return harness;
    }
}