import { render, screen, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { StatusPagesPage } from "@/pages/status-pages";
import { PublicStatusPage } from "@/pages/public-status";
import type { StatusPageDto, PublicStatusPageDto } from "@/lib/api-client";

// Mock hooks
vi.mock("@/context/auth-context", () => ({
  useAuth: () => ({
    user: { id: "u1", email: "admin@sentinelops.local", role: "Admin" },
  }),
}));

const mockStatusPages: StatusPageDto[] = [
  {
    id: "sp-1",
    organizationId: "org-1",
    name: "Acme Cloud Platform Status",
    slug: "acme-status",
    description: "Real-time uptime and incident tracking for Acme Cloud.",
    logoUrl: null,
    supportUrl: "https://support.acme.local",
    headerNotice: null,
    isPublic: true,
    autoRollupEnabled: true,
    components: [
      { id: "c1", name: "Core API", computedStatus: "Operational" },
      { id: "c2", name: "Web Application", computedStatus: "Operational" },
    ],
    maintenanceWindows: [],
    activeIncidentsCount: 0,
    createdAtUtc: "2026-10-13T12:00:00Z",
  },
];

const mockPublicStatus: PublicStatusPageDto = {
  name: "Acme Cloud Platform Status",
  slug: "acme-status",
  description: "Real-time uptime and incident tracking for Acme Cloud.",
  logoUrl: null,
  supportUrl: "https://support.acme.local",
  headerNotice: "Scheduled maintenance planned for Sunday.",
  overallStatus: "All Systems Operational",
  components: [
    { id: "c1", name: "Core API", description: "REST and Telemetry API", computedStatus: "Operational" },
    { id: "c2", name: "Web Application", description: "Dashboard portal", computedStatus: "Operational" },
  ],
  activeIncidents: [],
  pastIncidents: [
    {
      id: "inc-1",
      statusPageId: "sp-1",
      title: "Temporary Network Latency",
      status: "Resolved",
      severity: "Minor",
      affectedComponentIds: ["c1"],
      updates: [
        {
          id: "u1",
          status: "Resolved",
          message: "Network routing restored by upstream transit provider.",
          createdAtUtc: "2026-10-12T14:00:00Z",
          postedBy: "NetOps",
        },
      ],
      createdAtUtc: "2026-10-12T13:30:00Z",
      resolvedAtUtc: "2026-10-12T14:00:00Z",
    },
  ],
  scheduledMaintenance: [],
  generatedAtUtc: "2026-10-13T12:00:00Z",
};

const getStatusPagesMock = vi.fn().mockResolvedValue(mockStatusPages);
const createStatusPageMock = vi.fn().mockResolvedValue(mockStatusPages[0]);
const getPublicStatusPageMock = vi.fn().mockResolvedValue(mockPublicStatus);

vi.mock("@/lib/api-client", () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
  },
  getStatusPages: () => getStatusPagesMock(),
  createStatusPage: (...args: unknown[]) => createStatusPageMock(...args),
  deleteStatusPage: vi.fn().mockResolvedValue(true),
  postStatusPageIncident: vi.fn().mockResolvedValue({ id: "inc-1" }),
  addStatusPageIncidentUpdate: vi.fn().mockResolvedValue({ id: "inc-1" }),
  getPublicStatusPage: (...args: unknown[]) => getPublicStatusPageMock(...args),
}));

describe("Status Pages & Public Incident Communication", () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
  });

  it("renders status pages management with list of configured status pages", async () => {
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <StatusPagesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(await screen.findByText("Acme Cloud Platform Status")).toBeInTheDocument();
    expect(screen.getByText("Public Status Pages")).toBeInTheDocument();
    expect(screen.getByText("SaaS Multi-Tenant")).toBeInTheDocument();
    expect(screen.getByText("/status/acme-status")).toBeInTheDocument();
    expect(screen.getByText("2 Services")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /New Status Page/i })).toBeInTheDocument();
  });

  it("opens create status page modal and allows filling details", async () => {
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <StatusPagesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    const newBtn = await screen.findByRole("button", { name: /New Status Page/i });
    fireEvent.click(newBtn);

    expect(await screen.findByText("Create Public Status Page")).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/Acme Cloud Platform Status/i)).toBeInTheDocument();

    const titleInput = screen.getByPlaceholderText(/Acme Cloud Platform Status/i);
    fireEvent.change(titleInput, { target: { value: "Fintech Core Status" } });

    // Verify slug auto-generation
    const slugInput = screen.getByPlaceholderText("acme-status");
    expect((slugInput as HTMLInputElement).value).toBe("fintech-core-status");
  });

  it("renders customer-facing public status page with overall status and component grid", async () => {
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={["/status/acme-status"]}>
          <Routes>
            <Route path="/status/:slug" element={<PublicStatusPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(await screen.findByText("All Systems Operational")).toBeInTheDocument();
    expect(screen.getByText("Scheduled maintenance planned for Sunday.")).toBeInTheDocument();
    expect(screen.getByText("Core API")).toBeInTheDocument();
    expect(screen.getByText("Web Application")).toBeInTheDocument();
    expect(screen.getByText("Temporary Network Latency")).toBeInTheDocument();
    expect(screen.getByText("Powered by SentinelOps")).toBeInTheDocument();
  });
});
