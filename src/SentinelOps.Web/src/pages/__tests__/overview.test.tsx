import { render, screen } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "@/components/theme-provider";
import { OverviewPage } from "@/pages/overview";

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
  it("renders the overview heading", () => {
    renderWithProviders(<OverviewPage />);
    expect(screen.getByText("Overview")).toBeInTheDocument();
  });

  it("shows loading skeleton initially", () => {
    renderWithProviders(<OverviewPage />);
    // Loading skeletons are rendered as animated divs
    const skeletons = document.querySelectorAll(".animate-pulse");
    expect(skeletons.length).toBeGreaterThan(0);
  });
});
