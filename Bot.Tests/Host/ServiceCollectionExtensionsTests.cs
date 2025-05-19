using Bot.Core.Services;
using Bot.Host;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bot.Tests.Host;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBotServices_Should_Register_Core_Services()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=u;Password=p",
                ["ConnectionStrings:MassTransitConnection"] = "Host=localhost;Database=test;Username=u;Password=p",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["AzureOpenAI:Endpoint"] = "https://example.com",
                ["AzureOpenAI:ApiKey"] = "key",
                ["FormRecognizer:Endpoint"] = "https://example.com",
                ["FormRecognizer:ApiKey"] = "key",
                ["Transcription:Region"] = "eastus",
                ["Transcription:SubscriptionKey"] = "key",
                ["Schedules:RecurringTransfer"] = "* * * * *"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBotServices(config);
        services.AddDistributedMemoryCache();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IQuickReplyService>().Should().NotBeNull();
        provider.GetRequiredService<INlpService>().Should().NotBeNull();
        provider.GetRequiredService<IOcrService>().Should().NotBeNull();
        provider.GetRequiredService<ITranscriptionService>().Should().NotBeNull();
        provider.GetRequiredService<ITextToSpeechService>().Should().NotBeNull();
    }
}