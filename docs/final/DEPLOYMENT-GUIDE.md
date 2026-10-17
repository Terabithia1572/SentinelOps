# SentinelOps — Local Deployment & Setup Guide

## 1. System Requirements

- **Operating System**: Windows 10/11 (with WSL2), macOS 13+, or Ubuntu 22.04+ LTS
- **Docker Desktop**: Version 4.30+ with Docker Compose v2
- **Memory**: Minimum 4 GB RAM allocated to Docker (8 GB recommended)
- **Disk**: 5 GB free disk space
- **Local Ports Required**:
  - `3000`: Web Frontend (Nginx)
  - `5100`: Web API (ASP.NET Core)
  - `55432`: PostgreSQL Database (Containerized, avoids native 5432)
  - `56379`: Redis (Containerized, avoids native 6379)
  - `4317` / `4318`: OpenTelemetry Collector (gRPC / HTTP)

---

## 2. Quickstart with Docker Compose

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/Terabithia1572/SentinelOps.git
   cd SentinelOps
   ```

2. **Configure Environment (Optional)**:
   A sample `.env.example` is provided in the repository root:
   ```bash
   cp .env.example .env
   ```
   *(Default development settings work out-of-the-box without editing `.env`)*

3. **Start All Services**:
   ```bash
   docker compose up -d
   ```

4. **Verify Service Health**:
   ```bash
   docker compose ps
   ```
   All 6 services should be in state `Up` or `Up (healthy)`.

5. **Access the Application**:
   - Web Frontend: [http://localhost:3000](http://localhost:3000)
   - Swagger OpenAPI Docs: [http://localhost:5100/swagger](http://localhost:5100/swagger)
   - API Live Health: [http://localhost:5100/health/live](http://localhost:5100/health/live)
   - API Ready Health: [http://localhost:5100/health/ready](http://localhost:5100/health/ready)

---

## 3. Local Developer Setup (Bare Metal / IDE)

If developing outside Docker containers:

### Prerequisites
- .NET 10 SDK (`v10.0.401+`)
- Node.js `22.x LTS` and `npm 10+`
- PostgreSQL 17+ running on `localhost:55432`
- Redis 7+ running on `localhost:56379`

### Backend Execution
```bash
# Restore and build the solution
dotnet restore SentinelOps.slnx
dotnet build SentinelOps.slnx

# Run database migrations
dotnet run --project src/SentinelOps.Api -- --apply-migrations

# Start the Web API
dotnet run --project src/SentinelOps.Api

# In a separate terminal, start the Background Worker
dotnet run --project src/SentinelOps.Worker
```

### Frontend Execution
```bash
cd src/SentinelOps.Web
npm install
npm run dev
# Vite will serve the development frontend at http://localhost:5173
```

---

## 4. Ingestion Sample Telemetry

SentinelOps includes a built-in telemetry simulation tool to populate mock telemetry for local exploration:

```bash
dotnet run --project tools/SentinelOps.TelemetryGenerator
```

The generator dispatches high-cardinality spans, metric counters, and error logs directly to the OpenTelemetry Collector on `localhost:4318`.
