# SentinelOps Architecture Overview

## System Architecture

```
┌──────────────┐     ┌──────────────────┐     ┌──────────────────┐
│  Monitored   │────▶│   OpenTelemetry  │────▶│  SentinelOps API │
│ Applications │     │    Collector     │     │   (ASP.NET Core) │
│  (OTLP SDK)  │     │  (gRPC/HTTP)    │     │                  │
└──────────────┘     └──────────────────┘     └────────┬─────────┘
                                                       │
                                              ┌────────┼─────────┐
                                              │        │         │
                                         ┌────▼───┐ ┌──▼──┐ ┌───▼────┐
                                         │PostgreSQL│ │Redis│ │SignalR │
                                         │  (Data) │ │Cache│ │  Hub   │
                                         └────────┘ └─────┘ └───┬────┘
                                                                 │
                                              ┌──────────────────▼───┐
                                              │   SentinelOps Web    │
                                              │   (React + Vite)     │
                                              └──────────────────────┘
```

## Clean Architecture Layers

```
                    ┌─────────────────────────────┐
                    │          API / Worker        │
                    │   (Composition Root, DI)     │
                    └─────────────┬───────────────┘
                                  │
                    ┌─────────────▼───────────────┐
                    │       Infrastructure         │
                    │  (EF Core, Redis, Identity)  │
                    └─────────────┬───────────────┘
                                  │ implements
                    ┌─────────────▼───────────────┐
                    │        Application           │
                    │  (CQRS, Validators, Ports)   │
                    └─────────────┬───────────────┘
                                  │ depends on
                    ┌─────────────▼───────────────┐
                    │          Domain              │
                    │  (Entities, Value Objects)   │
                    └─────────────────────────────┘
```

## Data Flow

1. **Instrumented applications** emit telemetry via OpenTelemetry SDKs
2. **OTLP data** (gRPC/HTTP) is sent to the **OpenTelemetry Collector**
3. The Collector **batches, processes, and forwards** data to the SentinelOps API
4. The API **validates and stores** telemetry in PostgreSQL
5. **Redis** provides caching and pub/sub for real-time features
6. **SignalR** pushes real-time updates to the web dashboard
7. **Background workers** evaluate alert rules and manage retention
8. The **React frontend** provides the observability dashboard

## Project Structure

| Project | Purpose | Dependencies |
|---------|---------|-------------|
| `SentinelOps.Domain` | Business entities, enums, value objects | None |
| `SentinelOps.Contracts` | Shared DTOs, requests, responses | None |
| `SentinelOps.Application` | CQRS handlers, validators, interfaces | Domain, Contracts |
| `SentinelOps.Infrastructure` | EF Core, Redis, Identity implementations | Application, Domain |
| `SentinelOps.Api` | HTTP API, controllers, middleware | Application, Infrastructure |
| `SentinelOps.Worker` | Background processing services | Application, Infrastructure |
| `SentinelOps.Web` | React frontend dashboard | API (HTTP) |

## Telemetry Ingestion Pipeline & Reliability Architecture

```
┌───────────────────┐    HTTP / OTLP JSON    ┌───────────────────────┐
│ Instrumented Apps │ ─────────────────────▶ │ OtlpIngestionController│
│ (SDK / Sidecars)  │   X-API-Key (Strict)   │  (Size Limit: 10 MB)  │
└───────────────────┘                        └──────────┬────────────┘
                                                        │ Authenticated App ID
                                             ┌──────────▼────────────┐
                                             │  RedisTelemetryBuffer │
                                             └─────┬───────────┬─────┘
                                   Redis Online    │           │  Redis Offline
                         ┌─────────────────────────▼─┐       ┌─▼────────────────────────┐
                         │ Redis Streams             │       │ In-Memory Channels       │
                         │ - sentinelops:stream:logs │       │ (Bounded: 100,000 items) │
                         │ - sentinelops:stream:traces       │ Backpressure: Wait       │
                         │ - sentinelops:stream:metrics      └─┬────────────────────────┘
                         │ Consumer Groups & XACK    │         │
                         └─────────────┬─────────────┘         │
                                       └───────────┬───────────┘
                                                   │ Drained every 500ms / 100 items
                                       ┌───────────▼───────────┐
                                       │TelemetryConsumerService│
                                       │ (Hosted Background)  │
                                       └───────────┬───────────┘
                                                   │ Batched Persistence & Deduplication
                                       ┌───────────▼───────────┐
                                       │ PostgreSQL 17 Tables  │
                                       │ - LogEntries          │
                                       │ - TraceRecords        │
                                       │ - SpanRecords         │
                                       │ - MetricRecords       │
                                       └───────────────────────┘
```

### Ingestion Reliability & Fallback Constraints
- **Primary Redis Streams Buffer**: Provides bounded buffering (`MAXLEN 100,000`), consumer groups (`sentinelops-worker-group`), explicit acknowledgment (`XACK`), and dead-letter queue routing (`sentinelops:stream:dead-letter`) for malformed payloads.
- **In-Memory Channel Fallback**: When Redis is offline or disconnected, telemetry is accepted via bounded in-process `System.Threading.Channels.Channel<T>` with backpressure to protect memory.
- **Data Loss Constraints (Important Trade-Off)**:
  - When Redis is connected, messages are durable across service restarts via Redis persistence.
  - When Redis is offline and falling back to in-memory channels: **telemetry in volatile RAM will be lost if the API process terminates or crashes abruptly before the consumer persists the buffer to PostgreSQL**. Zero data loss is not guaranteed during in-memory fallback.

### Multi-Tenancy & OpenTelemetry Collector Attribution
- **Strict Identity Derivation**: SentinelOps enforces strict tenant isolation at the authentication boundary. Telemetry data is attributed **exclusively** to the application that owns the validated API key (`X-API-Key` or `Authorization: Bearer <key>`). Any application identifier supplied inside payload attributes (`Resource.Attributes`) is untrusted and discarded.
- **Central Collector Considerations**: If an OpenTelemetry Collector forwards all traffic to SentinelOps using a single static API key, all telemetry routed through that collector will be attributed to that key's application. For multi-application isolation:
  - Applications should configure their OpenTelemetry SDK to export directly to SentinelOps with their own application-specific API key, OR
  - Separate collector exporter pipelines must be configured per application/tenant.

## Technology Decisions

See [DECISIONS.md](../DECISIONS.md) for detailed Architecture Decision Records.

