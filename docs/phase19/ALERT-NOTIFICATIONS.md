# SentinelOps Phase 19: Alert Routing, Multi-Channel Notifications & Delivery Assurance

## 1. Overview & Architecture

SentinelOps provides an enterprise-grade, resilient notification architecture that guarantees alert and incident dispatches across multiple industry-standard destinations. The notification pipeline is built on a transactional outbox pattern to decouple event generation from external third-party network latency and outages.

```mermaid
flowchart LR
    Alert[Alert / Incident Event] --> Outbox[Notification Outbox (DB)]
    Dispatcher[Notification Dispatcher Worker] --> Outbox
    Dispatcher --> ProviderRouter{Channel Router}
    ProviderRouter -->|Email| EmailProv[Email Provider]
    ProviderRouter -->|Webhook| WebhookProv[Webhook Provider (SSRF Guarded)]
    ProviderRouter -->|Slack| SlackProv[Slack Provider (Incoming Webhook)]
    ProviderRouter -->|Teams| TeamsProv[Teams Provider (Office 365 Connector)]
    EmailProv --> External[External Receivers]
    WebhookProv --> External
    SlackProv --> External
    TeamsProv --> External
```

---

## 2. Supported Notification Channels

| Channel Type | Protocol / Target | Payload Format | Security Defenses |
| :--- | :--- | :--- | :--- |
| **Email** | SMTP / TLS | HTML + Plain Text Digest | Standard recipient email validation |
| **Webhook** | HTTP/HTTPS POST | JSON (`event`, `status`, `severity`, `details`) | SSRF DNS resolution check, HMAC SHA-256 signature header |
| **Slack** | Incoming Webhook | Slack Block Kit & formatted attachments | SSRF DNS check, strict HTTPS endpoint validation |
| **Teams** | Connector Webhook | Office 365 Adaptive Card / MessageCard JSON | SSRF DNS check, strict HTTPS endpoint validation |

---

## 3. Server-Side Request Forgery (SSRF) Guard

Every outgoing Webhook, Slack, and Microsoft Teams endpoint is validated through `SsrfValidator`:
- **Protocol Enforceability**: Only HTTP and HTTPS schemes are permitted.
- **DNS Resolution**: The host is resolved to all associated IPv4 and IPv6 addresses.
- **Forbidden Ranges**:
  - Loopback (`127.0.0.0/8`, `::1`)
  - Private RFC 1918 subnets (`10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`)
  - Link-local and cloud metadata addresses (`169.254.0.0/16`, AWS/GCP/Azure IMDS `169.254.169.254`)
  - Any private, link-local, or multicast IPv6 range (`fe80::/10`, `fc00::/7`)

Validation is enforced during channel creation (`POST /api/notification-channels`) and modification (`PUT /api/notification-channels/{id}`).

---

## 4. Channel Verification & Synthetic Testing

Operators can verify channel reachability and credential configuration prior to routing critical production incidents:

- **Endpoint**: `POST /api/notification-channels/{id:guid}/test`
- **Authorization**: `RequireOperator` policy + strict tenant isolation.
- **Execution Flow**:
  1. Verifies channel exists and belongs to the authenticated tenant (`OrganizationId` or associated `ApplicationId`).
  2. Resolves matching `INotificationProvider`.
  3. Synthesizes a test `NotificationOutbox` payload:
     ```json
     {
       "event": "channel_test",
       "channel": "Production SRE Slack",
       "timestamp": "2026-10-17T17:28:00Z"
     }
     ```
  4. Directly executes delivery and returns immediate feedback (`NotificationTestResultDto`):
     ```json
     {
       "success": true,
       "message": "Test notification dispatched successfully.",
       "errorMessage": null,
       "testedAtUtc": "2026-10-17T17:28:01.450Z"
     }
     ```

---

## 5. Delivery History & Outbox State Machine

Every channel maintains an audit trail of dispatches queryable via:

- **Endpoint**: `GET /api/notification-channels/{id:guid}/history`
- **Output**: Bounded list (up to 50 latest dispatches) mapped to standardized lifecycle states:

| Lifecycle State | DB Underlying Status | Description |
| :--- | :--- | :--- |
| `QUEUED` | `Pending` | Message staged in outbox, awaiting background dispatcher worker pickup. |
| `ATTEMPTED` | `Sending` | Worker is actively attempting external HTTP/SMTP delivery. |
| `DELIVERED` | `Delivered` | Successfully accepted by destination with 2xx HTTP response or SMTP OK. |
| `RETRYING` | `Failed` (`RetryCount < MaxRetries`) | Delivery failed transiently (e.g., HTTP 429/503); scheduled for exponential backoff retry. |
| `FAILED` | `DeadLetter` / `Failed` (`RetryCount >= MaxRetries`) | Terminal delivery failure; error message captured for operator triage. |

---

## 6. Multi-Tenant Isolation Guarantees

Notification channels and dispatch histories are strictly partitioned:
1. `OrganizationId` is set automatically on channel creation from the authenticated tenant context (`ICurrentTenantService.OrganizationId`).
2. Global queries (`GET /api/notification-channels`) filter by `OrganizationId == tenantId` or apps owned by the tenant.
3. Entity queries (`GET`, `PUT`, `DELETE`, `POST test`, `GET history`) return `404 Not Found` if the requested channel belongs to another organization, preventing cross-tenant enumeration attacks.
