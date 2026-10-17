import { useState, useEffect, useCallback, useRef } from "react";
import { Link, useSearchParams } from "react-router-dom";
import apiClient, {
  type ApiResponse,
  type PagedResponse,
  type LogEntryDto,
  type LogMetadataDto,
  type ApplicationDto,
} from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorState } from "@/components/ui/error-state";
import {
  Search,
  RefreshCw,
  FileText,
  GitBranch,
  ChevronRight,
  ChevronDown,
  Copy,
  Check,
  Pause,
} from "lucide-react";

const SEVERITIES = ["All", "Trace", "Debug", "Information", "Warning", "Error", "Critical"] as const;

export function LogsPage() {
  const [searchParams] = useSearchParams();

  // Filters state
  const [applications, setApplications] = useState<ApplicationDto[]>([]);
  const [selectedAppId, setSelectedAppId] = useState<string>(searchParams.get("applicationId") || "");
  const [selectedService, setSelectedService] = useState<string>(searchParams.get("service") || "");
  const [selectedEnv, setSelectedEnv] = useState<string>(searchParams.get("env") || "");
  const [selectedSeverity, setSelectedSeverity] = useState<string>(searchParams.get("severity") || "All");
  const [searchQuery, setSearchQuery] = useState<string>(searchParams.get("q") || "");
  const [traceIdFilter, setTraceIdFilter] = useState<string>(searchParams.get("traceId") || "");
  const [timeWindow, setTimeWindow] = useState<string>("1h");

  // Metadata for filter dropdowns
  const [servicesList, setServicesList] = useState<string[]>([]);
  const [envsList, setEnvsList] = useState<string[]>([]);

  // Logs data state
  const [logs, setLogs] = useState<LogEntryDto[]>([]);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [page, setPage] = useState<number>(1);
  const pageSize = 25;
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Live auto-refresh state
  const [autoRefreshInterval, setAutoRefreshInterval] = useState<number>(0); // 0 = off, 5, 10, 30 sec
  const refreshTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  // Expanded log rows state
  const [expandedLogIds, setExpandedLogIds] = useState<Set<string>>(new Set());
  const [copiedId, setCopiedId] = useState<string | null>(null);

  // Fetch applications list on mount
  useEffect(() => {
    async function loadApps() {
      try {
        const res = await apiClient.get<ApiResponse<ApplicationDto[]>>("/applications");
        if (res.data.success && res.data.data) {
          setApplications(res.data.data);
          const appParam = searchParams.get("app");
          if (appParam) {
            const matched = res.data.data.find(
              (a) => a.name.toLowerCase() === appParam.toLowerCase() || a.id === appParam
            );
            if (matched) setSelectedAppId(matched.id);
          }
        }
      } catch {
        // Handled silently
      }
    }
    loadApps();
  }, [searchParams]);

  // Fetch metadata whenever selectedAppId changes
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

  // Calculate timestamp range
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

  // Main fetch logs function
  const fetchLogs = useCallback(
    async (targetPage = 1, silent = false) => {
      try {
        if (!silent) setIsLoading(true);
        setErrorMessage(null);

        const { fromUtc, toUtc } = getTimeRangeUtc();
        const params: Record<string, string | number> = {
          page: targetPage,
          pageSize,
          fromUtc,
          toUtc,
        };

        if (selectedAppId) params.applicationId = selectedAppId;
        if (selectedService) params.serviceName = selectedService;
        if (selectedEnv) params.environmentName = selectedEnv;
        if (selectedSeverity !== "All") params.severity = selectedSeverity;
        if (searchQuery.trim()) params.search = searchQuery.trim();
        if (traceIdFilter.trim()) params.traceId = traceIdFilter.trim();

        const res = await apiClient.get<ApiResponse<PagedResponse<LogEntryDto>>>("/logs", { params });
        if (res.data.success && res.data.data) {
          setLogs(res.data.data.items || []);
          setTotalCount(res.data.data.totalCount || 0);
          setPage(targetPage);
        }
      } catch (err: unknown) {
        setErrorMessage(err instanceof Error ? err.message : "Failed to load log entries.");
      } finally {
        if (!silent) setIsLoading(false);
      }
    },
    [selectedAppId, selectedService, selectedEnv, selectedSeverity, searchQuery, traceIdFilter, getTimeRangeUtc]
  );

  // Trigger search on filter change
  useEffect(() => {
    fetchLogs(1);
  }, [fetchLogs]);

  // Auto-refresh interval effect
  useEffect(() => {
    if (refreshTimerRef.current) {
      clearInterval(refreshTimerRef.current);
      refreshTimerRef.current = null;
    }

    if (autoRefreshInterval > 0) {
      refreshTimerRef.current = setInterval(() => {
        fetchLogs(page, true);
      }, autoRefreshInterval * 1000);
    }

    return () => {
      if (refreshTimerRef.current) {
        clearInterval(refreshTimerRef.current);
      }
    };
  }, [autoRefreshInterval, fetchLogs, page]);

  const toggleRowExpanded = (id: string) => {
    setExpandedLogIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const copyToClipboard = (text: string, id: string) => {
    navigator.clipboard.writeText(text);
    setCopiedId(id);
    setTimeout(() => setCopiedId(null), 2000);
  };

  const getSeverityBadgeVariant = (severity: string) => {
    switch (severity?.toLowerCase()) {
      case "critical":
      case "error":
        return "danger";
      case "warning":
        return "warning";
      case "information":
      case "info":
        return "secondary";
      case "debug":
      case "trace":
        return "outline";
      default:
        return "secondary";
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-2xl font-bold tracking-tight">Log Explorer</h1>
            <Badge variant="outline" className="text-xs">
              Live Stream
            </Badge>
          </div>
          <p className="text-sm text-[hsl(var(--muted-foreground))]">
            Search, filter, and inspect structured application telemetry logs with correlated traces.
          </p>
        </div>

        {/* Live Refresh Controls */}
        <div className="flex items-center gap-2">
          <div className="flex items-center rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] p-1 text-xs">
            <Button
              variant="ghost"
              size="sm"
              className={`h-7 px-2 text-xs gap-1.5 ${autoRefreshInterval === 0 ? "font-semibold" : ""}`}
              onClick={() => setAutoRefreshInterval(0)}
            >
              <Pause className="h-3 w-3" /> Manual
            </Button>
            <Button
              variant={autoRefreshInterval === 5 ? "secondary" : "ghost"}
              size="sm"
              className="h-7 px-2 text-xs"
              onClick={() => setAutoRefreshInterval(5)}
            >
              5s
            </Button>
            <Button
              variant={autoRefreshInterval === 10 ? "secondary" : "ghost"}
              size="sm"
              className="h-7 px-2 text-xs"
              onClick={() => setAutoRefreshInterval(10)}
            >
              10s
            </Button>
            <Button
              variant={autoRefreshInterval === 30 ? "secondary" : "ghost"}
              size="sm"
              className="h-7 px-2 text-xs"
              onClick={() => setAutoRefreshInterval(30)}
            >
              30s
            </Button>
          </div>

          <Button
            variant="outline"
            size="sm"
            onClick={() => fetchLogs(page)}
            disabled={isLoading}
            className="gap-1.5 text-xs"
          >
            <RefreshCw className={`h-3.5 w-3.5 ${isLoading ? "animate-spin" : ""}`} />
            Refresh
          </Button>
        </div>
      </div>

      {/* Filter Toolbar */}
      <Card>
        <CardContent className="p-4 space-y-4">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 md:grid-cols-4 lg:grid-cols-6">
            {/* Application Selector */}
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

            {/* Service Selector */}
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

            {/* Environment Selector */}
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
              <label className="text-xs font-medium text-[hsl(var(--muted-foreground))] mb-1 block">Time Range</label>
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

            {/* Trace ID Filter */}
            <div className="md:col-span-2">
              <label className="text-xs font-medium text-[hsl(var(--muted-foreground))] mb-1 block">
                Trace ID
              </label>
              <div className="relative">
                <GitBranch className="absolute left-2.5 top-2 h-3.5 w-3.5 text-[hsl(var(--muted-foreground))]" />
                <Input
                  placeholder="Filter by Trace ID..."
                  value={traceIdFilter}
                  onChange={(e) => setTraceIdFilter(e.target.value)}
                  className="pl-8 h-8 text-xs font-mono"
                />
              </div>
            </div>
          </div>

          {/* Search bar & Severity Badges */}
          <div className="flex flex-col gap-3 pt-2 border-t border-[hsl(var(--border))] sm:flex-row sm:items-center sm:justify-between">
            <div className="relative flex-1 max-w-md">
              <Search className="absolute left-2.5 top-2 h-3.5 w-3.5 text-[hsl(var(--muted-foreground))]" />
              <Input
                placeholder="Search messages, templates, or exceptions..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                onKeyDown={(e) => e.key === "Enter" && fetchLogs(1)}
                className="pl-8 h-8 text-xs"
              />
            </div>

            {/* Severity Pill Buttons */}
            <div className="flex flex-wrap items-center gap-1">
              <span className="text-xs text-[hsl(var(--muted-foreground))] mr-1">Severity:</span>
              {SEVERITIES.map((sev) => {
                const isActive = selectedSeverity === sev;
                return (
                  <button
                    key={sev}
                    onClick={() => setSelectedSeverity(sev)}
                    className={`rounded px-2 py-0.5 text-xs font-medium transition-colors ${
                      isActive
                        ? "bg-[hsl(var(--primary))] text-[hsl(var(--primary-foreground))]"
                        : "bg-[hsl(var(--secondary))] text-[hsl(var(--muted-foreground))] hover:bg-[hsl(var(--accent))]"
                    }`}
                  >
                    {sev}
                  </button>
                );
              })}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Error Banner */}
      {errorMessage && (
        <ErrorState
          title="Telemetry Stream Ingestion Failed"
          message={errorMessage}
          onRetry={() => fetchLogs(page)}
        />
      )}

      {/* Logs Table */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between pb-2 pt-4 px-4">
          <CardTitle className="text-sm font-semibold flex items-center gap-2">
            <FileText className="h-4 w-4 text-[hsl(var(--primary))]" />
            Logs Stream
            <span className="text-xs font-normal text-[hsl(var(--muted-foreground))]">
              ({totalCount.toLocaleString()} total entries)
            </span>
          </CardTitle>
          <div className="text-xs text-[hsl(var(--muted-foreground))]">
            Page {page} of {Math.max(1, totalPages)}
          </div>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading && logs.length === 0 ? (
            <div className="p-4 space-y-3">
              {Array.from({ length: 6 }).map((_, i) => (
                <div key={i} className="flex items-center gap-3">
                  <Skeleton className="h-4 w-28" />
                  <Skeleton className="h-4 w-16" />
                  <Skeleton className="h-4 w-24" />
                  <Skeleton className="h-4 flex-1" />
                </div>
              ))}
            </div>
          ) : logs.length === 0 ? (
            <div className="p-6">
              <EmptyState
                icon={FileText}
                title="No log entries found"
                description="No telemetry logs matched the active application, severity, or temporal filters."
                actionLabel="Clear Filter Query"
                onAction={() => {
                  setSelectedAppId("");
                  setSelectedService("");
                  setSelectedSeverity("All");
                  setSearchQuery("");
                  setTraceIdFilter("");
                  fetchLogs(1);
                }}
              />
            </div>
          ) : (
            <div className="divide-y divide-[hsl(var(--border))] font-mono text-xs overflow-x-auto">
              {logs.map((log) => {
                const isExpanded = expandedLogIds.has(log.id);
                return (
                  <div key={log.id} className="transition-colors hover:bg-[hsl(var(--accent)/0.3)]">
                    {/* Log Row Summary */}
                    <div
                      className="flex cursor-pointer items-start gap-3 p-3 select-none min-w-[600px] md:min-w-0"
                      onClick={() => toggleRowExpanded(log.id)}
                    >
                      <button className="mt-0.5 text-[hsl(var(--muted-foreground))] hover:text-[hsl(var(--foreground))]">
                        {isExpanded ? (
                          <ChevronDown className="h-3.5 w-3.5" />
                        ) : (
                          <ChevronRight className="h-3.5 w-3.5" />
                        )}
                      </button>

                      {/* Timestamp */}
                      <span className="shrink-0 text-[11px] text-[hsl(var(--muted-foreground))] w-36">
                        {new Date(log.timestamp).toLocaleTimeString([], {
                          hour: "2-digit",
                          minute: "2-digit",
                          second: "2-digit",
                          fractionalSecondDigits: 3,
                        })}
                      </span>

                      {/* Severity Badge */}
                      <div className="shrink-0 w-20">
                        <Badge
                          variant={getSeverityBadgeVariant(log.severity)}
                          className="text-[10px] uppercase font-bold py-0 h-4 px-1.5"
                        >
                          {log.severity}
                        </Badge>
                      </div>

                      {/* Service & Env */}
                      <span className="shrink-0 text-amber-400 font-semibold w-32 truncate" title={log.serviceName}>
                        {log.serviceName}
                      </span>
                      <span className="shrink-0 text-[hsl(var(--muted-foreground))] w-24 truncate hidden md:inline">
                        [{log.environmentName}]
                      </span>

                      {/* Message preview */}
                      <span className="flex-1 truncate text-[hsl(var(--foreground))] font-normal">
                        {log.message}
                      </span>

                      {/* Trace link pill */}
                      {log.traceId && (
                        <Link
                          to={`/traces?traceId=${log.traceId}`}
                          onClick={(e) => e.stopPropagation()}
                          className="shrink-0 flex items-center gap-1 rounded bg-[hsl(var(--primary)/0.15)] px-1.5 py-0.5 text-[10px] text-[hsl(var(--primary))] hover:bg-[hsl(var(--primary)/0.3)] transition-colors"
                          title="View Trace Waterfall"
                        >
                          <GitBranch className="h-3 w-3" />
                          <span className="hidden sm:inline">Trace</span>
                        </Link>
                      )}
                    </div>

                    {/* Expanded Detail Panel */}
                    {isExpanded && (
                      <div className="bg-[hsl(var(--background))] p-4 border-t border-[hsl(var(--border))] space-y-3 font-sans text-xs">
                        {/* Meta Tags */}
                        <div className="grid grid-cols-2 md:grid-cols-4 gap-2 text-xs">
                          <div>
                            <span className="text-[hsl(var(--muted-foreground))] block">Log ID:</span>
                            <span className="font-mono text-[11px] select-all">{log.id}</span>
                          </div>
                          <div>
                            <span className="text-[hsl(var(--muted-foreground))] block">Service:</span>
                            <span className="font-medium">{log.serviceName}</span>
                          </div>
                          <div>
                            <span className="text-[hsl(var(--muted-foreground))] block">Environment:</span>
                            <span>{log.environmentName}</span>
                          </div>
                          <div>
                            <span className="text-[hsl(var(--muted-foreground))] block">Timestamp (UTC):</span>
                            <span className="font-mono text-[11px]">
                              {new Date(log.timestamp).toISOString()}
                            </span>
                          </div>
                          {log.traceId && (
                            <div>
                              <span className="text-[hsl(var(--muted-foreground))] block">Trace ID:</span>
                              <Link
                                to={`/traces?traceId=${log.traceId}`}
                                className="font-mono text-[11px] text-[hsl(var(--primary))] hover:underline flex items-center gap-1"
                              >
                                {log.traceId}
                              </Link>
                            </div>
                          )}
                          {log.spanId && (
                            <div>
                              <span className="text-[hsl(var(--muted-foreground))] block">Span ID:</span>
                              <span className="font-mono text-[11px]">{log.spanId}</span>
                            </div>
                          )}
                        </div>

                        {/* Full Message */}
                        <div>
                          <div className="flex items-center justify-between mb-1">
                            <span className="font-semibold text-xs text-[hsl(var(--muted-foreground))]">
                              Full Message:
                            </span>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="h-5 px-1.5 text-[10px] gap-1"
                              onClick={() => copyToClipboard(log.message, `msg-${log.id}`)}
                            >
                              {copiedId === `msg-${log.id}` ? (
                                <Check className="h-3 w-3 text-emerald-400" />
                              ) : (
                                <Copy className="h-3 w-3" />
                              )}
                              Copy
                            </Button>
                          </div>
                          <div className="rounded bg-[hsl(var(--card))] p-2.5 font-mono text-[11px] text-[hsl(var(--foreground))] whitespace-pre-wrap break-all border border-[hsl(var(--border))]">
                            {log.message}
                          </div>
                        </div>

                        {/* Exception Detail */}
                        {log.exceptionDetail && (
                          <div>
                            <div className="flex items-center justify-between mb-1">
                              <span className="font-semibold text-xs text-rose-400">
                                Exception Stack Trace:
                              </span>
                              <Button
                                variant="ghost"
                                size="sm"
                                className="h-5 px-1.5 text-[10px] gap-1 text-rose-400"
                                onClick={() => copyToClipboard(log.exceptionDetail!, `exc-${log.id}`)}
                              >
                                {copiedId === `exc-${log.id}` ? (
                                  <Check className="h-3 w-3 text-emerald-400" />
                                ) : (
                                  <Copy className="h-3 w-3" />
                                )}
                                Copy Stack
                              </Button>
                            </div>
                            <pre className="rounded bg-rose-950/20 p-2.5 font-mono text-[11px] text-rose-300 whitespace-pre-wrap break-all border border-rose-500/20 overflow-x-auto max-h-60">
                              {log.exceptionDetail}
                            </pre>
                          </div>
                        )}

                        {/* Structured Properties JSON */}
                        {log.properties && log.properties !== "{}" && (
                          <div>
                            <div className="flex items-center justify-between mb-1">
                              <span className="font-semibold text-xs text-[hsl(var(--muted-foreground))]">
                                Structured Properties (JSON):
                              </span>
                              <Button
                                variant="ghost"
                                size="sm"
                                className="h-5 px-1.5 text-[10px] gap-1"
                                onClick={() => copyToClipboard(log.properties!, `props-${log.id}`)}
                              >
                                {copiedId === `props-${log.id}` ? (
                                  <Check className="h-3 w-3 text-emerald-400" />
                                ) : (
                                  <Copy className="h-3 w-3" />
                                )}
                                Copy JSON
                              </Button>
                            </div>
                            <pre className="rounded bg-[hsl(var(--card))] p-2.5 font-mono text-[11px] text-[hsl(var(--foreground))] whitespace-pre-wrap break-all border border-[hsl(var(--border))] max-h-48 overflow-y-auto">
                              {(() => {
                                try {
                                  return JSON.stringify(JSON.parse(log.properties), null, 2);
                                } catch {
                                  return log.properties;
                                }
                              })()}
                            </pre>
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </CardContent>

        {/* Pagination Footer */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between border-t border-[hsl(var(--border))] p-4">
            <div className="text-xs text-[hsl(var(--muted-foreground))]">
              Showing {(page - 1) * pageSize + 1} to {Math.min(page * pageSize, totalCount)} of{" "}
              {totalCount.toLocaleString()}
            </div>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => fetchLogs(page - 1)}
                disabled={page <= 1 || isLoading}
                className="text-xs h-8"
              >
                Previous
              </Button>
              <span className="text-xs font-medium px-2">
                {page} / {totalPages}
              </span>
              <Button
                variant="outline"
                size="sm"
                onClick={() => fetchLogs(page + 1)}
                disabled={page >= totalPages || isLoading}
                className="text-xs h-8"
              >
                Next
              </Button>
            </div>
          </div>
        )}
      </Card>
    </div>
  );
}
