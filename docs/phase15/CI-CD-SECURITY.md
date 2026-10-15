# SentinelOps Phase 15 — CI/CD & Supply-Chain Security Review

## 1. Executive Summary

- **Status**: `PASS`
- **Assessment Date**: October 15, 2026
- **Scope**: GitHub Actions CI pipeline review, least-privilege permission validation, supply-chain vulnerability checks, secret scanning, Helm template validation, and deployment gating.
- **Workflow Path**: `.github/workflows/ci.yml`

---

## 2. GitHub Actions Security & Permission Hardening

### 2.1 Least-Privilege Execution
- The CI workflow declares global read-only token permissions:
  ```yaml
  permissions:
    contents: read
  ```
- Protects against unauthorized repository writes or branch modification in the event of third-party action compromise.

### 2.2 Supply Chain Hardening & Action Pinning
- Official, verified GitHub Action dependencies are pinned to standard major releases:
  - `actions/checkout@v4`
  - `actions/setup-dotnet@v4`
  - `actions/setup-node@v4`
  - `actions/upload-artifact@v4`
  - `azure/setup-helm@v4`
  - `gitleaks/gitleaks-action@v2`
- Ephemeral test secrets: PR builds use synthetic throwaway connection strings and secrets without requiring real production credentials.

---

## 3. Pipeline Job Matrix & Verification Stages

The CI pipeline executes five parallel and gated jobs on every push and pull request:

| Job | Responsibility | Enforced Security Controls |
| :--- | :--- | :--- |
| **`backend-build-test`** | Compile .NET 10 solution & run tests | `--warnaserror` compiler flags; tests executed against real service containers (`postgres:17-alpine`, `redis:7-alpine`). |
| **`frontend-build-test`**| Node 22.x verification | `npm ci` (clean install); `npm run lint`; `tsc -b --noEmit` (strict typecheck); Vitest component tests; Vite production bundle generation. |
| **`docker-build`** | Multi-stage image build | Validates `Dockerfile.api`, `Dockerfile.worker`, and `Dockerfile.web` buildability without deploying images. |
| **`security-scan`** | Vulnerability & secret scanning | `dotnet list package --vulnerable --include-transitive`; `npm audit --audit-level=high`; `gitleaks-action` scanning Git tree for credentials. |
| **`helm-validation`** | Kubernetes manifest validation | `helm lint`; template rendering across `values-development.yaml`, `values-staging.example.yaml`, and `values-production.example.yaml`. |

---

## 4. Production Release Gating

1. **No Automatic Production Deployments**: The CI workflow exclusively validates code quality, security posture, and build reproducibility. It contains zero continuous deployment (CD) steps that automatically push to external production environments.
2. **Pull Request Isolation**: Pull requests from forks cannot access organization secrets.
3. **Artifact Integrity**: Test result reports and vulnerability logs are captured as signed workflow artifacts.
