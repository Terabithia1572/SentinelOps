const path = require('path');
const { chromium } = require(path.resolve(__dirname, '../src/SentinelOps.Web/node_modules/@playwright/test'));
const fs = require('fs');

const outputDir = path.resolve(__dirname, '../docs/phase18/screenshots/redesign');
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

const mockOrgDetail = {
  success: true,
  data: {
    id: "org_enterprise_01",
    name: "Global FinTech Operations",
    slug: "global-fintech",
    planTier: "Enterprise",
    isActive: true,
    userRole: "Owner",
    memberCount: 28,
    applicationCount: 12,
    createdAtUtc: "2026-01-15T08:00:00Z",
    members: [
      { id: "mem-01", userId: "usr_admin_01", userName: "admin", email: "admin@sentinelops.local", role: "Owner", joinedAtUtc: "2026-01-15T08:00:00Z" },
      { id: "mem-02", userId: "usr_sre_02", userName: "alex.dev", email: "alex@sentinelops.local", role: "Admin", joinedAtUtc: "2026-02-01T10:00:00Z" },
      { id: "mem-03", userId: "usr_noc_03", userName: "sarah.noc", email: "sarah@sentinelops.local", role: "Operator", joinedAtUtc: "2026-03-12T14:30:00Z" }
    ]
  }
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
        message: "Mutual TLS handshake authenticated service client 'payment-gateway' via cert SHA256:7b91...c4a1",
        timestamp: new Date(Date.now() - 180000).toISOString()
      }
    ],
    totalCount: 48920,
    page: 1,
    pageSize: 25,
    totalPages: 1957
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
        rootSpanName: "POST /v2/checkout/orders/create",
        durationMs: 842.6,
        spanCount: 14,
        hasErrors: true,
        startedAtUtc: new Date(Date.now() - 30000).toISOString()
      },
      {
        traceId: "a1c9284e9f7311bb82a3ce929d0e4412",
        applicationId: "app-checkout",
        serviceName: "payment-gateway",
        environmentName: "production",
        rootSpanName: "POST /v1/payments/stripe/authorize",
        durationMs: 214.2,
        spanCount: 6,
        hasErrors: false,
        startedAtUtc: new Date(Date.now() - 90000).toISOString()
      }
    ],
    totalCount: 12490,
    page: 1,
    pageSize: 20,
    totalPages: 625
  }
};

const mockIncidentDetail = {
  success: true,
  data: {
    id: "inc-01",
    incidentNumber: "INC-2026-0842",
    title: "Payment Authorization Cascading Timeout on Primary Cluster",
    description: "Downstream Stripe gateway timeouts causing connection pool saturation across payment-gateway and order-service nodes.",
    severity: "Critical",
    status: "Investigating",
    serviceName: "payment-gateway",
    environmentName: "production",
    createdAtUtc: new Date(Date.now() - 1800000).toISOString(),
    assignedToUserName: "Site Reliability Director",
    relatedTraceId: "4bf92f3577b34da6a3ce929d0e0e4736",
    timeline: [
      { id: "tl-01", timestampUtc: new Date(Date.now() - 1800000).toISOString(), title: "Threshold Breached", description: "Error rate exceeded 5.0% for 3 consecutive evaluation cycles.", actorName: "Alert Worker", eventType: "AlertFired" },
      { id: "tl-02", timestampUtc: new Date(Date.now() - 1500000).toISOString(), title: "Incident Declared & Paged", description: "P1 Incident declared. Primary SRE on-call paged via PagerDuty.", actorName: "System", eventType: "StatusChange" },
      { id: "tl-03", timestampUtc: new Date(Date.now() - 900000).toISOString(), title: "Investigation Underway", description: "Identified 504 Gateway Timeout responses from external Stripe endpoints.", actorName: "Site Reliability Director", eventType: "Mitigation" }
    ],
    comments: [
      { id: "cm-01", createdAtUtc: new Date(Date.now() - 1200000).toISOString(), authorName: "Site Reliability Director", authorRole: "Admin", text: "Circuit breaker activated on payment-gateway to shedding non-critical transactions." }
    ],
    linkedAlerts: [
      { id: "al-01", name: "High Error Rate Breached (> 5.0%)", conditionType: "ErrorRateAboveThreshold" }
    ]
  }
};

