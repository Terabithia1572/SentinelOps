# Checkpoint 16.5 — Redis Streams Buffering, Consumer Groups & Worker Reliability

## 1. Executive Summary

This document records the verification of Redis Streams telemetry buffering, consumer group coordination, dead-letter stream handling, and background worker reliability under scaling and failover in the isolated local Kubernetes staging environment (`sentinelops-staging`).

All three primary telemetry streams (`sentinelops:stream:logs`, `traces`, `metrics`) and their consumer groups (`sentinelops-worker-group`) were verified. Message ingestion, distributed consumer group acknowledgment, Pending Entries List (PEL) drain, dead-letter queue routing for malformed payloads, worker horizontal scaling (1 -> 2 -> 1 replicas), and mid-flight worker pod termination recovery were confirmed with zero duplicate entries and zero dropped messages.

---

## 2. Infrastructure Architecture & StatefulSet Specification

### 2.1 Staging Redis StatefulSet
- **Resource Name:** `sentinelops-staging-redis`
- **Replicas:** 1
- **Image:** `redis:7-alpine`
- **Configuration:** Append-only file persistence enabled (`--appendonly yes`), authenticated via `--requirepass $(REDIS_PASSWORD)`
- **Persistent Volume:** `data-sentinelops-staging-redis-0` (1Gi, `ReadWriteOnce`)
- **Security Context:** Non-root execution (`runAsUser: 999`, `runAsGroup: 999`, `fsGroup: 999`)
- **Health Probes:**
  - Liveness: `redis-cli -a $REDIS_PASSWORD ping` (initialDelay: 10s, period: 10s)
  - Readiness: `redis-cli -a $REDIS_PASSWORD ping` (initialDelay: 5s, period: 5s)
- **Services:**
  - `sentinelops-staging-redis`: ClusterIP on port 6379
  - `redis`: DNS alias ClusterIP on port 6379 for standard connection strings

---

## 3. Telemetry Streams & Consumer Group Initialization

When the application stack (`sentinelops-staging-api` and `sentinelops-staging-worker`) initializes, `RedisTelemetryBuffer.EnsureConsumerGroups()` verifies and establishes the consumer groups:

| Stream Key | Consumer Group | Initial Position | Consumer Replicas | Status |
|------------|----------------|------------------|-------------------|--------|
| `sentinelops:stream:logs` | `sentinelops-worker-group` | `$` (NewMessages) | 2 (API + Worker) | ACTIVE |
| `sentinelops:stream:traces` | `sentinelops-worker-group` | `$` (NewMessages) | 2 (API + Worker) | ACTIVE |
| `sentinelops:stream:metrics` | `sentinelops-worker-group` | `$` (NewMessages) | 2 (API + Worker) | ACTIVE |
| `sentinelops:stream:dead-letter` | N/A (Inspection Queue) | N/A | N/A | ACTIVE |

---

## 4. Message Ingestion, Processing & PEL Drain

### 4.1 Ingestion and Consumption Cycle
A structured log message was published to `sentinelops:stream:logs`:
```bash
redis-cli -a $REDIS_PASSWORD XADD sentinelops:stream:logs '*' \
  data '{"Id":"11111111-1111-1111-1111-111111111111", ...}' appId '00000000-0000-0000-0000-000000000001'
```
Generated Stream Entry ID: `1792175679352-0`.

### 4.2 Consumption & Acknowledgment Verification
- `TelemetryConsumerService` dequeued the message via `ReadEntriesWithPelRecoveryAsync`.
- Batched and committed the record into PostgreSQL `LogEntries`.
- Executed `StreamAcknowledgeAsync(LogsStreamKey, ConsumerGroup, entry.Id)`.
- Verified `XPENDING sentinelops:stream:logs sentinelops-worker-group` returned `0`, confirming the Pending Entries List (PEL) is cleanly drained with zero stuck entries.

---

## 5. Dead-Letter Stream Handling for Malformed Payloads

To verify fail-safe behavior when invalid or unparseable payloads enter the ingestion pipeline:
1. A corrupted payload with invalid JSON delimiters was injected into `sentinelops:stream:logs`.
2. `RedisTelemetryBuffer` caught the `JsonException` during deserialization.
3. The corrupt entry was automatically re-routed to `sentinelops:stream:dead-letter` with metadata tag `type=log`.
4. The poison pill was acknowledged from the primary stream, preventing consumer thread deadlocks or infinite retry loops.
5. Inspection via `XRANGE sentinelops:stream:dead-letter - +` confirmed the exact malformed payload was safely quarantined.

---

## 6. Worker Scaling & Failover Resilience Drills

### 6.1 Horizontal Scaling Test
1. Scaled `sentinelops-staging-worker` from 1 to 2 replicas:
   ```bash
   kubectl scale deployment sentinelops-staging-worker --replicas=2 -n sentinelops-staging
   ```
2. Both pods reached `1/1 Running` and registered with Redis consumer group `sentinelops-worker-group` (total active consumers increased to 3).
3. Injected a batch of 5 distinct log items (`Scaling Resilience Test Item 1` through `5`).
4. Verified that all 5 items were consumed and committed to PostgreSQL with exact 1-to-1 parity (0 duplicates, 0 dropped).

### 6.2 Mid-Flight Pod Termination & Disaster Recovery Drill
1. While streaming messages, simultaneously terminated all active worker pods:
   ```bash
   kubectl delete pod -l app.kubernetes.io/component=worker -n sentinelops-staging --wait=false
   ```
2. Concurrently published 3 additional test items (`Scaling Resilience Test Item 6`, `7`, `8`).
3. Kubernetes ReplicaSet immediately created replacement worker pods.
4. Replacement workers acquired consumer locks, processed all remaining stream entries, and flushed them to the database.
5. Final database query verified all 8 test messages persisted successfully with zero duplicates:
   ```sql
   SELECT count(*), "Message" FROM "LogEntries"
   WHERE "Message" LIKE 'Scaling Resilience%' GROUP BY "Message" ORDER BY "Message";
   -- Result: Exactly 8 rows, count = 1 for each item
   ```
6. Post-test scaling returned worker deployment to 1 replica to adhere to the host memory budget.

---

## 7. Safety Compliance Audit

- [x] Host containers (`BasketDB`, `OrderDB`, `CommentDB`, `CargoDB`, `IdentityDB`, `portainer`) untouched and operational.
- [x] SentinelOps Docker Compose environment untouched.
- [x] Redis and Worker resources strictly budgeted within kind staging node limits.
- [x] Zero credential exposure in manifests or documentation.
- [x] All consumer groups and PEL states verified clean (`pending: 0`).
