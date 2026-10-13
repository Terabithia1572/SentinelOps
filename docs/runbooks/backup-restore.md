# SentinelOps — Disaster Recovery & Backup/Restore Runbook

## 1. Overview & Service Level Objectives (SLAs)

This runbook defines operational standards, automation procedures, and disaster recovery verification protocols for the SentinelOps observability platform.

| Objective | Target SLA | Strategy |
|:---|:---|:---|
| **RPO (Recovery Point Objective)** | < 15 minutes | Hourly automated PostgreSQL snapshots + Continuous WAL archiving / Redis AOF |
| **RTO (Recovery Time Objective)** | < 30 minutes | Automated database recreation scripts + Containerized orchestration |
| **Backup Retention** | 30 days daily, 12 months monthly | Immutable object storage with Object Lock (WORM) |
| **Data Encryption** | AES-256 / GPG | At rest and in transit |

---

## 2. PostgreSQL Backup Procedures

### 2.1 Online Compressed Backup (`pg_dump`)

Execute an online consistent backup using custom compressed format:

```bash
# Set secure environment credentials (never pass in command line)
export PGPASSWORD="<DATABASE_PASSWORD>"
BACKUP_DATE=$(date -u +"%Y%m%d_%H%M%SZ")
BACKUP_FILE="/backups/sentinelops_${BACKUP_DATE}.dump"

pg_dump \
  --host=localhost \
  --port=5432 \
  --username=sentinelops_user \
  --dbname=sentinelops \
  --format=custom \
  --compress=9 \
  --verbose \
  --file="${BACKUP_FILE}"
```

### 2.2 PowerShell Backup Execution (Windows Environments)

```powershell
$env:PGPASSWORD = $env:SENTINELOPS_DB_PASSWORD
$dateStr = (Get-Date).ToUniversalTime().ToString("yyyyMMdd_HHmmssZ")
$backupDir = "C:\SentinelOps\Backups"
if (!(Test-Path $backupDir)) { New-Item -ItemType Directory -Path $backupDir | Out-Null }
$backupPath = Join-Path $backupDir "sentinelops_$dateStr.dump"

& pg_dump `
    --host=localhost `
    --port=5432 `
    --username=postgres `
    --dbname=sentinelops `
    --format=custom `
    --compress=9 `
    --file=$backupPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "[SUCCESS] PostgreSQL backup completed: $backupPath" -ForegroundColor Green
} else {
    Write-Error "[FAILURE] PostgreSQL backup failed with exit code $LASTEXITCODE"
}
```

---

## 3. PostgreSQL Restoration Procedures

### 3.1 Cold Standby / Disaster Recovery Restore

To restore the SentinelOps database into a clean instance:

```bash
export PGPASSWORD="<ADMIN_PASSWORD>"
TARGET_BACKUP="/backups/sentinelops_20261013_180000Z.dump"

# 1. Terminate existing connections
psql -h localhost -U postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'sentinelops' AND pid <> pg_backend_pid();"

# 2. Drop and recreate database
psql -h localhost -U postgres -c "DROP DATABASE IF EXISTS sentinelops;"
psql -h localhost -U postgres -c "CREATE DATABASE sentinelops WITH OWNER = sentinelops_user ENCODING = 'UTF8';"

# 3. Restore schema, indexes, and data
pg_restore \
  --host=localhost \
  --port=5432 \
  --username=sentinelops_user \
  --dbname=sentinelops \
  --clean \
  --if-exists \
  --no-owner \
  --no-privileges \
  --verbose \
  "${TARGET_BACKUP}"

# 4. Verify table integrity
psql -h localhost -U sentinelops_user -d sentinelops -c "SELECT count(*) FROM \"Applications\";"
psql -h localhost -U sentinelops_user -d sentinelops -c "SELECT count(*) FROM \"LogEntries\";"
psql -h localhost -U sentinelops_user -d sentinelops -c "SELECT count(*) FROM \"AuditEvents\";"
```

---

## 4. Redis Stream & Telemetry Buffer Persistence

SentinelOps utilizes Redis Streams (`sentinel:telemetry:stream`) as an asynchronous shock-absorber.

### 4.1 Persistence Configuration (`redis.conf`)

Ensure both Append-Only File (AOF) and Point-In-Time Snapshots (RDB) are active:

```conf
# Append Only File
appendonly yes
appendfsync everysec
auto-aof-rewrite-percentage 100
auto-aof-rewrite-min-size 64mb

# RDB Snapshots
save 900 1
save 300 10
save 60 10000
```

### 4.2 Manual Redis Snapshot Generation

```bash
redis-cli bgsave
# Verify completion
redis-cli lastsave
```

---

## 5. Automated Backup Verification Script

Save this script as `scripts/verify-backup.ps1`:

```powershell
param(
    [string]$BackupFile
)

Write-Host "Starting SentinelOps Backup Verification Routine..." -ForegroundColor Cyan

if (-not (Test-Path $BackupFile)) {
    Write-Error "Backup file not found at: $BackupFile"
    exit 1
}

# 1. Inspect table of contents
Write-Host "1. Validating archive archive header and TOC..."
& pg_restore --list $BackupFile | Select-Object -First 10
if ($LASTEXITCODE -ne 0) {
    Write-Error "Archive validation failed: corrupted header or unsupported format."
    exit 1
}

# 2. Dry-run schema check
Write-Host "2. Running schema syntax parse check..."
& pg_restore --schema-only $BackupFile | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Schema validation failed."
    exit 1
}

Write-Host "[VERIFIED] Backup file is valid and consistent." -ForegroundColor Green
exit 0
```

---

## 6. Disaster Recovery Checklist

- [ ] Stop incoming ingestion traffic by disabling OpenTelemetry Collector endpoints or pausing ingress.
- [ ] Spin up target PostgreSQL instance and confirm network accessibility.
- [ ] Restore database using `pg_restore`.
- [ ] Execute `dotnet ef database update` to ensure any newer pending migrations are reconciled.
- [ ] Start SentinelOps Background Worker to resume consumer groups.
- [ ] Validate `/api/system/health` returns status `Healthy` for PostgreSQL, Redis, and Worker.
- [ ] Re-enable Collector and traffic ingress.
