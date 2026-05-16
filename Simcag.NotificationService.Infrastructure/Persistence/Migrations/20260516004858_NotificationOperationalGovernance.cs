using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simcag.NotificationService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NotificationOperationalGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlertId",
                table: "notifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContextJson",
                table: "notifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "notifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationalLink",
                table: "notifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayloadSummary",
                table: "notifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "notifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "notifications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "MuteAllUntilUtc",
                table: "notification_preferences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SnoozePriceAlertsUntilUtc",
                table: "notification_preferences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_CorrelationId",
                table: "notifications",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_Status",
                table: "notifications",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_CorrelationId",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_UserId_Status",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "AlertId",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "ContextJson",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "OperationalLink",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "PayloadSummary",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "MuteAllUntilUtc",
                table: "notification_preferences");

            migrationBuilder.DropColumn(
                name: "SnoozePriceAlertsUntilUtc",
                table: "notification_preferences");
        }
    }
}
