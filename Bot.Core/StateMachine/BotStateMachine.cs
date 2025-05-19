using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bot.Core.Services;
using Bot.Shared;
using Bot.Shared.DTOs;
using Bot.Shared.Enums;
using Bot.Shared.Models;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bot.Core.StateMachine;

public class BotStateMachine : MassTransitStateMachine<BotState>
{
    private static readonly JsonSerializerOptions DedupeJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<BotStateMachine> _logger;

    // ------------------------------------------------------------------------
    // CONSTRUCTOR
    // ------------------------------------------------------------------------
    public BotStateMachine(ILogger<BotStateMachine> logger)
    {
        _logger = logger;
        InstanceState(x => x.CurrentState);

        ConfigureEvents();
        ConfigureSchedules();
        ConfigureStates();
    }

    public Event<BalanceSent> BalSent { get; } = null!;
    public Event<BankLinkFailed> BankBad { get; } = null!;
    public Event<BankLinkSucceeded> BankOk { get; } = null!;
    public Event<BillPayFailed> BillBad { get; } = null!;
    public Event<BillPaid> BillOk { get; } = null!;
    public Event<BvnRejected> BvnBad { get; } = null!;
    public Event<BvnProvided> BvnEvt { get; } = null!;
    public Event<BvnVerified> BvnOk { get; } = null!;
    public Event<BudgetAlertTriggered> GoalAlert { get; } = null!;
    public Event<UserIntentDetected> IntentEvt { get; } = null!;
    public Event<KycRejected> KycBad { get; } = null!;
    public Event<KycApproved> KycOk { get; } = null!;
    public Event<MandateReadyToDebit> MandateReadyEvt { get; } = null!;
    public Event<FullNameProvided> NameEvt { get; } = null!;
    public Event<NinRejected> NinBad { get; } = null!;
    public Event<NinProvided> NinEvt { get; } = null!;
    public Event<NinVerified> NinOk { get; } = null!;
    public Event<PinInvalid> PinBad { get; } = null!;
    public Event<PinValidated> PinOk { get; } = null!;
    public Event<PinSetupFailed> PinSetBad { get; } = null!;
    public Event<PinSet> PinSetEvt { get; } = null!;
    public Event<RecurringFailed> RecBad { get; } = null!;
    public Event<RecurringCancelled> RecCancel { get; } = null!;
    public Event<RecurringExecuted> RecExec { get; } = null!;
    public Event<SignupFailed> SignBad { get; } = null!;
    public Event<SignupSucceeded> SignOk { get; } = null!;
    public Event<TransferFailed> TxBad { get; } = null!;
    public Event<TransferCompleted> TxOk { get; } = null!;

    // A "scheduled message" type for inactivity timeouts
    public Schedule<BotState, TimeoutExpired> TimeoutSchedule { get; } = null!;

