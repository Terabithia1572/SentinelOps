import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "@/context/auth-context";
import { TenantProvider } from "@/context/tenant-context";
import { BillingPage } from "@/pages/billing";

const { mockSubscription, mockPlans, mockUsage, mockInvoices, mockMembers } = vi.hoisted(() => {
  const mockSubscription = {
    id: "sub-123",
    organizationId: "org-123",
    planId: "plan-pro",
    planName: "Pro",
    status: "Active",
    billingInterval: "Monthly",
    currentPeriodStartUtc: "2026-10-01T00:00:00Z",
    currentPeriodEndUtc: "2026-11-01T00:00:00Z",
    cancelAtPeriodEnd: false,
    concurrencyToken: "token-abc"
  };

  const mockPlans = [
    {
      id: "plan-starter",
      name: "Starter",
      description: "For small teams and testing",
      monthlyPrice: 0,
      annualPrice: 0,
      currency: "USD",
      maxApplications: 3,
      maxMembers: 5,
      maxCustomDashboards: 3,
      maxMonthlyLogs: 100000,
      maxMonthlyTraces: 50000,
      maxMonthlyAiTokens: 50000,
      dataRetentionDays: 7,
      allowCustomReports: false,
      allowPrioritySupport: false
    },
    {
      id: "plan-pro",
      name: "Pro",
      description: "For production engineering workloads",
      monthlyPrice: 49,
      annualPrice: 490,
      currency: "USD",
      maxApplications: 20,
      maxMembers: 25,
      maxCustomDashboards: 15,
      maxMonthlyLogs: 1000000,
      maxMonthlyTraces: 500000,
      maxMonthlyAiTokens: 500000,
      dataRetentionDays: 30,
      allowCustomReports: true,
      allowPrioritySupport: false
    },
    {
      id: "plan-enterprise",
      name: "Enterprise",
      description: "For mission-critical operations",
      monthlyPrice: 249,
      annualPrice: 2490,
      currency: "USD",
      maxApplications: 100,
      maxMembers: 100,
      maxCustomDashboards: 50,
      maxMonthlyLogs: 10000000,
      maxMonthlyTraces: 5000000,
      maxMonthlyAiTokens: 2000000,
      dataRetentionDays: 90,
      allowCustomReports: true,
      allowPrioritySupport: true
    }
  ];

  const mockUsage = {
    organizationId: "org-123",
    planTier: "Pro",
    currentPeriodStartUtc: "2026-10-01T00:00:00Z",
    currentPeriodEndUtc: "2026-11-01T00:00:00Z",
    applicationsUsed: 5,
    applicationsQuota: 20,
    applicationsPercentUsed: 25,
    membersUsed: 8,
    membersQuota: 25,
    membersPercentUsed: 32,
    dashboardsUsed: 4,
    dashboardsQuota: 15,
    dashboardsPercentUsed: 26.6,
    logsUsed: 850000,
    logsQuota: 1000000,
    logsPercentUsed: 85,
    isLogsWarning: true,
    isLogsExceeded: false,
    tracesUsed: 550000,
    tracesQuota: 500000,
    tracesPercentUsed: 110,
    isTracesWarning: false,
    isTracesExceeded: true,
    aiTokensUsed: 15000,
    aiTokensQuota: 500000,
    aiTokensPercentUsed: 3,
    isAiTokensWarning: false,
    isAiTokensExceeded: false,
    estimatedStorageBytes: 1048576,
    hasAnyWarning: true,
    hasAnyExceeded: true
  };

  const mockInvoices = [
    {
      id: "inv-001",
      organizationId: "org-123",
      invoiceNumber: "INV-202610-00001",
      status: "Paid",
      subtotal: 49.0,
      taxAmount: 9.8,
      taxRatePercent: 20.0,
      total: 58.8,
      amountPaid: 58.8,
      amountDue: 0.0,
      currency: "USD",
      periodStartUtc: "2026-10-01T00:00:00Z",
      periodEndUtc: "2026-11-01T00:00:00Z",
      dueDateUtc: "2026-10-15T00:00:00Z",
      paidAtUtc: "2026-10-01T00:01:00Z",
      createdAtUtc: "2026-10-01T00:00:00Z",
      lineItems: [
        {
          id: "line-1",
          description: "SentinelOps Pro Plan (Monthly)",
          quantity: 1,
          unitPrice: 49.0,
          amount: 49.0
        }
      ]
    }
  ];

  const mockMembers = [
    {
      id: "mem-1",
      organizationId: "org-123",
      userId: "user-owner-1",
      userName: "alice@test.com",
      email: "alice@test.com",
      fullName: "Alice Owner",
      role: "Owner",
      joinedAtUtc: "2026-10-01T00:00:00Z"
    },
    {
      id: "mem-2",
      organizationId: "org-123",
      userId: "user-admin-2",
      userName: "bob@test.com",
      email: "bob@test.com",
      fullName: "Bob Admin",
      role: "Admin",
      joinedAtUtc: "2026-10-05T00:00:00Z"
    }
  ];

  return { mockSubscription, mockPlans, mockUsage, mockInvoices, mockMembers };
});

