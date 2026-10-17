using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentinelOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase19AdvancedObservability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "NotificationChannels",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SavedLogQueries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SearchTerm = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ServiceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EnvironmentName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TraceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PropertyFiltersJson = table.Column<string>(type: "jsonb", nullable: true),
                    TimeRange = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedByUserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsShared = table.Column<bool>(type: "boolean", nullable: false),
                    TagsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedLogQueries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedLogQueries_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SavedLogQueries_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpanRecords_TraceId_StartedAtUtc",
                table: "SpanRecords",
                columns: new[] { "TraceId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationChannels_OrganizationId",
                table: "NotificationChannels",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedLogQueries_ApplicationId",
                table: "SavedLogQueries",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedLogQueries_OrganizationId",
                table: "SavedLogQueries",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedLogQueries_OrganizationId_ApplicationId",
                table: "SavedLogQueries",
                columns: new[] { "OrganizationId", "ApplicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedLogQueries_OrganizationId_IsShared",
                table: "SavedLogQueries",
                columns: new[] { "OrganizationId", "IsShared" });

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationChannels_Organizations_OrganizationId",
                table: "NotificationChannels",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationChannels_Organizations_OrganizationId",
                table: "NotificationChannels");

            migrationBuilder.DropTable(
                name: "SavedLogQueries");

            migrationBuilder.DropIndex(
                name: "IX_SpanRecords_TraceId_StartedAtUtc",
                table: "SpanRecords");

            migrationBuilder.DropIndex(
                name: "IX_NotificationChannels_OrganizationId",
                table: "NotificationChannels");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "NotificationChannels");
        }
    }
}
