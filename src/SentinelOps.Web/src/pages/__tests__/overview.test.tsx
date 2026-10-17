import { render, screen, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "@/components/theme-provider";
import { OverviewPage } from "@/pages/overview";
import apiClient from "@/lib/api-client";

vi.mock("@/lib/api-client", () => {
  return {
    default: {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
      delete: vi.fn(),
      interceptors: {
        request: { use: vi.fn() },
        response: { use: vi.fn() },
      },
    },
  };
});

function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <ThemeProvider defaultTheme="dark">
        <BrowserRouter>{ui}</BrowserRouter>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

describe("OverviewPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders the overview heading and loading skeleton initially", () => {
    vi.mocked(apiClient.get).mockReturnValue(new Promise(() => {})); // pending promise
    renderWithProviders(<OverviewPage />);

    expect(screen.getByText("Overview")).toBeInTheDocument();
    const skeletons = document.querySelectorAll(".animate-pulse");
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it("renders loaded dashboard with metrics and operational status", async () => {
    vi.mocked(apiClient.get).mockImplementation((url: string) => {
      if (url.includes("/dashboard/summary")) {
        return Promise.resolve({
          data: {
            success: true,
            data: {
              totalApplications: 4,
              totalServices: 12,
              activeAlerts: 0,
              healthyServices: 12,
              errorRate: 0.15,
              avgResponseTimeMs: 42,
              totalLogsToday: 15420,
              totalTracesToday: 8930,
            },
          },
        });
      }
      if (url.includes("/incidents")) {
        return Promise.resolve({
          data: {
            success: true,
            data: {
              items: [],
              totalCount: 0,
            },
          },
        });
      }
      return Promise.reject(new Error("Unknown route"));
    });

    renderWithProviders(<OverviewPage />);

    await waitFor(() => {
      expect(screen.getByText("All Core Systems Operational")).toBeInTheDocument();
    });

    expect(screen.getByText("Applications")).toBeInTheDocument();
    expect(screen.getByText("Active Services")).toBeInTheDocument();
    expect(screen.getByText("Mean Latency")).toBeInTheDocument();
    expect(screen.getByText("Live Incident & Alert Feed")).toBeInTheDocument();
    expect(screen.getByText("Telemetry Topology")).toBeInTheDocument();
    expect(screen.getByText("Zero Active Incident Reports")).toBeInTheDocument();
  });
});
