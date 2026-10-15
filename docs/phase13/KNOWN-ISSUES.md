# SentinelOps — Known Issues & Host Prerequisites (Release Candidate v1.0.0-rc1)

This document catalogs all known environment-specific constraints, host service statuses, and configuration requirements for SentinelOps `v1.0.0-rc1`.

---

## 1. Host Infrastructure & Daemon Constraints

### Issue 1: Docker Desktop Linux Engine Daemon (Resolved)
- **Classification**: Host Environment / Infrastructure Gate (`P13-INF-01`)
- **Status**: **RESOLVED & VERIFIED**
- **Resolution**: Docker Desktop Linux Engine daemon is running and verified. Build context was reduced from ~380 MB to 49.6 kB via repository-root `.dockerignore`. All six SentinelOps containers (`sentinelops-api`, `sentinelops-worker`, `sentinelops-web`, `sentinelops-postgres`, `sentinelops-redis`, `sentinelops-otel-collector`) are healthy and operational. Host port bindings (`127.0.0.1:55432:5432` and `127.0.0.1:56379:6379`) safely isolate SentinelOps from unrelated existing host containers.

---

### Issue 2: Helm CLI Binary Not In Host Windows PATH
- **Classification**: Host Tooling (`P13-OPS-01`)
- **Impact**: Running `helm lint` or `helm template` directly from the local Windows developer command line returns `The term 'helm' is not recognized`.
- **Underlying Cause**: The Helm package manager binary is not installed in the Windows environment PATH. Static Helm templates located at `deploy/helm/sentinelops/` are structurally complete and validated by GitHub Actions workflows in Linux runner environments.
- **Remediation Steps**:
  1. Install Helm on Windows using `winget` or `choco`:
     ```powershell
     winget install Helm.Helm
     # or
     choco install kubernetes-helm
     ```
  2. Refresh the shell PATH and verify:
     ```powershell
     helm version
     helm lint deploy/helm/sentinelops/
     ```

---

## 2. Operational & Configuration Notices

### Issue 3: External AI / LLM Integration Operates in Local Statistical Mode
- **Classification**: Configuration by Design (`Safety Default`)
- **Impact**: External LLM natural language incident synthesis does not execute external API calls unless valid API credentials (`OpenAi:ApiKey` or `AzureOpenAi:ApiKey`) are configured.
- **Underlying Behavior**: The platform operates in fully secure, tenant-isolated fallback mode using deterministic z-score anomaly detection, DBSCAN log clustering, and deterministic root-cause heuristics. This is an enterprise security safeguard to prevent unauthorized telemetry egress.
- **To Enable External Inference**:
  Set the following environment variable in production:
  ```bash
  OPENAI__APIKEY="sk-live-enterprise-key-here"
  AI__ENABLEEXTERNALLLM="true"
  ```

---

### Issue 4: SaaS Billing Provider in Demonstration Mode
- **Classification**: Configuration by Design (`Safety Default`)
- **Impact**: Live credit card charging is disabled by default. Checkout sessions and subscription activations simulate the Stripe lifecycle using local deterministic signed webhook payloads.
- **Underlying Behavior**: Zero payment card data (PAN/CVV) is stored. Webhook signature verification enforces HMAC-SHA256 with 300s clock drift tolerance.
- **To Enable Live Stripe Gateway**:
  Provision real API keys in the production environment:
  ```bash
  STRIPE__SECRETKEY="sk_live_..."
  STRIPE__PUBLISHABLEKEY="pk_live_..."
  STRIPE__WEBHOOKSECRET="whsec_..."
  ```
