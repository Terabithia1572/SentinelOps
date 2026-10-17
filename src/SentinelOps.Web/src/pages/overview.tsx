import { useState } from "react";
import { Link } from "react-router-dom";
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
  RefreshCw,
  Network,
  Sparkles,
  ShieldCheck,
  ChevronRight,
  Radio,
  Layers,
  Activity,
  ArrowUpRight,
} from "lucide-react";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { StatusIndicator } from "@/components/ui/status-indicator";
import { Skeleton } from "@/components/ui/skeleton";
import { ErrorState } from "@/components/ui/error-state";
import { EmptyState } from "@/components/ui/empty-state";
import apiClient, { type ApiResponse, type DashboardSummary } from "@/lib/api-client";

interface IncidentSummaryItem {
  id: string;
  title: string;
  serviceName: string;
  severity: string;
  status: string;
  createdAt: string;
}

interface IncidentResponseData {
  items: IncidentSummaryItem[];
  totalCount: number;
}

const timeRanges = [
  { label: "15m", value: "15m" },
  { label: "1h", value: "1h" },
  { label: "6h", value: "6h" },
  { label: "24h", value: "24h" },
  { label: "7d", value: "7d" },
];

function StatCard({
  title,
  value,
  icon: Icon,
  description,
  href,
  variant = "default",
}: {
  title: string;
  value: string | number;
  icon: React.ElementType;
  description?: string;
  href?: string;
  variant?: "default" | "success" | "warning" | "destructive";
}) {
  const variantStyles = {
    default: "text-[#00E5FF] bg-[#00E5FF]/10 border-[#00E5FF]/20",
    success: "text-[#34D399] bg-[#34D399]/10 border-[#34D399]/20",
    warning: "text-[#FBBF24] bg-[#FBBF24]/10 border-[#FBBF24]/20",
    destructive: "text-[#F43F5E] bg-[#F43F5E]/10 border-[#F43F5E]/20",
  };

  const content = (
    <div className="group relative rounded border border-[#1E2333] bg-[#10121A] p-3.5 transition-all duration-150 hover:border-[#00E5FF]/40 hover:bg-[#141722]">
      <div className="flex items-center justify-between pb-2">
        <span className="text-[11px] font-mono font-medium uppercase tracking-wider text-[#758498]">
          {title}
        </span>
        <div className={`p-1 rounded ${variantStyles[variant]}`}>
          <Icon className="h-3.5 w-3.5" />
        </div>
      </div>
      <div>
        <div className="text-xl font-bold font-mono tracking-tight text-[#F0F3F9] tabular-nums">
          {value}
        </div>
        {description && (
          <p className="text-[11px] text-[#758498] mt-1 flex items-center justify-between font-mono">
            <span>{description}</span>
            {href && (
              <ArrowUpRight className="h-3 w-3 text-[#55657E] group-hover:text-[#00E5FF] transition-colors" />
            )}
          </p>
        )}
      </div>
    </div>
  );

  if (href) {
    return (
      <Link to={href} className="block">
        {content}
      </Link>
    );
  }

  return content;
}

function OverviewSkeleton() {
  return (
    <div className="space-y-4">
      {/* Banner Skeleton */}
      <Skeleton className="h-20 w-full rounded" />

      {/* KPI Grid Skeleton */}
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {Array.from({ length: 8 }).map((_, i) => (
          <div key={i} className="rounded border border-[#1E2333] bg-[#10121A] p-3.5 space-y-2">
            <div className="flex items-center justify-between">
              <Skeleton className="h-3.5 w-20" />
              <Skeleton className="h-6 w-6 rounded" />
            </div>
            <Skeleton className="h-7 w-24" />
            <Skeleton className="h-3 w-32" />
          </div>
        ))}
      </div>

      {/* Panels Skeleton */}
      <div className="grid gap-4 lg:grid-cols-3">
        <Skeleton className="h-72 lg:col-span-2 rounded" />
        <Skeleton className="h-72 rounded" />
      </div>
    </div>
  );
}

