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
  const variantBadge = {
    default: "text-blue-400 bg-blue-500/10 border-blue-500/20",
    success: "text-emerald-400 bg-emerald-500/10 border-emerald-500/20",
    warning: "text-amber-400 bg-amber-500/10 border-amber-500/20",
    destructive: "text-rose-400 bg-rose-500/10 border-rose-500/20",
  };

  const content = (
    <Card className="hover:border-slate-700 transition-all duration-200 bg-slate-900/60 border-slate-800">
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-xs font-medium text-slate-400">{title}</CardTitle>
        <div className={`p-1.5 rounded-lg border ${variantBadge[variant]}`}>
          <Icon className="h-4 w-4" />
        </div>
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold tracking-tight text-white">{value}</div>
        {description && (
          <p className="text-xs text-slate-400 mt-1 flex items-center justify-between">
            <span>{description}</span>
            {href && <ChevronRight className="h-3 w-3 text-slate-500" />}
          </p>
        )}
      </CardContent>
    </Card>
  );

  if (href) {
    return (
      <Link to={href} className="block transition-transform hover:-translate-y-0.5">
        {content}
      </Link>
    );
  }

  return content;
}

function OverviewSkeleton() {
  return (
    <div className="space-y-6">
      {/* Banner Skeleton */}
      <Skeleton className="h-24 w-full rounded-xl" />

      {/* KPI Grid Skeleton */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {Array.from({ length: 8 }).map((_, i) => (
          <Card key={i} className="bg-slate-900/40 border-slate-800">
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <Skeleton className="h-4 w-20" />
              <Skeleton className="h-7 w-7 rounded-lg" />
            </CardHeader>
            <CardContent>
              <Skeleton className="h-8 w-16 mb-2" />
              <Skeleton className="h-3 w-28" />
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Panels Skeleton */}
      <div className="grid gap-6 lg:grid-cols-3">
        <Skeleton className="h-80 lg:col-span-2 rounded-xl" />
        <Skeleton className="h-80 rounded-xl" />
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
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-white">Overview</h1>
            <p className="text-xs text-slate-400">Loading platform telemetry posture...</p>
          </div>
        </div>
        <OverviewSkeleton />
      </div>
    );
  }

  if (summaryError) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold tracking-tight text-white">Overview</h1>
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
    <div className="space-y-6">
      {/* Top Controls Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-white">Overview</h1>
          <p className="text-xs text-slate-400">
            Real-time platform telemetry posture and distributed service vitals
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {/* Time Range Pills */}
          <div className="flex items-center rounded-lg border border-slate-800 bg-slate-900/60 p-1">
            {timeRanges.map((range) => (
              <button
                key={range.value}
                type="button"
                onClick={() => setSelectedRange(range.value)}
                className={`px-2.5 py-1 text-xs font-medium rounded-md transition-colors ${
                  selectedRange === range.value
                    ? "bg-blue-600 text-white shadow-sm"
                    : "text-slate-400 hover:text-slate-200"
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
            className="rounded-lg border border-slate-800 bg-slate-900/60 px-2.5 py-1.5 text-xs text-slate-300 focus:outline-none focus:ring-1 focus:ring-blue-500"
          >
            <option value={0}>Auto-refresh Off</option>
            <option value={10000}>Every 10s</option>
            <option value={30000}>Every 30s</option>
            <option value={60000}>Every 1m</option>
          </select>

          {/* Manual Refresh Button */}
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            disabled={isRefetching}
            className="h-8 border-slate-800 bg-slate-900/60 hover:bg-slate-800 text-slate-300"
          >
            <RefreshCw className={`h-3.5 w-3.5 mr-1.5 ${isRefetching ? "animate-spin text-blue-400" : ""}`} />
            <span>Refresh</span>
          </Button>
        </div>
      </div>

      {/* System Posture Banner */}
      <div className={`rounded-xl border p-4 transition-all ${
        isHealthy
          ? "border-emerald-500/20 bg-emerald-950/20 shadow-lg shadow-emerald-950/10"
          : "border-amber-500/30 bg-amber-950/20 shadow-lg shadow-amber-950/10"
      }`}>
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <StatusIndicator
              status={isHealthy ? "healthy" : "degraded"}
              size="lg"
              pulse
            />
            <div>
              <div className="flex items-center gap-2">
                <h2 className="text-sm font-semibold text-white">
                  {isHealthy ? "All Core Systems Operational" : "Elevated Anomaly Alerting Active"}
                </h2>
                <Badge variant={isHealthy ? "success" : "warning"} className="text-[10px] px-2 py-0">
                  {isHealthy ? "SLA 99.99%" : "Attention Required"}
                </Badge>
              </div>
              <p className="text-xs text-slate-400 mt-0.5">
                {isHealthy
                  ? "All telemetry ingestion pipelines, alert evaluation workers, and background processors healthy."
                  : `${summary.activeAlerts} alert(s) firing or error rates exceeding normal operating boundaries.`}
              </p>
            </div>
          </div>

          <div className="flex items-center gap-6 border-t md:border-t-0 md:border-l border-slate-800 pt-3 md:pt-0 md:pl-6 text-xs text-slate-400">
            <div>
              <span className="block text-[10px] uppercase tracking-wider text-slate-500">Service Health</span>
              <span className="font-semibold text-slate-200">
                {summary.healthyServices} / {summary.totalServices} Active
              </span>
            </div>
            <div>
              <span className="block text-[10px] uppercase tracking-wider text-slate-500">Mean Latency</span>
              <span className="font-semibold text-slate-200">{summary.avgResponseTimeMs.toFixed(0)} ms</span>
            </div>
            <div>
              <span className="block text-[10px] uppercase tracking-wider text-slate-500">Error Rate</span>
              <span className={`font-semibold ${summary.errorRate > 1 ? "text-amber-400" : "text-emerald-400"}`}>
                {summary.errorRate.toFixed(2)}%
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Primary KPI Grid (8 Cards) */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
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

      {/* Multi-Panel Operational Observability Row */}
      <div className="grid gap-6 lg:grid-cols-3">
        {/* Recent Incidents Stream (2 columns wide) */}
        <Card className="lg:col-span-2 bg-slate-900/60 border-slate-800">
          <CardHeader className="flex flex-row items-center justify-between pb-3">
            <div>
              <CardTitle className="text-sm font-semibold text-white flex items-center gap-2">
                <Radio className="h-4 w-4 text-blue-400 animate-pulse" />
                Live Incident & Alert Feed
              </CardTitle>
              <p className="text-xs text-slate-400 mt-0.5">
                Active alerts and operational incidents across monitored infrastructure
              </p>
            </div>
            <Link
              to="/incidents"
              className="inline-flex items-center text-xs font-medium text-blue-400 hover:text-blue-300 transition-colors"
            >
              View All Incidents
              <ChevronRight className="ml-1 h-3.5 w-3.5" />
            </Link>
          </CardHeader>
          <CardContent>
            {isIncidentsLoading ? (
              <div className="space-y-3">
                <Skeleton className="h-12 w-full rounded-lg" />
                <Skeleton className="h-12 w-full rounded-lg" />
                <Skeleton className="h-12 w-full rounded-lg" />
              </div>
            ) : incidentData?.items && incidentData.items.length > 0 ? (
              <div className="space-y-2">
                {incidentData.items.map((incident) => (
                  <Link
                    key={incident.id}
                    to={`/incidents/${incident.id}`}
                    className="flex items-center justify-between p-3 rounded-lg border border-slate-800 bg-slate-950/40 hover:border-slate-700 hover:bg-slate-800/40 transition-colors"
                  >
                    <div className="flex items-center gap-3 truncate">
                      <div className={`h-2 w-2 rounded-full shrink-0 ${
                        incident.severity === "Critical" ? "bg-rose-500" : "bg-amber-500"
                      }`} />
                      <div className="truncate">
                        <span className="text-xs font-semibold text-slate-200 block truncate">
                          {incident.title}
                        </span>
                        <span className="text-[10px] text-slate-400 block">
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
        <Card className="bg-slate-900/60 border-slate-800">
          <CardHeader className="pb-3">
            <CardTitle className="text-sm font-semibold text-white flex items-center gap-2">
              <Layers className="h-4 w-4 text-blue-400" />
              Telemetry Topology
            </CardTitle>
            <p className="text-xs text-slate-400 mt-0.5">
              Direct access to deep inspection views
            </p>
          </CardHeader>
          <CardContent className="space-y-2.5">
            <Link
              to="/apm"
              className="flex items-center justify-between p-2.5 rounded-lg border border-slate-800 bg-slate-950/40 hover:border-blue-500/40 hover:bg-slate-800/40 transition-all text-xs text-slate-300 group"
            >
              <div className="flex items-center gap-2.5">
                <Network className="h-4 w-4 text-blue-400 group-hover:scale-110 transition-transform" />
                <div>
                  <span className="font-medium text-slate-200 block">APM & Service Map</span>
                  <span className="text-[10px] text-slate-400">Distributed latency DAGs</span>
                </div>
              </div>
              <ChevronRight className="h-3.5 w-3.5 text-slate-500 group-hover:text-slate-300" />
            </Link>

            <Link
              to="/ai-observability"
              className="flex items-center justify-between p-2.5 rounded-lg border border-slate-800 bg-slate-950/40 hover:border-purple-500/40 hover:bg-slate-800/40 transition-all text-xs text-slate-300 group"
            >
              <div className="flex items-center gap-2.5">
                <Sparkles className="h-4 w-4 text-purple-400 group-hover:scale-110 transition-transform" />
                <div>
                  <span className="font-medium text-slate-200 block">AI Root-Cause Assistant</span>
                  <span className="text-[10px] text-slate-400">Automated incident triage</span>
                </div>
              </div>
              <ChevronRight className="h-3.5 w-3.5 text-slate-500 group-hover:text-slate-300" />
            </Link>

            <Link
              to="/logs"
              className="flex items-center justify-between p-2.5 rounded-lg border border-slate-800 bg-slate-950/40 hover:border-emerald-500/40 hover:bg-slate-800/40 transition-all text-xs text-slate-300 group"
            >
              <div className="flex items-center gap-2.5">
                <FileText className="h-4 w-4 text-emerald-400 group-hover:scale-110 transition-transform" />
                <div>
                  <span className="font-medium text-slate-200 block">Structured Log Stream</span>
                  <span className="text-[10px] text-slate-400">Query OTel collector logs</span>
                </div>
              </div>
              <ChevronRight className="h-3.5 w-3.5 text-slate-500 group-hover:text-slate-300" />
            </Link>

            <div className="pt-2 border-t border-slate-800">
              <div className="flex items-center justify-between text-[11px] text-slate-400">
                <span className="flex items-center gap-1.5">
                  <span className="h-2 w-2 rounded-full bg-emerald-400 animate-pulse" />
                  OTel Pipeline Ingestion
                </span>
                <span className="font-semibold text-slate-300">Active</span>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