    // ------------------------------------------------------------------------
    // STATES
    // ------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------
    // EVENTS + CORRELATIONS
    // ------------------------------------------------------------------------
    private void ConfigureEvents()
    {
        Event(() => IntentEvt, e =>
        {
            e.CorrelateById(m => m.Message.CorrelationId);
            e.InsertOnInitial = true;
            e.SetSagaFactory(ctx => new BotState
            {
                CorrelationId = ctx.Message.CorrelationId,
                SessionId = Guid.NewGuid(),
                PhoneNumber = ctx.Message.PhoneNumber,
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
        Event(() => TxOk, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => TxBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => BillOk, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => BillBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => BalSent, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => RecExec, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => RecBad, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => RecCancel, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => GoalAlert, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => MandateReadyEvt, x => x.CorrelateById(m => m.Message.CorrelationId));
    }

    // ------------------------------------------------------------------------
    // SCHEDULES (TIMEOUTS)
    // ------------------------------------------------------------------------
    private void ConfigureSchedules()
    {
        Schedule(() => TimeoutSchedule,
            x => x.TimeoutTokenId,
            x =>
            {
                x.Delay = TimeSpan.FromMinutes(5);
                x.Received = r => r.CorrelateById(m => m.Message.CorrelationId);
            }
        );
    }

    private void ConfigureStates()
    {
        // --------------------------------------------------
        // 1) Onboarding
        // --------------------------------------------------
        Initially(
            When(IntentEvt, ctx => ctx.Message.Intent == IntentType.Signup && ctx.Message.SignupPayload is not null)
                .ThenAsync(async ctx =>
                {
                    var payload = ctx.Message.SignupPayload!;
                    _logger.LogInformation("[Initially:Signup] Auto-processing SignupPayload for CorrelationId: {Id}",
                        ctx.Saga.CorrelationId);

                    // Save the “temp” data we’ll need
                    ctx.Saga.TempName = payload.FullName;
                    ctx.Saga.TempNIN = payload.NIN;
                    ctx.Saga.TempBVN = payload.BVN;
                    ctx.Saga.PhoneNumber ??= payload.Phone;

                    // Start validating the NIN
                    await ctx.Publish(new ValidateNinCmd(ctx.Saga.CorrelationId, payload.NIN));
                    await SetState("NinValidating")(ctx);
                })
                .TransitionTo(NinValidating),

            // If user says signup, but no signup payload:
            When(IntentEvt, ctx => ctx.Message.Intent == IntentType.Signup && ctx.Message.SignupPayload == null)
                .ThenAsync(SetState("AskFullName"))
                .PublishAsync(ctx => Task.FromResult(new PromptFullNameCmd(ctx.Saga.CorrelationId)))
                .TransitionTo(AskFullName),

            // If user tries transfer or bill but we have no user yet:
            When(IntentEvt, ctx => ctx.Message.Intent is IntentType.Transfer or IntentType.BillPay)
                .ThenAsync(async ctx =>
                {
                    await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.SignupRequired,
                        ctx.Saga.PhoneNumber, "📝 Please sign up before making transactions."));
                })
                .ThenAsync(SetState("AskFullName"))
                .PublishAsync(ctx => Task.FromResult(new PromptFullNameCmd(ctx.Saga.CorrelationId)))
                .TransitionTo(AskFullName),

            // If user just says “hello” or “unknown” in the initial state, do a quick nudge:
            When(IntentEvt, ctx => ctx.Message.Intent == IntentType.Greeting)
                .ThenAsync(ctx => ctx.Publish(new NudgeCmd(
                    ctx.Saga.CorrelationId,
                    NudgeType.Greeting,
                    ctx.Saga.PhoneNumber,
                    "👋 Hello! Let me know what you'd like to do next."
                ))),
            When(IntentEvt, ctx => ctx.Message.Intent == IntentType.Unknown)
                .ThenAsync(ctx => ctx.Publish(new NudgeCmd(
                    ctx.Saga.CorrelationId,
                    NudgeType.Unknown,
                    ctx.Saga.PhoneNumber,
                    "❓ I didn’t get that. Try 'check balance' or 'send money'."
                ))),
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
                .PublishAsync(ctx =>
                    Task.FromResult(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.TransferFail, ctx.Saga.PhoneNumber)))
        );

        // AskFullName
        During(AskFullName,
            When(NameEvt)
                .Unschedule(TimeoutSchedule)
                .Then(ctx =>
                {
                    _logger.LogInformation("[AskFullName] Full name received: {Name} for CorrelationId: {Id}",
                        ctx.Message.FullName, ctx.Saga.CorrelationId);
                    ctx.Saga.TempName = ctx.Message.FullName;
                })
                .ThenAsync(SetState("AskNin"))
                .PublishAsync(ctx => Task.FromResult(new PromptNinCmd(ctx.Saga.CorrelationId)))
                .TransitionTo(AskNin),
            When(TimeoutSchedule.Received)
                .Unschedule(TimeoutSchedule)
                .ThenAsync(async ctx =>
                {
                    _logger.LogInformation("[AskFullName] Timeout reached for CorrelationId: {Id}",
                        ctx.Saga.CorrelationId);
                    await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.TimedOut,
                        ctx.Saga.PhoneNumber!, "⌛ Session expired due to inactivity."));
                })
                .Finalize()
        );

