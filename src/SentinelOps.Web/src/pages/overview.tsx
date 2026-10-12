import { useQuery } from "@tanstack/react-query";
import {
  AppWindow,
  Server,
  Bell,
  HeartPulse,
  AlertTriangle,
  Clock,
  FileText,
  GitBranch,
} from "lucide-react";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import apiClient, { type ApiResponse, type DashboardSummary } from "@/lib/api-client";

function StatCard({
  title,
  value,
  icon: Icon,
  description,
  variant = "default",
}: {
  title: string;
  value: string | number;
  icon: React.ElementType;
  description?: string;
  variant?: "default" | "success" | "warning" | "destructive";
}) {
  const variantColors = {
    default: "text-[hsl(var(--primary))]",
    success: "text-[hsl(var(--success))]",
    warning: "text-[hsl(var(--warning))]",
    destructive: "text-[hsl(var(--destructive))]",
  };

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
        <Icon className={`h-4 w-4 ${variantColors[variant]}`} />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{value}</div>
        {description && (
          <p className="text-xs text-[hsl(var(--muted-foreground))]">
            {description}
          </p>
        )}
      </CardContent>
    </Card>
  );
}

function LoadingSkeleton() {
  return (
    <div className="space-y-6">
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {Array.from({ length: 8 }).map((_, i) => (
          <Card key={i}>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <div className="h-4 w-24 animate-pulse rounded bg-[hsl(var(--muted))]" />
              <div className="h-4 w-4 animate-pulse rounded bg-[hsl(var(--muted))]" />
            </CardHeader>
            <CardContent>
              <div className="h-8 w-16 animate-pulse rounded bg-[hsl(var(--muted))]" />
              <div className="mt-1 h-3 w-32 animate-pulse rounded bg-[hsl(var(--muted))]" />
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}

export function OverviewPage() {
  const { data, isLoading, error } = useQuery({
    queryKey: ["dashboard-summary"],
    queryFn: async () => {
      const response = await apiClient.get<ApiResponse<DashboardSummary>>(
        "/dashboard/summary"
      );
      return response.data.data;
    },
    retry: 1,
    refetchInterval: 30000,
  });

  if (isLoading) {
    return (
      <div>
        <h1 className="mb-6 text-3xl font-bold tracking-tight">Overview</h1>
        <LoadingSkeleton />
      </div>
    );
  }

  if (error) {
    return (
      <div>
        <h1 className="mb-6 text-3xl font-bold tracking-tight">Overview</h1>
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <AlertTriangle className="mb-4 h-12 w-12 text-[hsl(var(--warning))]" />
            <h3 className="mb-2 text-lg font-semibold">Unable to load dashboard</h3>
            <p className="text-sm text-[hsl(var(--muted-foreground))]">
              Make sure the SentinelOps API is running and accessible.
            </p>
          </CardContent>
        </Card>
      </div>
    );
  }

  const summary = data ?? {
    totalApplications: 0,
    totalServices: 0,
    activeAlerts: 0,
    healthyServices: 0,
    errorRate: 0,
    avgResponseTimeMs: 0,
    totalLogsToday: 0,
    totalTracesToday: 0,
  };

  return (
    <div>
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Overview</h1>
          <p className="text-[hsl(var(--muted-foreground))]">
            Real-time observability dashboard
          </p>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title="Applications"
          value={summary.totalApplications}
          icon={AppWindow}
          description="Monitored applications"
        />
        <StatCard
          title="Services"
          value={summary.totalServices}
          icon={Server}
          description="Active services"
        />
        <StatCard
          title="Active Alerts"
          value={summary.activeAlerts}
          icon={Bell}
          variant={summary.activeAlerts > 0 ? "destructive" : "success"}
          description={
            summary.activeAlerts === 0 ? "All clear" : "Require attention"
          }
        />
        <StatCard
          title="Healthy Services"
          value={summary.healthyServices}
          icon={HeartPulse}
          variant="success"
          description="Passing health checks"
        />
        <StatCard
          title="Error Rate"
          value={`${summary.errorRate.toFixed(2)}%`}
          icon={AlertTriangle}
          variant={summary.errorRate > 5 ? "destructive" : "default"}
          description="Last 24 hours"
        />
        <StatCard
          title="Avg Response"
          value={`${summary.avgResponseTimeMs.toFixed(0)}ms`}
          icon={Clock}
          description="Mean response time"
        />
        <StatCard
          title="Logs Today"
          value={summary.totalLogsToday.toLocaleString()}
          icon={FileText}
          description="Log entries ingested"
        />
        <StatCard
          title="Traces Today"
          value={summary.totalTracesToday.toLocaleString()}
          icon={GitBranch}
          description="Distributed traces"
        />
      </div>
    </div>
  );
}
