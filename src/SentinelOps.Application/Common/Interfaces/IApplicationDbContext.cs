using Microsoft.EntityFrameworkCore;
using ApplicationEntity = SentinelOps.Domain.Entities.Application;
using Environment = SentinelOps.Domain.Entities.Environment;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ApplicationEntity> Applications { get; }
    DbSet<Environment> Environments { get; }
    DbSet<Service> Services { get; }
    DbSet<LogEntry> LogEntries { get; }
    DbSet<TraceRecord> TraceRecords { get; }
    DbSet<SpanRecord> SpanRecords { get; }
    DbSet<MetricRecord> MetricRecords { get; }
    DbSet<AlertRule> AlertRules { get; }
    DbSet<AlertIncident> AlertIncidents { get; }
    DbSet<AlertEvaluation> AlertEvaluations { get; }
    DbSet<AlertEvent> AlertEvents { get; }
    DbSet<AlertRuleNotificationChannel> AlertRuleNotificationChannels { get; }
    DbSet<NotificationChannel> NotificationChannels { get; }
    DbSet<NotificationOutbox> NotificationOutboxes { get; }
    DbSet<InAppNotification> InAppNotifications { get; }
    DbSet<Incident> Incidents { get; }
    DbSet<IncidentTimelineEvent> IncidentTimelineEvents { get; }
    DbSet<IncidentComment> IncidentComments { get; }
    DbSet<IncidentAlertRelation> IncidentAlertRelations { get; }
    DbSet<ServiceLevelObjective> ServiceLevelObjectives { get; }
    DbSet<SloEvaluation> SloEvaluations { get; }
    DbSet<HealthCheckResult> HealthCheckResults { get; }
    DbSet<AuditEvent> AuditEvents { get; }
    DbSet<ApiKey> ApiKeys { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<ApplicationMember> ApplicationMembers { get; }
    DbSet<SystemSetting> SystemSettings { get; }
    DbSet<Organization> Organizations { get; }
    DbSet<OrganizationMember> OrganizationMembers { get; }
    DbSet<OrganizationInvitation> OrganizationInvitations { get; }
    DbSet<OrganizationSettings> OrganizationSettings { get; }
    DbSet<OrganizationUsage> OrganizationUsages { get; }
    DbSet<CustomDashboard> Dashboards { get; }
    DbSet<StatusPage> StatusPages { get; }
    DbSet<StatusPageIncident> StatusPageIncidents { get; }
    DbSet<TelemetryAnomaly> TelemetryAnomalies { get; }
    DbSet<LogPattern> LogPatterns { get; }
    DbSet<IncidentRca> IncidentRcas { get; }
    DbSet<PredictiveForecast> PredictiveForecasts { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<BillingAccount> BillingAccounts { get; }
    DbSet<OrganizationSubscription> OrganizationSubscriptions { get; }
    DbSet<SubscriptionHistory> SubscriptionHistories { get; }
    DbSet<SaaSInvoice> Invoices { get; }
    DbSet<InvoiceLineItem> InvoiceLineItems { get; }
    DbSet<PaymentTransaction> PaymentTransactions { get; }
    DbSet<PaymentWebhookEvent> PaymentWebhookEvents { get; }
    DbSet<SavedLogQuery> SavedLogQueries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

