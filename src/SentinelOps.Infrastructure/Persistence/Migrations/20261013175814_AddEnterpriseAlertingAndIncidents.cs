using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentinelOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseAlertingAndIncidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Condition",
                table: "AlertRules",
                newName: "ConditionType");

            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveCountRequired",
                table: "AlertRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AlertRules",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentConsecutiveFailures",
                table: "AlertRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EnvironmentName",
                table: "AlertRules",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EvaluationFrequencyMinutes",
                table: "AlertRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFiredAtUtc",
                table: "AlertRules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LastMeasuredValue",
                table: "AlertRules",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastResolvedAtUtc",
                table: "AlertRules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Operator",
                table: "AlertRules",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ServiceName",
                table: "AlertRules",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "AlertRules",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "SuppressedUntilUtc",
                table: "AlertRules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "AlertRules",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AlertEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MeasuredValue = table.Column<double>(type: "double precision", nullable: false),
                    Threshold = table.Column<double>(type: "double precision", nullable: false),
                    IsTriggered = table.Column<bool>(type: "boolean", nullable: false),
                    Details = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertEvaluations_AlertRules_AlertRuleId",
                        column: x => x.AlertRuleId,
                        principalTable: "AlertRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MeasuredValue = table.Column<double>(type: "double precision", nullable: false),
                    Threshold = table.Column<double>(type: "double precision", nullable: false),
                    TriggeredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertEvents_AlertRules_AlertRuleId",
                        column: x => x.AlertRuleId,
                        principalTable: "AlertRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InAppNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Severity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LinkUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InAppNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvironmentName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ServiceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AssignedToUserId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    AssignedToUserName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    MitigatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MitigatedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    RootCauseSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RelatedTraceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RelatedLogId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationChannels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Destination = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    WebhookSecret = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CustomHeadersJson = table.Column<string>(type: "text", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationChannels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceLevelObjectives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EnvironmentName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IndicatorType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TargetPercentage = table.Column<double>(type: "double precision", nullable: false),
                    EvaluationWindow = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LatencyThresholdMs = table.Column<double>(type: "double precision", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentCompliancePercentage = table.Column<double>(type: "double precision", nullable: true),
                    CurrentErrorBudgetRemaining = table.Column<double>(type: "double precision", nullable: true),
                    BurnRate = table.Column<double>(type: "double precision", nullable: true),
                    IsBreached = table.Column<bool>(type: "boolean", nullable: false),
                    LastEvaluatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceLevelObjectives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncidentAlertRelations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentAlertRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentAlertRelations_AlertRules_AlertRuleId",
                        column: x => x.AlertRuleId,
                        principalTable: "AlertRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncidentAlertRelations_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncidentComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    UserName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CommentText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentComments_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncidentTimelineEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ActorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentTimelineEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentTimelineEvents_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertRuleNotificationChannels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRuleNotificationChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertRuleNotificationChannels_AlertRules_AlertRuleId",
                        column: x => x.AlertRuleId,
                        principalTable: "AlertRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlertRuleNotificationChannels_NotificationChannels_Notifica~",
                        column: x => x.NotificationChannelId,
                        principalTable: "NotificationChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationOutboxes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Subject = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAttemptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TraceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationOutboxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationOutboxes_NotificationChannels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "NotificationChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SloEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceLevelObjectiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalRequests = table.Column<long>(type: "bigint", nullable: false),
                    GoodRequests = table.Column<long>(type: "bigint", nullable: false),
                    CompliancePercentage = table.Column<double>(type: "double precision", nullable: false),
                    ErrorBudgetRemaining = table.Column<double>(type: "double precision", nullable: false),
                    BurnRate = table.Column<double>(type: "double precision", nullable: false),
                    IsBreached = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SloEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SloEvaluations_ServiceLevelObjectives_ServiceLevelObjective~",
                        column: x => x.ServiceLevelObjectiveId,
                        principalTable: "ServiceLevelObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_LastEvaluatedAtUtc",
                table: "AlertRules",
                column: "LastEvaluatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_Status",
                table: "AlertRules",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvaluations_AlertRuleId",
                table: "AlertEvaluations",
                column: "AlertRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvaluations_EvaluatedAtUtc",
                table: "AlertEvaluations",
                column: "EvaluatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_AlertRuleId",
                table: "AlertEvents",
                column: "AlertRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_TriggeredAtUtc",
                table: "AlertEvents",
                column: "TriggeredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRuleNotificationChannels_AlertRuleId_NotificationChann~",
                table: "AlertRuleNotificationChannels",
                columns: new[] { "AlertRuleId", "NotificationChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlertRuleNotificationChannels_NotificationChannelId",
                table: "AlertRuleNotificationChannels",
                column: "NotificationChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_InAppNotifications_ApplicationId",
                table: "InAppNotifications",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_InAppNotifications_CreatedAtUtc",
                table: "InAppNotifications",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InAppNotifications_IsRead",
                table: "InAppNotifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_InAppNotifications_Role",
                table: "InAppNotifications",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_InAppNotifications_UserId",
                table: "InAppNotifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentAlertRelations_AlertRuleId",
                table: "IncidentAlertRelations",
                column: "AlertRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentAlertRelations_IncidentId_AlertRuleId",
                table: "IncidentAlertRelations",
                columns: new[] { "IncidentId", "AlertRuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncidentComments_CreatedAtUtc",
                table: "IncidentComments",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentComments_IncidentId",
                table: "IncidentComments",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ApplicationId",
                table: "Incidents",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_CreatedAtUtc",
                table: "Incidents",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_IncidentNumber",
                table: "Incidents",
                column: "IncidentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_Severity",
                table: "Incidents",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_Status",
                table: "Incidents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentTimelineEvents_IncidentId",
                table: "IncidentTimelineEvents",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentTimelineEvents_TimestampUtc",
                table: "IncidentTimelineEvents",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationChannels_ApplicationId",
                table: "NotificationChannels",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationChannels_IsEnabled",
                table: "NotificationChannels",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutboxes_ChannelId",
                table: "NotificationOutboxes",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutboxes_CreatedAtUtc",
                table: "NotificationOutboxes",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutboxes_NextRetryAtUtc",
                table: "NotificationOutboxes",
                column: "NextRetryAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutboxes_Status",
                table: "NotificationOutboxes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceLevelObjectives_ApplicationId",
                table: "ServiceLevelObjectives",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceLevelObjectives_IsEnabled",
                table: "ServiceLevelObjectives",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceLevelObjectives_ServiceName",
                table: "ServiceLevelObjectives",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_SloEvaluations_EvaluatedAtUtc",
                table: "SloEvaluations",
                column: "EvaluatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SloEvaluations_ServiceLevelObjectiveId",
                table: "SloEvaluations",
                column: "ServiceLevelObjectiveId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertEvaluations");

            migrationBuilder.DropTable(
                name: "AlertEvents");

            migrationBuilder.DropTable(
                name: "AlertRuleNotificationChannels");

            migrationBuilder.DropTable(
                name: "InAppNotifications");

            migrationBuilder.DropTable(
                name: "IncidentAlertRelations");

            migrationBuilder.DropTable(
                name: "IncidentComments");

            migrationBuilder.DropTable(
                name: "IncidentTimelineEvents");

            migrationBuilder.DropTable(
                name: "NotificationOutboxes");

            migrationBuilder.DropTable(
                name: "SloEvaluations");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "NotificationChannels");

            migrationBuilder.DropTable(
                name: "ServiceLevelObjectives");

            migrationBuilder.DropIndex(
                name: "IX_AlertRules_LastEvaluatedAtUtc",
                table: "AlertRules");

            migrationBuilder.DropIndex(
                name: "IX_AlertRules_Status",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "ConsecutiveCountRequired",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "CurrentConsecutiveFailures",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "EnvironmentName",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "EvaluationFrequencyMinutes",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "LastFiredAtUtc",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "LastMeasuredValue",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "LastResolvedAtUtc",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "Operator",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "ServiceName",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "SuppressedUntilUtc",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "AlertRules");

            migrationBuilder.RenameColumn(
                name: "ConditionType",
                table: "AlertRules",
                newName: "Condition");
        }
    }
}
