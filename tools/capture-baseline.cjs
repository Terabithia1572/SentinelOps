const path = require('path');
const { chromium } = require(path.resolve(__dirname, '../src/SentinelOps.Web/node_modules/@playwright/test'));
const fs = require('fs');

const outputDir = path.resolve(__dirname, '../docs/phase18/screenshots/baseline');
if (!fs.existsSync(outputDir)) {
  fs.mkdirSync(outputDir, { recursive: true });
}

const mockSummary = {
  success: true,
  data: {
    totalApplications: 12,
    totalServices: 34,
    activeAlerts: 3,
    healthyServices: 31,
    errorRate: 0.42,
    avgResponseTimeMs: 46.8,
    totalLogsToday: 1842930,
    totalTracesToday: 642810
  }
};

const mockUser = {
  success: true,
  data: {
    id: "usr_admin_01",
    userName: "admin",
    email: "admin@sentinelops.local",
    fullName: "Site Reliability Director",
    roles: ["Admin", "Operator"],
    isActive: true
  }
};

const mockOrgs = {
  success: true,
  data: [
    {
      id: "org_enterprise_01",
      name: "Global FinTech Operations",
      slug: "global-fintech",
      planTier: "Enterprise",
      isActive: true,
      role: "Owner",
      memberCount: 28,
      applicationCount: 12
    }
  ]
};

const mockLogs = {
  success: true,
  data: {
    items: [
      {
        id: "log_01",
        applicationId: "app-checkout",
        serviceName: "payment-gateway",
        environmentName: "production",
        severity: "Error",
        message: "Downstream payment provider gateway timeout after 5000ms: HTTP 504 Gateway Timeout",
        messageTemplate: "Downstream payment provider {Provider} timeout after {TimeoutMs}ms: HTTP {StatusCode}",
        properties: JSON.stringify({ Provider: "StripeGateway", TimeoutMs: 5000, StatusCode: 504, RetryCount: 3 }),
        exceptionDetail: "System.TimeoutException: The operation has timed out.\n   at SentinelOps.Payments.Client.ExecuteAsync(PaymentRequest req)\n   at SentinelOps.Api.Controllers.PaymentController.Process(PaymentDto dto)",
        traceId: "4bf92f3577b34da6a3ce929d0e0e4736",
        spanId: "00f067aa0ba902b7",
        correlationId: "corr-9842-8812",
        timestamp: new Date().toISOString()
      },
      {
        id: "log_02",
        applicationId: "app-checkout",
        serviceName: "order-service",
        environmentName: "production",
        severity: "Critical",
        message: "Database connection pool saturated (100/100 active connections). Connection acquisition queue size: 48",
        messageTemplate: "Database connection pool saturated ({Active}/{Total} active connections). Connection acquisition queue size: {QueueSize}",
        properties: JSON.stringify({ Active: 100, Total: 100, QueueSize: 48 }),
        traceId: "4bf92f3577b34da6a3ce929d0e0e4736",
        spanId: "5fb397be34d23b0f",
        correlationId: "corr-9842-8812",
        timestamp: new Date(Date.now() - 45000).toISOString()
      },
      {
        id: "log_03",
        applicationId: "app-catalog",
        serviceName: "inventory-cache",
        environmentName: "production",
        severity: "Warning",
        message: "Redis cache memory utilization reached 84.5% (1.69 GB / 2.00 GB). Eviction policy volatile-lru active.",
        properties: JSON.stringify({ MemoryPercent: 84.5, UsedBytes: 1814691840, MaxBytes: 2147483648 }),
        timestamp: new Date(Date.now() - 120000).toISOString()
      },
      {
        id: "log_04",
        applicationId: "app-auth",
        serviceName: "identity-api",
        environmentName: "production",
        severity: "Information",
        message: "Issued OAuth2 bearer JWT token for service account payment-worker-sa [audience: SentinelOps, tenant: global-fintech]",
        timestamp: new Date(Date.now() - 180000).toISOString()
      }
    ],
    totalCount: 142050,
    page: 1,
    pageSize: 25,
    totalPages: 5682
  }
};

