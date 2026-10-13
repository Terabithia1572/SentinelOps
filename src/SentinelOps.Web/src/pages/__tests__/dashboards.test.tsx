import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "@/context/auth-context";
import { TenantProvider } from "@/context/tenant-context";
import { DashboardsPage } from "@/pages/dashboards";

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
    getDashboards: vi.fn().mockResolvedValue([
      {
        id: "dash-1",
        organizationId: "org-1",
        title: "Platform Overview",
        description: "Enterprise observability dashboard",
        isDefault: true,
        isShared: true,
        createdByUserName: "admin",
        refreshIntervalSeconds: 30,
        defaultTimeRange: "1h",
        tags: ["core", "infra"],
        widgetCount: 3,
        createdAtUtc: "2026-10-12T00:00:00Z"
      }
    ]),
    getDashboardById: vi.fn().mockResolvedValue({
      id: "dash-1",
      organizationId: "org-1",
      title: "Platform Overview",
      description: "Enterprise observability dashboard",
      isDefault: true,
      isShared: true,
      createdByUserName: "admin",
      refreshIntervalSeconds: 30,
      defaultTimeRange: "1h",
      tags: ["core", "infra"],
      widgetCount: 3,
      createdAtUtc: "2026-10-12T00:00:00Z",
      layoutJson: "[]",
      widgets: [
        {
          id: "w-1",
          type: "timeseries",
          title: "System CPU & Memory Utilization",
          x: 0,
          y: 0,
          w: 6,
          h: 4,
          configJson: "{\"metric\":\"system.cpu.utilization\"}"
        },
        {
          id: "w-2",
          type: "stat",
          title: "System Error Rate",
          x: 6,
          y: 0,
          w: 3,
          h: 2,
          configJson: "{}"
        },
        {
          id: "w-3",
          type: "incidents",
          title: "Active Infra Incidents",
          x: 9,
          y: 0,
          w: 3,
          h: 2,
          configJson: "{}"
        }
      ]
    }),
    createDashboard: vi.fn(),
    updateDashboard: vi.fn(),
    deleteDashboard: vi.fn(),
    duplicateDashboard: vi.fn(),
    importDashboard: vi.fn(),
    instantiateDashboardTemplate: vi.fn(),
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

describe("DashboardsPage", () => {
  beforeEach(() => {
    localStorage.setItem("sentinelops_token", "mock-token");
  });

  it("renders page header, controls, and widgets", async () => {
    renderWithProviders(<DashboardsPage />);

    expect(await screen.findByRole("heading", { name: /Custom Observability Dashboards/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Templates/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /New/i })).toBeInTheDocument();

    // Verify widgets rendered
    expect(await screen.findByText("System CPU & Memory Utilization")).toBeInTheDocument();
    expect(screen.getByText("System Error Rate")).toBeInTheDocument();
    expect(screen.getByText("Active Infra Incidents")).toBeInTheDocument();
  });

  it("opens template gallery modal when Templates button clicked", async () => {
    renderWithProviders(<DashboardsPage />);

    const templatesBtn = await screen.findByRole("button", { name: /Templates/i });
    fireEvent.click(templatesBtn);

    expect(await screen.findByText("Pre-Built Observability Templates")).toBeInTheDocument();
    expect(screen.getByText("Kubernetes & Infrastructure")).toBeInTheDocument();
    expect(screen.getByText("API Gateway & Microservices APM")).toBeInTheDocument();
    expect(screen.getByText("Executive SLO & Incident Summary")).toBeInTheDocument();
    expect(screen.getByText("Database & Redis Cache Health")).toBeInTheDocument();
  });
});
