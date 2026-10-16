# Checkpoint 16.13 — Full Regression Test Matrix

## 1. Executive Summary

This document presents the full regression test matrix executed during Phase 16 across backend domain, application, integration suites, frontend unit tests, end-to-end multi-viewport browser tests, Kubernetes cluster integration tests, and Helm chart validation.

All test suites were executed against the live, healthy infrastructure. Across all suites, **456 test executions** succeeded with **zero failures** (100% pass rate).

---

## 2. Test Execution Breakdown

### 2.1 Backend Automated Tests (.NET 10 / xUnit)
- Target: `.NET 10.0`
- Configuration: `Release`
- Execution Command: `dotnet test --configuration Release`

| Project | Total | Passed | Failed | Skipped | Duration |
| :--- | :---: | :---: | :---: | :---: | :---: |
| `SentinelOps.Domain.Tests` | 49 | 49 | 0 | 0 | 58 ms |
| `SentinelOps.Application.Tests` | 101 | 101 | 0 | 0 | 948 ms |
| `SentinelOps.Api.IntegrationTests` | 153 | 153 | 0 | 0 | 10.0 s |
| **Backend Total** | **303** | **303** | **0** | **0** | **11.0 s** |

### 2.2 Frontend Unit Tests (React 19 / Vitest)
- Target: `React 19`, `TypeScript 6`, `Vite 8`
- Execution Command: `npm run test:run` in `src/SentinelOps.Web`

| Test Suite / Domain Area | Tests | Status |
| :--- | :---: | :---: |
| `src/pages/__tests__/overview.test.tsx` | 2 | PASS |
| `src/pages/__tests__/applications.test.tsx` | 2 | PASS |
| `src/pages/__tests__/api-keys.test.tsx` | 2 | PASS |
| `src/pages/__tests__/onboarding.test.tsx` | 4 | PASS |
| `src/pages/__tests__/dashboards.test.tsx` | 2 | PASS |
| `src/pages/__tests__/admin-and-audit.test.tsx` | 4 | PASS |
| `src/pages/__tests__/apm.test.tsx` | 2 | PASS |
| `src/pages/__tests__/status-pages.test.tsx` | 3 | PASS |
| `src/pages/__tests__/auth.test.tsx` | 4 | PASS |
| `src/pages/__tests__/organizations.test.tsx` | 4 | PASS |
| `src/pages/__tests__/ai-observability.test.tsx` | 5 | PASS |
| `src/pages/__tests__/incident-intelligence.test.tsx` | 5 | PASS |
| `src/pages/__tests__/billing.test.tsx` | 6 | PASS |
| `src/pages/__tests__/alerting.test.tsx` | 4 | PASS |
| `src/pages/__tests__/observability.test.tsx` | 5 | PASS |
| **Frontend Unit Total** | **54** | **100% PASS** |

### 2.3 End-to-End Browser Automation (Playwright)
- Target: `Chromium` across 4 responsive profiles
- Execution Command: `npm run test:e2e` in `src/SentinelOps.Web`
- Target Environment: Live containerized platform (`http://localhost:3000`)

| Profile | Resolution | Tests Run | Passed | Failed |
| :--- | :--- | :---: | :---: | :---: |
| `setup` | Auth Session Seeding | 1 | 1 | 0 |
| `Desktop-1920x1080` | 1920x1080 | 22 | 22 | 0 |
| `Laptop-1440x900` | 1440x900 | 22 | 22 | 0 |
| `Tablet-768x1024` | 768x1024 | 22 | 22 | 0 |
| `Mobile-375x812` | 375x812 (mobile emulation) | 22 | 22 | 0 |
| **Playwright Total** | | **89** | **89** | **0** |

### 2.4 Kubernetes Staging Integration Suite
- Target: Local `kind` staging cluster (`sentinelops-staging`)
- Execution Command: `powershell -ExecutionPolicy Bypass -File .\tests\k8s\test-staging-cluster.ps1`

| Test ID | Area | Passed | Result |
| :--- | :--- | :---: | :---: |
| `K8S-01` | Control-Plane Node Status | Yes | PASS |
| `K8S-02` | All 6 Microservices Ready (1/1) | Yes | PASS |
| `K8S-03` | Persistent Volume Claims Bound | Yes | PASS |
| `K8S-04` | Service Discovery & Endpoints Populated | Yes | PASS |
| `K8S-05` | In-Cluster API Liveness (/health/live) | Yes | PASS |
| `K8S-06` | In-Cluster API Readiness (/health/ready) | Yes | PASS |
| `K8S-07` | External NodePort Ingestion (Port 14318) | Yes | PASS |
| `K8S-08` | Redis Streams Consumer Group Zero Lag | Yes | PASS |
| `K8S-09` | Pod Security Standards Non-Root & Drop Caps | Yes | PASS |
| `K8S-10` | Zero-Trust NetworkPolicy Isolation | Yes | PASS |
| **Kubernetes Staging Total** | | **10** | **100% PASS** |

### 2.5 Helm Chart Validation
- Command: `helm lint deploy/helm/sentinelops -f deploy/helm/sentinelops/values-staging-local.yaml`
- Result: `1 chart(s) linted, 0 chart(s) failed` (**0 errors, 0 warnings**)

---

## 3. Grand Total Regression Summary

| Category | Executions | Passed | Failed | Success Rate |
| :--- | :---: | :---: | :---: | :---: |
| Backend Unit & Integration Tests | 303 | 303 | 0 | 100.0% |
| Frontend Component & Unit Tests | 54 | 54 | 0 | 100.0% |
| Playwright Cross-Device E2E Tests | 89 | 89 | 0 | 100.0% |
| Kubernetes Staging Integration Tests | 10 | 10 | 0 | 100.0% |
| **Grand Total** | **456** | **456** | **0** | **100.0%** |