        // AskNin
        WhenEnter(AskNin,
            b => b.Schedule(TimeoutSchedule, ctx => new TimeoutExpired { CorrelationId = ctx.Saga.CorrelationId }));

        During(AskNin,
            When(NinEvt)
                .Unschedule(TimeoutSchedule)
                .Then(ctx =>
                {
                    _logger.LogInformation("[AskNin] NIN provided: {Nin} for CorrelationId: {Id}",
                        ctx.Message.NIN, ctx.Saga.CorrelationId);
                    ctx.Saga.TempNIN = ctx.Message.NIN;
                })
                .ThenAsync(SetState("NinValidating"))
                .PublishAsync(ctx => Task.FromResult(new ValidateNinCmd(ctx.Saga.CorrelationId, ctx.Message.NIN)))
                .TransitionTo(NinValidating),
            When(TimeoutSchedule.Received)
                .Unschedule(TimeoutSchedule)
                .ThenAsync(async ctx =>
                {
                    _logger.LogInformation("[AskNin] Timeout reached for CorrelationId: {Id}", ctx.Saga.CorrelationId);
                    await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.TimedOut,
                        ctx.Saga.PhoneNumber!, "⌛ Session expired due to inactivity."));
                })
                .Finalize()
        );

        // NinValidating
        During(NinValidating,
            When(NinOk)
                .ThenAsync(SetState("AskBvn"))
                .PublishAsync(ctx => Task.FromResult(new PromptBvnCmd(ctx.Saga.CorrelationId)))
                .TransitionTo(AskBvn),
            When(NinBad)
                .ThenAsync(async ctx =>
                {
                    _logger.LogWarning("[NinValidating] NIN validation failed for CorrelationId: {Id}",
                        ctx.Saga.CorrelationId);
                    await SetState("AskNin")(ctx);
                    await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.InvalidNin,
                        ctx.Saga.PhoneNumber, "❌ That NIN didn’t validate. Please re-enter your 11-digit NIN."));
                })
                .TransitionTo(AskNin)
        );

        // AskBvn
        WhenEnter(AskBvn,
            b => b.Schedule(TimeoutSchedule, ctx => new TimeoutExpired { CorrelationId = ctx.Saga.CorrelationId }));

        During(AskBvn,
            When(BvnEvt)
                .Unschedule(TimeoutSchedule)
                .Then(ctx =>
                {
                    _logger.LogInformation("[AskBvn] BVN provided: {Bvn} for CorrelationId: {Id}",
                        ctx.Message.BVN, ctx.Saga.CorrelationId);
                    ctx.Saga.TempBVN = ctx.Message.BVN;
                })
                .ThenAsync(SetState("BvnValidating"))
                .PublishAsync(ctx => Task.FromResult(new ValidateBvnCmd(ctx.Saga.CorrelationId, ctx.Message.BVN)))
                .TransitionTo(BvnValidating),
            When(TimeoutSchedule.Received)
                .Unschedule(TimeoutSchedule)
                .ThenAsync(async ctx =>
                {
                    _logger.LogInformation("[AskBvn] Timeout reached for CorrelationId: {Id}", ctx.Saga.CorrelationId);
                    await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.TimedOut,
                        ctx.Saga.PhoneNumber!, "⌛ Session expired due to inactivity."));
                })
                .Finalize()
        );

        // BvnValidating
        During(BvnValidating,
            When(BvnOk)
                .ThenAsync(async ctx =>
                {
                    // Double-check we have all the data
                    if (string.IsNullOrWhiteSpace(ctx.Saga.TempName)
                        || string.IsNullOrWhiteSpace(ctx.Saga.PhoneNumber)
                        || string.IsNullOrWhiteSpace(ctx.Saga.TempNIN)
                        || string.IsNullOrWhiteSpace(ctx.Saga.TempBVN))
                    {
                        _logger.LogError("[BvnValidating] Missing signup fields for CorrelationId: {Id}",
                            ctx.Saga.CorrelationId);

                        await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.Unknown, ctx.Saga.PhoneNumber,
                            "❗ Signup information incomplete. Please restart the process."));
                        return;
                    }

                    await SetState("AwaitingKyc")(ctx);

                    await ctx.Publish(new SignupCmd(
                        ctx.Saga.CorrelationId,
                        new SignupPayload(ctx.Saga.TempName, ctx.Saga.PhoneNumber, ctx.Saga.TempNIN, ctx.Saga.TempBVN)
                    ));
                })
                .TransitionTo(AwaitingKyc),
            When(BvnBad)
                .ThenAsync(async ctx =>
                {
                    _logger.LogWarning("[BvnValidating] BVN validation failed for CorrelationId: {Id}",
                        ctx.Saga.CorrelationId);
                    await SetState("AskBvn")(ctx);
                    await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.InvalidBvn, ctx.Saga.PhoneNumber,
                        "❌ That BVN didn’t validate. Please re-enter your 11-digit BVN."));
                })
                .TransitionTo(AskBvn)
        );

        // AwaitingKyc
        During(AwaitingKyc,
            When(SignOk)
                .ThenAsync(async ctx =>
                {
                    _logger.LogInformation("[AwaitingKyc] Signup succeeded for CorrelationId: {Id}",
                        ctx.Saga.CorrelationId);
                    await SetState("AwaitingBankLink")(ctx);

                    // Mark the user in conversation state
                    var sp = ctx.TryGetPayload<IServiceProvider>(out var provider) ? provider : null;
                    var svc = sp?.GetService<IConversationStateService>();
                    if (svc != null)
                        await svc.SetUserAsync(ctx.Saga.SessionId, ctx.Message.UserId);
                })
                .PublishAsync(ctx => Task.FromResult(new KycCmd(ctx.Saga.CorrelationId)))
                .TransitionTo(AwaitingBankLink),
            When(KycBad)
                .ThenAsync(async ctx =>
                {
                    ctx.Saga.LastFailureReason = "KycRejected";
                    await NudgeAndLog(ctx, _logger,
                        "❌ KYC process failed. Please check your details and try again.",
                        NudgeType.KycFailed, "AwaitingKyc");
                })
                .Finalize(),
            When(SignBad)
                .ThenAsync(async ctx =>
                {
                    _logger.LogWarning("[AwaitingKyc] Signup failed for CorrelationId: {Id}", ctx.Saga.CorrelationId);
                    ctx.Saga.LastFailureReason = "SignupFailed";
                    await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.SignupFailed, ctx.Saga.PhoneNumber,
                        "❌ Signup failed. Please try again later."));
                })
                .Finalize()
        );

        // AwaitingBankLink
        During(AwaitingBankLink,
            When(MandateReadyEvt)
                .ThenAsync(SetState("AwaitingPinSetup"))
                .TransitionTo(AwaitingPinSetup)
        );

        // AwaitingPinSetup
        During(AwaitingPinSetup,
            // Moved PinSetEvt from “AwaitingPinValidate” to here!
            When(PinSetEvt)
                .ThenAsync(async ctx =>
                {
                    _logger.LogInformation("[AwaitingPinSetup] PIN setup complete for CorrelationId: {Id}",
                        ctx.Saga.CorrelationId);
                    await SetState("Ready")(ctx);
                })
                .TransitionTo(Ready),
            When(BankOk)
                .ThenAsync(SetState("AwaitingPinSetup"))
                .PublishAsync(ctx =>
                    Task.FromResult(new PinSetupCmd(ctx.Saga.CorrelationId, string.Empty, string.Empty)))
                .TransitionTo(AwaitingPinSetup),
            When(BankBad)
                .ThenAsync(async ctx =>
                {
                    _logger.LogWarning("[AwaitingPinSetup] Bank linking failed for CorrelationId: {Id}",
                        ctx.Saga.CorrelationId);
                    ctx.Saga.LastFailureReason = "BankLinkFailed";
                    await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.BankFailed, ctx.Saga.PhoneNumber!,
                        "❌ We couldn't link your bank. Please try again later."));
                })
                .TransitionTo(AwaitingBankLink)
        );

        // --------------------------------------------------
        // 2) Transaction Flows (Transfer, BillPay, etc.)
        // --------------------------------------------------
        // Ready
        During(Ready,
            // In “Ready,” if user attempts a new Transfer or BillPay, we capture it, then ask for PIN
            When(IntentEvt, ctx => ctx.Message.Intent is IntentType.Transfer or IntentType.BillPay)
                .ThenAsync(async ctx =>
                {
                    // Save new “pending” intent
                    var json = JsonSerializer.Serialize(ctx.Message, DedupeJsonOptions);
                    var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(json)));

                    _logger.LogInformation("[READY->IntentEvt] New intent: {Intent} for CorrelationId: {Id}",
                        ctx.Message.Intent, ctx.Saga.CorrelationId);

                    ctx.Saga.PendingIntentType = ctx.Message.Intent;
                    ctx.Saga.PendingIntentPayload = json;
                    ctx.Saga.PendingPayloadHash = hash;

                    // Ask user for PIN
                    await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.BadPin,
                        ctx.Saga.PhoneNumber!, "🔐 Please enter your PIN to proceed."));

                    // Switch to AwaitingPinValidate
                    await SetState("AwaitingPinValidate")(ctx);
                })
                .TransitionTo(AwaitingPinValidate),

            // A fallback scenario: if “PinOk” arrives while we do NOT have a pending payload, 
            // we remain in Ready
            When(PinOk,
                    ctx => ctx.Saga.PendingIntentType.HasValue && string.IsNullOrEmpty(ctx.Saga.PendingIntentPayload))
                .ThenAsync(async ctx =>
                {
                    _logger.LogWarning("[READY->PinOk] Empty PendingIntentPayload for CorrelationId: {Id}",
                        ctx.Saga.CorrelationId);
                    await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.Unknown,
                        ctx.Saga.PhoneNumber, "❗ Something went wrong. Please try again."));

                    ctx.Saga.PendingIntentType = null;
                    ctx.Saga.PendingPayloadHash = null;
                    await SetState("Ready")(ctx);
                })
                .TransitionTo(Ready)
        );

        // AwaitingPinValidate
        WhenEnter(AwaitingPinValidate,
            b => b.Schedule(TimeoutSchedule, ctx => new TimeoutExpired { CorrelationId = ctx.Saga.CorrelationId }));

        During(AwaitingPinValidate,
            // If user enters a bad PIN
            When(PinBad)
                .Unschedule(TimeoutSchedule)
                .PublishAsync(ctx => Task.FromResult(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.BadPin,
                    ctx.Saga.PhoneNumber, "⛔ Incorrect PIN. Please try again.")))
                .TransitionTo(AwaitingPinValidate),

            // If user tries to change the pending intent (e.g., “Wait, I want to pay a bill instead”)
            When(IntentEvt, ctx => ctx.Message.Intent is IntentType.Transfer or IntentType.BillPay)
                .Unschedule(TimeoutSchedule)
                .Then(ctx =>
                {
                    ctx.Saga.PendingIntentType = ctx.Message.Intent;
                    ctx.Saga.PendingIntentPayload = JsonSerializer.Serialize(ctx.Message, DedupeJsonOptions);
                })
                .PublishAsync(ctx => Task.FromResult(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.BadPin,
                    ctx.Saga.PhoneNumber, "🔐 Please enter your PIN to proceed.")))
                .TransitionTo(AwaitingPinValidate),

            // If the user does PIN validation for a transaction:
            When(PinOk)
                .Unschedule(TimeoutSchedule)
                .Then(context => { context.Saga.PinValidated = false; })
                .ThenAsync(async ctx =>
                {
                    if (string.IsNullOrEmpty(ctx.Saga.PendingIntentPayload))
                    {
                        // If we have no pending payload, there's nothing to do
                        await NudgeAndLog(ctx, _logger,
                            "❗ No pending intent found.",
                            NudgeType.Unknown, "PIN_OK:NoIntent");
                        return;
                    }

                    try
                    {
                        switch (ctx.Saga.PendingIntentType)
                        {
                            case IntentType.Transfer:
                                var detectedXfer =
                                    JsonSerializer.Deserialize<UserIntentDetected>(ctx.Saga.PendingIntentPayload!,
                                        DedupeJsonOptions)!;
                                var sp = ctx.TryGetPayload<IServiceProvider>(out var provider) ? provider : null;
                                var refGen = sp?.GetService<IReferenceGenerator>();

                                if (refGen == null)
                                {
                                    await NudgeAndLog(ctx, _logger,
                                        "⚠️ Transfer could not be processed: missing reference generator",
                                        NudgeType.Unknown, "PIN_OK:MissingRefGen");
                                    return;
                                }

                                var reference = refGen.GenerateTransferRef(ctx.Saga.CorrelationId,
                                    detectedXfer.TransferPayload!.ToAccount, detectedXfer.TransferPayload.BankCode);

                                // Publish the TransferCmd
                                await ctx.Publish(new TransferCmd(
                                    ctx.Saga.CorrelationId,
                                    detectedXfer.TransferPayload,
                                    reference
                                ));
                                ctx.Saga.PinValidated = true;
                                break;

                            case IntentType.BillPay:
                                var detectedBill =
                                    JsonSerializer.Deserialize<UserIntentDetected>(ctx.Saga.PendingIntentPayload!,
                                        DedupeJsonOptions)!;
                                // Publish the BillPayCmd
                                await ctx.Publish(new BillPayCmd(ctx.Saga.CorrelationId, detectedBill.BillPayload!));
                                ctx.Saga.PinValidated = true;
                                break;

                            default:
                                await NudgeAndLog(ctx, _logger,
                                    "❗ Invalid intent state.",
                                    NudgeType.Unknown, "PIN_OK:InvalidIntent");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[PIN_OK] Error processing intent for {Id}", ctx.Saga.CorrelationId);
                        await NudgeAndLog(ctx, _logger,
                            "❗ Something went wrong.",
                            NudgeType.Unknown, "PIN_OK:Error");
                        throw;
                    }
                })
                .Then(context =>
                {
                    // If we succeeded, we set “PinValidated = true” and move to Ready
                    // If not, we remain in AwaitingPinValidate
                    if (!context.Saga.PinValidated) ResetIntentState(context.Saga);
                })
                .If(ctx => ctx.Saga.PinValidated,
                    thenBinder => thenBinder.TransitionTo(Ready))
                .If(ctx => !ctx.Saga.PinValidated,
                    elseBinder => elseBinder.TransitionTo(AwaitingPinValidate))
        );

        // --------------------------------------------------
        // 3) Recurring flows, timeouts, etc.
        // --------------------------------------------------
        DuringAny(
            When(BillBad)
                .ThenAsync(async ctx =>
                {
                    await NudgeAndLog(ctx, _logger,
                        "❌ Your bill payment could not be completed.",
                        NudgeType.BillFailed, "BillBad");
                }),
            When(RecExec)
                .ThenAsync(async ctx =>
                {
                    if (string.IsNullOrEmpty(ctx.Saga.PendingIntentPayload) || ctx.Saga.PendingIntentType is null)
                    {
                        await NudgeAndLog(ctx, _logger,
                            "⚠️ Couldn't process your recurring transaction.",
                            NudgeType.Unknown, "RecExec:MissingPayload");
                        return;
                    }

                    try
                    {
                        var detected =
                            JsonSerializer.Deserialize<UserIntentDetected>(ctx.Saga.PendingIntentPayload,
                                DedupeJsonOptions)!;
                        var sp = ctx.TryGetPayload<IServiceProvider>(out var provider) ? provider : null;
                        var refGen = sp?.GetService<IReferenceGenerator>();

                        switch (ctx.Saga.PendingIntentType)
                        {
                            case IntentType.Transfer:
                                var refId = refGen?.GenerateTransferRef(ctx.Saga.CorrelationId,
                                    detected.TransferPayload!.ToAccount, detected.TransferPayload.BankCode);
                                _logger.LogInformation("[RecExec] Exec recurring TransferCmd for CorrelationId: {Id}",
                                    ctx.Saga.CorrelationId);
                                await ctx.Publish(new TransferCmd(ctx.Saga.CorrelationId, detected.TransferPayload!,
                                    refId!));
                                break;

                            case IntentType.BillPay:
                                _logger.LogInformation("[RecExec] Exec recurring BillPayCmd for CorrelationId: {Id}",
                                    ctx.Saga.CorrelationId);
                                await ctx.Publish(new BillPayCmd(ctx.Saga.CorrelationId, detected.BillPayload!));
                                break;
                        }

                        ResetIntentState(ctx.Saga);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "[RecExec] Deserialization failed for CorrelationId: {Id}",
                            ctx.Saga.CorrelationId);
                        await NudgeAndLog(ctx, _logger,
                            "⚠️ Couldn't process your recurring transaction.",
                            NudgeType.Unknown, "RecExec:JsonError");
                    }
                }),
            When(RecBad)
                .ThenAsync(async ctx =>
                {
                    await NudgeAndLog(ctx, _logger,
                        "⚠️ A recurring transaction failed. Please re-authenticate.",
                        NudgeType.RecurringFailed, "RecBad");
                })
                .TransitionTo(AwaitingPinValidate),
            When(RecCancel)
                .ThenAsync(async ctx =>
                {
                    ResetIntentState(ctx.Saga);
                    await NudgeAndLog(ctx, _logger,
                        "✅ Your recurring transaction has been cancelled.",
                        NudgeType.Canceled, "RecCancel");
                })
                .Unschedule(TimeoutSchedule)
                .Finalize(),

            // Any state that times out finalizes
            When(TimeoutSchedule.Received)
                .Unschedule(TimeoutSchedule)
                .ThenAsync(async ctx =>
                {
                    _logger.LogInformation("[Timeout] Session expired for CorrelationId: {Id}", ctx.Saga.CorrelationId);
                    await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, NudgeType.TimedOut,
                        ctx.Saga.PhoneNumber!, "⌛ Session expired due to inactivity."));
                })
                .Finalize()
        );
    }

    // ------------------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------------------
    private static Func<BehaviorContext<BotState>, Task> SetState(string s)
    {
        return async ctx =>
        {
            var sp = ctx.TryGetPayload<IServiceProvider>(out var provider) ? provider : null;
            var service = sp?.GetService<IConversationStateService>();

            if (service == null)
                throw new InvalidOperationException("IConversationStateService is not registered.");

            await service.SetStateAsync(ctx.Saga.SessionId, s);
            ctx.Saga.LastIntentHandledAt = DateTime.UtcNow;
            ctx.Saga.UpdatedUtc = DateTime.UtcNow;
        };
    }

    private static async Task NudgeAndLog(
        BehaviorContext<BotState> ctx,
        ILogger logger,
        string message,
        NudgeType type,
        string reason)
    {
        logger.LogWarning("[{Reason}] {Message} (CorrelationId: {Id})", reason, message, ctx.Saga.CorrelationId);
        await ctx.Publish(new NudgeCmd(ctx.Saga.CorrelationId, type, ctx.Saga.PhoneNumber!, message));
    }

    private static void ResetIntentState(BotState saga)
    {
        saga.PendingIntentType = null;
        saga.PendingIntentPayload = null;
        saga.PendingPayloadHash = null;
    }
}