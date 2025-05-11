using Bot.Core.Providers;
using Bot.Core.Services;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.Payments;

public class BalanceCmdConsumer(IBankProviderFactory factory, IUserService users) : IConsumer<BalanceCmd>
{
    public async Task Consume(ConsumeContext<BalanceCmd> ctx)
    {
        var user = await users.GetByIdAsync(ctx.Message.CorrelationId);
        var provider = await factory.GetProviderAsync(user!.Id);
        var balance = await provider.GetBalanceAsync(user.PhoneNumber);

        await ctx.Publish(new BalanceSent(user.Id, balance));
    }
}