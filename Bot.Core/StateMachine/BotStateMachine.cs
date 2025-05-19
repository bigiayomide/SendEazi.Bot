using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bot.Core.Services;
using Bot.Shared;
using Bot.Shared.DTOs;
using Bot.Shared.Models;
using Bot.Core.StateMachine.Helpers;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    public Schedule<BotState, TimeoutExpired> TimeoutSchedule { get; private set; } = null!;
    
    private readonly ILogger<BotStateMachine> _logger;

    private static readonly JsonSerializerOptions DedupeJsonOptions = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    public BotStateMachine(ILogger<BotStateMachine> logger)
    {
        _logger = logger;
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
                PhoneNumber = context.Message.PhoneNumber, // Assign if available
                SagaVersion = "v1"
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

        // Inactivity timeout scheduling for input states
        Schedule(() => TimeoutSchedule,
                 x => x.TimeoutTokenId,
                 x =>
                 {
                     x.Delay = TimeSpan.FromMinutes(5);
                     x.Received = r => r.CorrelateById(m => m.Message.CorrelationId);
                 });

        // Idempotency guard: dedupe repeated intents and route them to appropriate flows
                DuringAny(
            When(IntentEvt)
                .ThenAsync(async ctx =>
                {
                    var now = DateTime.UtcNow;
                    var json = JsonSerializer.Serialize(ctx.Message, DedupeJsonOptions);
                    var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(json)));

                    // 🧘 Flood protection BEFORE updating intent timestamp
                    if ((now - ctx.Saga.LastIntentHandledAt)?.TotalSeconds < 2)
                    {
                        await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.FloodProtection, ctx.Saga.PhoneNumber, "⚠️ Please wait a moment before trying again."));
                        return;
                    }

                    if (ctx.Saga.PendingPayloadHash == hash)
                    {
                        _logger.LogWarning("Duplicate intent payload detected for CorrelationId: {Id}", ctx.Saga.CorrelationId);
                        return;
                    }

                    ctx.Saga.LastIntentHandledAt = now;
                    ctx.Saga.PendingIntentPayload = json;
                    ctx.Saga.PendingIntentType = ctx.Message.Intent;
                    ctx.Saga.PendingPayloadHash = hash;

                    // 📊 Publish audit trail
                    await ctx.Publish(new IntentHandledEvent(ctx.Saga.CorrelationId, ctx.Saga.PhoneNumber!, ctx.Message.Intent.ToString(),now));

                    switch (ctx.Message.Intent)
                    {
                        case Bot.Shared.Enums.IntentType.Signup:
                            if (ctx.Saga.KycApproved && ctx.Saga.BankLinked && ctx.Saga.PinSet)
                            {
                                await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.AlreadyOnboarded, ctx.Saga.PhoneNumber, "✅ You're already signed up. Type 'balance' or 'send money' to continue."));
                                return;
                            }
                            await ctx.Publish(new PromptFullNameCmd(ctx.Saga.CorrelationId));
                            await SetState("AskFullName")(ctx);
                            ctx.TransitionToState(AskFullName);
                            break;

                        case Bot.Shared.Enums.IntentType.Transfer:
                        case Bot.Shared.Enums.IntentType.BillPay:
                            await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.BadPin, ctx.Saga.PhoneNumber, "🔐 Please enter your PIN to proceed."));
                            await SetState("AwaitingPinValidate")(ctx);
                            ctx.TransitionToState(AwaitingPinValidate);
                            break;

                        case Bot.Shared.Enums.IntentType.Greeting:
                            await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.Greeting, ctx.Saga.PhoneNumber, "👋 Hello! Let me know what you'd like to do next."));
                            break;

                        case Bot.Shared.Enums.IntentType.Help:
                            await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.Help, ctx.Saga.PhoneNumber, "💡 Try 'send money', 'check balance', 'set goal', or 'cancel'."));
                            break;

                        case Bot.Shared.Enums.IntentType.Cancel:
                            await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.Canceled, ctx.Saga.PhoneNumber, "❌ Operation canceled. Start again anytime."));
                            await ctx.SetCompleted();
                            break;

                        case Bot.Shared.Enums.IntentType.SetGoal:
                            await ctx.Publish(new GoalsCmd(ctx.Saga.CorrelationId, ctx.Message.GoalPayload!));
                            break;

                        case Bot.Shared.Enums.IntentType.ScheduleRecurring:
                            await ctx.Publish(new RecurringCmd(ctx.Saga.CorrelationId, ctx.Message.RecurringPayload!));
                            break;

                        case Bot.Shared.Enums.IntentType.Memo:
                            await ctx.Publish(new MemoCmd(ctx.Saga.CorrelationId, ctx.Message.MemoPayload!));
                            break;

                        case Bot.Shared.Enums.IntentType.Feedback:
                            await ctx.Publish(new FeedbackCmd(ctx.Saga.CorrelationId, ctx.Message.FeedbackPayload!));
                            break;

                        case Bot.Shared.Enums.IntentType.Unknown:
                            await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.Unknown, ctx.Saga.PhoneNumber, "❓ I didn’t get that. Try saying 'check balance' or 'send money'."));
                            break;
                    }
                })
        );

        // Schedule inactivity timeout on state entry for critical input states
        WhenEnter(AskFullName, binder => binder.Schedule(TimeoutSchedule, ctx => new TimeoutExpired { CorrelationId = ctx.Saga.CorrelationId }));
        WhenEnter(AskNin,    binder => binder.Schedule(TimeoutSchedule, ctx => new TimeoutExpired { CorrelationId = ctx.Saga.CorrelationId }));
        WhenEnter(AskBvn,    binder => binder.Schedule(TimeoutSchedule, ctx => new TimeoutExpired { CorrelationId = ctx.Saga.CorrelationId }));
        WhenEnter(AwaitingPinValidate, binder => binder.Schedule(TimeoutSchedule, ctx => new TimeoutExpired { CorrelationId = ctx.Saga.CorrelationId }));

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
            When(IntentEvt, ctx => ctx.Message.Intent == Shared.Enums.IntentType.Signup)
                .ThenAsync(SetState("AskFullName"))
                .PublishAsync(ctx => Task.FromResult(new PromptFullNameCmd(ctx.Saga.CorrelationId)))
                .TransitionTo(AskFullName),

            When(IntentEvt, ctx => ctx.Message.Intent is Shared.Enums.IntentType.Transfer or Shared.Enums.IntentType.BillPay)
                .ThenAsync(ctx => ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.SignupRequired, ctx.Saga.PhoneNumber, "📝 Please sign up before making transactions.")))
                .ThenAsync(SetState("AskFullName"))
                .PublishAsync(ctx => Task.FromResult(new PromptFullNameCmd(ctx.Saga.CorrelationId)))
                .TransitionTo(AskFullName),
            
            When(IntentEvt, ctx => ctx.Message.Intent == Shared.Enums.IntentType.Greeting)
                .ThenAsync(ctx => ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.Greeting, ctx.Saga.PhoneNumber, "👋 Hello! Let me know what you'd like to do next."))),

            When(IntentEvt, ctx => ctx.Message.Intent == Shared.Enums.IntentType.Unknown)
                .ThenAsync(ctx => ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.Unknown, ctx.Saga.PhoneNumber,"❓ I didn’t get that. Try saying 'check balance' or 'send money'.")))
        );

        During(AskFullName,

            When(NameEvt)
                .Unschedule(TimeoutSchedule)
                .Then(ctx => ctx.Saga.TempName = ctx.Message.FullName)
                .ThenAsync(SetState("AskNin"))
                .PublishAsync(ctx => Task.FromResult(new PromptNinCmd(ctx.Saga.CorrelationId)))
                .TransitionTo(AskNin),

            When(TimeoutSchedule.Received)
                .Unschedule(TimeoutSchedule)
                .ThenAsync(async ctx =>
                {
                    await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.TimedOut, ctx.Saga.PhoneNumber!, "⌛ Session expired due to inactivity."));
                })
                .Finalize()
        );

        During(AskNin,

            When(NinEvt)
                .Unschedule(TimeoutSchedule)
                .Then(ctx => ctx.Saga.TempNIN = ctx.Message.NIN)
                .ThenAsync(SetState("NinValidating"))
                .PublishAsync(ctx => Task.FromResult(new ValidateNinCmd(ctx.Saga.CorrelationId, ctx.Message.NIN)))
                .TransitionTo(NinValidating),

            When(TimeoutSchedule.Received)
                .Unschedule(TimeoutSchedule)
                .ThenAsync(async ctx =>
                {
                    await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.TimedOut, ctx.Saga.PhoneNumber!, "⌛ Session expired due to inactivity."));
                })
                .Finalize()
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
                .Unschedule(TimeoutSchedule)
                .Then(ctx => ctx.Saga.TempBVN = ctx.Message.BVN)
                .ThenAsync(SetState("BvnValidating"))
                .PublishAsync(ctx => Task.FromResult(new ValidateBvnCmd(ctx.Saga.CorrelationId, ctx.Message.BVN)))
                .TransitionTo(BvnValidating),

            When(TimeoutSchedule.Received)
                .Unschedule(TimeoutSchedule)
                .ThenAsync(async ctx =>
                {
                    await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.TimedOut, ctx.Saga.PhoneNumber!, "⌛ Session expired due to inactivity."));
                })
                .Finalize()
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


        During(Ready,
            When(PinOk, ctx => ctx.Saga.PendingIntentType.HasValue && string.IsNullOrEmpty(ctx.Saga.PendingIntentPayload))
                .ThenAsync(SetState("AwaitingPinValidate"))
                .TransitionTo(AwaitingPinValidate),
            When(IntentEvt, ctx => ctx.Message.Intent is Shared.Enums.IntentType.Transfer or Shared.Enums.IntentType.BillPay)
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
            When(PinBad)
                .Unschedule(TimeoutSchedule)
                .PublishAsync(ctx => Task.FromResult(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.BadPin,
                    ctx.Saga.PhoneNumber, "⛔ Incorrect PIN. Please try again."))),

            When(IntentEvt,
                    ctx => ctx.Message.Intent is Shared.Enums.IntentType.Transfer or Shared.Enums.IntentType.BillPay)
                .Unschedule(TimeoutSchedule)
                .Then(ctx =>
                {
                    ctx.Saga.PendingIntentType = ctx.Message.Intent;
                    ctx.Saga.PendingIntentPayload = JsonSerializer.Serialize(ctx.Message);
                })
                .PublishAsync(ctx => Task.FromResult(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.BadPin,
                    ctx.Saga.PhoneNumber, "🔐 Please enter your PIN to proceed.")))
                .TransitionTo(AwaitingPinValidate),

            When(PinSetEvt)
                .Unschedule(TimeoutSchedule)
                .ThenAsync(SetState("Ready"))
                .TransitionTo(Ready),

            When(PinOk)
                .Unschedule(TimeoutSchedule)
                .ThenAsync(async ctx =>
                    {
                        if (ctx.Saga.PendingIntentType == Shared.Enums.IntentType.Transfer
                            && !string.IsNullOrEmpty(ctx.Saga.PendingIntentPayload))
                        {
                            try
                            {
                                var detected =
                                    JsonSerializer.Deserialize<UserIntentDetected>(ctx.Saga.PendingIntentPayload!)!;
                                var refGen = ctx.TryGetPayload<IServiceProvider>(out var sp)
                                    ? sp.GetService<IReferenceGenerator>()
                                    : null;
                                var reference = refGen?.GenerateTransferRef(
                                    ctx.Saga.CorrelationId,
                                    detected.TransferPayload!.ToAccount,
                                    detected.TransferPayload.BankCode
                                )!;
                                await ctx.Publish(new TransferCmd(ctx.Saga.CorrelationId, detected.TransferPayload,
                                    reference));
                                ctx.Saga.PendingIntentType = null;
                                ctx.Saga.PendingIntentPayload = null;
                                ctx.Saga.PendingPayloadHash = null;
                                await SetState("Ready")(ctx);
                                await ctx.TransitionToState(Ready);
                            }
                            catch (JsonException)
                            {
                            }
                        }
                        else if (ctx.Saga.PendingIntentType == Shared.Enums.IntentType.BillPay
                                 && !string.IsNullOrEmpty(ctx.Saga.PendingIntentPayload))
                        {
                            try
                            {
                                var detected =
                                    JsonSerializer.Deserialize<UserIntentDetected>(ctx.Saga.PendingIntentPayload!)!;
                                await ctx.Publish(new BillPayCmd(ctx.Saga.CorrelationId, detected.BillPayload!));
                                ctx.Saga.PendingIntentType = null;
                                ctx.Saga.PendingIntentPayload = null;
                                ctx.Saga.PendingPayloadHash = null;
                                await SetState("Ready")(ctx);
                                await ctx.TransitionToState(Ready);
                            }
                            catch (JsonException)
                            {
                            }
                        }
                    }
                ));

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
                                case Shared.Enums.IntentType.Transfer:
                                    var transferPayload = detected.TransferPayload!;
                                    var reference = refGen?.GenerateTransferRef(
                                        ctx.Saga.CorrelationId,
                                        transferPayload.ToAccount,
                                        transferPayload.BankCode
                                    )!;
                                    await ctx.Publish(new TransferCmd(ctx.Saga.CorrelationId, transferPayload, reference));
                                    break;
                                case Shared.Enums.IntentType.BillPay:
                                    await ctx.Publish(new BillPayCmd(ctx.Saga.CorrelationId, detected.BillPayload!));
                                    break;
                            }
                        }
                        catch (JsonException ex)
                        {
                            logger.LogError(ex, "Failed to deserialize RecExec payload for CorrelationId: {Id}", ctx.Saga.CorrelationId);
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
                    ctx.Saga.PendingPayloadHash = null;
                })
                .Unschedule(TimeoutSchedule)
                .Finalize(),
            When(TimeoutSchedule.Received)
                .Unschedule(TimeoutSchedule)
                .ThenAsync(async ctx =>
                {
                    _logger.LogInformation("Timeout reached for CorrelationId: {Id}", ctx.Saga.CorrelationId);
                    await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.TimedOut, ctx.Saga.PhoneNumber!, "⌛ Session expired due to inactivity."));
                })
                .Finalize()
        );


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
            ctx.Saga.LastIntentHandledAt = DateTime.UtcNow;
            ctx.Saga.UpdatedUtc = DateTime.UtcNow;
        };
    }

}
