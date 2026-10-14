import React from "react";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "@/context/auth-context";
import { TenantProvider } from "@/context/tenant-context";
import { OnboardingPage } from "@/pages/onboarding";

const mockStatus = {
  isCompleted: false,
  currentStep: "Organization",
  organizationName: "",
  applicationName: "",
  hasApiKey: false,
  hasReceivedTelemetry: false
};

const mockProvisioned = {
  organizationId: "org-test-123",
  organizationName: "Test Ops Org",
  organizationSlug: "test-ops-org",
  applicationId: "app-test-456",
  applicationName: "payment-api",
  apiKeyId: "key-test-789",
  rawApiKey: "sent_live_abcdef1234567890",
  keyPrefix: "sent_live_abc",
  planTier: "Pro",
  subscriptionStatus: "Trialing",
  trialEndsAtUtc: "2026-10-28T00:00:00Z",
  sdkSnippetCSharp: "// In Program.cs AddOpenTelemetry",
  sdkSnippetNodeJs: "// telemetry.js NodeSDK",
  sdkSnippetPython: "# telemetry.py TracerProvider",
  otelCollectorYaml: "receivers: [otlp]"
};

const mockVerification = {
  applicationId: "app-test-456",
  hasReceivedTelemetry: true,
  logsCount: 15,
  tracesCount: 8,
  metricsCount: 20,
  firstReceivedAtUtc: "2026-10-14T20:00:00Z",
  lastReceivedAtUtc: "2026-10-14T20:05:00Z"
};

vi.mock("@/lib/api-client", () => {
  return {
    getOnboardingStatus: vi.fn().mockImplementation(() => Promise.resolve(mockStatus)),
    provisionOnboardingWorkspace: vi.fn().mockImplementation(() => Promise.resolve(mockProvisioned)),
    verifyFirstTelemetry: vi.fn().mockImplementation(() => Promise.resolve(mockVerification)),
    completeOnboarding: vi.fn().mockResolvedValue(true),
    getUserOrganizations: vi.fn().mockResolvedValue([])
  };
});

describe("OnboardingPage Wizard", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const renderComponent = () =>
    render(
      <AuthProvider>
        <TenantProvider>
          <BrowserRouter>
            <OnboardingPage />
          </BrowserRouter>
        </TenantProvider>
      </AuthProvider>
    );

  it("renders the onboarding wizard header and Step 1 form", async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText("SentinelOps Setup")).toBeInTheDocument();
      expect(screen.getByText(/Name your Organization & Select a Plan/i)).toBeInTheDocument();
      expect(screen.getByPlaceholderText(/Acme Operations/i)).toBeInTheDocument();
      expect(screen.getByText("Starter")).toBeInTheDocument();
      expect(screen.getByText("Professional")).toBeInTheDocument();
      expect(screen.getByText("Enterprise")).toBeInTheDocument();
    });
  });

  it("requires organization name before progressing to step 2", async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText(/Continue to Service Setup/i)).toBeInTheDocument();
    });

    const submitBtn = screen.getByText(/Continue to Service Setup/i);
    fireEvent.click(submitBtn);

    // Form validation keeps it on Step 1
    expect(screen.getByText(/Name your Organization & Select a Plan/i)).toBeInTheDocument();
  });

  it("transitions to step 2, allows entering service details, and provisions workspace", async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByPlaceholderText(/Acme Operations/i)).toBeInTheDocument();
    });

    const orgInput = screen.getByPlaceholderText(/Acme Operations/i);
    fireEvent.change(orgInput, { target: { value: "Test Ops Org" } });

    const nextBtn = screen.getByText(/Continue to Service Setup/i);
    fireEvent.click(nextBtn);

    // Should now be on Step 2
    await waitFor(() => {
      expect(screen.getByText(/Register your First Application \/ Microservice/i)).toBeInTheDocument();
      expect(screen.getByPlaceholderText(/order-service, auth-api/i)).toBeInTheDocument();
    });

    const appInput = screen.getByPlaceholderText(/order-service, auth-api/i);
    fireEvent.change(appInput, { target: { value: "payment-api" } });

    const provisionBtn = screen.getByText(/Provision Workspace & Key/i);
    fireEvent.click(provisionBtn);

    // Should now be on Step 3 (API Key & Snippets)
    await waitFor(() => {
      expect(screen.getByText(/Copy your Telemetry Ingestion Key/i)).toBeInTheDocument();
      expect(screen.getByText("sent_live_abcdef1234567890")).toBeInTheDocument();
      expect(screen.getByText(/Security Warning: Copy-Once Display/i)).toBeInTheDocument();
    });
  });

  it("checks first telemetry and shows successful receipt", async () => {
    renderComponent();

    // Advance to Step 2
    await waitFor(() => {
      expect(screen.getByPlaceholderText(/Acme Operations/i)).toBeInTheDocument();
    });
    fireEvent.change(screen.getByPlaceholderText(/Acme Operations/i), { target: { value: "Test Org" } });
    fireEvent.click(screen.getByText(/Continue to Service Setup/i));

    // Advance to Step 3
    await waitFor(() => {
      expect(screen.getByPlaceholderText(/order-service/i)).toBeInTheDocument();
    });
    fireEvent.change(screen.getByPlaceholderText(/order-service/i), { target: { value: "payment-api" } });
    fireEvent.click(screen.getByText(/Provision Workspace & Key/i));

    // Advance to Step 4
    await waitFor(() => {
      expect(screen.getByText(/Proceed to Telemetry Verification/i)).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText(/Proceed to Telemetry Verification/i));

    // On Step 4
    await waitFor(() => {
      expect(screen.getByText(/Listening for Telemetry from payment-api/i)).toBeInTheDocument();
    });

    const checkBtn = screen.getByText(/Check Telemetry Now/i);
    fireEvent.click(checkBtn);

    await waitFor(() => {
      expect(screen.getByText(/Telemetry Received Successfully!/i)).toBeInTheDocument();
      expect(screen.getByText("Logs: 15")).toBeInTheDocument();
      expect(screen.getByText("Traces: 8")).toBeInTheDocument();
    });
  });
});
