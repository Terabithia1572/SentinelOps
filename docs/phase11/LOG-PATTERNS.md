# SentinelOps — Intelligent Log Pattern Analysis Report
## Checkpoint 11.3: Log Normalization, Template Extraction, Fingerprinting & Security

**Document Version:** 1.0.0  
**Implementation Date:** October 14, 2026  
**Status:** **PASSED & VERIFIED**  
**Repository:** [https://github.com/Terabithia1572/SentinelOps](https://github.com/Terabithia1572/SentinelOps)  
**Workspace:** `C:\Users\Yunus\Desktop\SentinelOps`  

---

## 1. Overview & Operational Need

Modern distributed services generate millions of unstructured, semi-repetitive log lines daily. High cardinality in dynamic variables (user IDs, request durations, IP addresses, trace identifiers, timestamps) prevents operators from quickly identifying systemic failure clusters.

Intelligent Log Pattern Analysis automatically clusters unstructured logs into deterministic operational templates, detects emerging error clusters, and tracks pattern frequency trends over time.

---

## 2. Normalization & Masking Pipeline

```mermaid
flowchart TD
    A[Raw Unstructured Log Message] --> B[HTML & Script Tag Strip: Prevent XSS]
    B --> C[URL & Email Masking: {url}, {email}]
    C --> D[GUID & Hex Pointer Masking: {guid}, {hex}]
    D --> E[IPv4/IPv6 Address Masking: {ip}]
    E --> F[Duration & Latency Masking: {duration}]
    F --> G[File Path Masking: {path}]
    G --> H[Standalone Number / ID Masking: {number}]
    H --> I{Exception Detail Attached?}
    I -- Yes --> J[Extract Exception Type Prefix: [Npgsql.NpgsqlException]]
    I -- No --> K[Collapse Excess Whitespace]
    J --> K
    K --> L[Generate SHA-256 Fingerprint]
    L --> M[Clustered LogPattern Record]
```

### 2.1 Masking Hierarchy
To prevent greedy regular expressions from clobbering sub-tokens, masking executes in strict specificity order:
1. **URLs**: `https?://[^\s"'>]+` $\rightarrow$ `{url}`
2. **Emails**: `\b[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+\b` $\rightarrow$ `{email}`
3. **GUIDs**: `\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b` $\rightarrow$ `{guid}`
4. **IP Addresses**: `\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(:\d+)?\b` $\rightarrow$ `{ip}`
5. **Durations**: `\b\d+(\.\d+)?\s*(ms|s|m|us|ns|sec|seconds|milliseconds)\b` $\rightarrow$ `{duration}`
6. **Hex Pointers**: `\b0x[0-9a-fA-F]+\b` $\rightarrow$ `{hex}`
7. **File Paths**: `([A-Za-z]:\\[^\s:]+|(/[a-zA-Z0-9_.-]+){2,})` $\rightarrow$ `{path}`
8. **Numbers & IDs**: `\b\d+\b` $\rightarrow$ `{number}`

### 2.2 Template & Fingerprinting Example
- **Raw Input 1:** `"Database timeout after 1200ms for user 12345 on host 10.0.1.42:5432"`
- **Raw Input 2:** `"Database timeout after 2450ms for user 98765 on host 192.168.1.100:5432"`
- **Normalized Template:** `"Database timeout after {duration} for user {number} on host {ip}"`
- **SHA-256 Fingerprint:** `100% Identical 64-character hash across both events`

---

## 3. Security Hardening & Zero-Trust Telemetry

Telemetry data is treated as **untrusted external input**:
1. **Stored XSS Prevention**: Raw HTML tags (`<script>`, `<iframe>`, `<img>`, etc.) are stripped, and templates/sample messages are HTML-encoded via `WebUtility.HtmlEncode` before storage and rendering.
2. **Personal Information Protection**: Emails, IP addresses, and user identifiers are stripped and replaced with generic tokens, preventing inadvertent PII leakage in aggregate dashboard cards.
3. **Tenant Boundary Enforcement**: Log pattern entities (`LogPattern`) enforce compound uniqueness on `(ApplicationId, PatternFingerprint)` and are partitioned by `OrganizationId`.

---

## 4. API Endpoints

| Method | Endpoint | Authorization | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/logs/patterns?applicationId={id}` | Bearer JWT | Retrieves existing clustered patterns ordered by frequency. |
| `POST` | `/api/logs/patterns/analyze?applicationId={id}&windowHours={h}` | Bearer JWT | Ingests recent logs and computes new pattern clusters. |

---

## 5. Verification Evidence

- **Unit Tests**: [`LogPatternAnalysisServiceTests.cs`](file:///c:/Users/Yunus/Desktop/SentinelOps/tests/SentinelOps.Application.Tests/Ai/LogPatternAnalysisServiceTests.cs) (6 tests passing):
  - Dynamic variable masking & deterministic SHA-256 fingerprints (PASS)
  - URL and email masking (PASS)
  - Stored XSS defense: HTML and script tag stripping (PASS)
  - Multiline exception detail prefixing (PASS)
  - Frequency tracking, first/last seen timestamps, and trace correlation (PASS)
  - Cross-tenant isolation verification (PASS)
- **Integration Tests**: [`LogPatternsIntegrationTests.cs`](file:///c:/Users/Yunus/Desktop/SentinelOps/tests/SentinelOps.Api.IntegrationTests/LogPatternsIntegrationTests.cs) (1 test passing).
- **Total Backend Tests**: 222 tests passed (29 Domain + 76 Application + 117 Api.IntegrationTests), 0 failures, 0 warnings.
- **Verdict**: **PASSED (Checkpoint 11.3 Complete)**.
