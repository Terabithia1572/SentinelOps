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

## Technology Decisions

See [DECISIONS.md](../DECISIONS.md) for detailed Architecture Decision Records.
