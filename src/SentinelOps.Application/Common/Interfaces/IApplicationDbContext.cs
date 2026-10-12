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
    DbSet<HealthCheckResult> HealthCheckResults { get; }
    DbSet<AuditEvent> AuditEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
