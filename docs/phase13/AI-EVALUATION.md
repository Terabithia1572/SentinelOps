# SENTINELOPS — AI SAFETY, CORRECTNESS & EVALUATION AUDIT
**Phase 13 AI Observability, Statistical Anomaly Detection & Incident Copilot Verification**  
**Repository**: `https://github.com/Terabithia1572/SentinelOps`  
**Baseline Commit**: `4c778b3`  

---

## 1. Executive Summary & Verification Boundary

SentinelOps Phase 11 delivered an explainable, deterministic intelligence layer augmented with an external LLM adapter for conversational root-cause analysis and incident copilot diagnostics.

In strict adherence to execution rule #6 ("Never describe deterministic rules as machine learning", "Never describe mocked LLM responses as real AI inference", "If live LLM inference is not authorized or configured, mark it NOT VERIFIED"):
- **Deterministic Heuristic Engine**: **VERIFIED (PASS)**.
- **Statistical Anomaly Detection (Z-Score & IQR)**: **VERIFIED (PASS)**.
- **Log Pattern Normalization & Redaction**: **VERIFIED (PASS)**.
- **Root Cause Evidence Provenance**: **VERIFIED (PASS)**.
- **Linear Trend Resource Forecasting**: **VERIFIED (PASS)**.
- **Live External LLM Cloud Provider**: **NOT VERIFIED / FALLBACK ACTIVE** (No external paid provider key is provisioned; system operates in secure deterministic fallback mode).

---

## 2. Statistical Anomaly Detection (`AnomalyDetectionService`)

The anomaly detection engine relies on statistical time-series algorithms over rolling metric windows:

### Algorithms & Mathematical Model:
1. **Z-Score Calculation**:
   $$\mu = \frac{1}{N} \sum_{i=1}^{N} x_i, \quad \sigma = \sqrt{\frac{1}{N-1} \sum_{i=1}^{N} (x_i - \mu)^2}$$
   $$z = \frac{x_t - \mu}{\sigma}$$
   Threshold: $|z| \ge 3.0$ triggers anomaly alert.
2. **IQR (Interquartile Range) Method**:
   Used for non-Gaussian/skewed metrics (latency percentiles, response time spikes).
   $$IQR = Q_3 - Q_1$$
   Outliers flagged if $x_t > Q_3 + 1.5 \times IQR$ or $x_t < Q_1 - 1.5 \times IQR$.

### Edge Case Handling Matrix:

| Dataset Condition | Expected Behavior | Verification Result |
|:---|:---|:---:|
| **Sparse Datasets (< 5 points)** | Flags insufficient baseline data; suppresses false positive alerts | **PASS** |
| **Zero Variance ($\sigma = 0$)** | Avoids division by zero; detects deadflat/stalled metric lines | **PASS** |
| **Noisy Datasets** | Moving average smoothing window suppresses single-sample jitter | **PASS** |
| **Extreme Outliers** | Truncates impact on rolling variance to avoid baseline poisoning | **PASS** |
| **Missing Metric Buckets** | Linear interpolation for gaps $\le 2$ intervals; marks gaps $> 2$ as degraded | **PASS** |

---

## 3. Log Pattern Normalization & Content Safety (`LogPatternAnalysisService`)

The pattern analyzer extracts recurring structural templates from unstructured log streams while safeguarding against sensitive credential leakage and stored XSS:

### 1. Sequential Variable Masking
Masks variables in strict specificity order:
1. **URLs**: `https?://[^\s"'>]+` $\rightarrow$ `{url}`
2. **GUIDs**: `\b[0-9a-fA-F-]{36}\b` $\rightarrow$ `{guid}`
3. **IPv4 Addresses**: `\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(:\d+)?\b` $\rightarrow$ `{ip}`
4. **Email Addresses**: `\b[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+\b` $\rightarrow$ `{email}`
5. **Durations**: `\b\d+(\.\d+)?\s*(ms|s|m|us|ns)\b` $\rightarrow$ `{duration}`
6. **File Paths**: `([A-Za-z]:\\[^\s:]+|(/[a-zA-Z0-9_.-]+){2,})` $\rightarrow$ `{path}`
7. **Numbers**: `\b\d+\b` $\rightarrow$ `{number}`

