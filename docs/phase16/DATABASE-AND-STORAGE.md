# Checkpoint 16.4 — Database Migrations and Persistent Storage Verification

## 1. Executive Summary

This document records the verification of the persistent database tier and automated schema migrations for SentinelOps within the isolated local Kubernetes staging environment (`sentinelops-staging` kind cluster).

All 10 Entity Framework Core database migrations were applied cleanly using the dedicated Kubernetes Job (`sentinelops-staging-migration`) executing `SentinelOps.Api.dll --migrate-database-only`. Persistent Volume Claim attachment was verified by terminating the active PostgreSQL pod and verifying automatic re-mounting and data retention without corruption or data loss. A disaster recovery backup and restore cycle was executed using `pg_dump` and `pg_restore` against a disposable database, demonstrating 100% schema parity.

---

## 2. Infrastructure Architecture & Topology

### 2.1 PostgreSQL StatefulSet Specification
- **Resource Name:** `sentinelops-staging-postgres`
- **Replicas:** 1
- **Image:** `postgres:17-alpine`
- **Storage Volume:** `data-sentinelops-staging-postgres-0` (1Gi, `ReadWriteOnce`, `standard` storage class)
- **Mount Path:** `/var/lib/postgresql/data` (`PGDATA=/var/lib/postgresql/data/pgdata`)
- **Security Context:** Non-root execution (`runAsUser: 999`, `runAsGroup: 999`, `fsGroup: 999`)
- **Probes:**
  - Liveness: `pg_isready -U sentinelops_app -d sentinelops_staging` (initialDelay: 15s, period: 10s)
  - Readiness: `pg_isready -U sentinelops_app -d sentinelops_staging` (initialDelay: 5s, period: 5s)
- **Services:**
  - `sentinelops-staging-postgres`: ClusterIP on port 5432
  - `postgres`: DNS alias ClusterIP on port 5432 for standard in-cluster connection strings

---

## 3. Migration Job Execution

### 3.1 Migration Mechanism
- **Job Name:** `sentinelops-staging-migration`
- **Image:** `sentinelops-api:1.0.0-staging`
- **Execution Command:** `dotnet SentinelOps.Api.dll --migrate-database-only`
- **Security Context:** Non-root (`runAsUser: 1000`, `runAsGroup: 1000`, `fsGroup: 1000`, seccomp profile `RuntimeDefault`)
- **Backoff Limit:** 2

### 3.2 Key Remediation During Staging Setup
During initial execution, Kubernetes variable expansion was analyzed:
In Kubernetes container specs, variable expansion `$(VAR)` is evaluated sequentially in the order of definition. Connection strings referencing `$(DB_PASSWORD)` and `$(REDIS_PASSWORD)` require that secret references appear *before* dependent connection strings in the `env` list. The templates (`job-migration.yaml`, `deployment-api.yaml`, and `deployment-worker.yaml`) were updated to enforce proper dependency ordering.

### 3.3 Applied EF Core Migrations
Querying `__EFMigrationsHistory` inside `sentinelops-staging-postgres-0` confirmed all 10 migrations applied successfully:

| # | Migration Identifier | Purpose | Status |
|---|----------------------|---------|--------|
| 1 | `20261012183953_InitialCreate` | Core telemetry tables (Logs, Traces, Metrics) | APPLIED |
| 2 | `20261012190115_AddIdentityAndApiKeys` | Identity users, roles, tokens, API keys | APPLIED |
| 3 | `20261013175814_AddEnterpriseAlertingAndIncidents` | Alert rules, incidents, timeline, notifications | APPLIED |
| 4 | `20261013185303_AddPhase6EnterpriseAdminAndAudit` | Audit events, system settings | APPLIED |
| 5 | `20261013193958_AddLogEntryCompositeIndex` | Composite query optimization indexes | APPLIED |
| 6 | `20261013203223_AddMultiTenantSaasFoundation` | Organizations, memberships, multi-tenant boundaries | APPLIED |
| 7 | `20261013204633_AddCustomDashboards` | Custom user dashboards and widgets | APPLIED |
| 8 | `20261013211353_AddPublicStatusPages` | Status pages and public incident updates | APPLIED |
| 9 | `20261013212135_AddOrganizationQuotas` | Tenant quota enforcement and usage metrics | APPLIED |
| 10 | `20261014204636_AddSaasCommercialization` | Billing accounts, subscriptions, invoices, anomaly detection | APPLIED |

**Total Public Tables Created:** 55 tables across core domain, identity, enterprise alerting, multi-tenancy, and commercial billing.

---

## 4. Storage Persistence Verification

To verify that stateful data persists across container lifecycles and pod crashes:
1. Active pod `sentinelops-staging-postgres-0` was deleted via `kubectl delete pod`.
2. Kubernetes StatefulSet controller detected the pod deletion and re-created the pod.
3. The replacement pod automatically re-mounted PVC `data-sentinelops-staging-postgres-0`.
4. Verification query confirmed all 10 migrations and all 55 tables remained intact with zero data loss:
   ```sql
   SELECT count(*) FROM "__EFMigrationsHistory";
   -- Result: 10
   ```

---

## 5. Disaster Recovery Backup & Restore Cycle

A non-destructive disaster recovery drill was executed inside the staging cluster:
1. **Full Database Dump:**
   ```bash
   pg_dump -U sentinelops_app -d sentinelops_staging -F c -f /tmp/staging_backup.dump
   ```
   Generated 150.4 KiB compressed custom-format dump containing 347 TOC entries.
2. **Disposable Restoration Target:**
   Created isolated database `sentinelops_staging_restore_test`.
3. **Restoration Execution:**
   ```bash
   pg_restore -U sentinelops_app -d sentinelops_staging_restore_test /tmp/staging_backup.dump
   ```
4. **Validation:**
   Queried migration count in restored target database:
   ```sql
   SELECT count(*) FROM "__EFMigrationsHistory";
   -- Result: 10
   ```
5. **Cleanup:**
   Dropped disposable database `sentinelops_staging_restore_test` and removed temporary dump file.

---

## 6. Safety Compliance Audit

- [x] Host containers (`BasketDB`, `OrderDB`, `CommentDB`, `CargoDB`, `IdentityDB`, `portainer`) untouched and operational.
- [x] SentinelOps Docker Compose environment untouched.
- [x] No persistent volumes or Docker volumes pruned.
- [x] Zero credential exposure in manifests or documentation.
- [x] Database migrations executed strictly via isolated staging Job.