const mockTraces = {
  success: true,
  data: {
    items: [
      {
        traceId: "4bf92f3577b34da6a3ce929d0e0e4736",
        applicationId: "app-checkout",
        serviceName: "api-gateway",
        environmentName: "production",
        rootSpanName: "POST /v1/checkout/process",
        durationMs: 3842.5,
        spanCount: 18,
        hasErrors: true,
        startedAtUtc: new Date(Date.now() - 300000).toISOString()
      },
      {
        traceId: "7cf81e2466a23cb5b2bd818c0d0d3625",
        applicationId: "app-catalog",
        serviceName: "catalog-service",
        environmentName: "production",
        rootSpanName: "GET /v1/products/sku/89412",
        durationMs: 42.1,
        spanCount: 6,
        hasErrors: false,
        startedAtUtc: new Date(Date.now() - 400000).toISOString()
      }
    ],
    totalCount: 840,
    page: 1,
    pageSize: 20,
    totalPages: 42
  }
};

const mockIncidents = {
  success: true,
  data: {
    items: [
      {
        id: "inc-01",
        incidentNumber: "INC-2026-0842",
        title: "Checkout API Elevated Latency & PostgreSQL Connection Saturation",
        description: "p99 latency spiked to 3.8s in checkout-service with 48 queued DB connection requests",
        severity: "Critical",
        status: "Investigating",
        applicationId: "app-checkout",
        environmentName: "production",
        serviceName: "order-service",
        assignedToUserName: "Sarah Chen (Principal SRE)",
        createdAtUtc: new Date(Date.now() - 1200000).toISOString(),
        timelineEventsCount: 5,
        commentsCount: 3
      },
      {
        id: "inc-02",
        incidentNumber: "INC-2026-0841",
        title: "Redis Cluster Memory Squeeze in Inventory Caching Tier",
        description: "Memory threshold crossed 85% with increasing LRU cache eviction rate",
        severity: "Warning",
        status: "Mitigated",
        applicationId: "app-catalog",
        environmentName: "production",
        serviceName: "inventory-cache",
        assignedToUserName: "Alex Rivera",
        createdAtUtc: new Date(Date.now() - 7200000).toISOString(),
        timelineEventsCount: 8,
        commentsCount: 6
      }
    ],
    totalCount: 14,
    page: 1,
    pageSize: 10,
    totalPages: 2
  }
};

const mockIncidentDetail = {
  success: true,
  data: {
    id: "inc-01",
    incidentNumber: "INC-2026-0842",
    title: "Checkout API Elevated Latency & PostgreSQL Connection Saturation",
    description: "p99 latency spiked to 3.8s in checkout-service with 48 queued DB connection requests. Payment gateway timeouts reported by 14 customers.",
    severity: "Critical",
    status: "Investigating",
    applicationId: "app-checkout",
    environmentName: "production",
    serviceName: "order-service",
    assignedToUserName: "Sarah Chen (Principal SRE)",
    createdAtUtc: new Date(Date.now() - 1200000).toISOString(),
    relatedTraceId: "4bf92f3577b34da6a3ce929d0e0e4736",
    timelineEvents: [
      {
        id: "te-1",
        eventType: "Created",
        timestampUtc: new Date(Date.now() - 1200000).toISOString(),
        actorName: "AlertEvaluator (Automated)",
        title: "Incident declared automatically",
        description: "Triggered by alert rule 'p99-latency-above-2000ms' consecutive failure count 3"
      },
      {
        id: "te-2",
        eventType: "StatusChanged",
        timestampUtc: new Date(Date.now() - 900000).toISOString(),
        actorName: "Sarah Chen",
        title: "Status changed to Investigating",
        description: "Assigned to Sarah Chen. Investigating PostgreSQL active connection pool leak."
      }
    ],
    comments: [
      {
        id: "c-1",
        userName: "Sarah Chen",
        commentText: "Confirmed connection leak in OrderRepository.cs: transaction scope not disposed during timeout. Deploying hotfix PR #412.",
        createdAtUtc: new Date(Date.now() - 600000).toISOString()
      }
    ],
    linkedAlerts: [
      {
        id: "al-1",
        name: "Order Service p99 Latency > 2000ms",
        severity: "Critical",
        status: "Firing",
        condition: "P99LatencyAboveThreshold",
        threshold: 2000
      }
    ]
  }
};

