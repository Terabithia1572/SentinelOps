# SentinelOps Phase 14 — Operational Readiness & Observability

## 1. Executive Summary

- **Status**: `PASS`
- **Date**: October 15, 2026
- **Scope**: Platform operational architecture, health probe semantics, Kubernetes readiness/liveness contract, structured logging & correlation ID tracing, configuration safety validation, and background worker daemon topology.
- **Target Environment**:
  - ASP.NET Core API (.NET 10)
  - Background Worker Host (.NET 10)
  - OpenTelemetry Collector (v0.121.0)
  - PostgreSQL 17 & Redis 7

---

## 2. Health Check Endpoints & Probe Contracts

SentinelOps implements standardized ASP.NET Core health check probes compatible with Kubernetes, Docker Compose, and load balancer health monitors:

### 2.1 Liveness Probe (`GET /health/live`)
- **Endpoint**: `http://localhost:5100/health/live` (Host) / `http://localhost:8080/health/live` (Container)
- **Predicate**: `_ => false` (evaluates process health without external dependency coupling).
- **Semantics**: Confirms the Kestrel web server event loop is responsive. Used by Kubernetes to detect deadlocks or thread pool starvation and restart the container if unhealthy.
- **Observed Response**:
  ```http
  HTTP/1.1 200 OK
  Content-Type: text/plain
  Healthy
  ```

### 2.2 Readiness Probe (`GET /health/ready`)
- **Endpoint**: `http://localhost:5100/health/ready` (Host) / `http://localhost:8080/health/ready` (Container)
- **Predicate**: `check => check.Tags.Contains("ready")`
- **Evaluated Dependencies**:
  1. `postgresql` (`AddNpgSql`): Executes `SELECT 1` on PostgreSQL.
  2. `redis` (`AddRedis`): Sends `PING` to Redis instance.
  3. `telemetry_buffer` (`TelemetryBufferHealthCheck`): Verifies connection and memory buffer operational status.
- **Semantics**: Determines whether the API pod can actively accept and route traffic. If PostgreSQL or Redis is unreachable, returns `503 Service Unavailable`, preventing incoming user traffic from failing.
- **Observed Response**:
  ```http
  HTTP/1.1 200 OK
  Content-Type: text/plain
  Healthy
  ```

---

## 3. Configuration Safety & Production Hardening

SentinelOps enforces strict startup validation to protect against accidental insecure production deployments:

### 3.1 Production JWT Validation Guard (`Program.cs`)
```csharp
var jwtKey = builder.Configuration["JwtSettings:Key"] ?? "PLACEHOLDER_DEV_SECRET_KEY_AT_LEAST_32_CHARS_LONG";
if (builder.Environment.IsProduction())
{
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Contains("PLACEHOLDER") || jwtKey.Length < 32)
    {
        throw new InvalidOperationException("CRITICAL SECURITY VIOLATION: Production requires a secure, non-placeholder JwtSettings:Key with at least 32 characters.");
    }
}
```
- In development/testing environments, convenient defaults are permitted.
- In `Production` (`ASPNETCORE_ENVIRONMENT=Production`), startup fails immediately if a placeholder or weak secret (< 32 characters) is detected, preventing weak key exploitation.

### 3.2 Reverse Proxy & Ingress Header Forwarding
```csharp
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
```
- Ensures client IPs and HTTPS schemes are accurately preserved behind Nginx, Kubernetes Ingress, or AWS ALB.

---

## 4. Structured Logging & Observability

- **Engine**: Serilog with `Console` sink and `Enrich.FromLogContext()`.
- **Format**: Structured JSON/Text logging capturing timestamps, log levels, HTTP verbs, paths, response status codes, and execution latency.
- **Request Logging Middleware**:
  ```text
  [19:28:58 INF] HTTP POST /api/keys/56edf15f-af71-44f8-9744-96b5c395e851/revoke responded 200 in 4ms
  [19:29:00 INF] HTTP GET /health/live responded 200 in 0ms
  ```
- **Error Handling**: `ExceptionHandlingMiddleware` intercepts uncaught exceptions and formats RFC 7807 `ProblemDetails` responses while masking internal database details in production.

---

## 5. Background Worker Topology

The `SentinelOps.Worker` process runs as an independent daemon hosting four scheduled background tasks:
1. **`RetentionCleanupService`**: Periodically removes telemetry records older than each tenant's retention policy (e.g. 7 days for Starter, 30 days for Pro, 90 days for Enterprise).
2. **`AlertEvaluationBackgroundService`**: Scans active `AlertRules` and evaluates thresholds against recent metric aggregates and log error frequencies.
3. **`NotificationDispatcherBackgroundService`**: Reliably delivers queued alerts from `NotificationOutboxes` with exponential backoff and dead-letter handling.
4. **`SloEvaluationBackgroundService`**: Evaluates SLO error budgets and burn rates across monitored services.
5. **`RedisStreamConsumerService`** (Infrastructure): Consumes high-throughput telemetry streams from Redis (`sentinelops:stream:logs`, `traces`, `metrics`) into PostgreSQL batches with `XACK` confirmation.
