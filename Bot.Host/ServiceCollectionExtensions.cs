using Azure;
using Bot.Core.Models;
using Bot.Core.Providers;
using Bot.Core.Services;
using Bot.Core.StateMachine;
using Bot.Core.StateMachine.Consumers.Chat;
using Bot.Host.BackgroundJobs;
using Bot.Infrastructure.Configuration;
using Bot.Infrastructure.Data;
using Bot.Shared.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenAI;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using StackExchange.Redis;
using SpeechOptions = Bot.Core.Services.SpeechOptions;

namespace Bot.Host;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBotServices(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<ApplicationDbContext>(o =>
            o.UseSqlServer(cfg.GetConnectionString("DefaultConnection")));

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(cfg.GetConnectionString("Redis")));
        services.Configure<ConversationStateOptions>(cfg.GetSection("ConversationState"));
        services.AddScoped<IConversationStateService, ConversationStateService>();

        services.Configure<TemplateSettings>(cfg.GetSection("Templates"));
        services.AddScoped<ITemplateRenderingService, TemplateRenderingService>();

        services.AddScoped<IQuickReplyService, QuickReplyService>()
            .AddScoped<IPinService, PinService>()
            .AddScoped<IUserService, UserService>()
            .AddScoped<IBeneficiaryService, BeneficiaryService>()
            .AddScoped<IBillPayService, BillPayService>()
            .AddScoped<IBudgetService, BudgetService>()
            .AddScoped<IRecurringTransferService, RecurringTransferService>()
            .AddScoped<IRewardService, RewardService>()
            .AddScoped<IPersonalityService, PersonalityService>()
            .AddScoped<ISpeechService, SpeechService>()
            .AddScoped<IFeedbackService, FeedbackService>()
            .AddScoped<INudgeService, NudgeService>()
            .AddScoped<IMemoService, MemoService>()
            .AddScoped<IBillingService, BillingService>()
            .AddScoped<INotificationService, NotificationService>()
            .AddScoped<IIdentityVerificationService, IdentityVerificationService>()
            .AddScoped<IMonoCallbackService, MonoCallbackService>()
            .AddScoped<IOnePipeCallbackService, OnePipeCallbackService>();

        services.Configure<AzureOpenAiOptions>(cfg.GetSection("AzureOpenAI"));
        services.AddSingleton(sp =>
        {
            var opts = cfg.GetSection("AzureOpenAI").Get<AzureOpenAiOptions>()!;
            return new OpenAIClient(new AzureKeyCredential(opts.ApiKey));
        });
        services.AddSingleton<INlpService, NlpService>();

        services.Configure<FormRecognizerOptions>(cfg.GetSection("FormRecognizer"));
        services.AddScoped<IOcrService, OcrService>();

        services.Configure<SpeechOptions>(cfg.GetSection("Speech"));
        services.AddScoped<ITranscriptionService, TranscriptionService>();
        services.Configure<TextToSpeechOptions>(cfg.GetSection("TextToSpeech"));
        services.AddSingleton<ITextToSpeechService, TextToSpeechService>();

        services.Configure<MonoOptions>(cfg.GetSection("Mono"));
        services.AddHttpClient<MonoBankProvider>();
        services.Configure<OnePipeOptions>(cfg.GetSection("OnePipe"));
        services.AddHttpClient<OnePipeBankProvider>();
        services.AddScoped<IBankProviderFactory, BankProviderFactory>();

        services.Configure<WhatsAppOptions>(cfg.GetSection("WhatsApp"));
        services.AddHttpClient<IWhatsAppService, WhatsAppService>();
        services.Configure<SmsOptions>(cfg.GetSection("Sms"));
        services.AddSingleton<ISmsBackupService, SmsBackupService>();

        services.AddSingleton<IJobFactory, SingletonJobFactory>();
        services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();

        services.AddSingleton<RecurringTransferJob>();
        services.AddSingleton(new JobSchedule(typeof(RecurringTransferJob), cfg["Schedules:RecurringTransfer"]));
        // services.AddSingleton<BudgetAlertJob>();
        // services.AddSingleton(new JobSchedule(typeof(BudgetAlertJob), cfg["Schedules:BudgetAlert"]));
        services.AddHostedService<QuartzHostedService>();

        services.AddMassTransit(x =>
        {
            x.AddConsumersFromNamespaceContaining<RawInboundMsgCmdConsumer>();
            x.AddSagaStateMachine<BotStateMachine, BotState, BotStateMachineDefinition>();
            x.AddSagaStateMachine<DirectDebitMandateStateMachine, DirectDebitMandateState,
                DirectDebitMandateMachineDefinition>();
            x.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
            {
                o.UseSqlServer();
                o.UseBusOutbox();
            });
            x.UsingAzureServiceBus((ctx, sbfCfg) =>
            {
                sbfCfg.Host(cfg["AzureSB"]);
                sbfCfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}

public record JobSchedule(Type JobType, string CronExpression);