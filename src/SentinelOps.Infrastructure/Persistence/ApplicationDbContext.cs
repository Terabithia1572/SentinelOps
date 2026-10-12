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
    public DbSet<HealthCheckResult> HealthCheckResults => Set<HealthCheckResult>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

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
