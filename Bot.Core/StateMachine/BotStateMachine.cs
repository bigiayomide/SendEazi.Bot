// File: Bot.Core.StateMachine/BotStateMachine.cs

using System.Text.Json;
using Bot.Core.Services;
using Bot.Shared;
using Bot.Shared.Models;
using MassTransit;

namespace Bot.Core.StateMachine;

public class BotStateMachine : MassTransitStateMachine<BotState>
{
    private readonly Event<BalanceSent> _balSent = null!;
    private readonly Event<BankLinkFailed> _bankBad = null!;
    private readonly Event<BankLinkSucceeded> _bankOk = null!;
    private readonly Event<BillPayFailed> _billBad = null!;
    private readonly Event<BillPaid> _billOk = null!;
    private readonly Event<BvnRejected> _bvnBad = null!;
    private readonly Event<BvnProvided> _bvnEvt = null!;
    private readonly Event<BvnVerified> _bvnOk = null!;
    private readonly Event<BudgetAlertTriggered> _goalAlert = null!;

    /*────────── Events ─────────*/
    private readonly Event<UserIntentDetected> _intentEvt = null!;
    private readonly Event<KycRejected> _kycBad = null!;
    private readonly Event<KycApproved> _kycOk = null!;
    private readonly Event<MandateReadyToDebit> _mandateReadyEvt = null!;
    private readonly Event<FullNameProvided> _nameEvt = null!;
    private readonly Event<NinRejected> _ninBad = null!;
    private readonly Event<NinProvided> _ninEvt = null!;
    private readonly Event<NinVerified> _ninOk = null!;
    private readonly Event<PinInvalid> _pinBad = null!;
    private readonly Event<PinValidated> _pinOk = null!;
    private readonly Event<PinSetupFailed> _pinSetBad = null!;
    private readonly Event<PinSet> _pinSetEvt = null!;
    private readonly Event<RecurringFailed> _recBad = null!;
    private readonly Event<RecurringCancelled> _recCancel = null!;
    private readonly Event<RecurringExecuted> _recExec = null!;
    private readonly Event<SignupFailed> _signBad = null!;
    private readonly Event<SignupSucceeded> _signOk = null!;
    private readonly Event<TransferFailed> _txBad = null!;
    private readonly Event<TransferCompleted> _txOk = null!;

