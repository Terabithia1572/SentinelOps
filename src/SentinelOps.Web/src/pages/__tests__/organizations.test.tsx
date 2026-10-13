import React from "react";
import { render, screen } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "@/context/auth-context";
import { TenantProvider } from "@/context/tenant-context";
import { OrganizationsPage } from "@/pages/organizations";

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
        createdAtUtc: "2026-10-12T00:00:00Z"
      }
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
          joinedAtUtc: "2026-10-12T00:00:00Z"
        }
      ],
      pendingInvitations: [],
      settings: {
        allowMemberInvites: true,
        requireTwoFactor: false,
        defaultRetentionDays: 30
      },
      usage: {
        logsIngestedMonthToDate: 12000,
        tracesIngestedMonthToDate: 4500,
        metricsIngestedMonthToDate: 89000,
        apiRequestsMonthToDate: 23000,
        estimatedStorageBytes: 104857600,
        lastResetAtUtc: "2026-10-01T00:00:00Z"
      }
    }),
    createOrganization: vi.fn(),
    inviteOrganizationMember: vi.fn(),
    revokeOrganizationInvitation: vi.fn(),
    removeOrganizationMember: vi.fn(),
    changeOrganizationMemberRole: vi.fn(),
    updateOrganizationSettings: vi.fn(),
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

describe("OrganizationsPage", () => {
  beforeEach(() => {
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
});
