using Bot.Core.Providers;
using Bot.Core.Services;
using Bot.Infrastructure.Configuration;
using Bot.Infrastructure.Data;
using Bot.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Bot.Tests.Providers;

public class BankProviderFactoryTests
{
    private static ServiceProvider BuildServices(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));

        // Register Mono provider instance
        services.AddSingleton(sp =>
            new MonoBankProvider(
                new HttpClient(),
                Options.Create(new MonoOptions
                {
                    BaseUrl = "http://mono",
                    SecretKey = "key",
                    BusinessSubAccountId = "id"
                }),
                Mock.Of<ILogger<MonoBankProvider>>(),
                Mock.Of<IEncryptionService>()));

        // Register OnePipe provider instance
        services.AddSingleton(sp =>
            new OnePipeBankProvider(
                new HttpClient(),
                Options.Create(new OnePipeOptions
                {
                    BaseUrl = "http://onepipe",
                    ApiKey = "api",
                    MerchantId = "m",
                    SecretKey = "s"
                }),
                Mock.Of<IEncryptionService>()));

        return services.BuildServiceProvider(true);
    }

    private static LinkedBankAccount CreateAccount(Guid userId, string provider, bool isDefault = false)
    {
        return new LinkedBankAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            AccountName = "name",
            AccountNumberEnc = "enc",
            AccountHash = "hash",
            BankCode = "001",
            IsDefault = isDefault
        };
    }

    [Fact]
    public async Task Returns_Mono_Provider_For_Default_Account()
    {
        var userId = Guid.NewGuid();
        await using var provider = BuildServices("mono-default");
        using var scope = provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.LinkedBankAccounts.Add(CreateAccount(userId, "Mono", true));
        await db.SaveChangesAsync();

        var factory = new BankProviderFactory(scope.ServiceProvider, db);
        var result = await factory.GetProviderAsync(userId);

        result.Should().BeSameAs(scope.ServiceProvider.GetRequiredService<MonoBankProvider>());
    }


    [Fact]
    public async Task Returns_Selected_Account_Provider()
    {
        var userId = Guid.NewGuid();
        await using var provider = BuildServices("onepipe-selected");
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var account = CreateAccount(userId, "OnePipe");
        db.LinkedBankAccounts.Add(account);
        await db.SaveChangesAsync();

        var factory = new BankProviderFactory(provider, db);
        var result = await factory.GetProviderAsync(userId, account.Id);

        result.Should().BeSameAs(provider.GetRequiredService<OnePipeBankProvider>());
    }

    [Fact]
    public async Task Throws_When_No_Account_Found()
    {
        await using var provider = BuildServices("no-account");
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var factory = new BankProviderFactory(provider, db);

        var act = async () => await factory.GetProviderAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Throws_When_Provider_Unsupported()
    {
        var userId = Guid.NewGuid();
        await using var provider = BuildServices("unsupported");
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.LinkedBankAccounts.Add(CreateAccount(userId, "Bad", true));
        await db.SaveChangesAsync();

        var factory = new BankProviderFactory(provider, db);
        var act = async () => await factory.GetProviderAsync(userId);

        await act.Should().ThrowAsync<NotSupportedException>();
    }
}