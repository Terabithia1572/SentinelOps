import puppeteer from "puppeteer-core";
import path from "path";
import fs from "fs";

const CHROME_PATH = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const BASE_URL = "http://127.0.0.1:3000";
const SCREENSHOTS_DIR = "C:\\Users\\Yunus\\Desktop\\SentinelOps\\docs\\screenshots";

if (!fs.existsSync(SCREENSHOTS_DIR)) {
  fs.mkdirSync(SCREENSHOTS_DIR, { recursive: true });
}

// Authentic Multi-Tenant & Phase 8 Mock Data Payload
const mockUser = {
  id: "usr-admin-01",
  userName: "admin",
  email: "admin@sentinelops.local",
  fullName: "System Administrator",
  roles: ["Admin", "Operator"],
};

const mockOrganizations = [
  {
    id: "org-acme-01",
    name: "Acme Global Technologies",
    slug: "acme-global",
    description: "Enterprise multi-cloud production workspace & observability hub",
    isActive: true,
    userRole: "Owner",
    membersCount: 8,
    applicationsCount: 14,
    createdAtUtc: "2026-10-01T00:00:00Z",
  },
  {
    id: "org-fintech-02",
    name: "FinTech Payments Corp",
    slug: "fintech-payments",
    description: "PCI-DSS compliant payment processing cluster",
    isActive: true,
    userRole: "Admin",
    membersCount: 4,
    applicationsCount: 6,
    createdAtUtc: "2026-10-05T00:00:00Z",
  },
];

const mockOrgDetail = {
  id: "org-acme-01",
  name: "Acme Global Technologies",
  slug: "acme-global",
  description: "Enterprise multi-cloud production workspace & observability hub",
  isActive: true,
  userRole: "Owner",
  createdAtUtc: "2026-10-01T00:00:00Z",
  members: [
    {
      id: "mem-01",
      organizationId: "org-acme-01",
      userId: "usr-admin-01",
      userName: "admin",
      email: "admin@sentinelops.local",
      fullName: "System Administrator",
      role: "Owner",
      joinedAtUtc: "2026-10-01T00:00:00Z",
    },
    {
      id: "mem-02",
      organizationId: "org-acme-01",
      userId: "usr-sre-02",
      userName: "sre.lead",
      email: "sre.lead@acme.com",
      fullName: "Elena Rostova",
      role: "Admin",
      joinedAtUtc: "2026-10-02T10:00:00Z",
    },
    {
      id: "mem-03",
      organizationId: "org-acme-01",
      userId: "usr-dev-03",
      userName: "dev.oncall",
      email: "dev.oncall@acme.com",
      fullName: "Marcus Vance",
      role: "Member",
      joinedAtUtc: "2026-10-03T14:30:00Z",
    },
  ],
  pendingInvitations: [
    {
      id: "inv-01",
      organizationId: "org-acme-01",
      email: "security.ops@acme.com",
      role: "Member",
      expiresAtUtc: "2026-10-20T00:00:00Z",
      invitedByUserName: "admin",
      createdAtUtc: "2026-10-13T12:00:00Z",
      isPending: true,
    },
  ],
  settings: {
    allowMemberInvites: true,
    requireTwoFactor: true,
    defaultRetentionDays: 60,
    planTier: "Enterprise",
    maxApplications: 25,
    maxMembers: 50,
    maxCustomDashboards: 30,
    maxMonthlyLogs: 10000000,
    maxMonthlyTraces: 5000000,
  },
  usage: {
    logsIngestedMonthToDate: 8420000,
    tracesIngestedMonthToDate: 2110000,
    metricsIngestedMonthToDate: 45200000,
    apiRequestsMonthToDate: 14890000,
    estimatedStorageBytes: 2576980377,
    lastResetAtUtc: "2026-10-01T00:00:00Z",
  },
};