const mockIncidents = {
  success: true,
  data: {
    items: [
      {
        id: "inc-01",
        incidentNumber: "INC-2026-0842",
        title: "Payment Authorization Cascading Timeout on Primary Cluster",
        serviceName: "payment-gateway",
        severity: "Critical",
        status: "Investigating",
        createdAtUtc: new Date(Date.now() - 1800000).toISOString(),
        assignedToUserName: "Site Reliability Director"
      },
      {
        id: "inc-02",
        incidentNumber: "INC-2026-0841",
        title: "Inventory Cache Replica Memory Utilization > 85%",
        serviceName: "inventory-cache",
        severity: "Warning",
        status: "Open",
        createdAtUtc: new Date(Date.now() - 7200000).toISOString(),
        assignedToUserName: "Unassigned"
      }
    ],
    totalCount: 2
  }
};

const mockAlerts = {
  success: true,
  data: {
    items: [
      {
        id: "al-01",
        alertRuleId: "rule-01",
        alertRuleName: "High Error Rate Breached (> 5.0%)",
        severity: "Critical",
        oldStatus: "Normal",
        newStatus: "Firing",
        measuredValue: 12.5,
        threshold: 5.0,
        triggeredAtUtc: new Date(Date.now() - 1800000).toISOString(),
        message: "Observed error rate 12.50% exceeded threshold 5.00%"
      },
      {
        id: "al-02",
        alertRuleId: "rule-02",
        alertRuleName: "API Gateway P99 Latency > 500ms",
        severity: "Warning",
        oldStatus: "Normal",
        newStatus: "Firing",
        measuredValue: 842.0,
        threshold: 500.0,
        triggeredAtUtc: new Date(Date.now() - 3600000).toISOString(),
        message: "P99 latency surged to 842ms across gateway nodes"
      }
    ],
    totalCount: 2,
    page: 1,
    pageSize: 20,
    totalPages: 1
  }
};

const mockAlertRules = {
  success: true,
  data: {
    items: [
      {
        id: "rule-01",
        name: "High Error Rate Breached (> 5.0%)",
        description: "Triggers when 5xx errors exceed 5% within a 5-minute rolling window",
        serviceName: "payment-gateway",
        environmentName: "production",
        conditionType: "ErrorRateAboveThreshold",
        threshold: 5.0,
        severity: "Critical",
        isEnabled: true,
        status: "Firing"
      },
      {
        id: "rule-02",
        name: "API Gateway P99 Latency > 500ms",
        description: "Triggers when p99 response time exceeds 500ms",
        serviceName: "api-gateway",
        environmentName: "production",
        conditionType: "LatencyAboveThreshold",
        threshold: 500.0,
        severity: "Warning",
        isEnabled: true,
        status: "Firing"
      }
    ],
    totalCount: 2,
    page: 1,
    pageSize: 20,
    totalPages: 1
  }
};

const mockPlans = {
  success: true,
  data: [
    { id: "free", name: "Free Tier", priceMonthly: 0, maxApplications: 3, maxServices: 10, dataRetentionDays: 7, maxMonthlyIngestionBytes: 5368709120, hasAdvancedAiAnalytics: false, hasCustomAlertRules: false, hasSloMonitoring: false, hasPrioritySupport: false },
    { id: "pro", name: "Team Pro", priceMonthly: 199, maxApplications: 25, maxServices: 100, dataRetentionDays: 30, maxMonthlyIngestionBytes: 53687091200, hasAdvancedAiAnalytics: true, hasCustomAlertRules: true, hasSloMonitoring: true, hasPrioritySupport: true },
    { id: "enterprise", name: "Enterprise Dedicated", priceMonthly: 899, maxApplications: 999, maxServices: 9999, dataRetentionDays: 90, maxMonthlyIngestionBytes: 536870912000, hasAdvancedAiAnalytics: true, hasCustomAlertRules: true, hasSloMonitoring: true, hasPrioritySupport: true }
  ]
};