const mockAi = {
  success: true,
  data: {
    tokenQuota: {
      usedTokens: 142800,
      monthlyLimit: 1000000,
      quotaResetDate: "2026-11-01T00:00:00Z"
    },
    anomalies: [
      {
        id: "anom-1",
        metricName: "http_requests_error_rate",
        serviceName: "order-service",
        zScore: 4.82,
        detectedAtUtc: new Date(Date.now() - 900000).toISOString(),
        baselineMean: 0.12,
        observedValue: 3.84,
        confidence: 0.96
      }
    ],
    patterns: [
      {
        patternSignature: "Downstream * timeout after * ms: HTTP *",
        count: 48,
        severity: "Error",
        sampleMessage: "Downstream payment provider gateway timeout after 5000ms: HTTP 504 Gateway Timeout"
      }
    ],
    forecasts: [
      {
        resourceName: "Postgres Connection Pool",
        currentUsage: 98,
        capacity: 100,
        projectedExhaustionHours: 0.4
      }
    ]
  }
};

const mockPlans = {
  success: true,
  data: [
    {
      id: "plan-starter",
      name: "Starter",
      tier: "Starter",
      description: "For small teams and side projects",
      priceMonthly: 49,
      priceAnnual: 490,
      maxApplications: 3,
      maxUsers: 5,
      maxCustomDashboards: 3,
      monthlyLogLimit: 1000000,
      monthlyTraceLimit: 500000,
      monthlyAiTokens: 50000,
      features: ["3 Applications", "5 Team Members", "1M Logs / mo", "Standard Support"]
    },
    {
      id: "plan-pro",
      name: "Pro",
      tier: "Pro",
      description: "For growing engineering organizations",
      priceMonthly: 199,
      priceAnnual: 1990,
      maxApplications: 10,
      maxUsers: 25,
      maxCustomDashboards: 15,
      monthlyLogLimit: 10000000,
      monthlyTraceLimit: 5000000,
      monthlyAiTokens: 500000,
      features: ["10 Applications", "25 Team Members", "10M Logs / mo", "AI Root Cause Analysis", "Priority Support"]
    },
    {
      id: "plan-enterprise",
      name: "Enterprise",
      tier: "Enterprise",
      description: "Mission-critical scale with custom SLA",
      priceMonthly: 499,
      priceAnnual: 4990,
      maxApplications: 50,
      maxUsers: 100,
      maxCustomDashboards: 50,
      monthlyLogLimit: 50000000,
      monthlyTraceLimit: 25000000,
      monthlyAiTokens: 2000000,
      features: ["50 Applications", "100 Team Members", "50M Logs / mo", "Dedicated Slack Channel", "99.99% SLA"]
    }
  ]
};

const mockSubscription = {
  success: true,
  data: {
    id: "sub_ent_01",
    organizationId: "org_enterprise_01",
    planId: "plan-enterprise",
    planName: "Enterprise",
    tier: "Enterprise",
    status: "Active",
    billingCycle: "Monthly",
    currentPeriodStartUtc: "2026-10-01T00:00:00Z",
    currentPeriodEndUtc: "2026-11-01T00:00:00Z",
    cancelAtPeriodEnd: false,
    trialEndUtc: null,
    amount: 499,
    currency: "USD"
  }
};

const mockEntitlements = {
  success: true,
  data: {
    planTier: "Enterprise",
    maxApplications: 50,
    maxUsers: 100,
    maxCustomDashboards: 50,
    monthlyLogLimit: 50000000,
    monthlyTraceLimit: 25000000,
    monthlyAiTokens: 2000000,
    hasAdvancedSecurity: true,
    hasAiRca: true,
    hasCustomRetention: true,
    hasDedicatedSupport: true
  }
};