const mockQuotaStatus = {
  organizationId: "org-acme-01",
  organizationName: "Acme Global Technologies",
  planTier: "Enterprise",
  logsUsed: 8420000,
  logsQuota: 10000000,
  logsPercentUsed: 84.2,
  isLogsWarning: true,
  isLogsExceeded: false,
  tracesUsed: 2110000,
  tracesQuota: 5000000,
  tracesPercentUsed: 42.2,
  isTracesWarning: false,
  isTracesExceeded: false,
  applicationsUsed: 14,
  applicationsQuota: 25,
  applicationsPercentUsed: 56.0,
  dashboardsUsed: 12,
  dashboardsQuota: 30,
  dashboardsPercentUsed: 40.0,
  membersUsed: 8,
  membersQuota: 50,
  membersPercentUsed: 16.0,
  estimatedStorageBytes: 2576980377,
  hasAnyQuotaWarning: true,
  hasAnyQuotaExceeded: false,
};

const mockDashboards = [
  {
    id: "dash-sre-01",
    title: "Production SRE Core Observability",
    description: "Multi-service Golden Signals, error budget burndown & distributed latency map",
    isDefault: true,
    isShared: true,
    theme: "Dark",
    refreshIntervalSeconds: 15,
    widgetCount: 6,
    tags: ["sre", "production", "goldensignals", "latency"],
    createdAtUtc: "2026-10-12T00:00:00Z",
    updatedAtUtc: "2026-10-13T20:00:00Z",
  },
  {
    id: "dash-api-02",
    title: "Checkout & Payments API Golden Signals",
    description: "Throughput, P99 latency percentiles, error rate spikes and gateway status",
    isDefault: false,
    isShared: true,
    theme: "Dark",
    refreshIntervalSeconds: 30,
    widgetCount: 5,
    tags: ["payments", "checkout", "api"],
    createdAtUtc: "2026-10-12T05:00:00Z",
    updatedAtUtc: "2026-10-13T18:00:00Z",
  },
];

const mockDashboardDetail = {
  id: "dash-sre-01",
  title: "Production SRE Core Observability",
  description: "Multi-service Golden Signals, error budget burndown & distributed latency map",
  isDefault: true,
  isShared: true,
  theme: "Dark",
  refreshIntervalSeconds: 15,
  layoutJson: "{}",
  tags: ["sre", "production", "goldensignals", "latency"],
  createdAtUtc: "2026-10-12T00:00:00Z",
  updatedAtUtc: "2026-10-13T20:00:00Z",
  widgets: [
    {
      id: "w-01",
      title: "Global HTTP Requests Throughput (RPS)",
      type: "MetricChart",
      colSpan: 8,
      rowSpan: 1,
      order: 1,
      configurationJson: JSON.stringify({ metric: "http_requests_total", aggregation: "sum" }),
    },
    {
      id: "w-02",
      title: "Monthly Ingestion Quota Consumption",
      type: "IngestionGauge",
      colSpan: 4,
      rowSpan: 1,
      order: 2,
      configurationJson: JSON.stringify({ threshold: 80 }),
    },
    {
      id: "w-03",
      title: "Service P95 Latency Heatmap",
      type: "ServiceLatency",
      colSpan: 6,
      rowSpan: 1,
      order: 3,
      configurationJson: JSON.stringify({ window: "1h" }),
    },
    {
      id: "w-04",
      title: "Active Error Rate & Anomaly Spike Detector",
      type: "ErrorRate",
      colSpan: 6,
      rowSpan: 1,
      order: 4,
      configurationJson: JSON.stringify({ threshold: 1.5 }),
    },
    {
      id: "w-05",
      title: "Active Alert Incidents Triage",
      type: "AlertSummary",
      colSpan: 6,
      rowSpan: 1,
      order: 5,
      configurationJson: JSON.stringify({ filter: "firing" }),
    },
    {
      id: "w-06",
      title: "Distributed Service Topology Status",
      type: "TopologyMap",
      colSpan: 6,
      rowSpan: 1,
      order: 6,
      configurationJson: JSON.stringify({ layout: "directed" }),
    },
  ],
};