vi.mock("@/lib/api-client", () => {
  return {
    getSubscription: vi.fn().mockImplementation(() => Promise.resolve(mockSubscription)),
    getPlans: vi.fn().mockImplementation(() => Promise.resolve(mockPlans)),
    getInvoices: vi.fn().mockImplementation(() => Promise.resolve(mockInvoices)),
    getInvoiceById: vi.fn().mockImplementation(() => Promise.resolve(mockInvoices[0])),
    getTenantUsage: vi.fn().mockImplementation(() => Promise.resolve(mockUsage)),
    getEntitlements: vi.fn().mockResolvedValue(null),
    getOrganizationMembers: vi.fn().mockImplementation(() => Promise.resolve(mockMembers)),
    changePlan: vi.fn().mockResolvedValue(mockSubscription),
    cancelSubscription: vi.fn().mockResolvedValue(mockSubscription),
    reactivateSubscription: vi.fn().mockResolvedValue(mockSubscription),
    createCheckoutSession: vi.fn().mockResolvedValue({ sessionId: "cs_test_123", checkoutUrl: "https://sandbox.checkout", provider: "Sandbox" }),
    createCustomerPortalSession: vi.fn().mockResolvedValue({ portalUrl: "https://sandbox.portal", provider: "Sandbox" }),
    transferOwnership: vi.fn().mockResolvedValue(true),
    exportTenantData: vi.fn().mockResolvedValue({ organizationId: "org-123", name: "Test Org" }),
    deleteOrganizationAccount: vi.fn().mockResolvedValue(true),
    getUserOrganizations: vi.fn().mockResolvedValue([
      { id: "org-123", name: "Acme Corp", slug: "acme-corp", userRole: "Owner", isActive: true }
    ]),
    default: {
      get: vi.fn().mockImplementation((url: string) => {
        if (url === "/auth/me") {
          return Promise.resolve({
            data: {
              success: true,
              data: {
                id: "user-owner-1",
                userName: "alice@test.com",
                email: "alice@test.com",
                fullName: "Alice Owner",
                roles: ["Admin", "Operator"]
              }
            }
          });
        }
        return Promise.resolve({ data: { success: true, data: [] } });
      }),
      post: vi.fn().mockResolvedValue({ data: { success: true, data: {} } }),
      interceptors: {
        request: { use: vi.fn() },
        response: { use: vi.fn() },
      },
    },
  };
});

