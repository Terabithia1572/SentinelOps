# SentinelOps — Known Issues & Host Prerequisites (Release Candidate v1.0.0-rc1)

This document catalogs all known environment-specific constraints, host service statuses, and configuration requirements for SentinelOps `v1.0.0-rc1`.

---

## 1. Host Infrastructure & Daemon Constraints

### Issue 1: Docker Desktop Linux Engine Daemon Offline
- **Classification**: Host Environment / Infrastructure Gate (`P13-INF-01`)
- **Impact**: Containerized multi-container execution (`docker compose up -d`) and live OTel Collector container execution fail on the local development host with error:
  `open //./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified.`
- **Underlying Cause**: WSL2 Linux distribution `docker-desktop` is currently in the `Stopped` state on the Windows host. Native PostgreSQL 18 is running directly as a Windows service (port 5432, PID 6124), allowing backend processes to run natively.
- **Remediation Steps**:
  1. Open a PowerShell terminal as Administrator or standard user.
  2. Launch Docker Desktop from the Windows Start menu or execute:
     ```powershell
     Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe"
     ```
  3. Alternatively, verify the WSL2 engine state:
     ```powershell
     wsl --list --verbose
     wsl -d docker-desktop
     ```
  4. Verify connectivity:
     ```powershell
     docker info
     docker compose up -d
     ```

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
