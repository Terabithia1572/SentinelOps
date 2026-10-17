# SentinelOps — Operations & Administration Runbook

## 1. Routine Service Management

### Service Startup & Shutdown
- **Start All Services**:
  ```bash
  docker compose up -d
  ```
- **Stop All Services Safely** (Preserves PostgreSQL and Redis data volumes):
  ```bash
  docker compose down
  ```
- **Restart Specific Service** (e.g. API):
  ```bash
  docker compose restart sentinelops-api
  ```

### Health Inspection
- **Container Status & Ports**:
  ```bash
  docker compose ps
  ```
- **Live Health Probe**:
  ```bash
  curl -f http://localhost:5100/health/live
  ```
- **Ready Health Probe** (Checks PostgreSQL and Redis connectivity):
  ```bash
  curl -f http://localhost:5100/health/ready
  ```

---

## 2. Container Logs & Troubleshooting

- **Tail API Logs**:
  ```bash
  docker logs -f --tail 100 sentinelops-api
  ```
- **Tail Worker Logs**:
  ```bash
  docker logs -f --tail 100 sentinelops-worker
  ```
- **Tail Web Nginx Logs**:
  ```bash
  docker logs -f --tail 100 sentinelops-web
  ```
- **Tail PostgreSQL Logs**:
  ```bash
  docker logs -f --tail 100 sentinelops-postgres
  ```

### Common Troubleshooting Scenarios

1. **Port Conflict on 55432 or 5100**:
   - Check if an old Docker container is occupying the port: `docker ps`.
   - SentinelOps uses `55432` for PostgreSQL to avoid collisions with default `5432`. Verify no conflicting local instances are bound to `55432`.

2. **Database Migration Sync**:
   - If schema changes are committed, rebuild the API container:
     ```bash
     docker compose build sentinelops-api
     docker compose up -d --no-deps sentinelops-api
     ```
   - In Development mode, the API auto-migrates on boot.

3. **Rate Limiting / Quota Block**:
   - If an organization encounters `HTTP 429 Too Many Requests` during OTLP ingestion, check current usage at `GET /api/organizations/current/usage` or inspect `OrganizationUsages` table.
   - Adjust organization quotas via `PUT /api/organizations/{id}/settings`.

---

## 3. Database Backup & Disaster Recovery

### Creating a Database Backup
```bash
docker exec sentinelops-postgres pg_dump -U sentinelops -d sentinelops -F c -b -v -f /tmp/sentinelops_backup.dump
docker cp sentinelops-postgres:/tmp/sentinelops_backup.dump ./sentinelops_backup_$(date +%Y%m%d).dump
```

### Restoring a Database Backup
```bash
docker cp ./sentinelops_backup.dump sentinelops-postgres:/tmp/sentinelops_backup.dump
docker exec sentinelops-postgres pg_restore -U sentinelops -d sentinelops -v --clean /tmp/sentinelops_backup.dump
```

---

## 4. Retention Policy Operations

- Data retention runs automatically every 60 minutes via [`RetentionCleanupService.cs`](file:///c:/Users/Yunus/Desktop/SentinelOps/src/SentinelOps.Worker/Services/RetentionCleanupService.cs) inside `sentinelops-worker`.
- To trigger an immediate manual retention prune:
  ```bash
  curl -X POST http://localhost:5100/api/settings/retention/cleanup \
       -H "Authorization: Bearer <ADMIN_TOKEN>"
  ```
- Records are deleted in batches of 500 rows to prevent table locking.
