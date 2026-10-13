import { render, screen, waitFor } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AuthProvider } from "@/context/auth-context";
import { UsersPage } from "@/pages/users";
import { AuditLogsPage } from "@/pages/audit-logs";
import { ReportsPage } from "@/pages/reports";
import { SettingsPage } from "@/pages/settings";

vi.mock("@/lib/api-client", () => {
  return {
    default: {
      get: vi.fn().mockResolvedValue({ data: { success: true, data: [] } }),
      post: vi.fn().mockResolvedValue({ data: { success: true, data: {} } }),
      put: vi.fn().mockResolvedValue({ data: { success: true, data: {} } }),
      delete: vi.fn().mockResolvedValue({ data: { success: true, data: {} } }),
    },
    getUsers: vi.fn().mockResolvedValue({
      items: [
        {
          id: "u-1",
          userName: "admin",
          email: "admin@sentinelops.local",
          fullName: "System Admin",
          roles: ["Admin"],
          isActive: true,
          isLockedOut: false,
          createdAtUtc: "2026-10-12T10:00:00Z",
        },
        {
          id: "u-2",
          userName: "operator",
          email: "ops@sentinelops.local",
          fullName: "Ops Engineer",
          roles: ["Operator"],
          isActive: true,
          isLockedOut: false,
          createdAtUtc: "2026-10-12T11:00:00Z",
        },
      ],
      totalCount: 2,
      page: 1,
      pageSize: 10,
      totalPages: 1,
    }),
    getUserDetail: vi.fn().mockResolvedValue({
      id: "u-1",
      userName: "admin",
      email: "admin@sentinelops.local",
      fullName: "System Admin",
      roles: ["Admin"],
      isActive: true,
      isLockedOut: false,
      createdAtUtc: "2026-10-12T10:00:00Z",
      applicationMemberships: [],
      recentSecurityActivity: [],
    }),
    updateUserRole: vi.fn().mockResolvedValue(true),
    updateUserStatus: vi.fn().mockResolvedValue(true),
    revokeUserSessions: vi.fn().mockResolvedValue(true),
    adminResetPassword: vi.fn().mockResolvedValue(true),
    getAuditEvents: vi.fn().mockResolvedValue({
      items: [
        {
          id: "ev-1",
          timestampUtc: "2026-10-13T12:00:00Z",
          actorId: "u-1",
          actorName: "admin",
          action: "Auth.LoginSuccess",
          targetEntityType: "User",
          targetEntityId: "u-1",
          outcome: "Success",
          ipAddress: "127.0.0.1",
          details: "Successful login for admin",
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 25,
      totalPages: 1,
    }),
    getAuditSummary: vi.fn().mockResolvedValue({
      totalEvents24h: 42,
      failureCount24h: 1,
      topActions: [{ key: "Auth.LoginSuccess", count: 20 }],
      topActors: [{ key: "admin", count: 25 }],
    }),
    getAuditEventDetail: vi.fn().mockResolvedValue({
      id: "ev-1",
      timestampUtc: "2026-10-13T12:00:00Z",
      actorId: "u-1",
      actorName: "admin",
      action: "Auth.LoginSuccess",
      targetEntityType: "User",
      targetEntityId: "u-1",
      outcome: "Success",
      ipAddress: "127.0.0.1",
      details: "Successful login for admin",
    }),
    getReportTypes: vi.fn().mockResolvedValue([
      { id: "Performance", name: "Service Performance & Latency", description: "Latency and throughput", category: "Telemetry" },
      { id: "ErrorsAndExceptions", name: "Errors & Exceptions Analysis", description: "Top exceptions", category: "Quality" },
    ]),
    previewReport: vi.fn().mockResolvedValue({
      reportType: "Performance",
      title: "Service Performance & Latency Report",
      description: "Throughput and latency",
      startUtc: "2026-10-12T12:00:00Z",
      endUtc: "2026-10-13T12:00:00Z",
      generatedAtUtc: "2026-10-13T12:00:00Z",
      summaryMetrics: { "Total Requests": 1500, "Error Rate": "0.50%" },
      columnHeaders: ["Service Name", "Total Spans", "Avg Latency (ms)"],
      rows: [["payments-api", "1500", "42.50"]],
    }),
    exportReportCsv: vi.fn().mockResolvedValue(new Blob(["mock csv"], { type: "text/csv" })),
    exportReportPrint: vi.fn().mockResolvedValue(new Blob(["mock html"], { type: "text/html" })),
    getSystemHealth: vi.fn().mockResolvedValue({
      status: "Healthy",
      checkedAtUtc: "2026-10-13T12:00:00Z",
      version: "1.0.0-phase6",
      database: { name: "PostgreSQL 18 Database", status: "Healthy", message: "Operational", latencyMs: 2 },
      redis: { name: "Redis Telemetry Stream Buffer", status: "Healthy", message: "Operational", latencyMs: 1 },
      ingestionEngine: { name: "OpenTelemetry Ingestion Engine", status: "Healthy", message: "Operational", latencyMs: 1 },
      alertEvaluator: { name: "Alert Evaluation Engine", status: "Healthy", message: "Operational", latencyMs: 1 },
      notificationDispatcher: { name: "Notification Dispatcher", status: "Healthy", message: "Operational", latencyMs: 1 },
    }),
    getSystemSettings: vi.fn().mockResolvedValue({
      platformName: "SentinelOps Enterprise",
      environmentName: "Production",
      publicRegistrationEnabled: true,
      alertEvaluationIntervalSeconds: 30,
      maxNotificationRetries: 5,
      webhookTimeoutSeconds: 10,
      systemNoticeBanner: "",
      systemNoticeBannerActive: false,
      updatedAtUtc: "2026-10-13T12:00:00Z",
      updatedBy: "admin",
    }),
    updateSystemSettings: vi.fn().mockResolvedValue({}),
    getRetentionPolicy: vi.fn().mockResolvedValue({
      logsRetentionDays: 30,
      tracesRetentionDays: 14,
      metricsRetentionDays: 30,
      alertEvaluationsRetentionDays: 30,
      resolvedIncidentsRetentionDays: 90,
      notificationsRetentionDays: 14,
      auditLogsRetentionDays: 365,
      lastExecutedUtc: "2026-10-13T12:00:00Z",
      lastExecutedBy: "system",
    }),
    updateRetentionPolicy: vi.fn().mockResolvedValue({}),
    previewRetentionCleanup: vi.fn().mockResolvedValue({
      estimatedAtUtc: "2026-10-13T12:00:00Z",
      logs: { tableName: "LogEntries", retentionDays: 30, cutoffUtc: "2026-09-13T12:00:00Z", estimatedCount: 500 },
      traces: { tableName: "TraceRecords", retentionDays: 14, cutoffUtc: "2026-09-29T12:00:00Z", estimatedCount: 200 },
      spans: { tableName: "SpanRecords", retentionDays: 14, cutoffUtc: "2026-09-29T12:00:00Z", estimatedCount: 800 },
      metrics: { tableName: "MetricRecords", retentionDays: 30, cutoffUtc: "2026-09-13T12:00:00Z", estimatedCount: 1500 },
      alertEvaluations: { tableName: "AlertEvaluations", retentionDays: 30, cutoffUtc: "2026-09-13T12:00:00Z", estimatedCount: 100 },
      resolvedIncidents: { tableName: "Incidents", retentionDays: 90, cutoffUtc: "2026-07-15T12:00:00Z", estimatedCount: 5 },
      notifications: { tableName: "NotificationOutboxes", retentionDays: 14, cutoffUtc: "2026-09-29T12:00:00Z", estimatedCount: 20 },
      auditLogs: { tableName: "AuditEvents", retentionDays: 365, cutoffUtc: "2025-10-13T12:00:00Z", estimatedCount: 50 },
      totalEstimatedRecordsToDelete: 3175,
    }),
    executeRetentionCleanup: vi.fn().mockResolvedValue({
      success: true,
      message: "Successfully purged 3,175 expired records.",
      executedAtUtc: "2026-10-13T12:00:00Z",
      deletedCountsByTable: {},
      totalDeletedRecords: 3175,
    }),
  };
});

function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>{ui}</BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}

describe("Phase 6 Enterprise Administration & Security Pages", () => {
  it("renders UsersPage directory with accounts and metrics", async () => {
    renderWithProviders(<UsersPage />);
    expect(screen.getByText("User & Access Management")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByText("Total Directory Users")).toBeInTheDocument();
      expect(screen.getByText(/admin@sentinelops.local/)).toBeInTheDocument();
      expect(screen.getByText(/ops@sentinelops.local/)).toBeInTheDocument();
    });

  });

  it("renders AuditLogsPage with forensic stream and search filters", async () => {
    renderWithProviders(<AuditLogsPage />);
    expect(screen.getByText("Security & Audit Center")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByText("Security Events (24h)")).toBeInTheDocument();
      expect(screen.getByText("Auth.LoginSuccess")).toBeInTheDocument();
    });
  });

  it("renders ReportsPage with report types and live preview", async () => {
    renderWithProviders(<ReportsPage />);
    expect(screen.getByText("Enterprise Reporting & Analytics")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByText("Service Performance & Latency Report")).toBeInTheDocument();
      expect(screen.getByText("Total Requests")).toBeInTheDocument();
      expect(screen.getByText("payments-api")).toBeInTheDocument();
    });
  });

  it("renders SettingsPage with platform health and retention sliders", async () => {
    renderWithProviders(<SettingsPage />);
    expect(screen.getByText("Platform Settings & Administration")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByText("Live Platform Infrastructure Health")).toBeInTheDocument();
      expect(screen.getByText("PostgreSQL 18 Database")).toBeInTheDocument();
      expect(screen.getByText("Telemetry & Data Retention Lifecycles")).toBeInTheDocument();
    });
  });
});
