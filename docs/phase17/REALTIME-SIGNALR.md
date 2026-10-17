# SentinelOps Real-Time SignalR Experience Specification

## 1. Executive Summary

Checkpoint 17.9 verifies and hardens the real-time SignalR push notifications and WebSocket streaming architecture (`/hubs/alerts`). It ensures live telemetry alerts, incident lifecycle updates, and notification outbox dispatches stream instantaneously to connected clients without page reloads, accompanied by resilient automatic reconnection and visual state indicators.

---

## 2. SignalR Hub Architecture & Hook Implementation

### 2.1 Backend Connection & Protocol Negotiation
- **Hub Endpoint**: `/hubs/alerts` configured on ASP.NET Core backend.
- **Transports**: Primary `WebSockets` with automatic fallback to `LongPolling`.
- **JWT Authentication**: `accessTokenFactory` dynamically extracts `sentinelops_token` from browser `localStorage`.
- **Automatic Reconnection**: Exponential backoff retry schedule: `[0, 2000, 5000, 10000, 30000]` ms.
- **Granular Channels**: Group subscriptions supported via `JoinApplicationGroup(applicationId)`.

### 2.2 Client State & Hook (`useAlertsHub`)
- **Status Model**: Strongly typed `SignalRConnectionStatus`:
  - `connecting`: Negotiation and handshake in progress.
  - `connected`: Active bi-directional WebSocket connection established.
  - `reconnecting`: Network interruption detected, automatic reconnecting active.
  - `disconnected`: Unauthenticated, offline, or cleanly closed.
- **Live Event Handlers & Automated Cache Invalidation**:
  - `AlertFired`: Sets `lastAlert`, invalidates `["alerts"]`, `["alert-summary"]`, `["alert-rules"]`, `["incidents"]`, `["dashboard-summary"]`, `["overview-incidents"]`, `["notifications"]`.
  - `AlertResolved`: Sets `lastAlert`, invalidates `["alerts"]`, `["alert-summary"]`, `["alert-rules"]`, `["incidents"]`, `["dashboard-summary"]`, `["overview-incidents"]`.
  - `IncidentUpdated`: Sets `lastIncident`, invalidates `["incidents"]`, `["incident", id]`, `["alert-summary"]`, `["dashboard-summary"]`, `["overview-incidents"]`.
  - `NotificationReceived`: Sets `lastNotification`, invalidates `["notifications"]`.

---

## 3. UI/UX Live Integration

### 3.1 Live Badges & Visual Feedback
- **Incident Management (`/incidents`)**: Displays emerald pulsing dot and `Live WebSocket` badge when connected, falling back gracefully to `Polling` badge when disconnected.
- **Incident Detail Command Center (`/incidents/:id`)**: Displays `Live Sync` badge alongside severity and state badges.
- **Alerting Engine (`/alerts`)**: Displays `Live WebSocket` vs `Polling` badge in top header.
- **Global Notification Center**: Displays real-time `Live` indicator in dropdown header, pulsing badge with live unread counter (`1`–`99+`).

---

## 4. Test Coverage & Verification

- `src/SentinelOps.Web/src/hooks/__tests__/use-alerts-hub.test.ts` (6 tests):
  - Verified no connection attempted when unauthenticated (`disconnected`).
  - Verified hub connection startup, WebSockets negotiation, and `connected` state.
  - Verified `JoinApplicationGroup` invocation on application scope changes.
  - Verified `AlertFired` dispatch, state recording, callback execution, and cache invalidation.
  - Verified status transitions through reconnection lifecycle (`connected` -> `reconnecting` -> `connected` -> `disconnected`).
  - Verified clean connection teardown (`stop()`) on component unmount.
- Vitest suite results: **19 test files passed (100%), 76 tests passed**.
- TypeScript type check: **0 errors**.
