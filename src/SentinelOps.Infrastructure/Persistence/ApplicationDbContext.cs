using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Domain.Common;
using SentinelOps.Domain.Entities;
using SentinelOps.Infrastructure.Identity;
using ApplicationEntity = SentinelOps.Domain.Entities.Application;
using Environment = SentinelOps.Domain.Entities.Environment;

namespace SentinelOps.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();
    public DbSet<Environment> Environments => Set<Environment>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<LogEntry> LogEntries => Set<LogEntry>();
    public DbSet<TraceRecord> TraceRecords => Set<TraceRecord>();
    public DbSet<SpanRecord> SpanRecords => Set<SpanRecord>();
    public DbSet<MetricRecord> MetricRecords => Set<MetricRecord>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<AlertIncident> AlertIncidents => Set<AlertIncident>();
    public DbSet<AlertEvaluation> AlertEvaluations => Set<AlertEvaluation>();
    public DbSet<AlertEvent> AlertEvents => Set<AlertEvent>();
    public DbSet<AlertRuleNotificationChannel> AlertRuleNotificationChannels => Set<AlertRuleNotificationChannel>();
    public DbSet<NotificationChannel> NotificationChannels => Set<NotificationChannel>();
    public DbSet<NotificationOutbox> NotificationOutboxes => Set<NotificationOutbox>();
    public DbSet<InAppNotification> InAppNotifications => Set<InAppNotification>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentTimelineEvent> IncidentTimelineEvents => Set<IncidentTimelineEvent>();
    public DbSet<IncidentComment> IncidentComments => Set<IncidentComment>();
    public DbSet<IncidentAlertRelation> IncidentAlertRelations => Set<IncidentAlertRelation>();
    public DbSet<ServiceLevelObjective> ServiceLevelObjectives => Set<ServiceLevelObjective>();
    public DbSet<SloEvaluation> SloEvaluations => Set<SloEvaluation>();
    public DbSet<HealthCheckResult> HealthCheckResults => Set<HealthCheckResult>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ApplicationMember> ApplicationMembers => Set<ApplicationMember>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<Entity>();

        var utcNow = DateTime.UtcNow;
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = utcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