const mockTenantUsage = {
  success: true,
  data: {
    organizationId: "org_enterprise_01",
    billingPeriod: "2026-10",
    applicationsCount: 12,
    usersCount: 28,
    customDashboardsCount: 8,
    monthlyLogsCount: 1842930,
    monthlyTracesCount: 642810,
    monthlyAiTokensUsed: 142800
  }
};

const mockInvoices = {
  success: true,
  data: {
    items: [
      {
        id: "inv-202610-001",
        invoiceNumber: "INV-202610-00042",
        billingPeriodStartUtc: "2026-10-01T00:00:00Z",
        billingPeriodEndUtc: "2026-11-01T00:00:00Z",
        subtotal: 499.00,
        taxAmount: 0.00,
        totalAmount: 499.00,
        currency: "USD",
        status: "Paid",
        paidAtUtc: "2026-10-01T00:05:00Z",
        invoicePdfUrl: "#",
        lineItems: [
          { description: "Enterprise Observability Plan (Monthly)", quantity: 1, unitPrice: 499.00, amount: 499.00 }
        ]
      }
    ],
    totalCount: 1,
    page: 1,
    pageSize: 10,
    totalPages: 1
  }
};

const mockOrgDetail = {
  success: true,
  data: {
    id: "org_enterprise_01",
    name: "Global FinTech Operations",
    slug: "global-fintech",
    description: "Primary enterprise production observability tenant",
    planTier: "Enterprise",
    isActive: true,
    createdAtUtc: "2026-01-15T10:00:00Z",
    members: [
      { id: "m-1", userId: "usr_admin_01", userName: "admin", email: "admin@sentinelops.local", role: "Owner", joinedAtUtc: "2026-01-15T10:00:00Z" },
      { id: "m-2", userId: "usr_sre_02", userName: "sarah.chen", email: "sarah.chen@sentinelops.local", role: "Admin", joinedAtUtc: "2026-02-01T09:30:00Z" }
    ],
    invitations: [
      { id: "inv-1", email: "devops-lead@sentinelops.local", role: "Admin", invitedByUserName: "admin", createdAtUtc: "2026-10-15T12:00:00Z", expiresAtUtc: "2026-10-22T12:00:00Z", isAccepted: false }
    ]
  }
};

const mockQuota = {
  success: true,
  data: {
    planTier: "Enterprise",
    maxApplications: 50,
    currentApplications: 12,
    maxMembers: 100,
    currentMembers: 28,
    maxAlertRules: 200,
    currentAlertRules: 42,
    monthlyLogQuotaBytes: 107374182400,
    currentMonthLogsBytes: 19842104000
  }
};

async function setupPageRoutes(page) {
  page.on('console', msg => {
    if (msg.type() === 'error') console.log(`[Browser Console Error] ${msg.text()}`);
  });
  page.on('pageerror', err => console.log(`[Browser Unhandled Exception] ${err.message}`));

  await page.route('**/api/auth/me', route => route.fulfill({ json: mockUser }));
  await page.route('**/api/dashboard/summary**', route => route.fulfill({ json: mockSummary }));
  await page.route('**/api/organizations/*/quota', route => route.fulfill({ json: mockQuota }));
  await page.route('**/api/organizations/*', route => route.fulfill({ json: mockOrgDetail }));
  await page.route('**/api/organizations**', route => route.fulfill({ json: mockOrgs }));
  await page.route('**/api/tenants/plans', route => route.fulfill({ json: mockPlans }));
  await page.route('**/api/tenants/*/subscription', route => route.fulfill({ json: mockSubscription }));
  await page.route('**/api/tenants/*/entitlements', route => route.fulfill({ json: mockEntitlements }));
  await page.route('**/api/tenants/*/usage', route => route.fulfill({ json: mockTenantUsage }));
  await page.route('**/api/tenants/*/invoices**', route => route.fulfill({ json: mockInvoices }));
  await page.route('**/api/tenants/*/members**', route => route.fulfill({ json: { success: true, data: mockOrgDetail.data.members } }));
  await page.route('**/api/logs**', route => route.fulfill({ json: mockLogs }));
  await page.route('**/api/traces**', route => route.fulfill({ json: mockTraces }));
  await page.route('**/api/incidents/inc-01**', route => route.fulfill({ json: mockIncidentDetail }));
  await page.route('**/api/incidents**', route => route.fulfill({ json: mockIncidents }));
  await page.route('**/api/ai/**', route => route.fulfill({ json: mockAi }));
  await page.route('**/api/users/**', route => route.fulfill({ json: { success: true, data: { items: [mockUser.data], totalCount: 1, page: 1, pageSize: 20, totalPages: 1 } } }));
  await page.route('**/api/applications/**', route => route.fulfill({ success: true, data: [{ id: "app-checkout", name: "Checkout Suite", displayName: "Checkout Suite", isActive: true }] }));
  await page.route('**/api/metrics/**', route => route.fulfill({ success: true, data: { points: [], avgLatencyMs: 46.8, p95LatencyMs: 120.4, overallErrorRate: 0.42, totalThroughputRps: 240 } }));
  await page.route('**/hubs/**', route => route.abort());
}

