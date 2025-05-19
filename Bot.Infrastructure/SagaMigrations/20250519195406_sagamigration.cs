using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bot.Infrastructure.SagaMigrations
{
    /// <inheritdoc />
    public partial class sagamigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastIntentHandledAt",
                table: "BotState",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingPayloadHash",
                table: "BotState",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PreviewPublished",
                table: "BotState",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SagaVersion",
                table: "BotState",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TimeoutTokenId",
                table: "BotState",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastIntentHandledAt",
                table: "BotState");

            migrationBuilder.DropColumn(
                name: "PendingPayloadHash",
                table: "BotState");

            migrationBuilder.DropColumn(
                name: "PreviewPublished",
                table: "BotState");

            migrationBuilder.DropColumn(
                name: "SagaVersion",
                table: "BotState");

            migrationBuilder.DropColumn(
                name: "TimeoutTokenId",
                table: "BotState");
        }
    }
}
