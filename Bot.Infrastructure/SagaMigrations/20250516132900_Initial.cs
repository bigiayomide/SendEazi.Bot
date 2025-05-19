#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Bot.Infrastructure.SagaMigrations;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "BotState",
            table => new
            {
                CorrelationId = table.Column<Guid>("uuid", nullable: false),
                CurrentState = table.Column<string>("character varying(64)", maxLength: 64, nullable: false),
                KycApproved = table.Column<bool>("boolean", nullable: false),
                BankLinked = table.Column<bool>("boolean", nullable: false),
                PinSet = table.Column<bool>("boolean", nullable: false),
                PinValidated = table.Column<bool>("boolean", nullable: false),
                ActiveBillId = table.Column<Guid>("uuid", nullable: true),
                ActiveRecurringId = table.Column<Guid>("uuid", nullable: true),
                ActiveGoalId = table.Column<Guid>("uuid", nullable: true),
                LastTransactionId = table.Column<Guid>("uuid", nullable: true),
                LastFailureReason = table.Column<string>("text", nullable: true),
                CreatedUtc = table.Column<DateTime>("timestamp with time zone", nullable: false),
                UpdatedUtc = table.Column<DateTime>("timestamp with time zone", nullable: false),
                TempName = table.Column<string>("text", nullable: true),
                TempNIN = table.Column<string>("text", nullable: true),
                TempBVN = table.Column<string>("text", nullable: true),
                SessionId = table.Column<Guid>("uuid", nullable: false),
                PhoneNumber = table.Column<string>("text", nullable: true),
                PendingIntentType = table.Column<int>("integer", nullable: true),
                PendingIntentPayload = table.Column<string>("text", nullable: true),
                RowVersion = table.Column<byte[]>("bytea", rowVersion: true, nullable: true)
            },
            constraints: table => { table.PrimaryKey("PK_BotState", x => x.CorrelationId); });

        migrationBuilder.CreateTable(
            "InboxState",
            table => new
            {
                Id = table.Column<long>("bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                MessageId = table.Column<Guid>("uuid", nullable: false),
                ConsumerId = table.Column<Guid>("uuid", nullable: false),
                LockId = table.Column<Guid>("uuid", nullable: false),
                RowVersion = table.Column<byte[]>("bytea", rowVersion: true, nullable: true),
                Received = table.Column<DateTime>("timestamp with time zone", nullable: false),
                ReceiveCount = table.Column<int>("integer", nullable: false),
                ExpirationTime = table.Column<DateTime>("timestamp with time zone", nullable: true),
                Consumed = table.Column<DateTime>("timestamp with time zone", nullable: true),
                Delivered = table.Column<DateTime>("timestamp with time zone", nullable: true),
                LastSequenceNumber = table.Column<long>("bigint", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InboxState", x => x.Id);
                table.UniqueConstraint("AK_InboxState_MessageId_ConsumerId", x => new { x.MessageId, x.ConsumerId });
            });

        migrationBuilder.CreateTable(
            "OutboxState",
            table => new
            {
                OutboxId = table.Column<Guid>("uuid", nullable: false),
                LockId = table.Column<Guid>("uuid", nullable: false),
                RowVersion = table.Column<byte[]>("bytea", rowVersion: true, nullable: true),
                Created = table.Column<DateTime>("timestamp with time zone", nullable: false),
                Delivered = table.Column<DateTime>("timestamp with time zone", nullable: true),
                LastSequenceNumber = table.Column<long>("bigint", nullable: true)
            },
            constraints: table => { table.PrimaryKey("PK_OutboxState", x => x.OutboxId); });

        migrationBuilder.CreateTable(
            "OutboxMessage",
            table => new
            {
                SequenceNumber = table.Column<long>("bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                EnqueueTime = table.Column<DateTime>("timestamp with time zone", nullable: true),
                SentTime = table.Column<DateTime>("timestamp with time zone", nullable: false),
                Headers = table.Column<string>("text", nullable: true),
                Properties = table.Column<string>("text", nullable: true),
                InboxMessageId = table.Column<Guid>("uuid", nullable: true),
                InboxConsumerId = table.Column<Guid>("uuid", nullable: true),
                OutboxId = table.Column<Guid>("uuid", nullable: true),
                MessageId = table.Column<Guid>("uuid", nullable: false),
                ContentType = table.Column<string>("character varying(256)", maxLength: 256, nullable: false),
                MessageType = table.Column<string>("text", nullable: false),
                Body = table.Column<string>("text", nullable: false),
                ConversationId = table.Column<Guid>("uuid", nullable: true),
                CorrelationId = table.Column<Guid>("uuid", nullable: true),
                InitiatorId = table.Column<Guid>("uuid", nullable: true),
                RequestId = table.Column<Guid>("uuid", nullable: true),
                SourceAddress = table.Column<string>("character varying(256)", maxLength: 256, nullable: true),
                DestinationAddress = table.Column<string>("character varying(256)", maxLength: 256, nullable: true),
                ResponseAddress = table.Column<string>("character varying(256)", maxLength: 256, nullable: true),
                FaultAddress = table.Column<string>("character varying(256)", maxLength: 256, nullable: true),
                ExpirationTime = table.Column<DateTime>("timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OutboxMessage", x => x.SequenceNumber);
                table.ForeignKey(
                    "FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId",
                    x => new { x.InboxMessageId, x.InboxConsumerId },
                    "InboxState",
                    new[] { "MessageId", "ConsumerId" });
                table.ForeignKey(
                    "FK_OutboxMessage_OutboxState_OutboxId",
                    x => x.OutboxId,
                    "OutboxState",
                    "OutboxId");
            });

        migrationBuilder.CreateIndex(
            "IX_InboxState_Delivered",
            "InboxState",
            "Delivered");

        migrationBuilder.CreateIndex(
            "IX_OutboxMessage_EnqueueTime",
            "OutboxMessage",
            "EnqueueTime");

        migrationBuilder.CreateIndex(
            "IX_OutboxMessage_ExpirationTime",
            "OutboxMessage",
            "ExpirationTime");

        migrationBuilder.CreateIndex(
            "IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber",
            "OutboxMessage",
            new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            "IX_OutboxMessage_OutboxId_SequenceNumber",
            "OutboxMessage",
            new[] { "OutboxId", "SequenceNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            "IX_OutboxState_Created",
            "OutboxState",
            "Created");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "BotState");

        migrationBuilder.DropTable(
            "OutboxMessage");

        migrationBuilder.DropTable(
            "InboxState");

        migrationBuilder.DropTable(
            "OutboxState");
    }
}