#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Bot.Infrastructure.SagaMigrations;

/// <inheritdoc />
public partial class sagamigration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            "LastIntentHandledAt",
            "BotState",
            "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            "PendingPayloadHash",
            "BotState",
            "text",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            "PreviewPublished",
            "BotState",
            "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            "SagaVersion",
            "BotState",
            "text",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            "TimeoutTokenId",
            "BotState",
            "uuid",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            "LastIntentHandledAt",
            "BotState");

        migrationBuilder.DropColumn(
            "PendingPayloadHash",
            "BotState");

        migrationBuilder.DropColumn(
            "PreviewPublished",
            "BotState");

        migrationBuilder.DropColumn(
            "SagaVersion",
            "BotState");

        migrationBuilder.DropColumn(
            "TimeoutTokenId",
            "BotState");
    }
}