const mockApmTopology = {
  services: [
    {
      serviceName: "api-gateway",
      applicationId: "app-01",
      environmentName: "Production",
      totalSpans: 48920,
      errorCount: 142,
      errorRate: 0.29,
      averageDurationMs: 24.5,
      p95DurationMs: 68.2,
      incomingCallCount: 124000,
      outgoingCallCount: 98000,
      healthStatus: "Healthy",
    },
    {
      serviceName: "checkout-api",
      applicationId: "app-01",
      environmentName: "Production",
      totalSpans: 31200,
      errorCount: 684,
      errorRate: 2.19,
      averageDurationMs: 142.0,
      p95DurationMs: 385.0,
      incomingCallCount: 62000,
      outgoingCallCount: 54000,
      healthStatus: "Degraded",
    },
    {
      serviceName: "payments-service",
      applicationId: "app-01",
      environmentName: "Production",
      totalSpans: 24500,
      errorCount: 1205,
      errorRate: 4.92,
      averageDurationMs: 284.0,
      p95DurationMs: 820.0,
      incomingCallCount: 41000,
      outgoingCallCount: 22000,
      healthStatus: "Critical",
    },
    {
      serviceName: "fraud-detector",
      applicationId: "app-01",
      environmentName: "Production",
      totalSpans: 19800,
      errorCount: 12,
      errorRate: 0.06,
      averageDurationMs: 18.2,
      p95DurationMs: 42.1,
      incomingCallCount: 38000,
      outgoingCallCount: 0,
      healthStatus: "Healthy",
    },
    {
      serviceName: "notification-worker",
      applicationId: "app-01",
      environmentName: "Production",
      totalSpans: 15400,
      errorCount: 45,
      errorRate: 0.29,
      averageDurationMs: 35.0,
      p95DurationMs: 92.0,
      incomingCallCount: 15400,
      outgoingCallCount: 12000,
      healthStatus: "Healthy",
    },
  ],
  dependencies: [
    {
      sourceService: "api-gateway",
      targetService: "checkout-api",
      callCount: 62000,
      errorCount: 412,
      errorRate: 0.66,
      averageDurationMs: 145.0,
      p95DurationMs: 380.0,
    },
    {
      sourceService: "checkout-api",
      targetService: "payments-service",
      callCount: 41000,
      errorCount: 1205,
      errorRate: 2.94,
      averageDurationMs: 290.0,
      p95DurationMs: 820.0,
    },
    {
      sourceService: "checkout-api",
      targetService: "fraud-detector",
      callCount: 38000,
      errorCount: 12,
      errorRate: 0.03,
      averageDurationMs: 18.0,
      p95DurationMs: 42.0,
    },
    {
      sourceService: "payments-service",
      targetService: "notification-worker",
      callCount: 15400,
      errorCount: 45,
      errorRate: 0.29,
      averageDurationMs: 35.0,
      p95DurationMs: 92.0,
    },
  ],
  totalServices: 5,
  totalEdges: 4,
};

const mockApmEndpoints = [
  {
    endpoint: "POST /api/v1/checkout/process",
    serviceName: "checkout-api",
    httpMethod: "POST",
    route: "/api/v1/checkout/process",
    callCount: 41200,
    errorCount: 1205,
    errorRate: 2.92,
    p50Ms: 120.0,
    p90Ms: 340.0,
    p95Ms: 620.0,
    p99Ms: 1450.0,
    slowestTraceId: "trc-89a1c3dfb2014e",
    healthStatus: "Degraded",
  },
  {
    endpoint: "GET /api/v1/products/search",
    serviceName: "api-gateway",
    httpMethod: "GET",
    route: "/api/v1/products/search",
    callCount: 112000,
    errorCount: 18,
    errorRate: 0.02,
    p50Ms: 14.5,
    p90Ms: 32.0,
    p95Ms: 58.0,
    p99Ms: 110.0,
    slowestTraceId: "trc-11f8b4498ec0",
    healthStatus: "Healthy",
  },
  {
    endpoint: "POST /api/v1/payments/authorize",
    serviceName: "payments-service",
    httpMethod: "POST",
    route: "/api/v1/payments/authorize",
    callCount: 28400,
    errorCount: 1390,
    errorRate: 4.89,
    p50Ms: 240.0,
    p90Ms: 590.0,
    p95Ms: 910.0,
    p99Ms: 2300.0,
    slowestTraceId: "trc-77d018bba491",
    healthStatus: "Critical",
  },
];