async function run() {
  const browser = await chromium.launch({ headless: true });

  const desktopContext = await browser.newContext({
    viewport: { width: 1920, height: 1080 },
    storageState: {
      cookies: [],
      origins: [
        {
          origin: "http://localhost:3000",
          localStorage: [
            { name: "sentinelops_token", value: "mock-valid-jwt-token" },
            { name: "sentinelops_active_org_id", value: "org_enterprise_01" }
          ]
        }
      ]
    }
  });

  const mobileContext = await browser.newContext({
    viewport: { width: 375, height: 812 },
    isMobile: true,
    storageState: {
      cookies: [],
      origins: [
        {
          origin: "http://localhost:3000",
          localStorage: [
            { name: "sentinelops_token", value: "mock-valid-jwt-token" },
            { name: "sentinelops_active_org_id", value: "org_enterprise_01" }
          ]
        }
      ]
    }
  });

  const unauthDesktopContext = await browser.newContext({
    viewport: { width: 1920, height: 1080 }
  });

  const unauthMobileContext = await browser.newContext({
    viewport: { width: 375, height: 812 },
    isMobile: true
  });

  const targets = [
    { url: "/login", name: "desktop-login.png", context: unauthDesktopContext },
    { url: "/overview", name: "desktop-overview.png", context: desktopContext },
    { url: "/logs", name: "desktop-logs.png", context: desktopContext },
    { url: "/traces", name: "desktop-traces.png", context: desktopContext },
    { url: "/metrics", name: "desktop-metrics.png", context: desktopContext },
    { url: "/incidents", name: "desktop-incidents.png", context: desktopContext },
    { url: "/incidents/inc-01", name: "desktop-incident-detail.png", context: desktopContext },
    { url: "/ai-observability", name: "desktop-ai-observability.png", context: desktopContext },
    { url: "/billing", name: "desktop-billing.png", context: desktopContext },
    { url: "/organizations", name: "desktop-organizations.png", context: desktopContext },
    { url: "/overview", name: "mobile-overview.png", context: mobileContext },
    { url: "/incidents", name: "mobile-incidents.png", context: mobileContext },
    { url: "/login", name: "mobile-login.png", context: unauthMobileContext }
  ];

  for (const target of targets) {
    const page = await target.context.newPage();
    await setupPageRoutes(page);
    try {
      console.log(`Navigating to ${target.url} -> ${target.name}`);
      await page.goto(`http://localhost:3000${target.url}`, { waitUntil: 'networkidle', timeout: 15000 });
      await page.waitForTimeout(500); // Allow react animations / state to settle
      const filePath = path.join(outputDir, target.name);
      await page.screenshot({ path: filePath, fullPage: false });
      console.log(`Saved screenshot: ${target.name}`);
    } catch (err) {
      console.error(`Failed ${target.name}: ${err.message}`);
    } finally {
      await page.close();
    }
  }

  await desktopContext.close();
  await mobileContext.close();
  await browser.close();
  console.log("All baseline screenshots captured successfully!");
}

run().catch(err => {
  console.error("Capture script error:", err);
  process.exit(1);
});
