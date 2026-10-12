import React from "react";
import { render, screen } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "@/context/auth-context";
import { ApiKeysPage } from "@/pages/api-keys";

vi.mock("@/lib/api-client", () => {
  return {
    default: {
      get: vi.fn((url: string) => {
        if (url === "/applications") {
          return Promise.resolve({
            data: {
              success: true,
              data: [
                {
                  id: "app-1",
                  name: "auth-service",
                  displayName: "Auth Service",
                  isActive: true,
                }
              ]
            }
          });
        }
        if (url.includes("/keys")) {
          return Promise.resolve({
            data: {
              success: true,
              data: [
                {
                  id: "key-1",
                  applicationId: "app-1",
                  name: "Prod Ingestion Key",
                  keyPrefix: "sent_live_abc123",
                  createdAtUtc: "2026-10-12T00:00:00Z",
                  expiresAtUtc: null,
                  revokedAtUtc: null,
                  lastUsedAtUtc: null,
                  createdBy: "admin",
                  isActive: true
                }
              ]
            }
          });
        }
        return Promise.resolve({ data: { success: true, data: [] } });
      }),
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
      <AuthProvider>{ui}</AuthProvider>
    </BrowserRouter>
  );
}

describe("ApiKeysPage", () => {
  it("renders API keys page title and description", async () => {
    renderWithProviders(<ApiKeysPage />);

    expect(await screen.findByRole("heading", { name: "API Keys" })).toBeInTheDocument();
    expect(screen.getByText(/Manage cryptographically secure ingestion tokens/i)).toBeInTheDocument();
  });

  it("loads applications into dropdown and displays existing API key in table", async () => {
    renderWithProviders(<ApiKeysPage />);

    expect(await screen.findByText("Prod Ingestion Key")).toBeInTheDocument();
    expect(screen.getByText("sent_live_abc123...")).toBeInTheDocument();
    expect(screen.getByText("Active")).toBeInTheDocument();
  });
});
