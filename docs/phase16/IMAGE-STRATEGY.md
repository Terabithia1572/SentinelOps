# SentinelOps — Phase 16.3: Container Image and Registry Strategy

## 1. Overview and Image Architecture
Phase 16 Checkpoint 16.3 validates the security, reproducibility, minimal footprint, and injection mechanism of all container images deployed to the local Kubernetes staging environment.

In compliance with absolute safety constraints:
- **Zero External Publishing**: No images are pushed to Docker Hub, GitHub Packages (GHCR), or any public registry.
- **Direct Cluster Loading**: Images are built locally and loaded directly into the containerd runtime of `sentinelops-staging` via `kind load docker-image`.
- **Deterministic Tagging**: Mutable `latest` tags are prohibited. All builds use deterministic version tags (e.g. `1.0.0-staging`).

---

## 2. Dockerfile Verification Matrix

| Container Image | Dockerfile Location | Base Image | Multi-Stage? | User Context | Security Context | Image Size |
| :--- | :--- | :--- | :---: | :---: | :---: | :---: |
| **`sentinelops-api`** | `deploy/docker/Dockerfile.api` | `mcr.microsoft.com/dotnet/aspnet:10.0` | **Yes (4 stages)** | Non-root `app` (UID 1000) | `drop: [ALL]`, `allowPrivilegeEscalation: false` | **104 MB** |
| **`sentinelops-worker`** | `deploy/docker/Dockerfile.worker` | `mcr.microsoft.com/dotnet/aspnet:10.0` | **Yes (4 stages)** | Non-root `app` (UID 1000) | `drop: [ALL]`, `allowPrivilegeEscalation: false` | **100 MB** |
| **`sentinelops-web`** | `deploy/docker/Dockerfile.web` | `nginx:alpine` | **Yes (2 stages)** | Non-root `nginx` (UID 101) | `drop: [ALL]`, `allowPrivilegeEscalation: false` | **29.2 MB** |

### Key Dockerfile Hardening Properties:
1. **Multi-Stage Build Separation**:
   - SDK images (`mcr.microsoft.com/dotnet/sdk:10.0` and `node:22-alpine`) are discarded after compilation.
   - Runtime images contain strictly published application binaries and trimmed runtime packages.
2. **Minimal Layer Footprint**:
   - API image size is only 104 MB.
   - Worker image size is only 100 MB.
   - Web Nginx image size is only 29.2 MB.
3. **Build Context Optimization**:
   - The repository root `.dockerignore` filters out `bin/`, `obj/`, `node_modules/`, `.git/`, test outputs, and secrets.
   - Build context transfer takes <0.4 seconds.
4. **Secret Hygiene**:
   - Zero credentials, JWT keys, or database connection strings are embedded in images or environment variables. All secrets are injected dynamically via Kubernetes `SecretKeyRef`.

---

## 3. Image Injection & Runtime Verification in kind

Images were built locally and injected into the isolated cluster:

```bash
docker build -f deploy/docker/Dockerfile.api -t sentinelops-api:1.0.0-staging .
docker build -f deploy/docker/Dockerfile.worker -t sentinelops-worker:1.0.0-staging .
docker build -f deploy/docker/Dockerfile.web -t sentinelops-web:1.0.0-staging .

kind load docker-image sentinelops-api:1.0.0-staging --name sentinelops-staging
kind load docker-image sentinelops-worker:1.0.0-staging --name sentinelops-staging
kind load docker-image sentinelops-web:1.0.0-staging --name sentinelops-staging
```

### Verification via containerd CLI (`crictl images`):
```
IMAGE                                 TAG             IMAGE ID        SIZE
docker.io/library/sentinelops-api     1.0.0-staging   665a367cc86ad   104MB
docker.io/library/sentinelops-worker  1.0.0-staging   cea746e02aa4f   100MB
docker.io/library/sentinelops-web     1.0.0-staging   d9513e72cb579   29.2MB
```

All three staging images are verified resident in the kind node's local containerd image store. Kubernetes will consume these images locally with `imagePullPolicy: IfNotPresent` without making external network calls.

---

## 4. Checkpoint 16.3 Status
**Status: PASS**
- All 3 Dockerfiles inspected and confirmed non-root, multi-stage, and .NET 10 compatible.
- Deterministic image tags applied (`1.0.0-staging`).
- Successful build and loading into kind node containerd store verified.
- Zero external image publishing occurred.