const mockSubscription = {
  success: true,
  data: {
    tenantId: "org_enterprise_01",
    planTier: "Enterprise",
    planName: "Enterprise Dedicated",
    billingCycle: "Annual",
    status: "Active",
    currentPeriodEndUtc: "2027-01-15T00:00:00Z",
    autoRenew: true
  }
};

const mockTenantUsage = {
  success: true,
  data: {
    totalIngestedBytes: 142890123000,
    maxIngestedBytes: 536870912000,
    ingestionPercent: 26.6,
    activeServicesCount: 34,
    maxServicesCount: 9999,
    retentionDays: 90
  }
};

const mockApiKeys = {
  success: true,
  data: [
    { id: "key_01", name: "OTel Collector Ingestion Daemon", keyPrefix: "sop_live_9a8f", createdAtUtc: "2026-02-10T12:00:00Z", lastUsedAtUtc: new Date().toISOString(), isActive: true },
    { id: "key_02", name: "Kubernetes Sidecar Exporter", keyPrefix: "sop_live_3c2d", createdAtUtc: "2026-03-01T09:30:00Z", lastUsedAtUtc: new Date(Date.now() - 3600000).toISOString(), isActive: true }
  ]
};

const mockAuditLogs = {
  success: true,
  data: {
    items: [
      { id: "aud_01", actorEmail: "admin@sentinelops.local", action: "AlertRule.Update", resourceType: "AlertRule", resourceId: "rule-01", timestampUtc: new Date(Date.now() - 600000).toISOString(), clientIp: "10.0.4.12", details: "Adjusted threshold from 8.0% to 5.0%" },
      { id: "aud_02", actorEmail: "admin@sentinelops.local", action: "Incident.Acknowledge", resourceType: "Incident", resourceId: "inc-01", timestampUtc: new Date(Date.now() - 1500000).toISOString(), clientIp: "10.0.4.12", details: "Incident assigned to on-call director" }
    ],
    totalCount: 2,
    page: 1,
    pageSize: 20,
    totalPages: 1
  }
};

const mockPublicStatus = {
  success: true,
  data: {
    title: "SentinelOps Global Telemetry Infrastructure",
    overallStatus: "Degraded",
    components: [
      { name: "Ingestion Ingress API", status: "Operational", uptimePercent: 99.99 },
      { name: "Payment Gateway", status: "Degraded", uptimePercent: 98.42 },
      { name: "Redis Stream Workers", status: "Operational", uptimePercent: 100.0 },
      { name: "SignalR Push Service", status: "Operational", uptimePercent: 99.95 }
    ]
  }
};

