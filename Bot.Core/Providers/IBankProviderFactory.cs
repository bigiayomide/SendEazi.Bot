namespace Bot.Core.Providers;

public interface IBankProviderFactory
{
    Task<IBankProvider> GetProviderAsync(Guid userId);
}