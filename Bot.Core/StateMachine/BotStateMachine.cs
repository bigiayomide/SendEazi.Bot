using System.Text.Json;
using Bot.Core.Services;
using Bot.Shared;
using Bot.Shared.DTOs;
using Bot.Shared.Models;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Bot.Core.StateMachine;

public class BotStateMachine : MassTransitStateMachine<BotState>
{
    public Event<BalanceSent> BalSent { get; private set; } = null!;
    public Event<BankLinkFailed> BankBad { get; private set; } = null!;
    public Event<BankLinkSucceeded> BankOk { get; private set; } = null!;
    public Event<BillPayFailed> BillBad { get; private set; } = null!;
    public Event<BillPaid> BillOk { get; private set; } = null!;
    public Event<BvnRejected> BvnBad { get; private set; } = null!;
    public Event<BvnProvided> BvnEvt { get; private set; } = null!;
    public Event<BvnVerified> BvnOk { get; private set; } = null!;
    public Event<BudgetAlertTriggered> GoalAlert { get; private set; } = null!;
    public Event<UserIntentDetected> IntentEvt { get; private set; } = null!;
    public Event<KycRejected> KycBad { get; private set; } = null!;
    public Event<KycApproved> KycOk { get; private set; } = null!;
    public Event<MandateReadyToDebit> MandateReadyEvt { get; private set; } = null!;
    public Event<FullNameProvided> NameEvt { get; private set; } = null!;
    public Event<NinRejected> NinBad { get; private set; } = null!;
    public Event<NinProvided> NinEvt { get; private set; } = null!;
    public Event<NinVerified> NinOk { get; private set; } = null!;
    public Event<PinInvalid> PinBad { get; private set; } = null!;
    public Event<PinValidated> PinOk { get; private set; } = null!;
    public Event<PinSetupFailed> PinSetBad { get; private set; } = null!;
    public Event<PinSet> PinSetEvt { get; private set; } = null!;
    public Event<RecurringFailed> RecBad { get; private set; } = null!;
    public Event<RecurringCancelled> RecCancel { get; private set; } = null!;
    public Event<RecurringExecuted> RecExec { get; private set; } = null!;
    public Event<SignupFailed> SignBad { get; private set; } = null!;
    public Event<SignupSucceeded> SignOk { get; private set; } = null!;
    public Event<TransferFailed> TxBad { get; private set; } = null!;
    public Event<TransferCompleted> TxOk { get; private set; } = null!;

    public BotStateMachine()
    {
        InstanceState(x => x.CurrentState);

        // Event correlations
        Event(() => IntentEvt, e =>
        {
            e.CorrelateBy((saga, context) => saga.PhoneNumber == context.Message.PhoneNumber);

            e.InsertOnInitial = true;

            e.SetSagaFactory(context => new BotState
            {
                CorrelationId = context.Message.CorrelationId,
                SessionId = Guid.NewGuid(),
                PhoneNumber = context.Message.PhoneNumber // Assign if available
            });
        });

        Event(() => NameEvt, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => NinEvt, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => NinOk, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => NinBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => BvnEvt, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => BvnOk, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => BvnBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => SignOk, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => SignBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => KycOk, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => KycBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => BankOk, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => BankBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => PinSetEvt, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => PinSetBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => PinOk, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => PinBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => TxOk, x =>
        {
            x.CorrelateById(m => m.Message.CorrelationId);
            x.InsertOnInitial = true;
        });
        Event(() => TxBad, x =>
        {
            x.CorrelateById(m => m.Message.CorrelationId);
            x.InsertOnInitial = true;
        });
        Event(() => BillOk, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => BillBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => BalSent, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => RecExec, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => RecBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => RecCancel, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => GoalAlert, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => MandateReadyEvt, x => x.CorrelateById(m => m.Message.CorrelationId));

        // Webhook: handle transfer callbacks
        Initially(
            When(TxOk)
                .ThenAsync(async ctx =>
                {
                    if (!ctx.Saga.PreviewPublished)
                    {
                        await ctx.Publish(new PreviewCmd(ctx.Saga.CorrelationId));
                        ctx.Saga.PreviewPublished = true;
                    }
                }),
            When(TxBad)
                .PublishAsync(ctx => Task.FromResult(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.TransferFail, ctx.Saga.PhoneNumber)))
        );

