import React from "react";
import { Link, useLocation } from "react-router-dom";
import { ChevronRight, Home } from "lucide-react";
import { cn } from "@/lib/utils";

const routeNameMap: Record<string, string> = {
  dashboards: "Dashboards",
  apm: "APM & Service Map",
  "ai-observability": "AI Observability",
  organizations: "Organizations",
  applications: "Applications",
  "api-keys": "API Keys",
  services: "Services",
  logs: "Logs",
  traces: "Traces",
  metrics: "Metrics",
  exceptions: "Exceptions",
  alerts: "Alerts",
  "alert-rules": "Alert Rules",
  incidents: "Incidents",
  "status-pages": "Status Pages",
  slos: "SLOs & Objectives",
  health: "Health",
  analytics: "Analytics & Reports",
  reports: "Reports",
  team: "Team",
  users: "Users",
  "audit-logs": "Audit Logs",
  billing: "Billing & Plans",
  subscription: "Subscription",
  settings: "Settings",
  onboarding: "Onboarding",
};

export interface BreadcrumbsProps extends React.HTMLAttributes<HTMLElement> {}

export function Breadcrumbs({ className, ...props }: BreadcrumbsProps) {
  const location = useLocation();
  const pathnames = location.pathname.split("/").filter((x) => x);

  if (pathnames.length === 0) {
    return (
      <nav aria-label="Breadcrumb" className={cn("flex items-center text-xs text-[hsl(var(--muted-foreground))]", className)} {...props}>
        <div className="flex items-center gap-1.5 font-medium text-[hsl(var(--foreground))]">
          <Home className="h-3.5 w-3.5" />
          <span>Overview</span>
        </div>
      </nav>
    );
  }

  return (
    <nav aria-label="Breadcrumb" className={cn("flex items-center text-xs text-[hsl(var(--muted-foreground))]", className)} {...props}>
      <ol className="flex items-center space-x-1.5">
        <li>
          <Link
            to="/"
            className="flex items-center gap-1 hover:text-[hsl(var(--foreground))] transition-colors"
          >
            <Home className="h-3.5 w-3.5" />
            <span className="sr-only">Home</span>
          </Link>
        </li>
        {pathnames.map((value, index) => {
          const to = `/${pathnames.slice(0, index + 1).join("/")}`;
          const isLast = index === pathnames.length - 1;
          const label = routeNameMap[value.toLowerCase()] || value;

          return (
            <li key={to} className="flex items-center gap-1.5">
              <ChevronRight className="h-3 w-3 shrink-0 text-[hsl(var(--muted-foreground))/0.6]" aria-hidden="true" />
              {isLast ? (
                <span className="font-semibold text-[hsl(var(--foreground))] truncate max-w-[200px]" aria-current="page">
                  {label}
                </span>
              ) : (
                <Link
                  to={to}
                  className="hover:text-[hsl(var(--foreground))] transition-colors truncate max-w-[150px]"
                >
                  {label}
                </Link>
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
