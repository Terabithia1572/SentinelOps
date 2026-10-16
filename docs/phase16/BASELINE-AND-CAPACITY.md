# SentinelOps — Phase 16.0: Baseline and Host Capacity Assessment

## 1. Overview and Objectives
Phase 16 establishes a safe, isolated, reproducible local Kubernetes staging environment to validate production-grade replica scaling, rolling deployments, resilience, and security hardening without external cloud dependencies.

Prior to provisioning any infrastructure, this assessment captures the host machine's actual hardware capacity, running container workloads, tool availability, and port bindings to guarantee zero disruption to existing environments.

---

## 2. Git Baseline and Codebase State

- **Target Repository**: `https://github.com/Terabithia1572/SentinelOps`
- **Active Branch**: `main`
- **Baseline Git Commit**: [`6fe6e9e12a3623c2d733a9bb8db52224b8e9fcb6`](https://github.com/Terabithia1572/SentinelOps/commit/6fe6e9e12a3623c2d733a9bb8db52224b8e9fcb6)
- **Working Tree**: Completely clean (0 uncommitted changes, 0 untracked files).
- **Remote Synchronization**: Verified in sync with `origin/main`.

---

## 3. Docker Platform and Container Topology

### 3.1 Docker Engine & Client Information
- **Docker Client Version**: 28.5.1 (API 1.51, Go 1.24.8, Git `e180ab8`)
- **Docker Engine Version**: 28.5.1 (Docker Desktop 4.49.0 - 208700, Linux x86_64)
- **Docker Compose Version**: v2.40.3-desktop.1
- **Active Docker Context**: `desktop-linux` (`npipe:////./pipe/dockerDesktopLinuxEngine`)
- **WSL2 Runtime Distro**: `docker-desktop` (State: `Running`, Version: 2)

### 3.2 Running Container Inventory (Preserved Zero-Touch Baseline)
All 12 running containers were audited. None were modified or stopped:

| Container Name | Image / Role | Status | Port Bindings | Host Isolation Strategy |
| :--- | :--- | :---: | :--- | :--- |
| **`sentinelops-api`** | .NET 10 Web API | Up (healthy) | `0.0.0.0:5100->8080` | Compose platform service |
| **`sentinelops-worker`** | .NET 10 Background Worker | Up | *Internal network* | Compose platform service |
| **`sentinelops-web`** | React 19 Nginx frontend | Up | `0.0.0.0:3000->80` | Compose platform service |
| **`sentinelops-otel-collector`** | OTel Collector Contrib | Up | `4317-4318, 8888-8889` | Compose platform service |
| **`sentinelops-redis`** | Redis 7 Alpine | Up (healthy) | `127.0.0.1:56379->6379` | Dedicated SentinelOps loopback port |
| **`sentinelops-postgres`** | PostgreSQL 17 Alpine | Up (healthy) | `127.0.0.1:55432->5432` | Dedicated SentinelOps loopback port |
| **`BasketDB`** | Redis (unrelated project) | Up | `0.0.0.0:6379->6379` | **UNTOUCHED / PRESERVED** |
| **`OrderDB`** | SQL Server (unrelated) | Up | `0.0.0.0:1440->1433` | **UNTOUCHED / PRESERVED** |
| **`CommentDB`** | SQL Server (unrelated) | Up | `0.0.0.0:1442->1433` | **UNTOUCHED / PRESERVED** |
| **`CargoDB`** | SQL Server (unrelated) | Up | `0.0.0.0:1441->1433` | **UNTOUCHED / PRESERVED** |
| **`IdentityDB`** | SQL Server (unrelated) | Up | `0.0.0.0:1435->1433` | **UNTOUCHED / PRESERVED** |
| **`portainer`** | Portainer CE | Up | `8000, 9000` | **UNTOUCHED / PRESERVED** |

---

## 4. Host Resource Capacity & Allocation Limits

### 4.1 Measured Host Hardware
- **Operating System**: Windows 11 Enterprise (Build 26100, amd64)
- **Total Physical RAM**: **15.75 GB** (16,511,784 KB)
- **Free Physical RAM**: **~2.45 GB** (2,568,000 KB)
- **Host Disk Storage (C:)**: **454.17 GB Used / 10.31 GB Free**

### 4.2 Resource Budget for Local Kubernetes Staging
Given the ~2.45 GB available RAM and 10.31 GB free disk space, strict resource ceilings are mandated:
- **Cluster Node**: Single control-plane node (avoids multi-node VM overhead).
- **Control Plane Memory**: ~500 MiB (kube-apiserver, etcd, kube-controller-manager).
- **Staging Pod Budget**:
  - `sentinelops-api`: 150 MiB request / 256 MiB limit
  - `sentinelops-worker`: 100 MiB request / 200 MiB limit
  - `sentinelops-web`: 30 MiB request / 64 MiB limit
  - `sentinelops-postgres` (staging): 80 MiB request / 192 MiB limit
  - `sentinelops-redis` (staging): 20 MiB request / 64 MiB limit
  - `sentinelops-otel-collector` (staging): 40 MiB request / 96 MiB limit
- **Total Staging Workload Ceiling**: **~872 MiB**, comfortably within the host's 2.45 GB free headroom.

---

## 5. Kubernetes Tooling Availability

The required CLI binaries have been verified on the host:
- **`kubectl`**: `v1.34.1` (installed via Docker Desktop resources at `C:\Program Files\Docker\Docker\resources\bin\kubectl.exe`)
- **`helm`**: `v3.17.1` (installed in `tools/bin/helm.exe`)
- **`kind`**: `v0.27.0` (installed in `tools/bin/kind.exe`)
- **Existing Contexts**: None (clean environment, `kubectl config get-contexts` returned 0 contexts).

---

## 6. Cluster Architecture & Isolation Strategy

### 6.1 Cluster Engine Selection Matrix

| Option | Isolation Level | Disrupts Host Docker? | Safety Constraint Compliance | Decision |
| :--- | :---: | :---: | :---: | :---: |
| **Docker Desktop K8s** | Low (shared daemon) | **YES** (requires daemon restart & context switch) | Violates Constraints #7 & #8 | **REJECTED** |
| **Minikube (Hyper-V)** | Medium | Requires hypervisor reconfiguration | Risk of network interface conflicts | **REJECTED** |
| **kind (Kubernetes in Docker)** | **High (Isolated container)** | **NO** (runs as an ordinary Docker container on `desktop-linux`) | **100% Compliant** with all 15 safety rules | **SELECTED** |

### 6.2 Cluster Identity & Boundaries
- **Cluster Name**: `sentinelops-staging`
- **Target Namespace**: `sentinelops-staging`
- **Docker Container Node**: `sentinelops-staging-control-plane`
- **Network**: Dedicated `kind` bridge network (isolated from `sentinelops_sentinelops`).

---

## 7. Port Binding & Conflict Mitigation

To avoid any overlap with host applications or the existing Docker Compose stack, the following ports are strictly reserved:

| Service | Docker Compose Port | Staging K8s NodePort / Ingress Port | Conflict Risk |
| :--- | :---: | :---: | :---: |
| **Web Ingress (HTTP)** | `3000` | **`8085`** (or `30080`) | **Zero Conflict** |
| **Web Ingress (HTTPS)** | N/A | **`8443`** (or `30443`) | **Zero Conflict** |
| **API Direct NodePort** | `5100` | **`8086`** (or internal ClusterIP only) | **Zero Conflict** |
| **Staging Postgres** | `55432` | **Internal ClusterIP only** (port 5432) | **Zero Conflict** |
| **Staging Redis** | `56379` | **Internal ClusterIP only** (port 6379) | **Zero Conflict** |
| **Staging OTel Receiver** | `4318` | **`14318`** (NodePort on host) | **Zero Conflict** |

---

## 8. Checkpoint 16.0 Status
**Status: PASS**
- Baseline environment audited and recorded.
- Zero-touch preservation of all 12 existing containers verified.
- Host capacity measured (15.75 GB RAM, 2.45 GB free, 10.3 GB disk).
- Tooling verified (`kubectl v1.34.1`, `helm v3.17.1`, `kind v0.27.0`).
- Isolated `kind` cluster architecture selected with non-conflicting port mapping.
