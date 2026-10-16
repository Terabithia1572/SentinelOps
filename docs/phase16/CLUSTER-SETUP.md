# SentinelOps — Phase 16.1: Isolated Kubernetes Staging Cluster Setup

## 1. Overview and Architecture
Phase 16 Checkpoint 16.1 provisions a dedicated, fully isolated local Kubernetes staging environment using `kind` (Kubernetes in Docker).

The staging cluster runs as an independent container (`sentinelops-staging-control-plane`) within the host Docker daemon, ensuring complete decoupling from the existing SentinelOps Docker Compose environment and preserving all unrelated host workloads (`BasketDB`, `OrderDB`, `CommentDB`, `CargoDB`, `IdentityDB`, `portainer`).

---

## 2. Cluster Specification & Port Mapping

### 2.1 Cluster Identity
- **Cluster Name**: `sentinelops-staging`
- **Kubernetes Version**: `v1.32.2` (kindest/node:v1.32.2)
- **Container Runtime**: `containerd://2.0.2`
- **Node OS**: Debian GNU/Linux 12 (bookworm) / WSL2 Linux Kernel 6.6.87
- **Target Namespace**: `sentinelops-staging`

### 2.2 Non-Conflicting Port Architecture
To prevent collisions with existing host services or Docker Compose containers, all host port exposures are routed through designated high-range ports:

| Container Port | Staging Host Port | Purpose | Docker Compose Port | Collision Risk |
| :---: | :---: | :--- | :---: | :---: |
| `80` | **`8085`** | Staging Ingress HTTP | `3000` (Web) | **Zero (Safe)** |
| `443` | **`8443`** | Staging Ingress HTTPS | N/A | **Zero (Safe)** |
| `31318` | **`14318`** | Staging OTLP HTTP Telemetry Ingest | `4318` (OTel) | **Zero (Safe)** |
| `6443` | **`49170`** | Kubernetes API Server | N/A | **Zero (Safe)** |

---

## 3. Configuration & Scripts

### 3.1 Cluster Configuration (`deploy/kind/sentinelops-staging.yaml`)
```yaml
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
name: sentinelops-staging
nodes:
  - role: control-plane
    kubeadmConfigPatches:
      - |
        kind: InitConfiguration
        nodeRegistration:
          kubeletExtraArgs:
            node-labels: "ingress-ready=true"
    extraPortMappings:
      - containerPort: 80
        hostPort: 8085
        protocol: TCP
      - containerPort: 443
        hostPort: 8443
        protocol: TCP
      - containerPort: 31318
        hostPort: 14318
        protocol: TCP
```

### 3.2 Automation Scripts
- **Startup Script**: [`scripts/staging/start-local-staging.ps1`](file:///c:/Users/Yunus/Desktop/SentinelOps/scripts/staging/start-local-staging.ps1)
  - Checks Docker engine status.
  - Provisions `sentinelops-staging` kind cluster idempotently.
  - Switches `kubectl` context to `kind-sentinelops-staging`.
  - Creates the `sentinelops-staging` namespace with managed labels.
  - Builds and loads deterministic staging images (`sentinelops-api:1.0.0-staging`, etc.).
- **Teardown Script**: [`scripts/staging/stop-local-staging.ps1`](file:///c:/Users/Yunus/Desktop/SentinelOps/scripts/staging/stop-local-staging.ps1)
  - Requires deliberate confirmation (`-Force` to bypass).
  - Targets strictly `sentinelops-staging`, completely shielding Docker Compose containers and databases.

---

## 4. Live Verification Evidence

### 4.1 Cluster and Node Status
```
NAME                                STATUS   ROLES           AGE   VERSION
sentinelops-staging-control-plane   Ready    control-plane   36s   v1.32.2
```

### 4.2 Control-Plane Pod Status
```
NAME                                                        READY   STATUS    RESTARTS   AGE
coredns-668d6bf9bc-cnjzp                                    1/1     Running   0          28s
coredns-668d6bf9bc-jprc7                                    1/1     Running   0          28s
etcd-sentinelops-staging-control-plane                      1/1     Running   0          34s
kindnet-vl2s8                                               1/1     Running   0          29s
kube-apiserver-sentinelops-staging-control-plane            1/1     Running   0          34s
kube-controller-manager-sentinelops-staging-control-plane   1/1     Running   0          34s
kube-proxy-7nc9g                                            1/1     Running   0          29s
kube-scheduler-sentinelops-staging-control-plane            1/1     Running   0          34s
```

### 4.3 Preserved Host Container Verification
```
NAMES                               STATUS                   PORTS
sentinelops-staging-control-plane   Up 52 seconds            0.0.0.0:8085->80/tcp, 0.0.0.0:8443->443/tcp, 0.0.0.0:14318->31318/tcp
sentinelops-api                     Up 5 minutes (healthy)   0.0.0.0:5100->8080/tcp
sentinelops-worker                  Up 6 minutes             (internal)
sentinelops-web                     Up 6 minutes             0.0.0.0:3000->80/tcp
sentinelops-otel-collector          Up 6 minutes             0.0.0.0:4317-4318, 8888-8889
sentinelops-redis                   Up 6 minutes (healthy)   127.0.0.1:56379->6379/tcp
sentinelops-postgres                Up 6 minutes (healthy)   127.0.0.1:55432->5432/tcp
BasketDB                            Up 6 minutes             0.0.0.0:6379->6379/tcp
OrderDB                             Up 6 minutes             0.0.0.0:1440->1433/tcp
CommentDB                           Up 6 minutes             0.0.0.0:1442->1433/tcp
CargoDB                             Up 6 minutes             0.0.0.0:1441->1433/tcp
IdentityDB                          Up 6 minutes             0.0.0.0:1435->1433/tcp
portainer                           Up 6 minutes             0.0.0.0:8000, 9000
```

---

## 5. Checkpoint 16.1 Status
**Status: PASS**
- Isolated kind cluster created and healthy.
- Dedicated namespace `sentinelops-staging` created.
- Zero host port conflicts confirmed.
- Complete preservation of all 12 existing containers verified.
- Automation scripts authored and verified.
