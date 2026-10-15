# SentinelOps Phase 15 — Database Migration Safety & Staging Governance

## 1. Executive Summary

- **Status**: `PASS`
- **Assessment Date**: October 15, 2026
- **Scope**: Migration decoupling from runtime API processes, elimination of concurrent DDL startup races, verification of explicit migration execution mode (`--apply-migrations`), pre-migration backup protocols, and rollback procedures.

---

## 2. Critique of Uncontrolled Application-Startup Migrations

In earlier versions, `Program.cs` invoked `await dbContext.Database.MigrateAsync()` unconditionally on web host startup.

### Identified Risks in Staging & Production:
1. **Concurrent Migration Race Conditions**:
   - In horizontally scaled environments (e.g., Kubernetes deployment with multiple replicas), all API pods boot in parallel. Multiple processes executing concurrent DDL operations against PostgreSQL cause locking conflicts, migration table corruptions, or startup timeouts.
2. **Elevated Privileges Required by Runtime**:
   - Forcing the API container to run migrations requires the runtime connection string to possess DDL privileges (`CREATE TABLE`, `ALTER TABLE`, `DROP COLUMN`, `CREATE INDEX`). If the application suffers an SQL injection vulnerability, the attacker possesses DDL privileges.
3. **Uncontrolled Schema Evolution**:
   - Any bad or destructive migration automatically executes on container start before operators can verify database health or create snapshots.

---

## 3. Environment-Aware Migration Handling

SentinelOps now decouples migration execution from runtime application startup in `Program.cs`:

```csharp
var isMigrateOnly = args.Contains("--apply-migrations") || args.Contains("--migrate-only");
var autoMigrate = app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("MigrationSettings:AutoMigrateOnStartup", false);

if (isMigrateOnly)
{
    Log.Information("Applying pending EF Core migrations (--migrate-only mode)...");
    await dbContext.Database.MigrateAsync();
    Log.Information("All pending EF Core migrations applied successfully. Exiting.");
    return; // Exits cleanly with code 0
}

if (autoMigrate)
{
    Log.Information("Applying EF Core migrations in Development mode...");
    await dbContext.Database.MigrateAsync();
}
else
{
    var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
    if (pendingMigrations.Count > 0)
    {
        Log.Fatal("CRITICAL DATABASE COMPATIBILITY ERROR: Detected {Count} unapplied EF Core migrations ({Migrations}) in {Environment} environment...",
            pendingMigrations.Count, string.Join(", ", pendingMigrations), app.Environment.EnvironmentName);
        throw new InvalidOperationException($"Database schema is not up to date in {app.Environment.EnvironmentName}. Please apply pending migrations before starting the application.");
    }
}
```

### Operational Modes:
1. **Development**: Automatic migration remains enabled for local developer ergonomics.
2. **Staging & Production**:
   - Startup migrations are **disabled by default**.
   - API containers check for pending migrations on boot: if the database schema is outdated, the container fails fast, preventing traffic routing to an incompatible schema.
3. **Dedicated Migration Execution**:
   - Kubernetes `initContainers` or CI/CD deployment jobs run:
     ```bash
     dotnet SentinelOps.Api.dll --apply-migrations
     ```
   - This executes migrations in a solitary, non-concurrent process and terminates with exit code 0 upon completion before normal API pods are scheduled.

---

## 4. Verification on Disposable Database

The `--migrate-only` mechanism was rigorously validated against an isolated disposable database on PostgreSQL 17:
1. **Disposable Target Created**: `sentinelops_migrate_test`
2. **Execution**:
   ```bash
   dotnet run --project src/SentinelOps.Api/SentinelOps.Api.csproj --no-build -- --migrate-only
   ```
3. **Result**:
   - Applied all 10 EF Core migrations sequentially.
   - Populated `__EFMigrationsHistory` table with complete lineage.
   - Successfully created all 55 relational tables and indexes.
   - Process output: `[22:52:12 INF] All pending EF Core migrations applied successfully. Exiting.` (Exit Code: 0).
4. **Cleanup**: Disposable database `sentinelops_migrate_test` was safely dropped. Main database `sentinelops` remained untouched.

---

## 5. Pre-Migration Backup & Rollback Protocol

Before executing migrations in staging or production, SREs must perform the following standard procedure:

### 5.1 Pre-Migration Backup
```bash
# Point-in-time logical backup with schema and data
pg_dump -h <host> -p 5432 -U sentinelops_admin -d sentinelops -F c -b -v -f "/backups/sentinelops_pre_migration_$(date +%Y%m%d_%H%M%S).dump"
```

### 5.2 Schema Rollback Protocol
If a migration fails or must be reverted:
1. Determine last stable migration identifier from `__EFMigrationsHistory`:
   ```sql
   SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 5;
   ```
2. Revert to target migration using the migration runner:
   ```bash
   dotnet ef database update <TargetMigrationName> --project src/SentinelOps.Infrastructure --startup-project src/SentinelOps.Api
   ```
3. In case of irreversible DDL failure, restore from pre-migration dump:
   ```bash
   pg_restore -h <host> -p 5432 -U sentinelops_admin -d sentinelops -c -v "/backups/sentinelops_pre_migration_<timestamp>.dump"
   ```

---

## 6. Least-Privilege Database Role Separation

| Role | Permissions | Environment | Used By |
| :--- | :--- | :--- | :--- |
| `sentinelops_migrator` | `ALL PRIVILEGES` on schema `public` (DDL: `CREATE`, `ALTER`, `DROP`) | Staging / Prod | Dedicated CI/CD Migration Job / InitContainer |
| `sentinelops_app` | `SELECT`, `INSERT`, `UPDATE`, `DELETE` on all tables, `USAGE` on sequences | Staging / Prod | `sentinelops-api` and `sentinelops-worker` runtime pods |
| `sentinelops_readonly` | `SELECT` on all tables | Reporting / Analytics | Ad-hoc analytics or audit queries |
