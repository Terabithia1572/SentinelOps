import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { Button } from "../button";
import { Badge } from "../badge";
import { Skeleton } from "../skeleton";
import { EmptyState } from "../empty-state";
import { ErrorState } from "../error-state";
import { StatusIndicator } from "../status-indicator";
import { ConfirmDialog } from "../confirm-dialog";
import { Breadcrumbs } from "../breadcrumbs";
import { Select } from "../select";
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from "../table";

describe("Enterprise Design System UI Primitives", () => {
  it("renders Button variants and triggers click events", () => {
    const handleClick = vi.fn();
    render(<Button onClick={handleClick}>Click Me</Button>);
    const btn = screen.getByRole("button", { name: /click me/i });
    expect(btn).toBeInTheDocument();
    fireEvent.click(btn);
    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it("renders Badge variants with correct semantic classes", () => {
    const { rerender } = render(<Badge variant="success">Operational</Badge>);
    expect(screen.getByText("Operational")).toBeInTheDocument();

    rerender(<Badge variant="danger">Outage</Badge>);
    expect(screen.getByText("Outage")).toBeInTheDocument();
  });

  it("renders Skeleton loading placeholder", () => {
    const { container } = render(<Skeleton className="h-6 w-24" />);
    expect(container.firstChild).toHaveClass("animate-pulse");
  });

  it("renders EmptyState with action callbacks", () => {
    const handleAction = vi.fn();
    render(
      <EmptyState
        title="No Applications Found"
        description="Get started by creating your first application."
        actionLabel="Create Application"
        onAction={handleAction}
      />
    );
    expect(screen.getByText("No Applications Found")).toBeInTheDocument();
    const actionBtn = screen.getByRole("button", { name: /create application/i });
    fireEvent.click(actionBtn);
    expect(handleAction).toHaveBeenCalledTimes(1);
  });

  it("renders ErrorState with retry trigger and technical details", () => {
    const handleRetry = vi.fn();
    render(
      <ErrorState
        title="Failed to Load"
        message="Backend timeout"
        error="Network error at 127.0.0.1"
        onRetry={handleRetry}
      />
    );
    expect(screen.getByText("Failed to Load")).toBeInTheDocument();
    const retryBtn = screen.getByRole("button", { name: /try again/i });
    fireEvent.click(retryBtn);
    expect(handleRetry).toHaveBeenCalledTimes(1);

    const toggleBtn = screen.getByText(/show technical details/i);
    fireEvent.click(toggleBtn);
    expect(screen.getByText(/Network error at 127.0.0.1/i)).toBeInTheDocument();
  });

  it("renders StatusIndicator with semantic colors and label", () => {
    render(<StatusIndicator status="healthy" label="Healthy" />);
    expect(screen.getByText("Healthy")).toBeInTheDocument();
  });

  it("renders ConfirmDialog and responds to confirmation and cancellation", () => {
    const handleConfirm = vi.fn();
    const handleOpenChange = vi.fn();
    render(
      <ConfirmDialog
        open={true}
        onOpenChange={handleOpenChange}
        title="Delete Resource?"
        description="This action cannot be undone."
        onConfirm={handleConfirm}
      />
    );
    expect(screen.getByText("Delete Resource?")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /confirm/i }));
    expect(handleConfirm).toHaveBeenCalledTimes(1);
  });

  it("renders Breadcrumbs with navigation path", () => {
    render(
      <BrowserRouter>
        <Breadcrumbs />
      </BrowserRouter>
    );
    expect(screen.getByLabelText("Breadcrumb")).toBeInTheDocument();
  });

  it("renders Select component with options and error state", () => {
    render(
      <Select label="Environment" error="Required field">
        <option value="prod">Production</option>
        <option value="stage">Staging</option>
      </Select>
    );
    expect(screen.getByText("Environment")).toBeInTheDocument();
    expect(screen.getByText("Required field")).toBeInTheDocument();
  });

  it("renders Table structure properly", () => {
    render(
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Service</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          <TableRow>
            <TableCell>PaymentAPI</TableCell>
          </TableRow>
        </TableBody>
      </Table>
    );
    expect(screen.getByText("Service")).toBeInTheDocument();
    expect(screen.getByText("PaymentAPI")).toBeInTheDocument();
  });
});