describe("BillingPage SaaS Management", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.setItem("sentinelops_token", "mock-valid-token");
    localStorage.setItem("sentinelops_active_org_id", "org-123");
  });

  const renderComponent = () =>
    render(
      <AuthProvider>
        <TenantProvider>
          <BrowserRouter>
            <BillingPage />
          </BrowserRouter>
        </TenantProvider>
      </AuthProvider>
    );

  it("renders deterministic sandbox alert banner", async () => {
    renderComponent();

    expect(await screen.findByText(/Deterministic SaaS Sandbox Environment/i)).toBeInTheDocument();
  });

  it("renders subscription overview and current plan badge", async () => {
    renderComponent();

    expect(await screen.findByText(/Subscription & SaaS Management/i)).toBeInTheDocument();
    expect(screen.getByText("Subscription Overview")).toBeInTheDocument();
  });

  it("renders canonical plans and allows switching tabs", async () => {
    renderComponent();

    await waitFor(() => expect(screen.getByText("Acme Corp")).toBeInTheDocument());
    const plansTab = await screen.findByRole("button", { name: /Plans & Pricing/i });
    fireEvent.click(plansTab);

    expect(await screen.findByText("Starter")).toBeInTheDocument();
    expect(screen.getByText("Enterprise")).toBeInTheDocument();
    expect(screen.getByText("$249")).toBeInTheDocument();
  });

  it("renders usage and quota bars with warning and exceeded badges", async () => {
    renderComponent();

    await waitFor(() => expect(screen.getByText("Acme Corp")).toBeInTheDocument());
    await waitFor(() => expect(screen.getByText(/Invoices & History \(1\)/i)).toBeInTheDocument());

    const usageTab = await screen.findByRole("button", { name: /Usage & Quotas/i });
    fireEvent.click(usageTab);

    expect(await screen.findByText(/Tenant Capability Matrix & Limits/i)).toBeInTheDocument();
    expect(await screen.findByText(/80% Quota Warning/i)).toBeInTheDocument();
    expect(await screen.findByText(/100% Exceeded/i)).toBeInTheDocument();
  });

  it("renders itemized invoice history and allows opening details modal", async () => {
    renderComponent();

    await waitFor(() => expect(screen.getByText("Acme Corp")).toBeInTheDocument());
    await waitFor(() => expect(screen.getByText(/Invoices & History \(1\)/i)).toBeInTheDocument());

    const invoicesTab = await screen.findByRole("button", { name: /Invoices & History/i });
    fireEvent.click(invoicesTab);

    expect(await screen.findByText("INV-202610-00001")).toBeInTheDocument();
    expect(screen.getByText("$58.80 USD")).toBeInTheDocument();

    const detailsBtn = screen.getByRole("button", { name: /Details/i });
    fireEvent.click(detailsBtn);

    expect(await screen.findByText(/Itemized tax-compliant invoice breakdown/i)).toBeInTheDocument();
    expect(screen.getByText("Subtotal:")).toBeInTheDocument();
    expect(screen.getByText("Tax (20%):")).toBeInTheDocument();
  });

  it("renders governance tab and validates soft account deletion phrase", async () => {
    renderComponent();

    await waitFor(() => expect(screen.getByText("Acme Corp")).toBeInTheDocument());

    const govTab = await screen.findByRole("button", { name: /Governance & Security/i });
    fireEvent.click(govTab);

    expect(await screen.findByText(/Ownership Transfer/i)).toBeInTheDocument();
    expect(screen.getByText(/Tenant Data Portability & GDPR Export/i)).toBeInTheDocument();
    expect(screen.getByText(/Danger Zone: Account Deletion/i)).toBeInTheDocument();

    const deleteBtn = screen.getByRole("button", { name: /Delete Organization Account/i });
    fireEvent.click(deleteBtn);

    expect(await screen.findByText(/Soft-Delete Organization Account/i)).toBeInTheDocument();
    const confirmDeleteBtn = screen.getByRole("button", { name: /Permanently Delete/i });
    expect(confirmDeleteBtn).toBeDisabled();
  });
});
