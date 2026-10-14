using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SentinelOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaasCommercialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AiInvocationsMonthToDate",
                table: "OrganizationUsages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "AiTokensUsedMonthToDate",
                table: "OrganizationUsages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "IsAiEnabled",
                table: "OrganizationSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxMonthlyAiTokens",
                table: "OrganizationSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PreferredAiProvider",
                table: "OrganizationSettings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BillingAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TaxId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    BillingAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    BillingCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BillingCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BillingPostalCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PaymentProviderCustomerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaymentMethodSummary = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsExemptFromTax = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingAccounts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncidentRcas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ImpactedServicesJson = table.Column<string>(type: "text", nullable: false),
                    HypothesesJson = table.Column<string>(type: "text", nullable: false),
                    CorrelatedAnomaliesJson = table.Column<string>(type: "text", nullable: false),
                    CorrelatedTraceIdsJson = table.Column<string>(type: "text", nullable: false),
                    GeneratedByProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsDeterministic = table.Column<bool>(type: "boolean", nullable: false),
                    OperatorVerificationStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VerifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OperatorNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentRcas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentRcas_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncidentRcas_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogPatterns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PatternFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Template = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalOccurrences = table.Column<long>(type: "bigint", nullable: false),
                    FirstSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SampleMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SampleTraceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogPatterns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogPatterns_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LogPatterns_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EventId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SignatureHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PredictiveForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MetricName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CurrentValue = table.Column<double>(type: "double precision", nullable: false),
                    PredictedValue = table.Column<double>(type: "double precision", nullable: false),
                    TrendSlope = table.Column<double>(type: "double precision", nullable: false),
                    ForecastHorizonMinutes = table.Column<int>(type: "integer", nullable: false),
                    ProjectedBreachTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfidenceIntervalLower = table.Column<double>(type: "double precision", nullable: false),
                    ConfidenceIntervalUpper = table.Column<double>(type: "double precision", nullable: false),
                    CalculationMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictiveForecasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PredictiveForecasts_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PredictiveForecasts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Tier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MonthlyPriceUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AnnualPriceUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxApplications = table.Column<int>(type: "integer", nullable: false),
                    MaxMembers = table.Column<int>(type: "integer", nullable: false),
                    MaxCustomDashboards = table.Column<int>(type: "integer", nullable: false),
                    MaxMonthlyLogs = table.Column<long>(type: "bigint", nullable: false),
                    MaxMonthlyTraces = table.Column<long>(type: "bigint", nullable: false),
                    MaxMonthlyMetrics = table.Column<long>(type: "bigint", nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    MaxMonthlyAiTokens = table.Column<int>(type: "integer", nullable: false),
                    AllowCustomReports = table.Column<bool>(type: "boolean", nullable: false),
                    AllowSloAlerts = table.Column<bool>(type: "boolean", nullable: false),
                    AllowApiAccess = table.Column<bool>(type: "boolean", nullable: false),
                    AllowDedicatedSupport = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelemetryAnomalies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SignalType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DetectionTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BaselineValue = table.Column<double>(type: "double precision", nullable: false),
                    ObservedValue = table.Column<double>(type: "double precision", nullable: false),
                    DeviationScore = table.Column<double>(type: "double precision", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DetectionMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Explanation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RelatedTelemetryReferences = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SuppressedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetryAnomalies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelemetryAnomalies_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TelemetryAnomalies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BillingInterval = table.Column<int>(type: "integer", nullable: false),
                    CurrentPeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TrialEndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GracePeriodEndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanceledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelAtPeriodEnd = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentProviderSubscriptionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ConcurrencyToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationSubscriptions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationSubscriptions_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaaSInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AmountDue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    PeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentProviderInvoiceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaymentProviderPaymentIntentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BillingReason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaaSInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaaSInvoices_OrganizationSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "OrganizationSubscriptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SaaSInvoices_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousStatus = table.Column<int>(type: "integer", nullable: true),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    PreviousPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    NewPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChangedByUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionHistories_OrganizationSubscriptions_Subscriptio~",
                        column: x => x.SubscriptionId,
                        principalTable: "OrganizationSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLineItems_SaaSInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "SaaSInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    PaymentProviderTransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaymentProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SettledByUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SettledReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_SaaSInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "SaaSInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id", "AllowApiAccess", "AllowCustomReports", "AllowDedicatedSupport", "AllowSloAlerts", "AnnualPriceUsd", "CreatedAtUtc", "Description", "IsActive", "MaxApplications", "MaxCustomDashboards", "MaxMembers", "MaxMonthlyAiTokens", "MaxMonthlyLogs", "MaxMonthlyMetrics", "MaxMonthlyTraces", "MonthlyPriceUsd", "Name", "RetentionDays", "Tier", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), true, false, false, false, 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Essential telemetry and observability for small teams and experimental services.", true, 3, 3, 3, 10000, 100000L, 100000L, 50000L, 0m, "Starter Plan", 7, "Starter", null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), true, true, false, true, 490m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Full-featured observability, distributed tracing, and statistical anomaly detection for growing engineering organizations.", true, 20, 15, 25, 100000, 1000000L, 1000000L, 500000L, 49m, "Professional Plan", 30, "Pro", null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), true, true, true, true, 2490m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "High-scale multi-cluster observability, predictive operations, long-term retention, and dedicated SLA.", true, 100, 50, 100, 1000000, 10000000L, 10000000L, 5000000L, 249m, "Enterprise Plan", 90, "Enterprise", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingAccounts_OrganizationId",
                table: "BillingAccounts",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingAccounts_PaymentProviderCustomerId",
                table: "BillingAccounts",
                column: "PaymentProviderCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentRcas_IncidentId",
                table: "IncidentRcas",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentRcas_OrganizationId",
                table: "IncidentRcas",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItems_InvoiceId",
                table: "InvoiceLineItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_LogPatterns_ApplicationId_PatternFingerprint",
                table: "LogPatterns",
                columns: new[] { "ApplicationId", "PatternFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogPatterns_OrganizationId",
                table: "LogPatterns",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_LogPatterns_OrganizationId_LastSeenAtUtc",
                table: "LogPatterns",
                columns: new[] { "OrganizationId", "LastSeenAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSubscriptions_OrganizationId",
                table: "OrganizationSubscriptions",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSubscriptions_PaymentProviderSubscriptionId",
                table: "OrganizationSubscriptions",
                column: "PaymentProviderSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSubscriptions_PlanId",
                table: "OrganizationSubscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSubscriptions_Status",
                table: "OrganizationSubscriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_InvoiceId",
                table: "PaymentTransactions",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_OrganizationId",
                table: "PaymentTransactions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PaymentProviderTransactionId",
                table: "PaymentTransactions",
                column: "PaymentProviderTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Status",
                table: "PaymentTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookEvents_IsProcessed",
                table: "PaymentWebhookEvents",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookEvents_Provider_EventId",
                table: "PaymentWebhookEvents",
                columns: new[] { "Provider", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PredictiveForecasts_ApplicationId_MetricName",
                table: "PredictiveForecasts",
                columns: new[] { "ApplicationId", "MetricName" });

            migrationBuilder.CreateIndex(
                name: "IX_PredictiveForecasts_OrganizationId",
                table: "PredictiveForecasts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PredictiveForecasts_OrganizationId_ExpiresAtUtc",
                table: "PredictiveForecasts",
                columns: new[] { "OrganizationId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SaaSInvoices_DueDateUtc",
                table: "SaaSInvoices",
                column: "DueDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SaaSInvoices_InvoiceNumber",
                table: "SaaSInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaaSInvoices_OrganizationId",
                table: "SaaSInvoices",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SaaSInvoices_Status",
                table: "SaaSInvoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SaaSInvoices_SubscriptionId",
                table: "SaaSInvoices",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionHistories_OrganizationId",
                table: "SubscriptionHistories",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionHistories_SubscriptionId",
                table: "SubscriptionHistories",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionHistories_TimestampUtc",
                table: "SubscriptionHistories",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Tier",
                table: "SubscriptionPlans",
                column: "Tier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryAnomalies_ApplicationId_Status",
                table: "TelemetryAnomalies",
                columns: new[] { "ApplicationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryAnomalies_OrganizationId",
                table: "TelemetryAnomalies",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryAnomalies_OrganizationId_DetectionTimestamp",
                table: "TelemetryAnomalies",
                columns: new[] { "OrganizationId", "DetectionTimestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingAccounts");

            migrationBuilder.DropTable(
                name: "IncidentRcas");

            migrationBuilder.DropTable(
                name: "InvoiceLineItems");

            migrationBuilder.DropTable(
                name: "LogPatterns");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "PaymentWebhookEvents");

            migrationBuilder.DropTable(
                name: "PredictiveForecasts");

            migrationBuilder.DropTable(
                name: "SubscriptionHistories");

            migrationBuilder.DropTable(
                name: "TelemetryAnomalies");

            migrationBuilder.DropTable(
                name: "SaaSInvoices");

            migrationBuilder.DropTable(
                name: "OrganizationSubscriptions");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "AiInvocationsMonthToDate",
                table: "OrganizationUsages");

            migrationBuilder.DropColumn(
                name: "AiTokensUsedMonthToDate",
                table: "OrganizationUsages");

            migrationBuilder.DropColumn(
                name: "IsAiEnabled",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "MaxMonthlyAiTokens",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "PreferredAiProvider",
                table: "OrganizationSettings");
        }
    }
}