        // Onboarding Flow
        Initially(
            When(IntentEvt, ctx => ctx.Message.Intent == Bot.Shared.Enums.IntentType.Signup)
                .ThenAsync(SetState("AskFullName"))
                .PublishAsync(ctx => Task.FromResult(new PromptFullNameCmd(ctx.Saga.CorrelationId)))
                .TransitionTo(AskFullName),

            When(IntentEvt, ctx => ctx.Message.Intent is Shared.Enums.IntentType.Transfer or Shared.Enums.IntentType.BillPay)
                .ThenAsync(ctx => ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.SignupRequired, ctx.Saga.PhoneNumber, "📝 Please sign up before making transactions.")))
                .ThenAsync(SetState("AskFullName"))
                .PublishAsync(ctx => Task.FromResult(new PromptFullNameCmd(ctx.Saga.CorrelationId)))
                .TransitionTo(AskFullName),
            
            When(IntentEvt, ctx => ctx.Message.Intent == Bot.Shared.Enums.IntentType.Greeting)
                .ThenAsync(ctx => ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.Greeting, ctx.Saga.PhoneNumber, "👋 Hello! Let me know what you'd like to do next."))),

            When(IntentEvt, ctx => ctx.Message.Intent == Bot.Shared.Enums.IntentType.Unknown)
                .ThenAsync(ctx => ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.Unknown, ctx.Saga.PhoneNumber,"❓ I didn’t get that. Try saying 'check balance' or 'send money'.")))
        );

        During(AskFullName,
            When(NameEvt)
                .Then(ctx => ctx.Saga.TempName = ctx.Message.FullName)
                .ThenAsync(SetState("AskNin"))
                .PublishAsync(ctx => Task.FromResult(new PromptNinCmd(ctx.Saga.CorrelationId)))
                .TransitionTo(AskNin)
        );

        During(AskNin,
            When(NinEvt)
                .Then(ctx => ctx.Saga.TempNIN = ctx.Message.NIN)
                .ThenAsync(SetState("NinValidating"))
                .PublishAsync(ctx => Task.FromResult(new ValidateNinCmd(ctx.Saga.CorrelationId, ctx.Message.NIN)))
                .TransitionTo(NinValidating)
        );

        During(NinValidating,
            When(NinOk)
                .ThenAsync(SetState("AskBvn"))
                .PublishAsync(ctx => Task.FromResult(new PromptBvnCmd(ctx.Saga.CorrelationId)))
                .TransitionTo(AskBvn),
            When(NinBad)
                .ThenAsync(SetState("AskNin"))
                .PublishAsync(ctx => Task.FromResult(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.InvalidNin,  ctx.Saga.PhoneNumber, "❌ That NIN didn’t validate. Please re-enter your 11-digit NIN.")))
                .TransitionTo(AskNin)
        );

        During(AskBvn,
            When(BvnEvt)
                .Then(ctx => ctx.Saga.TempBVN = ctx.Message.BVN)
                .ThenAsync(SetState("BvnValidating"))
                .PublishAsync(ctx => Task.FromResult(new ValidateBvnCmd(ctx.Saga.CorrelationId, ctx.Message.BVN)))
                .TransitionTo(BvnValidating)
        );

        During(BvnValidating,
            When(BvnOk)
                .ThenAsync(SetState("AwaitingKyc"))
                .PublishAsync(ctx => Task.FromResult(new SignupCmd(ctx.Saga.CorrelationId, new SignupPayload(
                    ctx.Saga.TempName!,
                    ctx.Saga.PhoneNumber!,
                    ctx.Saga.TempNIN!,
                    ctx.Saga.TempBVN!
                ))))
                .TransitionTo(AwaitingKyc),
            When(BvnBad)
                .ThenAsync(SetState("AskBvn"))
                .PublishAsync(ctx => Task.FromResult(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.InvalidBvn,  ctx.Saga.PhoneNumber, "❌ That BVN didn’t validate. Please re-enter your 11-digit BVN.")))
                .TransitionTo(AskBvn)
        );

        During(AwaitingKyc,
            When(SignOk)
                .ThenAsync(async ctx =>
                {
                    await SetState("AwaitingBankLink")(ctx);
                    var svc = ctx.TryGetPayload<IServiceProvider>(out var sp)
                        ? sp.GetService<IConversationStateService>()
                        : null;
                    if (svc != null)
                        await svc.SetUserAsync(ctx.Saga.SessionId, ctx.Message.UserId);
                })
                .PublishAsync(ctx => Task.FromResult(new KycCmd(ctx.Saga.CorrelationId)))
                .TransitionTo(AwaitingBankLink),
            When(SignBad)
                .Then(ctx => ctx.Saga.LastFailureReason = "SignupFailed")
                .Finalize()
        );

        During(AwaitingBankLink,
            When(MandateReadyEvt)
                .ThenAsync(SetState("AwaitingPinSetup"))
                .TransitionTo(AwaitingPinSetup)
        );

        During(AwaitingPinSetup,
            When(BankOk)
                .ThenAsync(SetState("AwaitingPinValidate"))
                .PublishAsync(ctx => Task.FromResult(new PinSetupCmd(ctx.Saga.CorrelationId, string.Empty, string.Empty)))
                .TransitionTo(AwaitingPinValidate),
            When(BankBad)
                .Then(ctx => ctx.Saga.LastFailureReason = "BankLinkFailed")
        );

        During(AwaitingPinValidate,
            When(PinBad)
                .PublishAsync(ctx => Task.FromResult(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.BadPin,  ctx.Saga.PhoneNumber, "⛔ Incorrect PIN. Please try again."))),
            When(IntentEvt, ctx => ctx.Message.Intent is Bot.Shared.Enums.IntentType.Transfer or Bot.Shared.Enums.IntentType.BillPay)
                .Then(ctx =>
                {
                    ctx.Saga.PendingIntentType = ctx.Message.Intent;
                    ctx.Saga.PendingIntentPayload = JsonSerializer.Serialize(ctx.Message);
                })
                .PublishAsync(ctx => Task.FromResult(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.BadPin, ctx.Saga.PhoneNumber, "🔐 Please enter your PIN to proceed."))),
            When(PinSetEvt)
                .ThenAsync(SetState("Ready"))
                .TransitionTo(Ready)
        );

        During(Ready,
            When(IntentEvt, ctx => ctx.Message.Intent is Bot.Shared.Enums.IntentType.Transfer or Bot.Shared.Enums.IntentType.BillPay)
                .ThenAsync(async ctx =>
                {
                    ctx.Saga.PendingIntentType = ctx.Message.Intent;
                    ctx.Saga.PendingIntentPayload = JsonSerializer.Serialize(ctx.Message);
                    await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.BadPin,  ctx.Saga.PhoneNumber, "🔐 Please enter your PIN to proceed."));
                })
                .ThenAsync(SetState("AwaitingPinValidate"))
                .TransitionTo(AwaitingPinValidate)
        );

        During(AwaitingPinValidate,
            When(PinOk)
                .ThenAsync(async ctx =>
                {
                    var intent = ctx.Saga.PendingIntentType;
                    var payloadJson = ctx.Saga.PendingIntentPayload;
                    
                    switch (intent)
                    {
                        case Bot.Shared.Enums.IntentType.Transfer:
                            var transfer = JsonSerializer.Deserialize<UserIntentDetected>(payloadJson!)!;
                            var refGen = ctx.TryGetPayload<IServiceProvider>(out var sp)
                                ? sp.GetService<IReferenceGenerator>()
                                : null;
                    
                    
                            var reference = refGen.GenerateTransferRef(
                                ctx.Saga.CorrelationId,
                                transfer.TransferPayload!.ToAccount,
                                transfer.TransferPayload.BankCode
                            );
                            await ctx.Publish(new TransferCmd(ctx.Saga.CorrelationId, transfer.TransferPayload, reference));
                            break;
                    
                        case Bot.Shared.Enums.IntentType.BillPay:
                            var bill = JsonSerializer.Deserialize<UserIntentDetected>(payloadJson!)!;
                            await ctx.Publish(new BillPayCmd(ctx.Saga.CorrelationId, bill.BillPayload!));
                            break;
                    }
                    
                    ctx.Saga.PendingIntentType = null;
                    ctx.Saga.PendingIntentPayload = null;
                })
                .ThenAsync(SetState("Ready"))
                .TransitionTo(Ready)
        );

        // Recurring flows: handle scheduled executions, failures, and cancellations
        DuringAny(
            When(RecExec)
                .ThenAsync(async ctx =>
                {
                    var intent = ctx.Saga.PendingIntentType;
                    var payload = ctx.Saga.PendingIntentPayload;
                    if (intent.HasValue && payload != null)
                    {
                        try
                        {
                            var detected = JsonSerializer.Deserialize<UserIntentDetected>(payload)!;
                            var refGen = ctx.TryGetPayload<IServiceProvider>(out var sp)
                                ? sp.GetService<IReferenceGenerator>()
                                : null;
                            switch (intent.Value)
                            {
                                case Bot.Shared.Enums.IntentType.Transfer:
                                    var transferPayload = detected.TransferPayload!;
                                    var reference = refGen?.GenerateTransferRef(
                                        ctx.Saga.CorrelationId,
                                        transferPayload.ToAccount,
                                        transferPayload.BankCode
                                    )!;
                                    await ctx.Publish(new TransferCmd(ctx.Saga.CorrelationId, transferPayload, reference));
                                    break;
                                case Bot.Shared.Enums.IntentType.BillPay:
                                    await ctx.Publish(new BillPayCmd(ctx.Saga.CorrelationId, detected.BillPayload!));
                                    break;
                            }
                        }
                        catch (JsonException)
                        {
                        }
                    }
                }),
            When(RecBad)
                .TransitionTo(AwaitingPinValidate),
            When(RecCancel)
                .Then(ctx =>
                {
                    ctx.Saga.PendingIntentType = null;
                    ctx.Saga.PendingIntentPayload = null;
                })
        );

        // Side-effect only: greeting/unknown handled everywhere
        DuringAny(
            When(IntentEvt, ctx => ctx.Message.Intent == Bot.Shared.Enums.IntentType.Greeting)
                .ThenAsync(ctx => ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.Greeting,  ctx.Saga.PhoneNumber, "👋 Hello! Let me know what you'd like to do next."))),
            When(IntentEvt, ctx => ctx.Message.Intent == Bot.Shared.Enums.IntentType.Unknown)
                .ThenAsync(ctx => ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.Unknown,  ctx.Saga.PhoneNumber, "❓ I didn’t get that. Try saying 'check balance' or 'send money'.")))
        );

        SetCompletedWhenFinalized();
    }

    // States
    public State AskFullName { get; } = null!;
    public State AskNin { get; } = null!;
    public State NinValidating { get; } = null!;
    public State AskBvn { get; } = null!;
    public State BvnValidating { get; } = null!;
    public State AwaitingKyc { get; } = null!;
    public State AwaitingBankLink { get; } = null!;
    public State AwaitingPinSetup { get; } = null!;
    public State AwaitingPinValidate { get; } = null!;
    public State Ready { get; } = null!;

    private static Func<BehaviorContext<BotState>, Task> SetState(string s)
    {
        return async ctx =>
        {
            var service = ctx.TryGetPayload<IServiceProvider>(out var sp)
                ? sp.GetRequiredService<IConversationStateService>()
                : null;

            if (service == null)
                throw new InvalidOperationException("IConversationStateService could not be resolved.");

            await service.SetStateAsync(ctx.Saga.SessionId, s);
        };
    }

}
