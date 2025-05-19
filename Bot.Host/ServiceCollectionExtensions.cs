using System.ClientModel;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.AI.OpenAI;
using Azure.Identity;
using Bot.Core.Models;
using Bot.Core.Providers;
using Bot.Core.Services;
using Bot.Core.StateMachine;
using Bot.Core.StateMachine.Consumers.Chat;
using Bot.Core.StateMachine.Consumers.MandateSaga;
using Bot.Core.StateMachine.Consumers.Payments;
using Bot.Core.StateMachine.Consumers.UX;
using Bot.Host.BackgroundJobs;
using Bot.Infrastructure.Configuration;
using Bot.Infrastructure.Data;
using Bot.Core.StateMachine.Helpers;
using Bot.Shared.Models;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenAI;
using OpenAI.Chat;
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
            o.UseNpgsql(cfg.GetConnectionString("DefaultConnection")));
        services.AddDbContext<BotStateDbContext>(o =>
            o.UseNpgsql(cfg.GetConnectionString("MassTransitConnection")));

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
            .AddScoped<IOnePipeCallbackService, OnePipeCallbackService>()
            .AddScoped<IPasswordHasher<User>, PasswordHasher<User>>()
            .AddScoped<IReferenceGenerator, ReferenceGenerator>()
            .AddScoped<IChatClientWrapper, ChatClientWrapper>()
            .AddMemoryCache()
            .AddScoped<IDocumentAnalysisClient>(sp =>
            {
                var config = sp.GetRequiredService<IOptions<FormRecognizerOptions>>().Value;
                var client = new DocumentAnalysisClient(new Uri(config.Endpoint), new AzureKeyCredential(config.ApiKey));
                return new DocumentAnalysisClientWrapper(client);
            })
            .AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cfg.GetConnectionString("Redis");
            });

        services.Configure<AzureOpenAiOptions>(cfg.GetSection("AzureOpenAI"));
        services.AddScoped<AzureOpenAIClient>(sp =>
        {
            var opts = cfg.GetSection("AzureOpenAI").Get<AzureOpenAiOptions>()!;
            return new AzureOpenAIClient(new Uri(opts.Endpoint), new ApiKeyCredential(opts.ApiKey));
        });
        services.AddScoped<INlpService, NlpService>();

        services.Configure<FormRecognizerOptions>(cfg.GetSection("FormRecognizer"));
        services.AddScoped<IOcrService, OcrService>();

        services.Configure<SpeechOptions>(cfg.GetSection("Speech"));
        services.AddScoped<ITranscriptionService, TranscriptionService>(x =>
        {
            var region = cfg.GetSection("Transcription").GetSection("Region")!.Value;
            var subscriptionKey = cfg.GetSection("Transcription").GetSection("SubscriptionKey")!.Value;
            return new TranscriptionService(subscriptionKey, region);
        });
        services.Configure<TextToSpeechOptions>(cfg.GetSection("TextToSpeech"));
        services.AddScoped<ITextToSpeechService, TextToSpeechService>();

        services.Configure<MonoOptions>(cfg.GetSection("Mono"));
services.AddHttpClient<MonoBankProvider>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));
        services.Configure<OnePipeOptions>(cfg.GetSection("OnePipe"));
services.AddHttpClient<OnePipeBankProvider>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));
        services.AddScoped<IBankProviderFactory, BankProviderFactory>();


        services.Configure<WhatsAppOptions>(cfg.GetSection("WhatsApp"));
        services.Configure<PromptSettings>(cfg.GetSection("PromptSettings"));
        
        services.AddHttpClient<IWhatsAppService, WhatsAppService>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));
        services.Configure<SmsOptions>(cfg.GetSection("Sms"));
        services.AddScoped<ISmsBackupService, SmsBackupService>();

        services.AddSingleton<IJobFactory, SingletonJobFactory>();
        services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
        services.AddScoped<IEncryptionService, EncryptionService>();

        services.AddScoped<RecurringTransferJob>();
        services.AddSingleton(new JobSchedule(typeof(RecurringTransferJob), cfg["Schedules:RecurringTransfer"]));
        // services.AddSingleton<BudgetAlertJob>();
        // services.AddSingleton(new JobSchedule(typeof(BudgetAlertJob), cfg["Schedules:BudgetAlert"]));
        services.AddHostedService<QuartzHostedService>();
        
        var connectionString = cfg.GetConnectionString("MassTransitConnection");
        ConfigurePostgresTransport(services, connectionString);
        services.AddMassTransit(x =>
        {
            x.AddConsumersFromNamespaceContaining<RawInboundMsgCmdConsumer>();
            x.AddConsumersFromNamespaceContaining<NudgeCmdConsumer>();
            x.AddConsumersFromNamespaceContaining<BalanceCmdConsumer>();
            x.AddConsumersFromNamespaceContaining<StartMandateSetupCmdConsumer>();
            x.AddConsumersFromNamespaceContaining<PromptFullNameCmdConsumer>();
            x.AddConsumersFromNamespaceContaining<PromptNinCmdConsumer>();
            x.AddConsumersFromNamespaceContaining<PromptBvnCmdConsumer>();
            x.AddSagaStateMachine<BotStateMachine, BotState, BotStateMachineDefinition>();
            x.AddSagaStateMachine<DirectDebitMandateStateMachine, DirectDebitMandateState,
                DirectDebitMandateMachineDefinition>();
            x.AddJobSagaStateMachines()
                .EntityFrameworkRepository(r =>
                {
                    r.ExistingDbContext<BotStateDbContext>();
                    r.UsePostgres();
                });
            
            x.SetEntityFrameworkSagaRepositoryProvider(r =>
            {
                r.ExistingDbContext<BotStateDbContext>();
                r.UsePostgres();
            });

            x.AddEntityFrameworkOutbox<BotStateDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });
            // x.UsingAzureServiceBus((ctx, sbfCfg) =>
            // {
            //     sbfCfg.Host(cfg["AzureSB"]);
            //     sbfCfg.ConfigureEndpoints(ctx);
            // });
            
            x.UsingPostgres((context, cfg) =>
            {
                cfg.UseSqlMessageScheduler();
            
                cfg.UseJobSagaPartitionKeyFormatters();
            
                cfg.AutoStart = true;
            
                cfg.ConfigureEndpoints(context);
            });

            x.AddSqlMessageScheduler();
        });

        return services;
    }
    
    private static IServiceCollection ConfigurePostgresTransport(IServiceCollection services, string? connectionString,
        bool create = true,
        bool delete = false)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        services.AddOptions<SqlTransportOptions>().Configure(options =>
        {
            options.Host = builder.Host ?? "localhost";
            options.Database = builder.Database ?? "sample";
            options.Schema = "transport";
            options.Role = "transport";
            options.Port = builder.Port;
            options.Username = builder.Username;
            options.Password = builder.Password;
            options.AdminUsername = builder.Username;
            options.AdminPassword = builder.Password;
        });

        services.AddPostgresMigrationHostedService(x =>
        {
            x.CreateDatabase = false;
            x.CreateInfrastructure = true;
            x.DeleteDatabase = false;
            x.CreateSchema = true;
        });

        return services;
    }

}

public record JobSchedule(Type JobType, string CronExpression);