import React from "react";
import { render, screen } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AuthProvider } from "@/context/auth-context";
import { AlertsPage } from "@/pages/alerts";
import { AlertRulesPage } from "@/pages/alert-rules";
import { IncidentsPage } from "@/pages/incidents";
import { SlosPage } from "@/pages/slos";

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
        if (url.startsWith("/alerts/summary")) {
          return Promise.resolve({
            data: {
              success: true,
              data: {
                totalRules: 8,
                enabledRules: 7,
                firingAlerts: 2,
                acknowledgedAlerts: 1,
                pendingAlerts: 0,
                criticalAlerts: 1,
                warningAlerts: 1,
              },
            },
          });
        }
        if (url.startsWith("/alerts?")) {
          return Promise.resolve({
            data: {
              success: true,
              data: {
                items: [
                  {
                    id: "alert-1",
                    alertRuleId: "rule-1",
                    alertRuleName: "High Error Rate Breached",
                    severity: "Critical",
                    oldStatus: "Normal",
                    newStatus: "Firing",
                    measuredValue: 12.5,
                    threshold: 5.0,
                    triggeredAtUtc: "2026-10-13T12:00:00Z",
                    message: "Observed error rate 12.50% exceeded threshold 5.00%",
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
        if (url.startsWith("/alert-rules")) {
          return Promise.resolve({
            data: {
              success: true,
              data: {
                items: [
                  {
                    id: "rule-1",
                    name: "Checkout 5xx Error Spike",
                    description: "Triggers when 5xx errors exceed 5%",
                    applicationId: "app-1",
                    serviceName: "checkout-api",
                    environmentName: "Production",
                    conditionType: "ErrorRateAboveThreshold",
                    operator: "GreaterThan",
                    condition: "GreaterThan 5.0",
                    metricName: "error_rate",
                    threshold: 5.0,
                    evaluationWindowMinutes: 5,
                    evaluationFrequencyMinutes: 1,
                    consecutiveCountRequired: 1,
                    currentConsecutiveFailures: 0,
                    cooldownMinutes: 15,
                    status: "Normal",
                    severity: "Critical",
                    isEnabled: true,
                    lastMeasuredValue: 1.2,
                  },
                ],
                totalCount: 1,
                page: 1,
                pageSize: 100,
                totalPages: 1,
              },
            },
          });
        }
        if (url === "/notification-channels") {
          return Promise.resolve({
            data: {
              success: true,
              data: [
                {
                  id: "ch-1",
                  name: "SRE Slack Webhook",
                  description: "Main alerting webhook",
                  type: "Webhook",
                  destination: "https://hooks.slack.com/services/T00/B00/X00",
                  isEnabled: true,
                  createdAtUtc: "2026-10-13T10:00:00Z",
                },
              ],
            },
          });
        }
        if (url.startsWith("/incidents")) {
          return Promise.resolve({
            data: {
              success: true,
              data: {
                items: [
                  {
                    id: "inc-1",
                    incidentNumber: "INC-2026-0001",
                    title: "Elevated Payment Checkout Latency",
                    description: "P95 latency exceeding 800ms across EU-West",
                    severity: "Critical",
                    status: "Open",
                    applicationId: "app-1",
                    serviceName: "payments-api",
                    environmentName: "Production",
                    assignedToUserName: "Yunus Inan",
                    createdAtUtc: "2026-10-13T12:00:00Z",
                    timelineEventsCount: 3,
                    commentsCount: 2,
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
        if (url.startsWith("/slos")) {
          return Promise.resolve({
            data: {
              success: true,
              data: [
                {
                  id: "slo-1",
                  name: "Checkout API 99.9% Availability",
                  description: "Core payment transaction uptime",
                  applicationId: "app-1",
                  serviceName: "checkout-api",
                  environmentName: "Production",
                  indicatorType: "Availability",
                  targetPercentage: 99.9,
                  evaluationWindow: "Rolling24Hours",
                  isEnabled: true,
                  currentCompliancePercentage: 99.95,
                  currentErrorBudgetRemaining: 75.0,
                  burnRate: 0.85,
                  isBreached: false,
                  createdAtUtc: "2026-10-13T10:00:00Z",
                },
              ],
            },
          });
        }
        return Promise.resolve({ data: { success: true, data: null } });
      }),
      post: vi.fn(() => Promise.resolve({ data: { success: true, data: {} } })),
      put: vi.fn(() => Promise.resolve({ data: { success: true, data: {} } })),
      delete: vi.fn(() => Promise.resolve({ data: { success: true, data: {} } })),
      interceptors: {
        request: { use: vi.fn() },
        response: { use: vi.fn() },
      },
    },
  };
});

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });
}

function renderWithProviders(ui: React.ReactElement) {
  const queryClient = createTestQueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>{ui}</AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  );
}

describe("Phase 5 Enterprise Alerting & Incident Management Pages", () => {
  it("renders AlertsPage with KPI cards, filters and alerts table", async () => {
    renderWithProviders(<AlertsPage />);
    expect(screen.getByText("Alerts & Incidents")).toBeInTheDocument();
    expect(await screen.findByText("Firing Alerts")).toBeInTheDocument();
    expect(await screen.findByText("Critical Breaches")).toBeInTheDocument();
    expect(await screen.findByText("High Error Rate Breached")).toBeInTheDocument();
  });

  it("renders AlertRulesPage with rules table and test evaluate action", async () => {
    renderWithProviders(<AlertRulesPage />);
    expect(screen.getByText("Alert Rules & Channels")).toBeInTheDocument();
    expect(await screen.findByText("Checkout 5xx Error Spike")).toBeInTheDocument();
    expect(await screen.findByText("ErrorRateAboveThreshold")).toBeInTheDocument();
  });

  it("renders IncidentsPage with triage cards and incident stream", async () => {
    renderWithProviders(<IncidentsPage />);
    expect(screen.getByText("Incident Management")).toBeInTheDocument();
    expect(await screen.findByText("Elevated Payment Checkout Latency")).toBeInTheDocument();
    expect(await screen.findByText("INC-2026-0001")).toBeInTheDocument();
  });

  it("renders SlosPage with objective cards, compliance and burn rate", async () => {
    renderWithProviders(<SlosPage />);
    expect(screen.getByText("SLO & Error Budget Monitoring")).toBeInTheDocument();
    expect(await screen.findByText("Checkout API 99.9% Availability")).toBeInTheDocument();
    expect((await screen.findAllByText("99.95%")).length).toBeGreaterThan(0);
    expect(await screen.findByText("75.0%")).toBeInTheDocument();
  });
});
