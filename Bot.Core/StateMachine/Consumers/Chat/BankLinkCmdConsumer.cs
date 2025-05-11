using Bot.Core.Services;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.Chat;

public class BankLinkCmdConsumer(IUserService users, IEncryptionService encryption) : IConsumer<BankLinkCmd>
{

    public async Task Consume(ConsumeContext<BankLinkCmd> ctx)
    {
        var user = await users.GetByIdAsync(ctx.Message.CorrelationId);
        if (user == null) return;

        await ctx.Publish(new StartMandateSetupCmd(
            ctx.Message.CorrelationId,
            user.FullName!,
            user.PhoneNumber,
            encryption.Decrypt(user.BVNEnc)));
    }
}