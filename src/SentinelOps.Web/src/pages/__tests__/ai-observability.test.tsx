import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AiObservabilityPage } from "@/pages/ai-observability";
import * as apiClientModule from "@/lib/api-client";

// Mock hooks
vi.mock("@/context/auth-context", () => ({
  useAuth: () => ({
    user: { id: "u1", email: "admin@sentinelops.local", role: "Admin" },
    isOperator: true,
  }),
}));

vi.mock("@/context/tenant-context", () => ({
  useTenant: () => ({
    activeOrg: { id: "org-1", name: "Acme Corp", slug: "acme" },
    organizations: [{ id: "org-1", name: "Acme Corp" }],
    setActiveOrg: vi.fn(),
  }),
}));

describe("AiObservabilityPage", () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });

    const mockAnomalies = [
      {
        id: "anom-1",
        organizationId: "org-1",
        applicationId: "app-1",
        serviceName: "payment-api",
        signalType: "ErrorRate",
        detectionTimestamp: "2026-10-14T12:00:00Z",
        baselineValue: 1.2,
        observedValue: 45.0,
        deviationScore: 5.8,
        confidenceScore: 0.94,
        severity: "Critical",
        detectionMethod: "RobustMedianMAD",
        explanation: "Surge above historical median",
        status: "Active",
      },
    ];

    const mockPatterns = [
      {
        id: "pat-1",
        organizationId: "org-1",
        applicationId: "app-1",
        serviceName: "payment-api",
        patternFingerprint: "sha256-abcdef123456",
        template: "Database timeout connecting to <IP> after <NUM>ms",
        severity: "Error",
        totalOccurrences: 1420,
        firstSeenAtUtc: "2026-10-14T10:00:00Z",
        lastSeenAtUtc: "2026-10-14T12:00:00Z",
        sampleMessage: "Database timeout connecting to 10.0.0.5 after 3000ms",
        sampleTraceId: "tr-987654",
      },
    ];

    const mockForecasts = [
      {
        id: "fc-1",
        organizationId: "org-1",
        applicationId: "app-1",
        serviceName: "cache-tier",
        metricName: "memory_usage_mb",
        currentValue: 750.0,
        predictedValue: 1024.0,
        trendSlope: 4.5,
        forecastHorizonMinutes: 60,
        projectedBreachTimeUtc: "2026-10-14T13:00:00Z",
        confidenceIntervalLower: 980.0,
        confidenceIntervalUpper: 1068.0,
        calculationMethod: "LinearRegressionOrdinaryLeastSquares",
        sampleCount: 24,
        calculatedAtUtc: "2026-10-14T12:00:00Z",
        expiresAtUtc: "2026-10-14T13:00:00Z",
      },
    ];

    const mockUsage = {
      organizationId: "org-1",
      tokensUsedMonthToDate: 12500,
      invocationsMonthToDate: 42,
      monthlyTokenBudget: 500000,
      remainingTokens: 487500,
      isQuotaExceeded: false,
      estimatedCostUsd: 0.025,
    };

    // Mock API client methods
    vi.spyOn(apiClientModule.default, "get").mockImplementation((url: string) => {
      // debug
      if (url === "/applications") {
        return Promise.resolve({ data: { data: [{ id: "app-1", name: "checkout-service", displayName: "Checkout Service" }], success: true } } as any);
      }
      if (url.includes("/anomalies")) {
        return Promise.resolve({ data: { data: mockAnomalies, success: true } } as any);
      }
      if (url.includes("/logs/patterns")) {
        return Promise.resolve({ data: { data: mockPatterns, success: true } } as any);
      }
      if (url.includes("/forecasts")) {
        return Promise.resolve({ data: { data: mockForecasts, success: true } } as any);
      }
      if (url.includes("/incidentintelligence/copilot/usage")) {
        return Promise.resolve({ data: { data: mockUsage, success: true } } as any);
      }
      return Promise.resolve({ data: { data: [], success: true } } as any);
    });

    vi.spyOn(apiClientModule.default, "post").mockImplementation((url: string) => {
      if (url === "/incidentintelligence/copilot/chat") {
        return Promise.resolve({
          data: {
            data: {
              response: "Identified connection pool starvation as primary cause.",
              hypothesis: "Pool exhausted by unindexed queries",
              confidenceScore: 0.88,
              safetyChecksPassed: true,
              recommendedActions: ["Scale up connection pool to 200", "Terminate slow queries in pg_stat_activity"],
              requiresHumanApproval: true,
            },
            success: true,
          },
        } as any);
      }
      return Promise.resolve({ data: { data: true, success: true } } as any);
    });
  });

  const renderComponent = () =>
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AiObservabilityPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

  it("renders page header and token quota cards accurately", async () => {
    renderComponent();

    expect(screen.getByText("AI Observability & Incident Intelligence")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByText(/12[,.]500 \/ 500[,.]000/i)).toBeInTheDocument();
      expect(screen.getByText("42 requests")).toBeInTheDocument();
    });
  });

  it("renders active statistical anomalies with severity badge and details", async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText("payment-api")).toBeInTheDocument();
      expect(screen.getByText("RobustMedianMAD")).toBeInTheDocument();
      expect(screen.getByText("Surge above historical median")).toBeInTheDocument();
      expect(screen.getByText("Mark Resolved")).toBeInTheDocument();
    });
  });

  it("switches to Log Patterns tab and renders clustered templates", async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText("Checkout Service")).toBeInTheDocument();
    });

    const patternsTab = screen.getByTestId("tab-patterns");
    fireEvent.click(patternsTab);

    expect(screen.getByText("Clustered Log Patterns")).toBeInTheDocument();
    expect(screen.getByText(/after <NUM>ms/i)).toBeInTheDocument();
    expect(screen.getByText(/1[,.]420 occurrences/i)).toBeInTheDocument();
  });

  it("switches to Predictive Forecasts tab and renders OLS trajectory with 95% CI", async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText("Checkout Service")).toBeInTheDocument();
    });

    const forecastsTab = screen.getByTestId("tab-forecasts");
    fireEvent.click(forecastsTab);

    await waitFor(() => {
      expect(screen.getByText("cache-tier")).toBeInTheDocument();
      expect(screen.getByText("memory_usage_mb")).toBeInTheDocument();
      expect(screen.getByText("750.0")).toBeInTheDocument();
      expect(screen.getByText("1024.0")).toBeInTheDocument();
      expect(screen.getByText(/95% CI: \[980.0, 1068.0\]/i)).toBeInTheDocument();
    });
  });

  it("switches to Incident Copilot and displays safety disclaimer banner and handles question", async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText("Checkout Service")).toBeInTheDocument();
    });

    const copilotTab = screen.getByTestId("tab-copilot");
    fireEvent.click(copilotTab);

    // Safety disclaimer must be visible
    expect(
      screen.getByText(/All AI-generated remediation commands require explicit human operator confirmation/i)
    ).toBeInTheDocument();

    const input = screen.getByPlaceholderText(/Ask Copilot about an incident/i);
    fireEvent.change(input, { target: { value: "Why is checkout failing?" } });

    const submitBtn = screen.getByTestId("copilot-send");
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText("Identified connection pool starvation as primary cause.")).toBeInTheDocument();
      expect(screen.getByText("Safety Checks: Passed")).toBeInTheDocument();
      expect(screen.getByText("Human Approval Required")).toBeInTheDocument();
      expect(screen.getByText("Scale up connection pool to 200")).toBeInTheDocument();
    });
  });
});