async function setupPageRoutes(page) {
  await page.route('**/api/auth/me', route => route.fulfill({ json: mockUser }));
  await page.route('**/api/dashboard/summary**', route => route.fulfill({ json: mockSummary }));
  await page.route('**/api/organizations**', route => route.fulfill({ json: mockOrgs }));
  await page.route('**/api/organizations/*', route => route.fulfill({ json: mockOrgDetail }));
  await page.route('**/api/tenants/plans', route => route.fulfill({ json: mockPlans }));
  await page.route('**/api/tenants/*/subscription', route => route.fulfill({ json: mockSubscription }));
  await page.route('**/api/tenants/*/usage', route => route.fulfill({ json: mockTenantUsage }));
  await page.route('**/api/logs**', route => route.fulfill({ json: mockLogs }));
  await page.route('**/api/traces**', route => route.fulfill({ json: mockTraces }));
  await page.route('**/api/incidents/inc-01**', route => route.fulfill({ json: mockIncidentDetail }));
  await page.route('**/api/incidents**', route => route.fulfill({ json: mockIncidents }));
  await page.route('**/api/alerts/summary**', route => route.fulfill({ json: { success: true, data: { totalRules: 8, enabledRules: 7, firingAlerts: 2, criticalAlerts: 1, warningAlerts: 1 } } }));
  await page.route('**/api/alerts**', route => route.fulfill({ json: mockAlerts }));
  await page.route('**/api/alert-rules**', route => route.fulfill({ json: mockAlertRules }));
  await page.route('**/api/api-keys**', route => route.fulfill({ json: mockApiKeys }));
  await page.route('**/api/audit-logs**', route => route.fulfill({ json: mockAuditLogs }));
  await page.route('**/api/status-pages**', route => route.fulfill({ json: mockPublicStatus }));
  await page.route('**/api/metrics/**', route => route.fulfill({ json: { success: true, data: { points: [], avgLatencyMs: 46.8, p95LatencyMs: 120.4, overallErrorRate: 0.42, totalThroughputRps: 240, totalRequests: 750 } } }));
  await page.route('**/api/applications/**', route => route.fulfill({ json: { success: true, data: [{ id: "app-checkout", name: "Checkout Suite", displayName: "Checkout Suite", isActive: true }] } }));
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
    { url: "/login", name: "mobile-login.png", context: unauthMobileContext },
    { url: "/", name: "desktop-overview.png", context: desktopContext },
    { url: "/", name: "mobile-overview.png", context: mobileContext },
    { url: "/logs", name: "desktop-logs.png", context: desktopContext },
    { url: "/logs", name: "mobile-logs.png", context: mobileContext },
    { url: "/traces", name: "desktop-traces.png", context: desktopContext },
    { url: "/metrics", name: "desktop-metrics.png", context: desktopContext },
    { url: "/alerts", name: "desktop-alerts.png", context: desktopContext },
    { url: "/alert-rules", name: "desktop-alert-rules.png", context: desktopContext },
    { url: "/incidents", name: "desktop-incidents.png", context: desktopContext },
    { url: "/incidents", name: "mobile-incidents.png", context: mobileContext },
    { url: "/incidents/inc-01", name: "desktop-incident-detail.png", context: desktopContext },
    { url: "/incidents/inc-01", name: "mobile-incident-detail.png", context: mobileContext },
    { url: "/apm", name: "desktop-apm.png", context: desktopContext },
    { url: "/ai-observability", name: "desktop-ai-observability.png", context: desktopContext },
    { url: "/billing", name: "desktop-billing.png", context: desktopContext },
    { url: "/organizations", name: "desktop-organizations.png", context: desktopContext },
    { url: "/api-keys", name: "desktop-api-keys.png", context: desktopContext },
    { url: "/audit-logs", name: "desktop-audit-logs.png", context: desktopContext },
    { url: "/settings", name: "desktop-settings.png", context: desktopContext },
    { url: "/status-pages", name: "desktop-status-pages.png", context: desktopContext },
    { url: "/status-pages", name: "mobile-status-pages.png", context: mobileContext }
  ];

  for (const target of targets) {
    const page = await target.context.newPage();
    await setupPageRoutes(page);
    try {
      console.log(`Navigating to ${target.url} -> ${target.name}`);
      await page.goto(`http://localhost:3000${target.url}`, { waitUntil: 'networkidle', timeout: 15000 });
      await page.waitForTimeout(600);
      const filePath = path.join(outputDir, target.name);
      await page.screenshot({ path: filePath, fullPage: false });
      console.log(`✓ Saved: ${target.name}`);
    } catch (err) {
      console.error(`✕ Failed ${target.name}: ${err.message}`);
    } finally {
      await page.close();
    }
  }

  await desktopContext.close();
  await mobileContext.close();
  await unauthDesktopContext.close();
  await unauthMobileContext.close();
  await browser.close();
  console.log("All redesign screenshots captured successfully!");
}

run().catch(err => {
  console.error("Capture script error:", err);
  process.exit(1);
});
