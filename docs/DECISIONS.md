# Architecture Decision Records

## ADR-001: Clean Architecture with Modular Monolith
**Date:** 2026-10-12  
**Status:** Accepted

### Context
SentinelOps needs a maintainable architecture that separates concerns without the operational complexity of microservices.

### Decision
Use Clean Architecture with a modular monolith approach:
- **Domain**: Pure business entities and logic, no external dependencies
- **Application**: CQRS handlers, validators, interfaces
- **Infrastructure**: EF Core, Redis, Identity implementations
- **Api**: HTTP entry point, controllers, middleware
- **Worker**: Background processing services
- **Contracts**: Shared DTOs, request/response models

### Consequences
- Clear dependency direction (Domain ← Application ← Infrastructure ← Api/Worker)
- Easy to test each layer independently
- Can extract services later if scale demands it
- Single deployment unit simplifies operations

---

## ADR-002: PostgreSQL for Primary Storage
**Date:** 2026-10-12  
**Status:** Accepted

### Context
Need a reliable, performant database for telemetry, configuration, and identity data.

### Decision
Use PostgreSQL 17 with:
- EF Core for application data (applications, alerts, users)
- Proper indexing for time-series telemetry queries
- JSONB columns for semi-structured data (log properties, span attributes)

### Rationale
- Mature, battle-tested RDBMS
- Excellent JSON support via JSONB
- Strong indexing capabilities including GIN indexes
- Free and open source
- Good EF Core provider (Npgsql)

### Trade-offs
- For very high-volume telemetry, a dedicated time-series database (TimescaleDB, ClickHouse) may be needed
- Current design is sufficient for single-instance observability scenarios
- Can add TimescaleDB extension later if needed

---

## ADR-003: MediatR for CQRS
**Date:** 2026-10-12  
**Status:** Accepted

### Context
Need a pattern to separate read and write operations, and enable cross-cutting concerns.

### Decision
Use MediatR for command/query handling with:
- Commands for mutations
- Queries for reads
- Pipeline behaviors for validation and logging

### Rationale
- Well-established library in .NET ecosystem
- Enables clean separation of handler logic
- Pipeline behaviors enable AOP-style cross-cutting concerns
- Not over-engineering: simple request/handler pattern

---

## ADR-004: Manual Mapping Over AutoMapper
**Date:** 2026-10-12  
**Status:** Accepted

### Context
Need to map between domain entities and DTOs.

### Decision
Use manual extension methods for mapping instead of AutoMapper.

### Rationale
- Explicit and debuggable
- No hidden magic or runtime reflection
- Compile-time safety
- Better performance
- Fewer dependencies

---

## ADR-005: OpenTelemetry Collector for Ingestion
**Date:** 2026-10-12  
**Status:** Accepted

### Context
Applications need to send telemetry (logs, traces, metrics) to SentinelOps.

### Decision
Use the OpenTelemetry Collector as the primary ingestion point:
- Applications send OTLP data to the collector
- Collector batches, processes, and forwards to the SentinelOps API
- Custom HTTP endpoints on the API receive processed telemetry

### Rationale
- Industry standard for telemetry collection
- Supports OTLP natively
- Handles batching, retry, and backpressure
- Language-agnostic — any OTLP-compatible SDK can send data
- Avoids reinventing ingestion infrastructure

---

## ADR-006: MIT License
**Date:** 2026-10-12  
**Status:** Accepted

### Context
Need an open-source license compatible with all project dependencies.

### Decision
Use the MIT License.

### Rationale
- Permissive and widely understood
- Compatible with all listed dependencies
- Suitable for portfolio projects
- No copyleft restrictions

---

## ADR-007: Cryptographically Secure API Key Lifecycle
**Date:** 2026-10-12  
**Status:** Accepted

### Context
Monitored applications and OpenTelemetry SDKs require a secure, long-lived ingestion mechanism that avoids transmitting user credentials while defending against database leak credential extraction.

### Decision
Implement a prefix-and-hash API key architecture:
- Format: `sent_{environment}_{randomBase64Url}` (e.g. `sent_live_abc123...`)
- Database stores only `KeyPrefix` (first 16 characters for index lookup) and `KeyHash` (SHA-256 digest of raw key)
- The raw key secret is returned to the user exactly once during creation or rotation and cannot be retrieved thereafter
- Ingestion authentication performs constant-time comparison against the SHA-256 hash
- Full support for rotation, explicit revocation, and optional expiration dates

### Rationale
- Zero plaintext credential persistence ensures compromised database dumps cannot be used to forge telemetry
- Constant-time verification mitigates side-channel timing attacks
- Prefix matching ensures efficient indexed database querying without scanning full tables

---

## ADR-008: JWT Access Tokens with Refresh Token Rotation (RTR)
**Date:** 2026-10-12  
**Status:** Accepted

### Context
Browser and client sessions require stateless authentication with minimal latency while preserving immediate revocation capability for compromised credentials.

### Decision
Employ short-lived JWT access tokens alongside cryptographically secure refresh token rotation (RTR):
- JWT access tokens (HMAC-SHA256, 15-minute expiration) encapsulate user ID, username, email, and roles
- Refresh tokens are 64-byte cryptographically random values, stored exclusively as SHA-256 hashes in PostgreSQL
- Each refresh request rotates the token: the presented token is invalidated, and a new refresh token is issued
- If a revoked or already-replaced refresh token is presented, the token family is revoked to mitigate replay attacks

### Rationale
- Balances high-performance stateless authorization with real-time revocation guarantees
- Single-use refresh token semantics protect against stolen token replay

