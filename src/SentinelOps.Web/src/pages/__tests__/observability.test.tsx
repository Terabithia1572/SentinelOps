import React from "react";
import { render, screen } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "@/context/auth-context";
import { LogsPage } from "@/pages/logs";
import { TracesPage } from "@/pages/traces";
import { MetricsPage } from "@/pages/metrics";
import { ServiceHealthPage } from "@/pages/health";
import { ExceptionsPage } from "@/pages/exceptions";

vi.mock("@/lib/api-client", () => {
  return {
    default: {
      get: vi.fn((url: string) => {
        if (url === "/applications") {
          return Promise.resolve({
            data: {
              success: true,
              data: [
                { id: "app-1", name: "payments-api", displayName: "Payments Service", isActive: true },
              ],
            },
          });
        }
        if (url === "/logs/metadata") {
          return Promise.resolve({
            data: {
              success: true,
              data: {
                services: ["payments-svc", "auth-svc"],
                environments: ["Production", "Staging"],
              },
            },
          });
        }
        if (url === "/logs") {
          return Promise.resolve({
            data: {
              success: true,
              data: {
                items: [
                  {
                    id: "log-1",
                    applicationId: "app-1",
                    serviceName: "payments-svc",
                    environmentName: "Production",
                    severity: "Error",
                    message: "Database transaction deadlocked during payment checkout",
                    timestamp: "2026-10-13T12:00:00Z",
                    traceId: "trace-abc-123",
                  },
                ],
                totalCount: 1,
                page: 1,
                pageSize: 25,
                totalPages: 1,
              },
            },
          });
        }
        if (url === "/traces") {
          return Promise.resolve({
            data: {
              success: true,
              data: {
                items: [
                  {
                    traceId: "trace-abc-123",
                    applicationId: "app-1",
                    serviceName: "payments-svc",
                    environmentName: "Production",
                    rootSpanName: "POST /api/v1/charge",
                    durationMs: 420.5,
                    spanCount: 3,
                    hasErrors: true,
                    startedAtUtc: "2026-10-13T12:00:00Z",
                  },
                ],
                totalCount: 1,
                page: 1,
                pageSize: 20,
                totalPages: 1,
              },
            },
          });
        }
        if (url === "/metrics/timeseries") {
          return Promise.resolve({
            data: {
              success: true,
              data: {
                points: [
                  {
                    timestamp: "2026-10-13T12:00:00Z",
                    requestsPerSecond: 12.5,
                    errorRatePercent: 2.5,
                    avgDurationMs: 85.0,
                    p50DurationMs: 65.0,
                    p95DurationMs: 210.0,
                    p99DurationMs: 340.0,
                    totalRequests: 750,
                    errorRequests: 18,
                  },
                ],
                avgLatencyMs: 85.0,
                p95LatencyMs: 210.0,
                overallErrorRate: 2.5,
                totalThroughputRps: 12.5,
                totalRequests: 750,
              },
            },
          });
        }
        if (url === "/services/health") {
          return Promise.resolve({
            data: {
              success: true,
              data: [
                {
                  applicationId: "app-1",
                  applicationName: "Payments Service",
                  serviceName: "payments-svc",
                  environmentName: "Production",
                  status: "Healthy",
                  totalRequests: 750,
                  totalErrors: 5,
                  errorRatePercent: 0.67,
                  avgDurationMs: 45.0,
                  p95DurationMs: 120.0,
                  lastSeenAtUtc: "2026-10-13T12:00:00Z",
                },
              ],
            },
          });
        }
        if (url === "/exceptions") {
          return Promise.resolve({
            data: {
              success: true,
              data: [
                {
                  fingerprint: "a1b2c3d4e5f67890",
                  exceptionType: "SqlException",
                  message: "Timeout expired while attempting to execute command",
                  serviceName: "payments-svc",
                  environmentName: "Production",
                  count: 42,
                  firstSeenAtUtc: "2026-10-13T10:00:00Z",
                  lastSeenAtUtc: "2026-10-13T12:00:00Z",
                  sampleTraceId: "trace-abc-123",
                  sampleLogId: "log-1",
                },
              ],
            },
          });
        }
        return Promise.resolve({ data: { success: true, data: null } });
      }),
      interceptors: {
        request: { use: vi.fn() },
        response: { use: vi.fn() },
      },
    },
  };
});

function renderWithProviders(ui: React.ReactElement) {
  return render(
    <BrowserRouter>
      <AuthProvider>{ui}</AuthProvider>
    </BrowserRouter>
  );
}

describe("Observability Explorer Pages", () => {
  it("renders LogsPage with filters and stream", async () => {
    renderWithProviders(<LogsPage />);
    expect(await screen.findByRole("heading", { name: "Log Explorer" })).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/Search messages, templates/i)).toBeInTheDocument();
    expect(await screen.findByText(/Database transaction deadlocked/i)).toBeInTheDocument();
    expect(screen.getAllByText("payments-svc").length).toBeGreaterThan(0);
  });

  it("renders TracesPage with trace list and duration pill", async () => {
    renderWithProviders(<TracesPage />);
    expect(await screen.findByRole("heading", { name: "Distributed Traces" })).toBeInTheDocument();
    expect(await screen.findByText("POST /api/v1/charge")).toBeInTheDocument();
    expect(screen.getByText("420.5 ms")).toBeInTheDocument();
    expect(screen.getByText("3 spans")).toBeInTheDocument();
  });

  it("renders MetricsPage with aggregate metrics", async () => {
    renderWithProviders(<MetricsPage />);
    expect(await screen.findByRole("heading", { name: "Metrics Explorer" })).toBeInTheDocument();
    expect(await screen.findByText("750")).toBeInTheDocument();
    expect(screen.getByText("12.50")).toBeInTheDocument();
    expect(screen.getByText("2.50%")).toBeInTheDocument();
  });

  it("renders ServiceHealthPage with health matrix cards", async () => {
    renderWithProviders(<ServiceHealthPage />);
    expect(await screen.findByRole("heading", { name: "Service Health Monitoring" })).toBeInTheDocument();
    expect(await screen.findByText("Payments Service • Production")).toBeInTheDocument();
    expect(screen.getAllByText("Healthy").length).toBeGreaterThanOrEqual(1);
  });

  it("renders ExceptionsPage with fingerprinted exception groups", async () => {
    renderWithProviders(<ExceptionsPage />);
    expect(await screen.findByRole("heading", { name: "Exceptions Explorer" })).toBeInTheDocument();
    expect(await screen.findByText("SqlException")).toBeInTheDocument();
    expect(screen.getByText(/Timeout expired while attempting to execute command/i)).toBeInTheDocument();
    expect(screen.getByText("42")).toBeInTheDocument();
    expect(screen.getByText("#a1b2c3d4e5f67890")).toBeInTheDocument();
  });
});
