import { render, screen } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { NotFoundPage } from "@/pages/not-found";

describe("NotFoundPage", () => {
  it("renders 404 header and description", () => {
    render(
      <BrowserRouter>
        <NotFoundPage />
      </BrowserRouter>
    );

    expect(screen.getByText("HTTP 404")).toBeInTheDocument();
    expect(screen.getByText("Page Not Located in Topology")).toBeInTheDocument();
    expect(
      screen.getByText(/The requested path does not match any active SentinelOps dashboard/i)
    ).toBeInTheDocument();
  });

  it("renders quick navigation links to Overview, Incidents, and Logs", () => {
    render(
      <BrowserRouter>
        <NotFoundPage />
      </BrowserRouter>
    );

    const overviewBtn = screen.getByRole("button", { name: /Operations Overview/i });
    expect(overviewBtn).toBeInTheDocument();

    const incidentsLink = screen.getByRole("link", { name: /Active Incidents/i });
    expect(incidentsLink).toBeInTheDocument();
    expect(incidentsLink).toHaveAttribute("href", "/incidents");

    const logsLink = screen.getByRole("link", { name: /Log Explorer/i });
    expect(logsLink).toBeInTheDocument();
    expect(logsLink).toHaveAttribute("href", "/logs");
  });
});
