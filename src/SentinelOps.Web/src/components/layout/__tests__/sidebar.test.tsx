import { render, screen } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { Sidebar } from "@/components/layout/sidebar";

vi.mock("@/context/auth-context", () => ({
  useAuth: () => ({
    user: {
      id: "u-1",
      userName: "sre-lead",
      fullName: "SRE Platform Lead",
      email: "sre@sentinelops.local",
      roles: ["Admin"],
    },
    logout: vi.fn(),
  }),
}));

vi.mock("@/context/tenant-context", () => ({
  useTenant: () => ({
    activeOrg: { id: "org-1", name: "Acme Corp", userRole: "Admin" },
    organizations: [{ id: "org-1", name: "Acme Corp", userRole: "Admin" }],
    setActiveOrg: vi.fn(),
  }),
}));

describe("Sidebar Navigation Component", () => {
  it("renders brand logo and four structured navigation categories", () => {
    render(
      <BrowserRouter>
        <Sidebar />
      </BrowserRouter>
    );

    expect(screen.getByText("SentinelOps")).toBeInTheDocument();
    expect(screen.getByText("Operations")).toBeInTheDocument();
    expect(screen.getByText("Telemetry")).toBeInTheDocument();
    expect(screen.getByText("Incident Response")).toBeInTheDocument();
    expect(screen.getByText("Administration")).toBeInTheDocument();
  });

  it("renders key telemetry and operational links", () => {
    render(
      <BrowserRouter>
        <Sidebar />
      </BrowserRouter>
    );

    expect(screen.getByRole("link", { name: /Overview/i })).toHaveAttribute("href", "/");
    expect(screen.getByRole("link", { name: /APM & Service Map/i })).toHaveAttribute("href", "/apm");
    expect(screen.getByRole("link", { name: /^Logs$/i })).toHaveAttribute("href", "/logs");
    expect(screen.getByRole("link", { name: /Active Alerts/i })).toHaveAttribute("href", "/alerts");
    expect(screen.getByRole("link", { name: /Billing & Plans/i })).toHaveAttribute("href", "/billing");
  });

  it("displays user profile information and role badge", () => {
    render(
      <BrowserRouter>
        <Sidebar />
      </BrowserRouter>
    );

    expect(screen.getByText("SRE Platform Lead")).toBeInTheDocument();
    expect(screen.getByText("sre@sentinelops.local")).toBeInTheDocument();
    expect(screen.getAllByText("Admin").length).toBeGreaterThanOrEqual(1);
  });
});
