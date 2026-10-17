import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { IncidentDetailPage } from "@/pages/incident-detail";
import type {
  IncidentAnalysisResult,
  IncidentPostmortemResult,
  RemediationSuggestionsResult,
} from "@/lib/api-client";

// Mock hooks
vi.mock("@/context/auth-context", () => ({
  useAuth: () => ({
    user: { id: "u1", email: "admin@sentinelops.local", role: "Admin" },
    isOperator: true,
  }),
}));

vi.mock("@/hooks/use-alerts-hub", () => ({
  useAlertsHub: () => ({ isConnected: true, status: "connected" }),
}));

const mockIncidentData = {
  id: "inc-100",
  incidentNumber: "INC-2026-0001",
  title: "Critical Database Connection Pool Exhaustion",
  description: "Checkout service connection timeout during peak load",
  severity: "Critical",
  status: "Open",
  serviceName: "checkout-api",
  environmentName: "Production",
  createdAtUtc: "2026-10-13T12:00:00Z",
  timeline: [],
  comments: [],
  linkedAlerts: [],
};

const mockAnalysisResult: IncidentAnalysisResult = {
  incidentId: "inc-100",
  summary: "Database connection pool exhausted due to prolonged slow queries in checkout-api.",
  providerName: "Deterministic Heuristic Diagnostic Engine",
  generatedAtUtc: "2026-10-13T12:05:00Z",
  hypotheses: [
    {
      id: "hyp-1",
      title: "Connection Pool Starvation",
      confidenceScore: 92,
      description: "PostgreSQL active connections reached limit 100/100.",
      evidence: [
        "14 database timeout error logs in the last 15 minutes",
        "P95 latency surged from 45ms to 2400ms",
      ],
      recommendedDiagnostics: ["Check pg_stat_activity for long transactions"],
    },
  ],
  correlatedTelemetry: {
    logCount: 14,
    topExceptions: ["Npgsql.NpgsqlException: Timeout waiting for connection"],
    anomalousMetrics: ["db_connection_wait_time_ms > 2000"],
    slowTraceIds: ["tr-123", "tr-456"],
  },
  similarIncidents: [
    {
      incidentId: "inc-050",
      title: "Previous connection pool saturation",
      severity: "Critical",
      resolvedAtUtc: "2026-09-10T08:00:00Z",
      resolutionNotes: "Increased max pool size to 250",
      mttrMinutes: 42,
    },
  ],
};

const mockPostmortemResult: IncidentPostmortemResult = {
  incidentId: "inc-100",
  title: "Postmortem: Critical Database Connection Pool Exhaustion",
  generatedAtUtc: "2026-10-13T12:10:00Z",
  markdownContent: "# Postmortem: Critical Database Connection Pool Exhaustion\n\n**Impact:** 14% error rate on checkout.",
};

const mockRemediationResult: RemediationSuggestionsResult = {
  incidentId: "inc-100",
  actions: [
    {
      stepNumber: 1,
      title: "Restart checkout-api pods",
      commandOrAction: "kubectl rollout restart deployment/checkout-api",
      riskLevel: "Low",
      rationale: "Flushes stuck TCP sockets and releases leaked connection handles",
    },
  ],
};

const analyzeMock = vi.fn().mockResolvedValue(mockAnalysisResult);
const postmortemMock = vi.fn().mockResolvedValue(mockPostmortemResult);
const remediateMock = vi.fn().mockResolvedValue(mockRemediationResult);
const feedbackMock = vi.fn().mockResolvedValue(true);

