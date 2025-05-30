using Bot.Core.Services;
using Bot.Shared.DTOs;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.Chat;

public class SignupCmdConsumer(IUserService users) : IConsumer<SignupCmd>
{
    public async Task Consume(ConsumeContext<SignupCmd> ctx)
    {
        try
        {
            var user = await users.CreateAsync(ctx.Message.CorrelationId, ctx.Message.Payload);
            await ctx.Publish(new SignupSucceeded(ctx.Message.CorrelationId, user.Id));
        }
        catch (Exception)
        {
            await ctx.Publish(new SignupFailed(ctx.Message.CorrelationId, "DuplicateOrInvalid"));
            throw;
        }
    }
}