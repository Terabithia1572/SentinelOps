import React from "react";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "@/context/auth-context";
import { TenantProvider } from "@/context/tenant-context";
import { OrganizationsPage } from "@/pages/organizations";
import type { OrganizationQuotaStatusDto } from "@/lib/api-client";

const mockQuota: OrganizationQuotaStatusDto = {
  organizationId: "org-1",
  organizationName: "Acme Corp",
  planTier: "Pro",
  logsUsed: 12000,
  logsQuota: 1000000,
  logsPercentUsed: 1.2,
  isLogsWarning: false,
  isLogsExceeded: false,
  tracesUsed: 4500,
  tracesQuota: 500000,
  tracesPercentUsed: 0.9,
  isTracesWarning: false,
  isTracesExceeded: false,
  applicationsUsed: 5,
  applicationsQuota: 20,
  applicationsPercentUsed: 25.0,
  dashboardsUsed: 3,
  dashboardsQuota: 15,
  dashboardsPercentUsed: 20.0,
  membersUsed: 1,
  membersQuota: 25,
  membersPercentUsed: 4.0,
  estimatedStorageBytes: 104857600,
  hasAnyQuotaWarning: false,
  hasAnyQuotaExceeded: false,
};

const getOrganizationQuotaMock = vi.fn().mockResolvedValue(mockQuota);
const updateOrganizationQuotaMock = vi.fn().mockResolvedValue(true);

vi.mock("@/lib/api-client", () => {
  return {
    getUserOrganizations: vi.fn().mockResolvedValue([
      {
        id: "org-1",
        name: "Acme Corp",
        slug: "acme-corp",
        description: "Acme observability workspace",
        isActive: true,
        userRole: "Owner",
        membersCount: 3,
        applicationsCount: 5,
        createdAtUtc: "2026-10-12T00:00:00Z",
      },
    ]),
    getOrganizationById: vi.fn().mockResolvedValue({
      id: "org-1",
      name: "Acme Corp",
      slug: "acme-corp",
      description: "Acme observability workspace",
      isActive: true,
      userRole: "Owner",
      createdAtUtc: "2026-10-12T00:00:00Z",
      members: [
        {
          id: "mem-1",
          organizationId: "org-1",
          userId: "user-1",
          userName: "admin",
          email: "admin@sentinelops.local",
          fullName: "System Admin",
          role: "Owner",
          joinedAtUtc: "2026-10-12T00:00:00Z",
        },
      ],
      pendingInvitations: [],
      settings: {
        allowMemberInvites: true,
        requireTwoFactor: false,
        defaultRetentionDays: 30,
        planTier: "Pro",
        maxApplications: 20,
        maxMembers: 25,
        maxCustomDashboards: 15,
        maxMonthlyLogs: 1000000,
        maxMonthlyTraces: 500000,
      },
      usage: {
        logsIngestedMonthToDate: 12000,
        tracesIngestedMonthToDate: 4500,
        metricsIngestedMonthToDate: 89000,
        apiRequestsMonthToDate: 23000,
        estimatedStorageBytes: 104857600,
        lastResetAtUtc: "2026-10-01T00:00:00Z",
      },
    }),
    createOrganization: vi.fn(),
    inviteOrganizationMember: vi.fn(),
    revokeOrganizationInvitation: vi.fn(),
    removeOrganizationMember: vi.fn(),
    changeOrganizationMemberRole: vi.fn(),
    updateOrganizationSettings: vi.fn(),
    getOrganizationQuota: () => getOrganizationQuotaMock(),
    updateOrganizationQuota: (...args: unknown[]) => updateOrganizationQuotaMock(...args),
    default: {
      get: vi.fn().mockResolvedValue({ data: { success: true, data: [] } }),
      post: vi.fn(),
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
      <AuthProvider>
        <TenantProvider>{ui}</TenantProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}

describe("OrganizationsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.setItem("sentinelops_token", "mock-valid-token");
  });

  it("renders organizations management header and action button", async () => {
    renderWithProviders(<OrganizationsPage />);

    expect(await screen.findByRole("heading", { name: /Organizations/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /New Organization/i })).toBeInTheDocument();
  });

  it("loads and displays organization details and tabs", async () => {
    renderWithProviders(<OrganizationsPage />);

    expect(await screen.findByText("Acme Corp")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Members/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Tenant Settings/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Resource Usage/i })).toBeInTheDocument();
  });

  it("displays SaaS quotas and resource usage meters in usage tab", async () => {
    renderWithProviders(<OrganizationsPage />);

    const usageTabBtn = await screen.findByRole("button", { name: /Resource Usage/i });
    fireEvent.click(usageTabBtn);

    expect(await screen.findByText("SaaS Quotas & Resource Usage")).toBeInTheDocument();
    expect(screen.getByText("Pro Plan")).toBeInTheDocument();
    expect(screen.getByText("Monthly Logs Ingestion")).toBeInTheDocument();
    expect(screen.getByText("Monthly Distributed Traces")).toBeInTheDocument();
    expect(screen.getByText("Monitored Applications")).toBeInTheDocument();
    expect(screen.getByText("Custom Dashboards")).toBeInTheDocument();
    expect(screen.getByText("Team Members")).toBeInTheDocument();
    expect(await screen.findByRole("button", { name: /Configure Plan Quotas/i })).toBeInTheDocument();
  });

  it("opens plan quotas configuration modal and submits updates", async () => {
    renderWithProviders(<OrganizationsPage />);

    const usageTabBtn = await screen.findByRole("button", { name: /Resource Usage/i });
    fireEvent.click(usageTabBtn);

    const configBtn = await screen.findByRole("button", { name: /Configure Plan Quotas/i });
    fireEvent.click(configBtn);

    expect(await screen.findByText("Configure Organization Plan Quotas")).toBeInTheDocument();
    const saveBtn = screen.getByRole("button", { name: /Save Quota Configuration/i });
    fireEvent.submit(saveBtn.closest("form")!);

    await waitFor(() => {
      expect(updateOrganizationQuotaMock).toHaveBeenCalledWith(
        "org-1",
        expect.objectContaining({
          planTier: "Pro",
        })
      );
    });
  });
});
