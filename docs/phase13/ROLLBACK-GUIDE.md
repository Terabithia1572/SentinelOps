# SentinelOps Rollback & Disaster Recovery Runbook (v1.0.0-rc1)

This runbook defines exact operational procedures for executing software rollbacks, database restoration, and incident containment across all deployment tiers.

---

## 1. Application Software Rollback

### 1.1. Kubernetes Deployment Rollback
When an application defect or regression is detected post-deployment on Kubernetes:

1. **Check Rollout History**:
   ```bash
   kubectl rollout history deployment/sentinelops-api -n sentinelops
   kubectl rollout history deployment/sentinelops-worker -n sentinelops
   kubectl rollout history deployment/sentinelops-web -n sentinelops
   ```

2. **Roll Back to Preceding Revision**:
   ```bash
   # Revert API service
   kubectl rollout undo deployment/sentinelops-api -n sentinelops
   
   # Revert Worker service
   kubectl rollout undo deployment/sentinelops-worker -n sentinelops
   
   # Revert Web service
   kubectl rollout undo deployment/sentinelops-web -n sentinelops
   ```

3. **Monitor Rollback Progress**:
   ```bash
   kubectl rollout status deployment/sentinelops-api -n sentinelops
   kubectl rollout status deployment/sentinelops-worker -n sentinelops
   kubectl rollout status deployment/sentinelops-web -n sentinelops
   ```

---

### 1.2. Docker Compose Rollback
When operating via Docker Compose:

1. **Pin Previous Image Tag or Git Commit**:
   Update `docker-compose.yml` image tags or check out the target release tag:
   ```bash
   git checkout tags/v0.9.0 # or specific stable commit
   ```

2. **Re-deploy Containers**:
   ```bash
   docker compose up -d --no-deps api worker web
   ```

3. **Verify Health**:
   ```bash
   docker compose ps
   curl -f http://localhost:5100/health/ready
   ```

---

## 2. Database Rollback Procedures

### 2.1. EF Core Migration Rollback
If a database migration must be rolled back:

1. **Identify Target Migration**:
   List applied migrations:
   ```bash
   dotnet ef migrations list --project src/SentinelOps.Infrastructure --startup-project src/SentinelOps.Api
   ```

2. **Roll Back to Target Migration**:
   To roll back to a specific migration (e.g., `AddPublicStatusPages`):
   ```bash
   dotnet ef database update 20261013211353_AddPublicStatusPages \
     --project src/SentinelOps.Infrastructure \
     --startup-project src/SentinelOps.Api
   ```

3. **Transactional Safeguards**:
   All EF Core migrations are executed inside explicit PostgreSQL transactions. If a step fails, the migration is aborted and rolled back automatically without data corruption.

---

### 2.2. Disaster Recovery Database Restoration Runbook
When catastrophic database corruption occurs, execute point-in-time restoration from automated backup dumps.

As verified in Checkpoint 13.6 (`docs/phase13/PERFORMANCE-AND-RECOVERY.md`), SentinelOps DR restoration achieves 100% table/row/index parity with a measured Recovery Time Objective (RTO) of under 4 seconds.

#### Step 1: Terminate Active Connections
Prevent active writes during restoration:
```sql
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = 'sentinelops' AND pid <> pg_backend_pid();
```

#### Step 2: Restore from Compressed Dump
```bash
# Drop and recreate database
dropdb -h localhost -U postgres sentinelops
createdb -h localhost -U postgres -O sentinelops_user sentinelops

# Execute pg_restore
pg_restore -h localhost -U postgres -d sentinelops \
  --clean --if-exists --no-owner --no-privileges \
  backups/sentinelops_backup_latest.dump
```

#### Step 3: Run Integrity & Parity Validation
Execute the SentinelOps Recovery Validator tool:
```bash
dotnet run --project tools/SentinelOps.RecoveryValidator
```
Verify that:
- Total table count matches baseline.
- Row counts on `Applications`, `Organizations`, `LogEntries`, and `TraceRecords` match backup records.
- All primary keys and indexes are operational.

---

## 3. Emergency Incident Mitigation & Isolation Kill-Switches

### 3.1. Disabling AI Background Forecasting Loop
If the AI processing engine causes unexpected CPU pressure:
```bash
# In Kubernetes: scale down AI or set environment variable
kubectl set env deployment/sentinelops-worker -n sentinelops AI__ENABLEANOMALYDETECTION="false"
```

### 3.2. Disabling Legacy Tenant Auto-Reconciliation
If legacy single-tenant reconciliation causes unexpected resource mappings:
Verify that `MigrationSettings:AutoReconcileLegacyTenants` is set to `false` in `appsettings.json` (the default in `v1.0.0-rc1`).

### 3.3. Tenant Lockdown (Quarantining a Compromised Tenant)
To immediately block a compromised organization without affecting platform availability:
```sql
UPDATE "Organizations"
SET "IsActive" = false, "UpdatedAtUtc" = NOW()
WHERE "Id" = '00000000-0000-0000-0000-000000000000';
```
The tenant authorization gate will immediately reject all subsequent requests from users belonging to that organization.
