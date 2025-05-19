using Bot.Infrastructure.Data;
using Bot.Shared;
using Bot.Shared.DTOs;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bot.Core.StateMachine.Consumers.UX;

public class ResolveQuickReplyCmdConsumer(
    ApplicationDbContext db,
    IPublishEndpoint bus,
    ILogger<ResolveQuickReplyCmdConsumer> log)
    : IConsumer<ResolveQuickReplyCmd>
{
    public async Task Consume(ConsumeContext<ResolveQuickReplyCmd> ctx)
    {
        var label = ctx.Message.Label.Trim().ToLower();
        var userId = ctx.Message.CorrelationId;

        var payee = await db.Payees
            .Where(p => p.UserId == userId && p.Nickname != null)
            .ToListAsync();

        var match = payee.FirstOrDefault(p =>
            string.Equals(p.Nickname!.Trim(), label, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            log.LogInformation("No matching payee found for label '{Label}'", ctx.Message.Label);
            //TODO: fix
            await bus.Publish(new NudgeCmd(userId, NudgeType.TransferFail, "+2349043844316", 
                $"❌ I couldn’t find anyone named \"{ctx.Message.Label}\" in your payees."));
            return;
        }

        // Proceed with partial payload (we’ll ask for amount next)
        var payload = new TransferPayload(
            match.AccountNumber,
            match.BankCode,
            0m,
            match.Nickname
        );

        await bus.Publish(new UserIntentDetected(
            userId,
            Shared.Enums.IntentType.Transfer,
            payload));
    }
}