### 2. Stored XSS Mitigation
- All incoming log messages pass through `HtmlTagRegex.Replace(rawMessage, "")` before template extraction.
- Prevents malicious HTML/JavaScript payloads embedded in exceptions from executing in admin dashboards.

---

## 4. Root Cause Analysis & Evidence Provenance (`RootCauseAnalysisService`)

### Provenance Principles:
1. **Real Telemetry References Only**:
   - Every RCA finding links to actual database records (`TraceId`, `SpanId`, `LogEntry.Id`, or `AlertIncident.Id`).
   - Synthetic or unsupported causation claims are forbidden.
2. **Confidence Terminology**:
   - **High Confidence ($\ge 85\%$)**: Direct correlation between error span exception stack trace and incident firing timestamp within $\pm 60$ seconds.
   - **Medium Confidence ($60\% - 84\%$)**: Elevated error rate coincident with upstream dependency latency spike.
   - **Low Confidence ($< 60\%$)**: Correlated anomaly without direct span linkage; labeled strictly as "Potential Contributor".
3. **Tenant Boundary Guarantee**:
   - RCA queries are scoped to the active `OrganizationId`. Telemetry from external organizations is never queried or correlated.

---

## 5. Predictive Alerting & Resource Exhaustion (`PredictiveAlertingService`)

### Mathematical Model:
Linear trend projection using Ordinary Least Squares (OLS):
$$y = \beta_0 + \beta_1 t, \quad \beta_1 = \frac{\sum (t_i - \bar{t})(y_i - \bar{y})}{\sum (t_i - \bar{t})^2}$$
$$\text{Time to Exhaustion (TTE)} = \frac{\text{Threshold} - \beta_0}{\beta_1} - t_{\text{current}}$$

### Validated Boundary Conditions:
- If $\beta_1 \le 0$ (usage is flat or decreasing), TTE is reported as `Infinite` (no exhaustion).
- If $R^2 < 0.6$ (low goodness of fit / noisy oscillations), prediction is marked `Unreliable (Low Confidence)`.
- If dataset spans $< 1$ hour, forecast returns `Insufficient Data`.

---

## 6. Incident Copilot & AI Safety Architecture

### 1. Mandatory Human-in-the-Loop Approval
- **Absolute Rule**: No AI-generated command or script may execute automatically.
- Every Copilot response schema includes:
  ```json
  {
    "requiresHumanApproval": true,
    "recommendedActions": [
      {
        "action": "Restart Pod",
        "command": "kubectl rollout restart deployment/cart-service",
        "riskLevel": "Medium",
        "requiresApproval": true
      }
    ]
  }
  ```
- The frontend presents action buttons that require explicit administrator confirmation before executing external commands.

### 2. Prompt Injection & Redaction Controls
- Inbound user queries pass through `DeterministicAiProvider.ScrubSensitiveData` to redact database connection strings, JWT tokens, and private keys.
- System prompts are strictly separated from user messages to mitigate prompt injection.
- User inputs cannot alter the underlying tenant boundary or override authorization rules.

### 3. Circuit Breaker & Quota Controls
- Consecutive failure threshold: 5 failures $\rightarrow$ Circuit trips into `OPEN` state for 60 seconds.
- Token consumption is tracked per organization (`AiUsageMeter`); operations are rejected once monthly quota is reached.

---

## 7. AI Evaluation Verdict

| Module | Verification Result | Evidence |
|:---|:---:|:---|
| Statistical Anomaly Detection | **PASS** | Z-score and IQR algorithms verified |
| Log Pattern Extraction & XSS Defense | **PASS** | Dynamic variable masking and HTML strip verified |
| Deterministic RCA & Provenance | **PASS** | Validated with real telemetry citations |
| OLS Linear Trend Forecasting | **PASS** | Bounded exhaustion projections verified |
| Mandatory Human Approval Guard | **PASS** | `requiresHumanApproval == true` enforced |
| Live External LLM Provider | **NOT VERIFIED** | Fallback to deterministic provider active |

**Overall Checkpoint 13.5 Status**: **PASS (Live External LLM Provider marked NOT VERIFIED as required)**.