const mockApmExternal = [
  {
    target: "postgresql://payments-db.prod.internal:5432",
    type: "Database",
    callingService: "payments-service",
    callCount: 84000,
    errorCount: 320,
    errorRate: 0.38,
    averageDurationMs: 42.5,
    p95DurationMs: 210.0,
  },
  {
    target: "redis://cache-cluster.prod.internal:6379",
    type: "Cache",
    callingService: "checkout-api",
    callCount: 125000,
    errorCount: 4,
    errorRate: 0.003,
    averageDurationMs: 2.1,
    p95DurationMs: 6.4,
  },
  {
    target: "https://api.stripe.com/v1/charges",
    type: "HTTP External",
    callingService: "payments-service",
    callCount: 28400,
    errorCount: 280,
    errorRate: 0.98,
    averageDurationMs: 185.0,
    p95DurationMs: 480.0,
  },
];

const mockIncidents = [
  {
    id: "inc-payment-01",
    title: "Elevated Payment Checkout Latency across US-East Region",
    description: "P95 latency elevated to 840ms due to database connection pool saturation. Multiple payment retries observed.",
    severity: "Critical",
    status: "Investigating",
    applicationId: "app-01",
    serviceName: "payments-service",
    environmentName: "Production",
    firedAtUtc: "2026-10-13T21:15:00Z",
    acknowledgedAtUtc: "2026-10-13T21:18:00Z",
    acknowledgedBy: "Elena Rostova",
    hasAiDiagnosis: true,
  },
];

const mockIncidentDetail = {
  id: "inc-payment-01",
  title: "Elevated Payment Checkout Latency across US-East Region",
  description: "P95 latency elevated to 840ms due to database connection pool saturation. Multiple payment retries observed.",
  severity: "Critical",
  status: "Investigating",
  applicationId: "app-01",
  serviceName: "payments-service",
  environmentName: "Production",
  firedAtUtc: "2026-10-13T21:15:00Z",
  acknowledgedAtUtc: "2026-10-13T21:18:00Z",
  acknowledgedBy: "Elena Rostova",
  comments: [
    {
      id: "comm-01",
      userName: "Elena Rostova",
      userEmail: "sre.lead@acme.com",
      commentText: "Observed sudden spike in 504 Gateway Timeouts from checkout-api to payments-service. Connection pool exhausted.",
      createdAtUtc: "2026-10-13T21:20:00Z",
    },
  ],
  timelineEvents: [
    {
      id: "time-01",
      eventType: "StatusChanged",
      description: "Incident status transitioned from Firing to Investigating",
      actorUserName: "Elena Rostova",
      timestampUtc: "2026-10-13T21:18:00Z",
    },
    {
      id: "time-02",
      eventType: "AlertFired",
      description: "Alert rule 'Elevated P95 API Latency' triggered",
      actorUserName: "System",
      timestampUtc: "2026-10-13T21:15:00Z",
    },
  ],
};

