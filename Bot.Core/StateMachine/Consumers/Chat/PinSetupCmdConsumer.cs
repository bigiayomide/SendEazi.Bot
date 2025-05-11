using Bot.Core.Services;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.Chat;

public class PinSetupCmdConsumer(IPinService pins, IWhatsAppService wa, IUserService users)
    : IConsumer<PinSetupCmd>
{
    public async Task Consume(ConsumeContext<PinSetupCmd> ctx)
    {
        try
        {
            await pins.SetAsync(ctx.Message.CorrelationId, ctx.Message.PinHash);
            await ctx.Publish(new PinSet(ctx.Message.CorrelationId));

            var user = await users.GetByIdAsync(ctx.Message.CorrelationId);
            if (user is not null)
            {
                // Delete the original WhatsApp PIN message
                await wa.DeleteMessageAsync(ctx.Message.MessageId);

                // Confirm success to user
                await wa.SendTextMessageAsync(user.PhoneNumber,
                    "✅ Your PIN is now set and secured. You're ready to bank.");
            }
        }
        catch (Exception)
        {
            await ctx.Publish(new PinSetupFailed(ctx.Message.CorrelationId, "Could not set PIN"));
            throw;
        }
    }
}