export function OverviewPage() {
  const [selectedRange, setSelectedRange] = useState("24h");
  const [refreshInterval, setRefreshInterval] = useState<number>(30000);

  // Fetch KPI Summary
  const {
    data: summaryData,
    isLoading: isSummaryLoading,
    isRefetching,
    error: summaryError,
    refetch,
  } = useQuery({
    queryKey: ["dashboard-summary", selectedRange],
    queryFn: async () => {
      const response = await apiClient.get<ApiResponse<DashboardSummary>>(
        "/dashboard/summary"
      );
      return response.data.data;
    },
    retry: 1,
    refetchInterval: refreshInterval || false,
  });

  // Fetch Recent Incidents
  const { data: incidentData, isLoading: isIncidentsLoading } = useQuery({
    queryKey: ["recent-incidents"],
    queryFn: async () => {
      const response = await apiClient.get<ApiResponse<IncidentResponseData>>(
        "/incidents?page=1&pageSize=5"
      );
      return response.data.data;
    },
    retry: 1,
    refetchInterval: refreshInterval || false,
  });

  if (isSummaryLoading) {
    return (
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-xl font-bold font-mono tracking-tight text-[#F0F3F9]">Overview</h1>
            <p className="text-xs text-[#758498] font-mono">Connecting to telemetry ingestion pipeline...</p>
          </div>
        </div>
        <OverviewSkeleton />
      </div>
    );
  }

  if (summaryError) {
    return (
      <div className="space-y-4">
        <h1 className="text-xl font-bold font-mono tracking-tight text-[#F0F3F9]">Overview</h1>
        <ErrorState
          title="Unable to connect to telemetry pipeline"
          message="Could not load system summary metrics. Verify that the SentinelOps API service is online."
          details={summaryError instanceof Error ? summaryError.message : "Network error"}
          onRetry={() => refetch()}
        />
      </div>
    );
  }

  const summary = summaryData ?? {
    totalApplications: 0,
    totalServices: 0,
    activeAlerts: 0,
    healthyServices: 0,
    errorRate: 0,
    avgResponseTimeMs: 0,
    totalLogsToday: 0,
    totalTracesToday: 0,
  };

  const isDegraded = summary.activeAlerts > 0 || summary.errorRate > 5;
  const isHealthy = !isDegraded && (summary.totalServices === 0 || summary.healthyServices >= summary.totalServices);

  return (
    <div className="space-y-4">
      {/* Precision Top Controls Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-[#1E2333] pb-3">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-lg font-bold font-mono tracking-tight text-[#F0F3F9]">Overview</h1>
            <Badge variant="outline" className="text-[10px] font-mono border-[#1E2333] text-[#00E5FF]">
              CLUSTER_EAST_1
            </Badge>
          </div>
          <p className="text-xs text-[#758498] font-mono mt-0.5">
            Real-time platform telemetry posture and distributed service vitals
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {/* Time Range Selector */}
          <div className="flex items-center rounded border border-[#1E2333] bg-[#0E1017] p-0.5">
            {timeRanges.map((range) => (
              <button
                key={range.value}
                type="button"
                onClick={() => setSelectedRange(range.value)}
                className={`px-2 py-0.5 text-xs font-mono font-medium rounded transition-colors ${
                  selectedRange === range.value
                    ? "bg-[#00E5FF] text-[#08090D] font-bold"
                    : "text-[#758498] hover:text-[#F0F3F9]"
                }`}
              >
                {range.label}
              </button>
            ))}
          </div>

          {/* Refresh Frequency */}
          <select
            value={refreshInterval}
            onChange={(e) => setRefreshInterval(Number(e.target.value))}
            aria-label="Refresh Frequency"
            className="rounded border border-[#1E2333] bg-[#0E1017] px-2 py-1 text-xs text-[#A6B4C9] font-mono focus:outline-none focus:border-[#00E5FF]"
          >
            <option value={0}>Auto-refresh Off</option>
            <option value={10000}>10s interval</option>
            <option value={30000}>30s interval</option>
            <option value={60000}>60s interval</option>
          </select>

          {/* Manual Refresh Button */}
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            disabled={isRefetching}
            className="h-7 border-[#1E2333] bg-[#0E1017] hover:bg-[#141722] text-[#F0F3F9]"
          >
            <RefreshCw className={`h-3 w-3 mr-1.5 ${isRefetching ? "animate-spin text-[#00E5FF]" : ""}`} />
            <span className="font-mono text-xs">Refresh</span>
          </Button>
        </div>
      </div>

      {/* Precision System Posture Banner */}
      <div className={`rounded border p-3.5 transition-all ${
        isHealthy
          ? "border-[#34D399]/30 bg-[#0E1017]"
          : "border-[#F43F5E]/30 bg-[#0E1017]"
      }`}>
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-3">
          <div className="flex items-center gap-3">
            <StatusIndicator
              status={isHealthy ? "healthy" : "degraded"}
              size="lg"
              pulse
            />
            <div>
              <div className="flex items-center gap-2">
                <h2 className="text-sm font-semibold text-[#F0F3F9] font-mono">
                  {isHealthy ? "All Core Systems Operational" : "Elevated Anomaly Alerting Active"}
                </h2>
                <Badge variant={isHealthy ? "success" : "warning"} className="text-[10px] px-1.5 py-0">
                  {isHealthy ? "SLO TARGET MET" : "ATTENTION REQUIRED"}
                </Badge>
              </div>
              <p className="text-xs text-[#758498] mt-0.5">
                {isHealthy
                  ? "All telemetry ingestion pipelines, alert evaluation workers, and background processors healthy."
                  : `${summary.activeAlerts} alert(s) firing or error rates exceeding normal operating boundaries.`}
              </p>
            </div>
          </div>

          <div className="flex items-center gap-5 border-t md:border-t-0 md:border-l border-[#1E2333] pt-2.5 md:pt-0 md:pl-5 text-xs text-[#758498] font-mono">
            <div>
              <span className="block text-[10px] uppercase tracking-wider text-[#55657E]">Service Health</span>
              <span className="font-semibold text-[#F0F3F9] tabular-nums">
                {summary.healthyServices} / {summary.totalServices} Active
              </span>
            </div>
            <div>
              <span className="block text-[10px] uppercase tracking-wider text-[#55657E]">Mean Latency</span>
              <span className="font-semibold text-[#00E5FF] tabular-nums">{summary.avgResponseTimeMs.toFixed(0)} ms</span>
            </div>
            <div>
              <span className="block text-[10px] uppercase tracking-wider text-[#55657E]">Error Rate</span>
              <span className={`font-semibold tabular-nums ${summary.errorRate > 1 ? "text-[#FBBF24]" : "text-[#34D399]"}`}>
                {summary.errorRate.toFixed(2)}%
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Primary KPI Grid (8 Stat Cards) */}
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title="Applications"
          value={summary.totalApplications}
          icon={AppWindow}
          description="Monitored apps"
          href="/applications"
        />
        <StatCard
          title="Active Services"
          value={summary.totalServices}
          icon={Server}
          description="Tracked components"
          href="/services"
        />
        <StatCard
          title="Active Alerts"
          value={summary.activeAlerts}
          icon={Bell}
          variant={summary.activeAlerts > 0 ? "destructive" : "success"}
          description={summary.activeAlerts === 0 ? "All thresholds normal" : "Fired rule conditions"}
          href="/alerts"
        />
        <StatCard
          title="Healthy Services"
          value={summary.healthyServices}
          icon={HeartPulse}
          variant="success"
          description="Passing uptime probes"
          href="/services"
        />
        <StatCard
          title="Error Rate"
          value={`${summary.errorRate.toFixed(2)}%`}
          icon={AlertTriangle}
          variant={summary.errorRate > 5 ? "destructive" : summary.errorRate > 1 ? "warning" : "default"}
          description="Trace failure ratio"
          href="/exceptions"
        />
        <StatCard
          title="Avg Response"
          value={`${summary.avgResponseTimeMs.toFixed(0)} ms`}
          icon={Clock}
          description="P50 service latency"
          href="/apm"
        />
        <StatCard
          title="Logs Today"
          value={summary.totalLogsToday.toLocaleString()}
          icon={FileText}
          description="Ingested log stream"
          href="/logs"
        />
        <StatCard
          title="Traces Today"
          value={summary.totalTracesToday.toLocaleString()}
          icon={GitBranch}
          description="Distributed trace spans"
          href="/traces"
        />
      </div>

      {/* Operational Observability Row */}
      <div className="grid gap-4 lg:grid-cols-3">
        {/* Recent Incidents Stream (2 columns wide) */}
        <Card className="lg:col-span-2 bg-[#10121A] border-[#1E2333]">
          <CardHeader className="flex flex-row items-center justify-between pb-2.5 border-b border-[#1E2333]">
            <div>
              <CardTitle className="text-xs font-semibold text-[#F0F3F9] font-mono flex items-center gap-2">
                <Radio className="h-3.5 w-3.5 text-[#00E5FF] animate-pulse" />
                Live Incident & Alert Feed
              </CardTitle>
              <p className="text-[11px] text-[#758498] mt-0.5">
                Active alerts and operational incidents across monitored infrastructure
              </p>
            </div>
            <Link
              to="/incidents"
              className="inline-flex items-center text-xs font-mono text-[#00E5FF] hover:text-[#33EBFF] transition-colors"
            >
              <span>View All Incidents</span>
              <ChevronRight className="ml-0.5 h-3 w-3" />
            </Link>
          </CardHeader>
          <CardContent className="pt-3">
            {isIncidentsLoading ? (
              <div className="space-y-2">
                <Skeleton className="h-10 w-full rounded" />
                <Skeleton className="h-10 w-full rounded" />
                <Skeleton className="h-10 w-full rounded" />
              </div>
            ) : incidentData?.items && incidentData.items.length > 0 ? (
              <div className="space-y-1.5">
                {incidentData.items.map((incident) => (
                  <Link
                    key={incident.id}
                    to={`/incidents/${incident.id}`}
                    className="flex items-center justify-between p-2.5 rounded border border-[#1E2333] bg-[#0E1017] hover:border-[#00E5FF]/40 hover:bg-[#141722] transition-colors"
                  >
                    <div className="flex items-center gap-2.5 truncate">
                      <div className={`h-2 w-2 rounded-full shrink-0 ${
                        incident.severity === "Critical" ? "bg-[#F43F5E]" : "bg-[#FBBF24]"
                      }`} />
                      <div className="truncate">
                        <span className="text-xs font-medium text-[#F0F3F9] block truncate font-mono">
                          {incident.title}
                        </span>
                        <span className="text-[10px] text-[#758498] block font-mono">
                          Service: {incident.serviceName || "Global"} • Status: {incident.status}
                        </span>
                      </div>
                    </div>
                    <Badge variant={incident.severity === "Critical" ? "danger" : "warning"} className="text-[10px] shrink-0 ml-2">
                      {incident.severity}
                    </Badge>
                  </Link>
                ))}
              </div>
            ) : (
              <EmptyState
                icon={ShieldCheck}
                title="Zero Active Incident Reports"
                description="All alert rules and threshold monitors are in a normal operational state."
              />
            )}
          </CardContent>
        </Card>

        {/* Quick Operations & Topology Jump (1 column wide) */}
        <Card className="bg-[#10121A] border-[#1E2333]">
          <CardHeader className="pb-2.5 border-b border-[#1E2333]">
            <CardTitle className="text-xs font-semibold text-[#F0F3F9] font-mono flex items-center gap-2">
              <Layers className="h-3.5 w-3.5 text-[#00E5FF]" />
              Telemetry Topology
            </CardTitle>
            <p className="text-[11px] text-[#758498] mt-0.5">
              Direct access to deep inspection views
            </p>
          </CardHeader>
          <CardContent className="space-y-2 pt-3">
            <Link
              to="/apm"
              className="flex items-center justify-between p-2 rounded border border-[#1E2333] bg-[#0E1017] hover:border-[#00E5FF]/40 hover:bg-[#141722] transition-all text-xs text-[#A6B4C9] group"
            >
              <div className="flex items-center gap-2.5">
                <Network className="h-3.5 w-3.5 text-[#00E5FF] group-hover:scale-105 transition-transform" />
                <div>
                  <span className="font-medium text-[#F0F3F9] block font-mono text-xs">APM & Service Map</span>
                  <span className="text-[10px] text-[#758498] font-mono">Distributed latency DAGs</span>
                </div>
              </div>
              <ChevronRight className="h-3.5 w-3.5 text-[#55657E] group-hover:text-[#00E5FF]" />
            </Link>

            <Link
              to="/ai-observability"
              className="flex items-center justify-between p-2 rounded border border-[#1E2333] bg-[#0E1017] hover:border-[#00E5FF]/40 hover:bg-[#141722] transition-all text-xs text-[#A6B4C9] group"
            >
              <div className="flex items-center gap-2.5">
                <Sparkles className="h-3.5 w-3.5 text-[#00E5FF] group-hover:scale-105 transition-transform" />
                <div>
                  <span className="font-medium text-[#F0F3F9] block font-mono text-xs">AI Root-Cause Assistant</span>
                  <span className="text-[10px] text-[#758498] font-mono">Automated anomaly triage</span>
                </div>
              </div>
              <ChevronRight className="h-3.5 w-3.5 text-[#55657E] group-hover:text-[#00E5FF]" />
            </Link>

            <Link
              to="/logs"
              className="flex items-center justify-between p-2 rounded border border-[#1E2333] bg-[#0E1017] hover:border-[#34D399]/40 hover:bg-[#141722] transition-all text-xs text-[#A6B4C9] group"
            >
              <div className="flex items-center gap-2.5">
                <FileText className="h-3.5 w-3.5 text-[#34D399] group-hover:scale-105 transition-transform" />
                <div>
                  <span className="font-medium text-[#F0F3F9] block font-mono text-xs">Structured Log Stream</span>
                  <span className="text-[10px] text-[#758498] font-mono">Query OTel collector logs</span>
                </div>
              </div>
              <ChevronRight className="h-3.5 w-3.5 text-[#55657E] group-hover:text-[#34D399]" />
            </Link>

            <div className="pt-2 border-t border-[#1E2333]">
              <div className="flex items-center justify-between text-[11px] text-[#758498] font-mono">
                <span className="flex items-center gap-1.5">
                  <Activity className="h-3 w-3 text-[#34D399] animate-pulse" />
                  OTel Pipeline Ingestion
                </span>
                <span className="font-semibold text-[#34D399]">ACTIVE</span>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