    public BotStateMachine()
    {
        InstanceState(x => x.CurrentState);

        /*────────── Event Correlations ─────────*/
        Event(() => _intentEvt, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _nameEvt, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _ninEvt, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _ninOk, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _ninBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _bvnEvt, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _bvnOk, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _bvnBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _signOk, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _signBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _kycOk, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _kycBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _bankOk, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _bankBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _pinSetEvt, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _pinSetBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _pinOk, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _pinBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _txOk, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _txBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _billOk, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _billBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _balSent, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _recExec, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _recBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _recCancel, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _goalAlert, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => _mandateReadyEvt, x => x.CorrelateById(m => m.Message.CorrelationId));

        /*────────── Onboarding Flow ─────────*/
        Initially(
            When(_intentEvt, i => i.Message.Intent == "signup")
                .TransitionTo(AskFullName)
                .ThenAsync(SetState("AskFullName"))
                .PublishAsync(c => c.Init<PromptFullNameCmd>(new { c.Saga.CorrelationId }))
        );

        During(AskFullName,
            When(_nameEvt)
                .Then(c => c.Saga.TempName = c.Message.FullName)
                .TransitionTo(AskNin)
                .ThenAsync(SetState("AskNin"))
                .PublishAsync(c => c.Init<PromptNinCmd>(new { c.Saga.CorrelationId }))
        );

        During(AskNin,
            When(_ninEvt)
                .Then(c => c.Saga.TempNIN = c.Message.NIN)
                .TransitionTo(NinValidating)
                .ThenAsync(SetState("NinValidating"))
                .PublishAsync(c => c.Init<ValidateNinCmd>(new { c.Saga.CorrelationId, c.Message.NIN }))
        );

        During(NinValidating,
            When(_ninOk)
                .TransitionTo(AskBvn)
                .ThenAsync(SetState("AskBvn"))
                .PublishAsync(c => c.Init<PromptBvnCmd>(new { c.Saga.CorrelationId })),
            When(_ninBad)
                .PublishAsync(c => c.Init<NudgeCmd>(new
                {
                    c.Saga.CorrelationId,
                    NudgeType = NudgeType.InvalidNin,
                    Text = "❌ That NIN didn’t validate. Please re-enter your 11-digit NIN."
                }))
                .TransitionTo(AskNin)
                .ThenAsync(SetState("AskNin"))
        );

        During(AskBvn,
            When(_bvnEvt)
                .Then(c => c.Saga.TempBVN = c.Message.BVN)
                .TransitionTo(BvnValidating)
                .ThenAsync(SetState("BvnValidating"))
                .PublishAsync(c => c.Init<ValidateBvnCmd>(new { c.Saga.CorrelationId, c.Message.BVN }))
        );

        During(BvnValidating,
            When(_bvnOk)
                .TransitionTo(AwaitingKyc)
                .PublishAsync(c => c.Init<SignupCmd>(new
                {
                    c.Saga.CorrelationId,
                    Payload = new SignupPayload(
                        c.Saga.TempName!,
                        c.Saga.PhoneNumber!,
                        c.Saga.TempNIN!,
                        c.Saga.TempBVN!)
                })),
            When(_bvnBad)
                .PublishAsync(c => c.Init<NudgeCmd>(new
                {
                    c.Saga.CorrelationId,
                    NudgeType = NudgeType.InvalidBvn,
                    Text = "❌ That BVN didn’t validate. Please re-enter your 11-digit BVN."
                }))
                .TransitionTo(AskBvn)
                .ThenAsync(SetState("AskBvn"))
        );

        During(AwaitingKyc,
            When(_signOk)
                .PublishAsync(c => c.Init<KycCmd>(new { c.Saga.CorrelationId }))
                .TransitionTo(AwaitingBankLink),
            When(_signBad)
                .Then(c => c.Saga.LastFailureReason = "SignupFailed")
                .Finalize()
        );

        During(AwaitingBankLink,
            When(_mandateReadyEvt)
                .TransitionTo(AwaitingPinSetup)
                .ThenAsync(SetState("AwaitingPinSetup"))
        );

        During(AwaitingPinSetup,
            When(_bankOk)
                .PublishAsync(c => c.Init<PinSetupCmd>(new { c.Saga.CorrelationId, PinHash = string.Empty }))
                .TransitionTo(AwaitingPinValidate),
            When(_bankBad)
                .Then(c => c.Saga.LastFailureReason = "BankLinkFailed")
        );

        During(AwaitingPinValidate,
            When(_pinBad)
                .PublishAsync(c => c.Init<NudgeCmd>(new
                {
                    c.Saga.CorrelationId,
                    NudgeType = NudgeType.BadPin,
                    Text = "⛔ Incorrect PIN. Please try again."
                }))
        );

        /*────────── Sensitive Intent → PIN Confirmation Flow ─────────*/
        During(Ready,
            When(_intentEvt, ctx => ctx.Message.Intent is "transfer" or "billpay")
                .ThenAsync(async ctx =>
                {
                    ctx.Saga.PendingIntentType = ctx.Message.Intent;
                    ctx.Saga.PendingIntentPayload = JsonSerializer.Serialize(ctx.Message);

                    await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.BadPin,
                        "🔐 Please enter your PIN to proceed."));
                })
                .TransitionTo(AwaitingPinValidate)
                .ThenAsync(SetState("AwaitingPinValidate"))
        );

        During(AwaitingPinValidate,
            When(_pinOk)
                .ThenAsync(async ctx =>
                {
                    var intent = ctx.Saga.PendingIntentType;
                    var payloadJson = ctx.Saga.PendingIntentPayload;

                    switch (intent)
                    {
                        case "transfer":
                            var transfer = JsonSerializer.Deserialize<UserIntentDetected>(payloadJson!)!;
                            var refGen = ctx.GetPayload<IReferenceGenerator>();
                            var reference = refGen.GenerateTransferRef(
                                ctx.Saga.CorrelationId,
                                transfer.TransferPayload!.ToAccount,
                                transfer.TransferPayload.BankCode);

                            await ctx.Publish(new TransferCmd(
                                ctx.Saga.CorrelationId,
                                transfer.TransferPayload,
                                reference));
                            break;

                        case "billpay":
                            var bill = JsonSerializer.Deserialize<UserIntentDetected>(payloadJson!)!;
                            await ctx.Publish(new BillPayCmd(ctx.Saga.CorrelationId, bill.BillPayload!));
                            break;
                    }

                    ctx.Saga.PendingIntentType = null;
                    ctx.Saga.PendingIntentPayload = null;
                })
                .TransitionTo(Ready)
        );

        SetCompletedWhenFinalized();
    }

    /*────────── States ─────────*/
    private State AskFullName { get; } = null!;
    private State AskNin { get; } = null!;
    private State NinValidating { get; } = null!;
    private State AskBvn { get; } = null!;
    private State BvnValidating { get; } = null!;
    private State AwaitingKyc { get; } = null!;
    private State AwaitingBankLink { get; } = null!;
    private State AwaitingPinSetup { get; } = null!;
    private State AwaitingPinValidate { get; } = null!;
    private State Ready { get; } = null!;
    private State AwaitingTransferPin { get; } = null!;

    private static Func<BehaviorContext<BotState>, Task> SetState(string s)
    {
        return ctx => ctx.GetPayload<IConversationStateService>().SetStateAsync(ctx.Saga.SessionId, s);
    }
}