vi.mock("@/lib/api-client", () => ({
  default: {
    get: vi.fn((url: string) => {
      if (url.startsWith("/incidents/inc-100")) {
        return Promise.resolve({
          data: {
            success: true,
            data: mockIncidentData,
          },
        });
      }
      return Promise.resolve({ data: { success: true, data: [] } });
    }),
    post: vi.fn().mockResolvedValue({ data: { success: true } }),
  },
  analyzeIncident: (...args: unknown[]) => analyzeMock(...args),
  generateIncidentPostmortem: (...args: unknown[]) => postmortemMock(...args),
  suggestIncidentRemediation: (...args: unknown[]) => remediateMock(...args),
  submitIncidentFeedback: (...args: unknown[]) => feedbackMock(...args),
}));

describe("Incident Intelligence & Diagnostic Assistant Frontend", () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
  });

  const renderComponent = () =>
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={["/incidents/inc-100"]}>
          <Routes>
            <Route path="/incidents/:id" element={<IncidentDetailPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );

  it("renders the AI Incident Intelligence card with action buttons", async () => {
    renderComponent();

    expect(await screen.findByText(/Critical Database Connection Pool Exhaustion/i)).toBeInTheDocument();
    expect(screen.getByText(/AI Incident Intelligence & Diagnostic Assistant/i)).toBeInTheDocument();
    expect(screen.getByText(/AI-Generated Heuristic Insights/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Analyze Incident/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Playbook/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Postmortem/i })).toBeInTheDocument();
  });

  it("executes AI analysis and renders root cause hypotheses, confidence, and correlated telemetry", async () => {
    renderComponent();

    const analyzeBtn = await screen.findByRole("button", { name: /Analyze Incident/i });
    fireEvent.click(analyzeBtn);

    await waitFor(() => {
      expect(analyzeMock).toHaveBeenCalledWith("inc-100");
    });

    // Verify hypotheses and evidence
    expect(await screen.findByText(/Connection Pool Starvation/i)).toBeInTheDocument();
    expect(screen.getByText(/Confidence: 92%/i)).toBeInTheDocument();
    expect(screen.getByText(/14 database timeout error logs in the last 15 minutes/i)).toBeInTheDocument();
    expect(screen.getByText(/Executive Diagnostic Summary:/i)).toBeInTheDocument();

    // Verify similar incidents
    expect(screen.getByText(/Previous connection pool saturation/i)).toBeInTheDocument();
    expect(screen.getByText(/Increased max pool size to 250/i)).toBeInTheDocument();
  });

  it("submits user feedback on diagnostic hypothesis", async () => {
    renderComponent();

    const analyzeBtn = await screen.findByRole("button", { name: /Analyze Incident/i });
    fireEvent.click(analyzeBtn);

    const helpfulBtn = await screen.findByTitle("Helpful");
    fireEvent.click(helpfulBtn);

    await waitFor(() => {
      expect(feedbackMock).toHaveBeenCalledWith("inc-100", {
        hypothesisId: "hyp-1",
        isHelpful: true,
      });
    });
  });

  it("renders remediation playbook checklist when requested", async () => {
    renderComponent();

    const playbookBtn = await screen.findByRole("button", { name: /Playbook/i });
    fireEvent.click(playbookBtn);

    await waitFor(() => {
      expect(remediateMock).toHaveBeenCalledWith("inc-100");
    });

    expect(await screen.findByText(/Remediation Playbook Checklist \(1 Steps\)/i)).toBeInTheDocument();
    expect(screen.getByText(/Restart checkout-api pods/i)).toBeInTheDocument();
    expect(screen.getByText(/kubectl rollout restart deployment\/checkout-api/i)).toBeInTheDocument();
  });

  it("opens postmortem modal with markdown report", async () => {
    renderComponent();

    const postmortemBtn = await screen.findByRole("button", { name: /Postmortem/i });
    fireEvent.click(postmortemBtn);

    await waitFor(() => {
      expect(postmortemMock).toHaveBeenCalledWith("inc-100");
    });

    expect(await screen.findByText("Incident Postmortem Report")).toBeInTheDocument();
    expect(screen.getByText(/# Postmortem: Critical Database Connection Pool Exhaustion/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Copy Markdown/i })).toBeInTheDocument();
  });
});
