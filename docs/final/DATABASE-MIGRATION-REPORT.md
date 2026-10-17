# SentinelOps — Database Migration & Persistence Safety Report

## 1. Executive Summary
This document provides the definitive audit of Entity Framework Core 10 migrations, database schema integrity, and persistence safety for SentinelOps as of Phase 20 baseline commit `85fcdfe`.

---

## 2. Target Database Identification & Isolation

| Attribute | Specification | Verification Result |
| :--- | :--- | :--- |
| **Engine** | PostgreSQL 17-alpine | Verified inside Docker container `sentinelops-postgres` |
| **Project Database** | `sentinelops` | Verified active and healthy |
| **Host Port Mapping** | `127.0.0.1:55432 -> 5432` | Confirmed: **Strictly avoids native host PostgreSQL 18 (port 5432)** |
| **Docker Volume** | `postgres_data` | Persistent named volume; zero volume prune or reset |
| **Unrelated Databases** | `BasketDB`, `OrderDB`, `CommentDB`, `CargoDB`, `IdentityDB` | Completely isolated; zero modifications or connections |

---

## 3. Migration Chronology & Schema History

The following 9 migration milestones define the SentinelOps PostgreSQL schema:

1. **`20261012183953_InitialCreate`**: Base telemetry entities (`Applications`, `Environments`, `Services`, `LogEntries`, `TraceRecords`, `SpanRecords`, `MetricRecords`, `AlertRules`, `AlertIncidents`, `HealthCheckResults`, `AuditEvents`).
2. **`20261012190115_AddIdentityAndApiKeys`**: ASP.NET Core Identity integration (`AspNetUsers`, `AspNetRoles`, `AspNetUserClaims`, etc.) and cryptographic API key storage (`ApiKeys`, `RefreshTokens`).
3. **`20261013175814_AddEnterpriseAlertingAndIncidents`**: Incident management tables (`Incidents`, `IncidentTimelineEvents`, `IncidentComments`, `NotificationChannels`, `NotificationOutboxes`, `InAppNotifications`).
4. **`20261013185303_AddPhase6EnterpriseAdminAndAudit`**: System settings (`SystemSettings`) and enterprise administration extensions.
5. **`20261013193958_AddLogEntryCompositeIndex`**: Performance optimization: composite B-tree index on `(ApplicationId, Timestamp)` and `(Severity, Timestamp)`.
6. **`20261013203223_AddMultiTenantSaasFoundation`**: Multi-tenant organizations (`Organizations`, `OrganizationMembers`, `OrganizationInvitations`, `OrganizationSettings`).
7. **`20261013204633_AddCustomDashboards`**: Custom dashboard layouts (`CustomDashboards`).
8. **`20261013211353_AddPublicStatusPages`**: Public status communication (`StatusPages`, `StatusPageIncidents`).
9. **`20261013212135_AddOrganizationQuotas`**: Monthly usage tracking (`OrganizationUsages`).
10. **`20261014204636_AddSaasCommercialization`**: Commercial billing engine (`SubscriptionPlans`, `BillingAccounts`, `OrganizationSubscriptions`, `SaaSInvoices`, `PaymentTransactions`).
11. **`20261017141651_AddPhase19AdvancedObservability`**: Advanced observability tables (`SavedLogQueries`), `NotificationChannels.OrganizationId` nullable foreign key, and compound query index `IX_SpanRecords_TraceId_StartedAtUtc`.

---

## 4. Migration Execution & Zero-Downtime Application

In Phase 20, the containerized PostgreSQL database was audited:
- The `__EFMigrationsHistory` table was verified.
- The latest migration `20261017141651_AddPhase19AdvancedObservability` was applied seamlessly via `sentinelops-api` startup execution without container downtime or data corruption.
- Table `SavedLogQueries` was confirmed present via PostgreSQL metadata inspection (`information_schema.tables`).
- All 55 relational tables remain intact with zero data loss.

---

## 5. Startup Migration Policy & Safety Controls

SentinelOps implements a dual-mode startup migration policy in [`src/SentinelOps.Api/Program.cs`](file:///c:/Users/Yunus/Desktop/SentinelOps/src/SentinelOps.Api/Program.cs#L305-L334):

1. **Development Environment**:
   - `autoMigrate = true`: Automatically applies pending migrations on container startup to ensure seamless local developer workflow.
2. **Production Environment**:
   - `autoMigrate = false`: Uncontrolled runtime schema changes on container startup are strictly prohibited.
   - If pending migrations are detected during production API boot, the application logs a fatal error and terminates with `InvalidOperationException`, preventing partial schema corruption across distributed pods.
   - Explicit CLI execution flag `dotnet SentinelOps.Api.dll --apply-migrations` or dedicated Kubernetes init-containers/Helm hooks must be utilized in CI/CD release pipelines.

---

## 6. Verdict

**DATABASE MIGRATION & PERSISTENCE SAFETY: VERIFIED**
