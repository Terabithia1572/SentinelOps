import React from "react";
import { render, screen } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "@/context/auth-context";
import { ApplicationsPage } from "@/pages/applications";

vi.mock("@/lib/api-client", () => {
  return {
    default: {
      get: vi.fn().mockResolvedValue({
        data: {
          success: true,
          data: [
            {
              id: "app-1",
              name: "orders-api",
              displayName: "Orders Service",
              description: "Handles customer checkout and orders",
              isActive: true,
              createdAtUtc: "2026-10-12T00:00:00Z",
              environments: [
                { id: "env-1", applicationId: "app-1", name: "Production", environmentType: "Production", isActive: true, createdAtUtc: "2026-10-12T00:00:00Z" }
              ],
              services: [
                { id: "svc-1", applicationId: "app-1", name: "web-api", displayName: "Web API", isActive: true, createdAtUtc: "2026-10-12T00:00:00Z" }
              ]
            }
          ]
        }
      }),
      post: vi.fn(),
      delete: vi.fn(),
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
      <AuthProvider>{ui}</AuthProvider>
    </BrowserRouter>
  );
}

describe("ApplicationsPage", () => {
  it("renders applications page header and search input", async () => {
    renderWithProviders(<ApplicationsPage />);

    expect(await screen.findByRole("heading", { name: "Applications" })).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/Search applications/i)).toBeInTheDocument();
  });

  it("loads and displays application cards with environments and services", async () => {
    renderWithProviders(<ApplicationsPage />);

    expect(await screen.findByText("Orders Service")).toBeInTheDocument();
    expect(screen.getByText("orders-api")).toBeInTheDocument();
    expect(screen.getByText("Production")).toBeInTheDocument();
    expect(screen.getByText("Web API")).toBeInTheDocument();
  });

  it("renders quick action telemetry links (Keys, Health, Logs)", async () => {
    renderWithProviders(<ApplicationsPage />);

    expect(await screen.findByTitle("View API Keys")).toHaveAttribute("href", "/api-keys?app=app-1");
    expect(screen.getByTitle("Service Health")).toHaveAttribute("href", "/services?app=app-1");
    expect(screen.getByTitle("Application Logs")).toHaveAttribute("href", "/logs?app=orders-api");
  });
});