const mockIncidentIntelligence = {
  incidentId: "inc-payment-01",
  diagnosticSummary: "Database connection pool exhaustion on payments-db caused by unindexed batch transaction lookup queries during 10x traffic spike.",
  rootCause: "PostgreSQL connection pool max ceiling (50 connections) reached; slow unindexed queries on transactions_ledger held connections open >2.4s.",
  confidenceScore: 0.94,
  severityAssessment: "Critical — Active customer checkout failures detected on 4.9% of incoming payment authorizations.",
  recommendedActions: [
    {
      id: "act-01",
      title: "Scale Connection Pool Ceiling to 150",
      description: "Increase MaxPoolSize in payments-service connection string from 50 to 150 to immediately relieve pool contention.",
      priority: "Immediate",
      status: "Pending",
      commandSnippet: "az appservice update --set-env CONNECTION_MAX_POOL_SIZE=150",
    },
    {
      id: "act-02",
      title: "Enable Read-Replica Routing for Ledger Lookups",
      description: "Divert read-only historical ledger queries to replica pool replica-db-01.prod.",
      priority: "High",
      status: "Pending",
      commandSnippet: "kubectl set env deployment/payments-service DB_READ_REPLICA_HOST=replica-db-01.prod",
    },
    {
      id: "act-03",
      title: "Apply Composite Index on transactions_ledger",
      description: "Add index CONCURRENTLY on (merchant_id, created_at_utc, status) to drop query scan time from 2.4s to 8ms.",
      priority: "Recommended",
      status: "Pending",
      commandSnippet: "CREATE INDEX CONCURRENTLY idx_tx_merchant_created ON transactions_ledger (merchant_id, created_at_utc);",
    },
  ],
  scrubbedEvidence: [
    "LOG [ERROR] payments-service [Npgsql.NpgsqlException]: Timeout during connection acquisition from pool (max 50 connections reached in 15000ms). User [REDACTED_USER], client [REDACTED_IP].",
    "SPAN trace_id=trc-77d018bba491: operation=db.execute duration=2450ms target=postgresql://payments-db.prod.internal:5432 status=ERROR",
    "METRIC p95_latency: baseline=140ms, current=840ms (+500% surge).",
  ],
  similarPastIncidents: [
    {
      incidentId: "inc-past-08",
      title: "Connection pool exhaustion during Black Friday 2025 checkout rush",
      resolvedAtUtc: "2025-11-28T14:30:00Z",
      resolutionSummary: "Increased pool ceiling to 150 and implemented query statement timeouts.",
      similarityScore: 0.89,
    },
  ],
  generatedAtUtc: "2026-10-13T21:22:00Z",
  postmortemMarkdown: `# Postmortem Report: Elevated Payment Latency & Connection Pool Exhaustion

## Incident Summary
- **Incident ID:** inc-payment-01
- **Severity:** Critical
- **Fired At:** 2026-10-13 21:15 UTC
- **Duration:** 35 minutes
- **Impact:** 4.9% authorization failure rate across checkout transactions

## Root Cause Analysis
During high transaction volume, unindexed queries on \`transactions_ledger\` accumulated, holding database connections for up to 2.4s. The connection pool ceiling of 50 connections was rapidly saturated, causing downstream HTTP 504 timeouts at the API gateway.

## Corrective Actions & Prevention
1. Scaled connection pool ceiling from 50 to 150.
2. Implemented composite index on \`transactions_ledger(merchant_id, created_at_utc)\`.
3. Configured statement timeout of 3000ms to prevent hanging queries.
`,
};

const mockPublicStatus = {
  id: "sp-01",
  name: "Acme Cloud Production Services",
  slug: "acme-production",
  description: "Official real-time platform status, component health, and incident updates for Acme Cloud.",
  isPublic: true,
  overallStatus: "Degraded",
  activeIncidentsCount: 1,
  components: [
    {
      id: "comp-01",
      name: "Global API Gateway & Edge Routers",
      description: "Public edge ingress and TLS termination",
      status: "Operational",
      uptimePercentage: 99.99,
    },
    {
      id: "comp-02",
      name: "Core Payments & Checkout Engine",
      description: "Payment authorization, capture, and settlement pipeline",
      status: "Degraded",
      uptimePercentage: 99.21,
    },
    {
      id: "comp-03",
      name: "Customer Identity & Authentication",
      description: "OAuth 2.0 / JWT issuance and tenant validation",
      status: "Operational",
      uptimePercentage: 100.0,
    },
    {
      id: "comp-04",
      name: "Telemetry & Observability Ingestion",
      description: "OTel OpenTelemetry Collector, logs and metrics ingestion",
      status: "Operational",
      uptimePercentage: 99.98,
    },
  ],
  activeIncidents: [
    {
      id: "spi-01",
      title: "Elevated Payment Checkout Latency across US-East Region",
      status: "Investigating",
      impact: "Degraded",
      createdAtUtc: "2026-10-13T21:15:00Z",
      updates: [
        {
          id: "upd-02",
          status: "Identified",
          message: "Root cause isolated to database connection pool saturation. Scaling connection pool ceiling and applying query optimizations.",
          createdAtUtc: "2026-10-13T21:28:00Z",
        },
        {
          id: "upd-01",
          status: "Investigating",
          message: "Engineers are actively investigating elevated response times in the payments processing pipeline.",
          createdAtUtc: "2026-10-13T21:18:00Z",
        },
      ],
    },
  ],
};

