using Bot.Core.Providers;
using Bot.Core.Services;
using Bot.Shared.Models;
using MassTransit;

namespace Bot.Core.StateMachine;


public class DirectDebitMandateStateMachine : MassTransitStateMachine<DirectDebitMandateState>
{
    public State AwaitingApproval { get; private set; } = null!;
    public State Ready { get; private set; } = null!;

    public Event<StartMandateSetupCmd> StartCmd = default!;
    public Event<MandateReadyToDebit> ReadyEvt = default!;  // internal loopback

    public DirectDebitMandateStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => StartCmd, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => ReadyEvt, x => x.CorrelateById(m => m.Message.CorrelationId));

        Initially(
            When(StartCmd)
                .ThenAsync(async ctx =>
                {
                    // call provider.CreateCustomer + CreateMandate
                    var provider = ctx.GetPayload<IBankProvider>(); // injected via context
                    var user = await ctx.GetPayload<IUserService>()
                                   .GetByIdAsync(ctx.Data.CorrelationId)
                               ?? throw new Exception("User not found");

                    var customerId = await provider.CreateCustomerAsync(user);
                    var mandateId =
                        await provider.CreateMandateAsync(user, customerId, 50000, Guid.NewGuid().ToString());

                    ctx.Saga.MandateId = mandateId;
                    ctx.Saga.Provider = "Mono"; // or OnePipe – decide from logic

                    // wait for webhook to confirm mandate approval
                })
                .TransitionTo(AwaitingApproval)
        );

        During(AwaitingApproval,
            When(ReadyEvt)
                .PublishAsync(ctx => ctx.Init<MandateReadyToDebit>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.MandateId,
                    ctx.Saga.Provider
                }))
                .TransitionTo(Ready)
        );

        SetCompletedWhenFinalized();
    }
}
