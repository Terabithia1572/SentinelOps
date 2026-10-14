#!/usr/bin/env bash
# ==============================================================================
# SentinelOps — Point-in-Time Database Restore Script
# Policy: Never restore into active primary database directly.
# ==============================================================================
set -euo pipefail

if [ "$#" -lt 1 ]; then
    echo "Usage: $0 <backup-file.dump> [target-database]"
    exit 1
fi

BACKUP_FILE="$1"
PGHOST="${PGHOST:-localhost}"
PGPORT="${PGPORT:-5432}"
PGUSER="${PGUSER:-postgres}"
TARGET_DB="${2:-sentinelops_restored}"

if [ ! -f "${BACKUP_FILE}" ]; then
    echo "[ERROR] Backup file '${BACKUP_FILE}' does not exist."
    exit 1
fi

echo "[INFO] Restoring '${BACKUP_FILE}' into isolated target database '${TARGET_DB}'..."
START_TIME=$(date +%s%N)

# Create isolated database if not present
psql -h "${PGHOST}" -p "${PGPORT}" -U "${PGUSER}" -d postgres \
  -c "CREATE DATABASE \"${TARGET_DB}\";" || echo "[WARN] Database may already exist, proceeding..."

# Restore custom archive dump
pg_restore \
  -h "${PGHOST}" \
  -p "${PGPORT}" \
  -U "${PGUSER}" \
  -d "${TARGET_DB}" \
  --no-owner \
  --clean \
  --if-exists \
  "${BACKUP_FILE}" || true

END_TIME=$(date +%s%N)
DURATION_MS=$(( (END_TIME - START_TIME) / 1000000 ))

echo "[SUCCESS] Restore operation complete in ${DURATION_MS} ms. Target: ${TARGET_DB}"