async function runVisualQA() {
  console.log("=================================================================");
  console.log("   SENTINELOPS — PHASE 8 MULTI-TENANT SAAS BROWSER VISUAL QA");
  console.log("=================================================================");

  const browser = await puppeteer.launch({
    executablePath: CHROME_PATH,
    headless: "new",
    args: ["--no-sandbox", "--disable-setuid-sandbox", "--disable-gpu", "--window-size=1600,1000"],
  });

  const page = await browser.newPage();
  await page.setViewport({ width: 1440, height: 900 });

  await page.setRequestInterception(true);
  page.on("request", (req) => {
    const url = req.url();
    const method = req.method();

    if (url.includes("/api/auth/me")) {
      req.respond({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ success: true, data: mockUser, errors: [] }),
      });
    } else if (url.includes("/api/organizations/org-acme-01/quota") || url.includes("/api/organizations/org-1/quota")) {
      req.respond({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ success: true, data: mockQuotaStatus, errors: [] }),
      });
    } else if (url.includes("/api/organizations/org-acme-01") || url.includes("/api/organizations/org-1")) {
      req.respond({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ success: true, data: mockOrgDetail, errors: [] }),
      });
    } else if (url.includes("/api/organizations")) {
      req.respond({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ success: true, data: mockOrganizations, errors: [] }),
      });
    } else if (url.includes("/api/dashboards/dash-sre-01") || url.includes("/api/dashboards/dash-1")) {
      req.respond({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ success: true, data: mockDashboardDetail, errors: [] }),
      });
    } else if (url.includes("/api/dashboards/templates")) {
      req.respond({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          success: true,
          data: [
            { id: "tmpl-1", name: "Production SRE Core Observability", description: "Golden Signals" },
            { id: "tmpl-2", name: "API Gateway & Microservices", description: "Edge latency" },
          ],
          errors: [],
        }),
      });
    } else if (url.includes("/api/dashboards")) {
      req.respond({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ success: true, data: mockDashboards, errors: [] }),
      });
    } else if (url.includes("/api/apm/topology")) {
      req.respond({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ success: true, data: mockApmTopology, errors: [] }),
      });
    } else if (url.includes("/api/apm/endpoints")) {
      req.respond({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ success: true, data: mockApmEndpoints, errors: [] }),
      });
    } else if (url.includes("/api/apm/external-dependencies")) {
      req.respond({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ success: true, data: mockApmExternal, errors: [] }),
      });
    } else if (url.includes("/api/incidents/inc-payment-01/intelligence/postmortem")) {
      req.respond({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ success: true, data: { postmortemMarkdown: mockIncidentIntelligence.postmortemMarkdown }, errors: [] }),
      });
    } else if (url.includes("/api/incidents/inc-payment-01/intelligence") || url.includes("/intelligence")) {
      req.respond({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ success: true, data: mockIncidentIntelligence, errors: [] }),
      });
    } else if (url.includes("/api/incidents/inc-payment-01")) {
      req.respond({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ success: true, data: mockIncidentDetail, errors: [] }),
      });
    } else if (url.includes("/api/incidents")) {
      req.respond({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          success: true,
          data: { items: mockIncidents, totalCount: 1, page: 1, pageSize: 20, totalPages: 1 },
          errors: [],
        }),
      });
    } else if (url.includes("/api/status-pages")) {
      req.respond({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ success: true, data: [mockPublicStatus], errors: [] }),
      });
    } else if (url.includes("/api/status/acme-production") || url.includes("/api/status/")) {
      req.respond({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ success: true, data: mockPublicStatus, errors: [] }),
      });
    } else if (url.includes("/api/applications")) {
      req.respond({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          success: true,
          data: [
            { id: "app-01", name: "acme-ecommerce", displayName: "Acme E-Commerce Core", isActive: true },
          ],
          errors: [],
        }),
      });
    } else {
      req.continue();
    }
  });

  try {
    // Inject auth token and active org into localStorage
    await page.goto(`${BASE_URL}/login`, { waitUntil: "networkidle0" });
    await page.evaluate(() => {
      localStorage.setItem("sentinelops_token", "authentic-qa-token");
      localStorage.setItem("sentinelops_active_org_id", "org-acme-01");
    });

    // 1. Checkpoint 8.1 & 8.6: Organizations & Quotas Management
    console.log("\n[1/7] Capturing Checkpoint 8.1/8.6: Organizations & SaaS Quotas...");
    await page.goto(`${BASE_URL}/organizations`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1000));
    // Click on Resource Usage tab to display SaaS quotas
    const usageTabBtn = await page.$('button::-p-text(Resource Usage)');
    if (usageTabBtn) {
      await usageTabBtn.click();
      await new Promise((r) => setTimeout(r, 800));
    }
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "phase8-organizations-quotas.png") });
    console.log("Saved: docs/screenshots/phase8-organizations-quotas.png");

    // 2. Checkpoint 8.2: Custom Observability Dashboard Builder
    console.log("\n[2/7] Capturing Checkpoint 8.2: Custom Observability Dashboards...");
    await page.goto(`${BASE_URL}/dashboards`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1200));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "phase8-custom-dashboards.png") });
    console.log("Saved: docs/screenshots/phase8-custom-dashboards.png");

    // 3. Checkpoint 8.3: Advanced APM & Service Dependency Topology Map
    console.log("\n[3/7] Capturing Checkpoint 8.3: Advanced APM & Dependency Topology...");
    await page.goto(`${BASE_URL}/apm`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1200));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "phase8-apm-service-map.png") });
    console.log("Saved: docs/screenshots/phase8-apm-service-map.png");

    // 4. Checkpoint 8.4: AI Incident Intelligence & Diagnostic Assistant
    console.log("\n[4/7] Capturing Checkpoint 8.4: AI Incident Intelligence Assistant...");
    await page.goto(`${BASE_URL}/incidents/inc-payment-01`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1500));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "phase8-incident-intelligence.png") });
    console.log("Saved: docs/screenshots/phase8-incident-intelligence.png");

    // 5. Checkpoint 8.5: Public Status Page
    console.log("\n[5/7] Capturing Checkpoint 8.5: Public Status Page...");
    await page.goto(`${BASE_URL}/status/acme-production`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1200));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "phase8-public-status-page.png") });
    console.log("Saved: docs/screenshots/phase8-public-status-page.png");

    // 6. Responsive Tablet Viewport (1024x768): Custom Dashboards
    console.log("\n[6/7] Capturing Tablet Viewport (1024x768): Custom Dashboards...");
    await page.setViewport({ width: 1024, height: 768 });
    await page.goto(`${BASE_URL}/dashboards`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1000));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "phase8-responsive-dashboards-tablet.png") });
    console.log("Saved: docs/screenshots/phase8-responsive-dashboards-tablet.png");

    // 7. Responsive Mobile Viewport (390x844): APM Service Map
    console.log("\n[7/7] Capturing Mobile Viewport (390x844): APM Service Map...");
    await page.setViewport({ width: 390, height: 844 });
    await page.goto(`${BASE_URL}/apm`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1000));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "phase8-responsive-apm-mobile.png") });
    console.log("Saved: docs/screenshots/phase8-responsive-apm-mobile.png");

    console.log("\n=================================================================");
    console.log("   ALL 7 PHASE 8 VISUAL SCREENSHOTS SUCCESSFULLY CAPTURED!");
    console.log("=================================================================");
  } finally {
    await browser.close();
  }
}

runVisualQA().catch((err) => {
  console.error("Phase 8 Visual QA failed:", err);
  process.exit(1);
});
