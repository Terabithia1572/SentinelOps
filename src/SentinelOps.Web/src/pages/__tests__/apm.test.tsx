import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "@/context/auth-context";
import { TenantProvider } from "@/context/tenant-context";
import { ApmPage } from "@/pages/apm";

vi.mock("@/lib/api-client", () => {
  return {
    getUserOrganizations: vi.fn().mockResolvedValue([
      {
        id: "org-1",
        name: "Acme Corp",
        slug: "acme-corp",
        description: "Test Org",
        isActive: true,
        userRole: "Owner",
        membersCount: 2,
        applicationsCount: 3,
        createdAtUtc: "2026-10-12T00:00:00Z"
      }
    ]),
    getServiceMap: vi.fn().mockResolvedValue({
      computedAtUtc: "2026-10-12T00:00:00Z",
      timeRange: "1h",
      nodes: [
        {
          id: "api-gateway",
          serviceName: "api-gateway",
          displayName: "API Gateway",
          environmentName: "Production",
          throughput: 450.0,
          errorRate: 0.2,
          avgLatencyMs: 18.4,
          p95LatencyMs: 45.0,
          healthStatus: "Healthy",
          totalCalls: 27000,
          errorCount: 54
        },
        {
          id: "orders-service",
          serviceName: "orders-service",
          displayName: "Orders Service",
          environmentName: "Production",
          throughput: 210.0,
          errorRate: 0.5,
          avgLatencyMs: 34.2,
          p95LatencyMs: 82.0,
          healthStatus: "Healthy",
          totalCalls: 12600,
          errorCount: 63
        }
      ],
      edges: [
        {
          sourceService: "api-gateway",
          targetService: "orders-service",
          callCount: 12600,
          errorCount: 63,
          errorRate: 0.5,
          avgLatencyMs: 34.2,
          p95LatencyMs: 82.0
        }
      ]
    }),
    getServiceDependencies: vi.fn().mockResolvedValue({
      serviceName: "orders-service",
      inbound: [
        {
          sourceService: "api-gateway",
          targetService: "orders-service",
          callCount: 12600,
          errorCount: 63,
          errorRate: 0.5,
          avgLatencyMs: 34.2,
          p95LatencyMs: 82.0
        }
      ],
      outbound: []
    }),
    getEndpoints: vi.fn().mockResolvedValue([
      {
        endpointKey: "POST:/api/orders:orders-service",
        method: "POST",
        route: "/api/orders",
        serviceName: "orders-service",
        environmentName: "Production",
        throughputPerMin: 210.0,
        totalCalls: 12600,
        errorCount: 63,
        errorRate: 0.5,
        avgDurationMs: 45.2,
        p50DurationMs: 32.0,
        p95DurationMs: 95.0,
        p99DurationMs: 240.0
      }
    ]),
    getEndpointBreakdown: vi.fn().mockResolvedValue({
      endpointKey: "POST:/api/orders:orders-service",
      method: "POST",
      route: "/api/orders",
      serviceName: "orders-service",
      throughputPerMin: 210.0,
      avgDurationMs: 45.2,
      p95DurationMs: 95.0,
      errorRate: 0.5,
      latencyBuckets: [
        { rangeLabel: "< 25ms", count: 820 }
      ],
      errorBreakdown: [],
      sampleTraceIds: ["trace-123456"]
    }),
    getExternalDependencies: vi.fn().mockResolvedValue([
      {
        type: "Database",
        targetHostOrSystem: "PostgreSQL",
        operationOrDb: "sentinelops_db",
        callCount: 48200,
        errorCount: 12,
        errorRate: 0.02,
        avgDurationMs: 3.8,
        p95DurationMs: 12.4
      }
    ]),
    default: {
      get: vi.fn().mockResolvedValue({ data: { success: true, data: [] } }),
      post: vi.fn(),
      interceptors: {
        request: { use: vi.fn() },
        response: { use: vi.fn() }
      }
    }
  };
});

function renderWithProviders(ui: React.ReactElement) {
  return render(
    <BrowserRouter>
      <AuthProvider>
        <TenantProvider>{ui}</TenantProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}

describe("ApmPage", () => {
  beforeEach(() => {
    localStorage.setItem("sentinelops_token", "mock-token");
  });

  it("renders page header and tabs", async () => {
    renderWithProviders(<ApmPage />);

    expect(await screen.findByRole("heading", { name: /Application Performance Monitoring/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Service Dependency Map/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Endpoints Explorer/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /External & DB Dependencies/i })).toBeInTheDocument();
  });

  it("displays service nodes and allows switching to Endpoints tab", async () => {
    renderWithProviders(<ApmPage />);

    expect(await screen.findByText("API Gateway")).toBeInTheDocument();
    expect(screen.getByText("Orders Service")).toBeInTheDocument();

    const endpointsTab = screen.getByRole("button", { name: /Endpoints Explorer/i });
    fireEvent.click(endpointsTab);

    expect(await screen.findByPlaceholderText(/Search routes or services/i)).toBeInTheDocument();
    expect(screen.getByText("/api/orders")).toBeInTheDocument();
  });
});
