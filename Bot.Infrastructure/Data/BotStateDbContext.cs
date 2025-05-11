// Bot.Core.StateMachine/BotStateDbContext.cs

using Bot.Shared.Models;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bot.Infrastructure.Data;

public class BotStateDbContext(DbContextOptions<BotStateDbContext> options) : SagaDbContext(options)
{
    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new BotStateMap(); }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}

public class BotStateMap : SagaClassMap<BotState>
{
    protected override void Configure(EntityTypeBuilder<BotState> entity, ModelBuilder model)
    {
        entity.Property(x => x.CurrentState).HasMaxLength(64);
        // optional concurrency token:
        entity.Property(x => x.RowVersion).IsRowVersion();
    }
}