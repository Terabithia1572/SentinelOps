using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentinelOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomDashboards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomDashboards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsShared = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedByUserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RefreshIntervalSeconds = table.Column<int>(type: "integer", nullable: false),
                    DefaultTimeRange = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TagsJson = table.Column<string>(type: "jsonb", nullable: false),
                    LayoutJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomDashboards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomDashboards_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomDashboards_OrganizationId",
                table: "CustomDashboards",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomDashboards_OrganizationId_IsDefault",
                table: "CustomDashboards",
                columns: new[] { "OrganizationId", "IsDefault" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomDashboards");
        }
    }
}
