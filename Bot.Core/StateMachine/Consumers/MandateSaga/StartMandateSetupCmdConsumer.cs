using Bot.Core.Providers;
using Bot.Core.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Bot.Core.StateMachine.Consumers.MandateSaga;

public class StartMandateSetupCmdConsumer(
    IUserService users,
    IBankProviderFactory factory,
    ILogger<StartMandateSetupCmdConsumer> log)
    : IConsumer<StartMandateSetupCmd>
{
    public async Task Consume(ConsumeContext<StartMandateSetupCmd> ctx)
    {
        var user = await users.GetByIdAsync(ctx.Message.CorrelationId);
        if (user is null)
        {
            log.LogWarning("User not found for mandate setup: {Cid}", ctx.Message.CorrelationId);
            return;
        }

        var provider = await factory.GetProviderAsync(user.Id);
        var customerId = await provider.CreateCustomerAsync(user);

        var mandateId = await provider.CreateMandateAsync(
            user,
            customerId,
            maxAmount: 50000, // adjust as needed
            mandateReference: $"mandate:{user.Id}");

        log.LogInformation("Mandate started: {MandateId} for user {UserId}", mandateId, user.Id);
    }
}