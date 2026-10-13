using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentinelOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationQuotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxApplications",
                table: "OrganizationSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxCustomDashboards",
                table: "OrganizationSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxMembers",
                table: "OrganizationSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "MaxMonthlyLogs",
                table: "OrganizationSettings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "MaxMonthlyTraces",
                table: "OrganizationSettings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "PlanTier",
                table: "OrganizationSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxApplications",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "MaxCustomDashboards",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "MaxMembers",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "MaxMonthlyLogs",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "MaxMonthlyTraces",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "PlanTier",
                table: "OrganizationSettings");
        }
    }
}
