using Bot.Core.Services;
using Bot.Shared.DTOs;
using MassTransit;

namespace Bot.Core.StateMachine.Consumers.Payments;

public class RewardCmdConsumer(IRewardService rewards) : IConsumer<RewardCmd>
{
    public async Task Consume(ConsumeContext<RewardCmd> ctx)
    {
        await rewards.GrantAsync(ctx.Message.CorrelationId, ctx.Message.RewardType);
        await ctx.Publish(new RewardIssued(ctx.Message.CorrelationId, ctx.Message.RewardType));
    }
}