#!/usr/bin/env bash
# ==============================================================================
# SentinelOps — Automated Database Snapshot Backup Script
# Standard: Compressed Custom Archive (-F c) with Blobs (-b)
# ==============================================================================
set -euo pipefail

PGHOST="${PGHOST:-localhost}"
PGPORT="${PGPORT:-5432}"
PGUSER="${PGUSER:-postgres}"
PGDATABASE="${PGDATABASE:-sentinelops}"
BACKUP_DIR="${BACKUP_DIR:-/var/backups/sentinelops}"

mkdir -p "${BACKUP_DIR}"
TIMESTAMP=$(date -u +"%Y%m%d_%H%M%SZ")
BACKUP_FILE="${BACKUP_DIR}/sentinelops_${TIMESTAMP}.dump"

echo "[INFO] Initiating backup of '${PGDATABASE}' from ${PGHOST}:${PGPORT}..."
START_TIME=$(date +%s%N)

pg_dump \
  -h "${PGHOST}" \
  -p "${PGPORT}" \
  -U "${PGUSER}" \
  -d "${PGDATABASE}" \
  -F c \
  -b \
  -f "${BACKUP_FILE}"

END_TIME=$(date +%s%N)
DURATION_MS=$(( (END_TIME - START_TIME) / 1000000 ))
FILE_SIZE=$(du -h "${BACKUP_FILE}" | cut -f1)

echo "[SUCCESS] Backup complete in ${DURATION_MS} ms. File: ${BACKUP_FILE} (${FILE_SIZE})"
