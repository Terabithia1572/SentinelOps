import React, { useState, useEffect, useCallback } from "react";
import apiClient, {
  type ApiResponse,
  type MetricSeriesResponse,
  type ApplicationDto,
  type LogMetadataDto,
} from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorState } from "@/components/ui/error-state";
import {
  RefreshCw,
  TrendingUp,
  Activity,
  AlertTriangle,
  Clock,
  Zap,
} from "lucide-react";
import {
  ResponsiveContainer,
  AreaChart,
  Area,
  LineChart,
  Line,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
} from "recharts";

export function MetricsPage() {
  // Filters
  const [applications, setApplications] = useState<ApplicationDto[]>([]);
  const [selectedAppId, setSelectedAppId] = useState<string>("");
  const [selectedService, setSelectedService] = useState<string>("");
  const [selectedEnv, setSelectedEnv] = useState<string>("");
  const [timeWindow, setTimeWindow] = useState<string>("1h");
  const [bucketMinutes, setBucketMinutes] = useState<number>(5);

  // Metadata
  const [servicesList, setServicesList] = useState<string[]>([]);
  const [envsList, setEnvsList] = useState<string[]>([]);

  // Data
  const [metricsData, setMetricsData] = useState<MetricSeriesResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Load apps
  useEffect(() => {
    async function loadApps() {
      try {
        const res = await apiClient.get<ApiResponse<ApplicationDto[]>>("/applications");
        if (res.data.success && res.data.data) {
          setApplications(res.data.data);
        }
      } catch {
        // Handled silently
      }
    }
    loadApps();
  }, []);

  // Load metadata
  useEffect(() => {
    async function loadMetadata() {
      try {
        const params: Record<string, string> = {};
        if (selectedAppId) params.applicationId = selectedAppId;
        const res = await apiClient.get<ApiResponse<LogMetadataDto>>("/logs/metadata", { params });
        if (res.data.success && res.data.data) {
          setServicesList(res.data.data.services || []);
          setEnvsList(res.data.data.environments || []);
        }
      } catch {
        // Handled silently
      }
    }
    loadMetadata();
  }, [selectedAppId]);

  // Compute time range
  const getTimeRangeUtc = useCallback(() => {
    const toUtc = new Date();
    let fromUtc: Date;
    switch (timeWindow) {
      case "15m":
        fromUtc = new Date(toUtc.getTime() - 15 * 60 * 1000);
        break;
      case "1h":
        fromUtc = new Date(toUtc.getTime() - 60 * 60 * 1000);
        break;
      case "6h":
        fromUtc = new Date(toUtc.getTime() - 6 * 60 * 60 * 1000);
        break;
      case "24h":
        fromUtc = new Date(toUtc.getTime() - 24 * 60 * 60 * 1000);
        break;
      case "7d":
        fromUtc = new Date(toUtc.getTime() - 7 * 24 * 60 * 60 * 1000);
        break;
      default:
        fromUtc = new Date(toUtc.getTime() - 60 * 60 * 1000);
    }
    return { fromUtc: fromUtc.toISOString(), toUtc: toUtc.toISOString() };
  }, [timeWindow]);

  // Fetch metrics time series
  const fetchMetrics = useCallback(async () => {
    try {
      setIsLoading(true);
      setErrorMessage(null);

      const { fromUtc, toUtc } = getTimeRangeUtc();
      const params: Record<string, string | number> = {
        fromUtc,
        toUtc,
        bucketMinutes,
      };

      if (selectedAppId) params.applicationId = selectedAppId;
      if (selectedService) params.serviceName = selectedService;
      if (selectedEnv) params.environmentName = selectedEnv;

      const res = await apiClient.get<ApiResponse<MetricSeriesResponse>>("/metrics/timeseries", { params });
      if (res.data.success && res.data.data) {
        setMetricsData(res.data.data);
      }
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : "Failed to load metrics data.");
    } finally {
      setIsLoading(false);
    }
  }, [selectedAppId, selectedService, selectedEnv, bucketMinutes, getTimeRangeUtc]);

  useEffect(() => {
    fetchMetrics();
  }, [fetchMetrics]);

  // Format chart time labels
  const formattedChartPoints = React.useMemo(() => {
    if (!metricsData?.points) return [];
    return metricsData.points.map((p) => {
      const d = new Date(p.timestamp);
      const label =
        timeWindow === "7d" || timeWindow === "24h"
          ? `${d.toLocaleDateString([], { month: "numeric", day: "numeric" })} ${d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}`
          : d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
      return {
        ...p,
        timeLabel: label,
      };
    });
  }, [metricsData, timeWindow]);

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-2xl font-bold tracking-tight">Metrics Explorer</h1>
            <Badge variant="outline" className="text-xs">
              Time-Series Analytics
            </Badge>
          </div>
          <p className="text-sm text-[hsl(var(--muted-foreground))]">
            Analyze request throughput, latency percentiles (p50/p95/p99), and error rates.
          </p>
        </div>

        <Button
          variant="outline"
          size="sm"
          onClick={fetchMetrics}
          disabled={isLoading}
          className="gap-1.5 text-xs"
        >
          <RefreshCw className={`h-3.5 w-3.5 ${isLoading ? "animate-spin" : ""}`} />
          Refresh
        </Button>
      </div>

      {/* Filter Toolbar */}
      <Card>
        <CardContent className="p-4">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 md:grid-cols-5">
            {/* Application */}
            <div>
              <label className="text-xs font-medium text-[hsl(var(--muted-foreground))] mb-1 block">
                Application
              </label>
              <select
                value={selectedAppId}
                onChange={(e) => setSelectedAppId(e.target.value)}
                className="w-full rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--background))] px-3 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-[hsl(var(--primary))]"
              >
                <option value="">All Applications</option>
                {applications.map((app) => (
                  <option key={app.id} value={app.id}>
                    {app.displayName || app.name}
                  </option>
                ))}
              </select>
            </div>

            {/* Service */}
            <div>
              <label className="text-xs font-medium text-[hsl(var(--muted-foreground))] mb-1 block">Service</label>
              <select
                value={selectedService}
                onChange={(e) => setSelectedService(e.target.value)}
                className="w-full rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--background))] px-3 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-[hsl(var(--primary))]"
              >
                <option value="">All Services</option>
                {servicesList.map((svc) => (
                  <option key={svc} value={svc}>
                    {svc}
                  </option>
                ))}
              </select>
            </div>

            {/* Environment */}
            <div>
              <label className="text-xs font-medium text-[hsl(var(--muted-foreground))] mb-1 block">
                Environment
              </label>
              <select
                value={selectedEnv}
                onChange={(e) => setSelectedEnv(e.target.value)}
                className="w-full rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--background))] px-3 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-[hsl(var(--primary))]"
              >
                <option value="">All Environments</option>
                {envsList.map((env) => (
                  <option key={env} value={env}>
                    {env}
                  </option>
                ))}
              </select>
            </div>

            {/* Time Window */}
            <div>
              <label className="text-xs font-medium text-[hsl(var(--muted-foreground))] mb-1 block">Time Window</label>
              <select
                value={timeWindow}
                onChange={(e) => setTimeWindow(e.target.value)}
                className="w-full rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--background))] px-3 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-[hsl(var(--primary))]"
              >
                <option value="15m">Last 15 minutes</option>
                <option value="1h">Last 1 hour</option>
                <option value="6h">Last 6 hours</option>
                <option value="24h">Last 24 hours</option>
                <option value="7d">Last 7 days</option>
              </select>
            </div>

            {/* Bucket Step */}
            <div>
              <label className="text-xs font-medium text-[hsl(var(--muted-foreground))] mb-1 block">
                Resolution Bucket
              </label>
              <select
                value={bucketMinutes}
                onChange={(e) => setBucketMinutes(Number(e.target.value))}
                className="w-full rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--background))] px-3 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-[hsl(var(--primary))]"
              >
                <option value={1}>1 Minute</option>
                <option value={5}>5 Minutes</option>
                <option value={15}>15 Minutes</option>
                <option value={60}>1 Hour</option>
              </select>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Error Message */}
      {errorMessage && (
        <ErrorState
          title="Metrics Aggregation Failed"
          message={errorMessage}
          onRetry={fetchMetrics}
        />
      )}

      {/* Aggregate Stats Cards */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-xs font-medium text-[hsl(var(--muted-foreground))]">
              Total Requests
            </CardTitle>
            <Activity className="h-4 w-4 text-[hsl(var(--primary))]" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold font-mono">
              {(metricsData?.totalRequests ?? 0).toLocaleString()}
            </div>
            <p className="text-[11px] text-[hsl(var(--muted-foreground))] mt-1">in selected window</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-xs font-medium text-[hsl(var(--muted-foreground))]">
              Throughput (RPS)
            </CardTitle>
            <Zap className="h-4 w-4 text-amber-400" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold font-mono text-amber-400">
              {(metricsData?.totalThroughputRps ?? 0).toFixed(2)}
            </div>
            <p className="text-[11px] text-[hsl(var(--muted-foreground))] mt-1">requests / second</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-xs font-medium text-[hsl(var(--muted-foreground))]">
              Avg Latency
            </CardTitle>
            <Clock className="h-4 w-4 text-emerald-400" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold font-mono text-emerald-400">
              {(metricsData?.avgLatencyMs ?? 0).toFixed(1)} <span className="text-sm font-normal">ms</span>
            </div>
            <p className="text-[11px] text-[hsl(var(--muted-foreground))] mt-1">mean duration</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-xs font-medium text-[hsl(var(--muted-foreground))]">
              p95 Latency
            </CardTitle>
            <Clock className="h-4 w-4 text-indigo-400" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold font-mono text-indigo-400">
              {(metricsData?.p95LatencyMs ?? 0).toFixed(1)} <span className="text-sm font-normal">ms</span>
            </div>
            <p className="text-[11px] text-[hsl(var(--muted-foreground))] mt-1">95th percentile</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-xs font-medium text-[hsl(var(--muted-foreground))]">
              Error Rate
            </CardTitle>
            <AlertTriangle className="h-4 w-4 text-rose-400" />
          </CardHeader>
          <CardContent>
            <div
              className={`text-2xl font-bold font-mono ${
                (metricsData?.overallErrorRate ?? 0) > 0 ? "text-rose-400" : "text-emerald-400"
              }`}
            >
              {(metricsData?.overallErrorRate ?? 0).toFixed(2)}%
            </div>
            <p className="text-[11px] text-[hsl(var(--muted-foreground))] mt-1">failed requests</p>
          </CardContent>
        </Card>
      </div>

      {/* Chart 1: Latency Percentiles */}
      <Card>
        <CardHeader className="p-4 pb-2">
          <CardTitle className="text-sm font-semibold flex items-center gap-2">
            <TrendingUp className="h-4 w-4 text-indigo-400" />
            Response Time Percentiles (ms)
          </CardTitle>
          <CardDescription className="text-xs">
            Comparison of Mean, p50, p95, and p99 latency across resolution buckets
          </CardDescription>
        </CardHeader>
        <CardContent className="p-4 pt-2">
          {isLoading && formattedChartPoints.length === 0 ? (
            <Skeleton className="h-72 w-full rounded-lg" />
          ) : formattedChartPoints.length === 0 ? (
            <EmptyState
              icon={TrendingUp}
              title="No latency metric datapoints"
              description="No timeseries telemetry was captured in the selected resolution bucket and time window."
            />
          ) : (
            <div className="h-72 w-full">
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={formattedChartPoints} margin={{ top: 10, right: 30, left: 0, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" opacity={0.6} />
                  <XAxis dataKey="timeLabel" stroke="hsl(var(--muted-foreground))" fontSize={11} tickLine={false} />
                  <YAxis stroke="hsl(var(--muted-foreground))" fontSize={11} unit=" ms" tickLine={false} />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: "hsl(var(--card))",
                      borderColor: "hsl(var(--border))",
                      borderRadius: "6px",
                      fontSize: "12px",
                    }}
                  />
                  <Legend wrapperStyle={{ fontSize: "12px", paddingTop: "8px" }} />
                  <Line type="monotone" dataKey="p99DurationMs" name="p99 Latency" stroke="#f43f5e" strokeWidth={2} dot={false} />
                  <Line type="monotone" dataKey="p95DurationMs" name="p95 Latency" stroke="#a855f7" strokeWidth={2} dot={false} />
                  <Line type="monotone" dataKey="p50DurationMs" name="p50 (Median)" stroke="#3b82f6" strokeWidth={1.5} dot={false} />
                  <Line type="monotone" dataKey="avgDurationMs" name="Avg Latency" stroke="#10b981" strokeWidth={1.5} strokeDasharray="3 3" dot={false} />
                </LineChart>
              </ResponsiveContainer>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Chart 2: Throughput (RPS) & Error Rate Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Throughput Area Chart */}
        <Card>
          <CardHeader className="p-4 pb-2">
            <CardTitle className="text-sm font-semibold flex items-center gap-2">
              <Zap className="h-4 w-4 text-amber-400" />
              Request Rate (RPS)
            </CardTitle>
            <CardDescription className="text-xs">Requests handled per second</CardDescription>
          </CardHeader>
          <CardContent className="p-4 pt-2">
            {formattedChartPoints.length === 0 ? (
              <div className="flex h-56 items-center justify-center text-xs text-[hsl(var(--muted-foreground))]">
                No throughput data in window.
              </div>
            ) : (
              <div className="h-60 w-full">
                <ResponsiveContainer width="100%" height="100%">
                  <AreaChart data={formattedChartPoints} margin={{ top: 10, right: 20, left: 0, bottom: 0 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" opacity={0.6} />
                    <XAxis dataKey="timeLabel" stroke="hsl(var(--muted-foreground))" fontSize={11} tickLine={false} />
                    <YAxis stroke="hsl(var(--muted-foreground))" fontSize={11} tickLine={false} />
                    <Tooltip
                      contentStyle={{
                        backgroundColor: "hsl(var(--card))",
                        borderColor: "hsl(var(--border))",
                        borderRadius: "6px",
                        fontSize: "12px",
                      }}
                    />
                    <Area
                      type="monotone"
                      dataKey="requestsPerSecond"
                      name="RPS"
                      stroke="#f59e0b"
                      fill="#f59e0b"
                      fillOpacity={0.2}
                    />
                  </AreaChart>
                </ResponsiveContainer>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Error Rate Bar Chart */}
        <Card>
          <CardHeader className="p-4 pb-2">
            <CardTitle className="text-sm font-semibold flex items-center gap-2">
              <AlertTriangle className="h-4 w-4 text-rose-400" />
              Error Rate (%) &amp; Failures
            </CardTitle>
            <CardDescription className="text-xs">
              Percentage of transactions failing in each time bucket
            </CardDescription>
          </CardHeader>
          <CardContent className="p-4 pt-2">
            {formattedChartPoints.length === 0 ? (
              <div className="flex h-56 items-center justify-center text-xs text-[hsl(var(--muted-foreground))]">
                No error data in window.
              </div>
            ) : (
              <div className="h-60 w-full">
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={formattedChartPoints} margin={{ top: 10, right: 20, left: 0, bottom: 0 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" opacity={0.6} />
                    <XAxis dataKey="timeLabel" stroke="hsl(var(--muted-foreground))" fontSize={11} tickLine={false} />
                    <YAxis stroke="hsl(var(--muted-foreground))" fontSize={11} unit="%" tickLine={false} />
                    <Tooltip
                      contentStyle={{
                        backgroundColor: "hsl(var(--card))",
                        borderColor: "hsl(var(--border))",
                        borderRadius: "6px",
                        fontSize: "12px",
                      }}
                    />
                    <Bar dataKey="errorRatePercent" name="Error Rate %" fill="#ef4444" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
