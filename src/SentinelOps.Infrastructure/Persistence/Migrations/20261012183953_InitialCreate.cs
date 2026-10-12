using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SentinelOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Condition = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Threshold = table.Column<double>(type: "double precision", nullable: false),
                    EvaluationWindowMinutes = table.Column<int>(type: "integer", nullable: false),
                    CooldownMinutes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastEvaluatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ApiKeyHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HealthCheckResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EnvironmentName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ResponseTimeMs = table.Column<double>(type: "double precision", nullable: false),
                    CheckedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthCheckResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EnvironmentName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    MessageTemplate = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Properties = table.Column<string>(type: "jsonb", nullable: false),
                    ExceptionDetail = table.Column<string>(type: "text", nullable: true),
                    TraceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SpanId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetricRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EnvironmentName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MetricName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MetricType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    Tags = table.Column<string>(type: "jsonb", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpanRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpanId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TraceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ParentSpanId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    OperationName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Attributes = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpanRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TraceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TraceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EnvironmentName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RootSpanName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    SpanCount = table.Column<int>(type: "integer", nullable: false),
                    HasErrors = table.Column<bool>(type: "boolean", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertIncidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FiredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertIncidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertIncidents_AlertRules_AlertRuleId",
                        column: x => x.AlertRuleId,
                        principalTable: "AlertRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Environments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EnvironmentType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Environments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Environments_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Services_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertIncidents_AlertRuleId",
                table: "AlertIncidents",
                column: "AlertRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertIncidents_FiredAtUtc",
                table: "AlertIncidents",
                column: "FiredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AlertIncidents_Status",
                table: "AlertIncidents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_ApplicationId",
                table: "AlertRules",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_IsEnabled",
                table: "AlertRules",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_Name",
                table: "Applications",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Action",
                table: "AuditEvents",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Timestamp",
                table: "AuditEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_UserId",
                table: "AuditEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Environments_ApplicationId_Name",
                table: "Environments",
                columns: new[] { "ApplicationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckResults_ApplicationId",
                table: "HealthCheckResults",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckResults_ApplicationId_ServiceName_EnvironmentName",
                table: "HealthCheckResults",
                columns: new[] { "ApplicationId", "ServiceName", "EnvironmentName" });

            migrationBuilder.CreateIndex(
                name: "IX_HealthCheckResults_CheckedAtUtc",
                table: "HealthCheckResults",
                column: "CheckedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_ApplicationId",
                table: "LogEntries",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_ApplicationId_Timestamp",
                table: "LogEntries",
                columns: new[] { "ApplicationId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Severity",
                table: "LogEntries",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Timestamp",
                table: "LogEntries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_TraceId",
                table: "LogEntries",
                column: "TraceId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricRecords_ApplicationId",
                table: "MetricRecords",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricRecords_ApplicationId_MetricName_Timestamp",
                table: "MetricRecords",
                columns: new[] { "ApplicationId", "MetricName", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MetricRecords_MetricName",
                table: "MetricRecords",
                column: "MetricName");

            migrationBuilder.CreateIndex(
                name: "IX_MetricRecords_Timestamp",
                table: "MetricRecords",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Services_ApplicationId_Name",
                table: "Services",
                columns: new[] { "ApplicationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpanRecords_ParentSpanId",
                table: "SpanRecords",
                column: "ParentSpanId");

            migrationBuilder.CreateIndex(
                name: "IX_SpanRecords_SpanId",
                table: "SpanRecords",
                column: "SpanId");

            migrationBuilder.CreateIndex(
                name: "IX_SpanRecords_TraceId",
                table: "SpanRecords",
                column: "TraceId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceRecords_ApplicationId",
                table: "TraceRecords",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceRecords_ApplicationId_StartedAtUtc",
                table: "TraceRecords",
                columns: new[] { "ApplicationId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TraceRecords_StartedAtUtc",
                table: "TraceRecords",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TraceRecords_TraceId",
                table: "TraceRecords",
                column: "TraceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertIncidents");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "Environments");

            migrationBuilder.DropTable(
                name: "HealthCheckResults");

            migrationBuilder.DropTable(
                name: "LogEntries");

            migrationBuilder.DropTable(
                name: "MetricRecords");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "SpanRecords");

            migrationBuilder.DropTable(
                name: "TraceRecords");

            migrationBuilder.DropTable(
                name: "AlertRules");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Applications");
        }
    }
}
