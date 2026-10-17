import { useState, useEffect, useCallback } from "react";
import { useSearchParams, Link } from "react-router-dom";
import apiClient, {
  type ApiResponse,
  type PagedResponse,
  type TraceDto,
  type TraceDetailDto,
  type SpanDetailDto,
  type ApplicationDto,
  type LogMetadataDto,
} from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorState } from "@/components/ui/error-state";
import {
  GitBranch,
  Search,
  RefreshCw,
  Layers,
  CheckCircle2,
  XCircle,
  ChevronRight,
  ArrowLeft,
  Copy,
  Check,
  FileText,
  Activity,
} from "lucide-react";

export function TracesPage() {
  const [searchParams, setSearchParams] = useSearchParams();

  // Filters
  const [applications, setApplications] = useState<ApplicationDto[]>([]);
  const [selectedAppId, setSelectedAppId] = useState<string>(searchParams.get("applicationId") || "");
  const [selectedService, setSelectedService] = useState<string>(searchParams.get("service") || "");
  const [errorsOnly, setErrorsOnly] = useState<boolean>(searchParams.get("hasErrors") === "true");
  const [searchQuery, setSearchQuery] = useState<string>(searchParams.get("q") || "");
  const [minDurationMs, setMinDurationMs] = useState<string>(searchParams.get("minDuration") || "");

  // Metadata
  const [servicesList, setServicesList] = useState<string[]>([]);

  // List data
  const [traces, setTraces] = useState<TraceDto[]>([]);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [page, setPage] = useState<number>(1);
  const pageSize = 20;
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Detail / Waterfall State
  const [selectedTraceId, setSelectedTraceId] = useState<string | null>(searchParams.get("traceId") || null);
  const [traceDetail, setTraceDetail] = useState<TraceDetailDto | null>(null);
  const [isLoadingDetail, setIsLoadingDetail] = useState<boolean>(false);
  const [selectedSpan, setSelectedSpan] = useState<SpanDetailDto | null>(null);
  const [copiedId, setCopiedId] = useState<string | null>(null);

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
        }
      } catch {
        // Handled silently
      }
    }
    loadMetadata();
  }, [selectedAppId]);

  // Fetch trace list
  const fetchTraces = useCallback(
    async (targetPage = 1) => {
      try {
        setIsLoading(true);
        setErrorMessage(null);

        const params: Record<string, string | number | boolean> = {
          page: targetPage,
          pageSize,
        };

        if (selectedAppId) params.applicationId = selectedAppId;
        if (selectedService) params.serviceName = selectedService;
        if (errorsOnly) params.hasErrors = true;
        if (searchQuery.trim()) params.search = searchQuery.trim();
        if (minDurationMs && !isNaN(Number(minDurationMs))) params.minDurationMs = Number(minDurationMs);

        const res = await apiClient.get<ApiResponse<PagedResponse<TraceDto>>>("/traces", { params });
        if (res.data.success && res.data.data) {
          setTraces(res.data.data.items || []);
          setTotalCount(res.data.data.totalCount || 0);
          setPage(targetPage);
        }
      } catch (err: unknown) {
        setErrorMessage(err instanceof Error ? err.message : "Failed to load traces.");
      } finally {
        setIsLoading(false);
      }
    },
    [selectedAppId, selectedService, errorsOnly, searchQuery, minDurationMs]
  );

  useEffect(() => {
    fetchTraces(1);
  }, [fetchTraces]);

  // Fetch single trace detail
  const fetchTraceDetail = useCallback(async (traceId: string) => {
    try {
      setIsLoadingDetail(true);
      setSelectedSpan(null);
      const res = await apiClient.get<ApiResponse<TraceDetailDto>>(`/traces/${traceId}`);
      if (res.data.success && res.data.data) {
        setTraceDetail(res.data.data);
        if (res.data.data.spans.length > 0) {
          setSelectedSpan(res.data.data.spans[0]);
        }
      }
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : `Failed to load trace ${traceId}`);
    } finally {
      setIsLoadingDetail(false);
    }
  }, []);

  // Update selectedTraceId when URL param changes or clicked
  useEffect(() => {
    const paramId = searchParams.get("traceId");
    if (paramId && paramId !== selectedTraceId) {
      setSelectedTraceId(paramId);
      fetchTraceDetail(paramId);
    } else if (!paramId && selectedTraceId) {
      setSelectedTraceId(null);
      setTraceDetail(null);
    }
  }, [searchParams, fetchTraceDetail, selectedTraceId]);

  const handleSelectTrace = (traceId: string) => {
    setSelectedTraceId(traceId);
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev);
      next.set("traceId", traceId);
      return next;
    });
    fetchTraceDetail(traceId);
  };

  const handleBackToList = () => {
    setSelectedTraceId(null);
    setTraceDetail(null);
    setSelectedSpan(null);
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev);
      next.delete("traceId");
      return next;
    });
  };

  const copyToClipboard = (text: string, id: string) => {
    navigator.clipboard.writeText(text);
    setCopiedId(id);
    setTimeout(() => setCopiedId(null), 2000);
  };

  const getDurationBadgeColor = (durationMs: number) => {
    if (durationMs > 1000) return "text-rose-400 bg-rose-500/10 border-rose-500/20";
    if (durationMs > 300) return "text-amber-400 bg-amber-500/10 border-amber-500/20";
    return "text-emerald-400 bg-emerald-500/10 border-emerald-500/20";
  };

  const getServiceColor = (serviceName: string) => {
    const colors = [
      "border-indigo-500/40 bg-indigo-500/10 text-indigo-400",
      "border-emerald-500/40 bg-emerald-500/10 text-emerald-400",
      "border-amber-500/40 bg-amber-500/10 text-amber-400",
      "border-cyan-500/40 bg-cyan-500/10 text-cyan-400",
      "border-purple-500/40 bg-purple-500/10 text-purple-400",
      "border-rose-500/40 bg-rose-500/10 text-rose-400",
    ];
    let hash = 0;
    for (let i = 0; i < serviceName.length; i++) {
      hash = (hash << 5) - hash + serviceName.charCodeAt(i);
    }
    const index = Math.abs(hash) % colors.length;
    return colors[index];
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  // If viewing single trace waterfall detail:
  if (selectedTraceId) {
    return (
      <div className="space-y-6">
        {/* Detail Header */}
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-3">
            <Button
              variant="outline"
              size="sm"
              onClick={handleBackToList}
              className="gap-1.5 text-xs h-8"
            >
              <ArrowLeft className="h-3.5 w-3.5" /> Back to Traces
            </Button>
            <div>
              <div className="flex items-center gap-2">
                <h1 className="text-xl font-bold tracking-tight font-mono">
                  {traceDetail?.trace.rootSpanName || "Trace Waterfall"}
                </h1>
                {traceDetail?.trace.hasErrors ? (
                  <Badge variant="danger" className="text-[10px] gap-1">
                    <XCircle className="h-3 w-3" /> Error
                  </Badge>
                ) : (
                  <Badge variant="success" className="text-[10px] gap-1">
                    <CheckCircle2 className="h-3 w-3" /> Success
                  </Badge>
                )}
              </div>
              <div className="flex items-center gap-2 text-xs text-[hsl(var(--muted-foreground))] mt-1">
                <span>Trace ID:</span>
                <span className="font-mono text-xs text-[hsl(var(--foreground))]">{selectedTraceId}</span>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-5 px-1 text-[10px]"
                  onClick={() => copyToClipboard(selectedTraceId, "trace-id")}
                >
                  {copiedId === "trace-id" ? (
                    <Check className="h-3 w-3 text-emerald-400" />
                  ) : (
                    <Copy className="h-3 w-3" />
                  )}
                </Button>
              </div>
            </div>
          </div>

          {traceDetail && (
            <div className="flex items-center gap-3 text-xs bg-[hsl(var(--card))] border border-[hsl(var(--border))] rounded-md p-2">
              <div>
                <span className="text-[hsl(var(--muted-foreground))] block">Total Duration</span>
                <span className="font-mono font-bold text-amber-400">
                  {traceDetail.totalDurationMs.toFixed(2)} ms
                </span>
              </div>
              <div className="border-l border-[hsl(var(--border))] pl-3">
                <span className="text-[hsl(var(--muted-foreground))] block">Total Spans</span>
                <span className="font-bold">{traceDetail.spans.length}</span>
              </div>
              <div className="border-l border-[hsl(var(--border))] pl-3">
                <span className="text-[hsl(var(--muted-foreground))] block">Services</span>
                <span className="font-bold">{traceDetail.services.length}</span>
              </div>
              {traceDetail.hasMissingParents && (
                <div className="border-l border-[hsl(var(--border))] pl-3">
                  <span className="text-purple-400 block font-semibold">Context</span>
                  <span className="text-[10px] text-purple-300 font-mono">Missing Parents</span>
                </div>
              )}
            </div>
          )}
        </div>

        {isLoadingDetail || !traceDetail ? (
          <div className="space-y-4">
            <Skeleton className="h-64 w-full rounded-lg" />
            <Skeleton className="h-48 w-full rounded-lg" />
          </div>
        ) : (
          <div className="space-y-6">
            {/* Waterfall Chart */}
            <Card>
              <CardHeader className="pb-3 pt-4 px-4 border-b border-[hsl(var(--border))]">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-sm font-semibold flex items-center gap-2">
                    <Activity className="h-4 w-4 text-[hsl(var(--primary))]" />
                    Span Execution Waterfall
                  </CardTitle>
                  <div className="flex items-center gap-3 text-xs text-[hsl(var(--muted-foreground))]">
                    <span>Timeline: 0 ms &rarr; {traceDetail.totalDurationMs.toFixed(1)} ms</span>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="p-0">
                {/* Timeline Header Ruler */}
                <div className="flex border-b border-[hsl(var(--border))] bg-[hsl(var(--accent)/0.2)] px-4 py-2 text-[10px] text-[hsl(var(--muted-foreground))] font-mono">
                  <div className="w-2/5 shrink-0">Service &amp; Span Operation</div>
                  <div className="w-3/5 flex justify-between px-2">
                    <span>0ms</span>
                    <span>{(traceDetail.totalDurationMs * 0.25).toFixed(0)}ms</span>
                    <span>{(traceDetail.totalDurationMs * 0.5).toFixed(0)}ms</span>
                    <span>{(traceDetail.totalDurationMs * 0.75).toFixed(0)}ms</span>
                    <span>{traceDetail.totalDurationMs.toFixed(0)}ms</span>
                  </div>
                </div>

                {/* Waterfall Span Rows */}
                <div className="divide-y divide-[hsl(var(--border))]">
                  {traceDetail.spans.map((span) => {
                    const isSelected = selectedSpan?.spanId === span.spanId;
                    const isError = span.status?.toLowerCase() === "error";

                    return (
                      <div
                        key={span.spanId}
                        onClick={() => setSelectedSpan(span)}
                        className={`flex items-center px-4 py-2 text-xs cursor-pointer transition-colors ${
                          isSelected
                            ? "bg-[hsl(var(--primary)/0.1)] border-l-2 border-l-[hsl(var(--primary))]"
                            : "hover:bg-[hsl(var(--accent)/0.3)]"
                        }`}
                      >
                        {/* Left Side: Span Details with Indentation */}
                        <div
                          className="w-2/5 shrink-0 flex items-center gap-2 truncate pr-2"
                          style={{ paddingLeft: `${span.depth * 18}px` }}
                        >
                          <span
                            className={`rounded px-1.5 py-0.5 text-[10px] font-semibold border ${getServiceColor(
                              span.serviceName
                            )}`}
                          >
                            {span.serviceName}
                          </span>
                          <span className="font-mono text-xs truncate text-[hsl(var(--foreground))]">
                            {span.operationName}
                          </span>
                          {span.isSlowestSpan && (
                            <span className="bg-amber-500/20 text-amber-300 border border-amber-500/40 rounded px-1.5 py-0.5 text-[9px] font-semibold shrink-0">
                              Slowest
                            </span>
                          )}
                          {span.isOrphanedParent && (
                            <span className="bg-purple-500/20 text-purple-300 border border-purple-500/40 rounded px-1.5 py-0.5 text-[9px] font-semibold shrink-0">
                              Orphaned
                            </span>
                          )}
                        </div>

                        {/* Right Side: Waterfall Timeline Bar */}
                        <div className="w-3/5 relative h-6 flex items-center px-2">
                          {/* Guide Gridlines */}
                          <div className="absolute inset-0 grid grid-cols-4 pointer-events-none opacity-20 border-r border-dashed border-[hsl(var(--border))]">
                            <div className="border-r border-dashed border-[hsl(var(--border))] h-full" />
                            <div className="border-r border-dashed border-[hsl(var(--border))] h-full" />
                            <div className="border-r border-dashed border-[hsl(var(--border))] h-full" />
                            <div className="h-full" />
                          </div>

                          {/* Span Bar */}
                          <div
                            className={`absolute h-4 rounded text-[10px] font-mono flex items-center px-1 text-white shadow-sm transition-all ${
                              isError ? "bg-rose-600" : "bg-[hsl(var(--primary))]"
                            }`}
                            style={{
                              left: `${span.offsetPercent}%`,
                              width: `${Math.max(span.durationPercent, 1.5)}%`,
                            }}
                            title={`${span.durationMs.toFixed(2)} ms (+${span.offsetMs.toFixed(2)} ms)`}
                          >
                            <span className="truncate text-[9px] font-semibold leading-none drop-shadow">
                              {span.durationMs >= 10 ? `${span.durationMs.toFixed(0)}ms` : ""}
                            </span>
                          </div>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </CardContent>
            </Card>

            {/* Selected Span Inspector & Correlated Logs Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {/* Selected Span Inspector */}
              <Card>
                <CardHeader className="p-4 pb-2">
                  <CardTitle className="text-sm font-semibold flex items-center justify-between">
                    <span>Span Attributes Inspector</span>
                    {selectedSpan && (
                      <Badge variant="outline" className="font-mono text-[10px]">
                        ID: {selectedSpan.spanId}
                      </Badge>
                    )}
                  </CardTitle>
                </CardHeader>
                <CardContent className="p-4 pt-2 text-xs space-y-3 font-mono">
                  {selectedSpan ? (
                    <>
                      <div className="grid grid-cols-2 gap-2 text-xs font-sans">
                        <div>
                          <span className="text-[hsl(var(--muted-foreground))] block">Operation:</span>
                          <span className="font-bold">{selectedSpan.operationName}</span>
                        </div>
                        <div>
                          <span className="text-[hsl(var(--muted-foreground))] block">Service:</span>
                          <span className="font-medium text-amber-400">{selectedSpan.serviceName}</span>
                        </div>
                        <div>
                          <span className="text-[hsl(var(--muted-foreground))] block">Duration:</span>
                          <span className="font-bold text-emerald-400">
                            {selectedSpan.durationMs.toFixed(2)} ms
                          </span>
                        </div>
                        <div>
                          <span className="text-[hsl(var(--muted-foreground))] block">Status:</span>
                          <span
                            className={
                              selectedSpan.status?.toLowerCase() === "error"
                                ? "text-rose-400 font-bold"
                                : "text-emerald-400 font-medium"
                            }
                          >
                            {selectedSpan.status || "OK"}
                          </span>
                        </div>
                        <div>
                          <span className="text-[hsl(var(--muted-foreground))] block">Parent Span ID:</span>
                          <span>{selectedSpan.parentSpanId || "None (Root)"}</span>
                        </div>
                        <div>
                          <span className="text-[hsl(var(--muted-foreground))] block">Started At:</span>
                          <span>{new Date(selectedSpan.startedAtUtc).toLocaleTimeString()}</span>
                        </div>
                      </div>

                      {selectedSpan.attributes && (
                        <div>
                          <span className="font-sans font-semibold text-[hsl(var(--muted-foreground))] block mb-1">
                            Attributes:
                          </span>
                          <pre className="rounded bg-[hsl(var(--card))] p-2 text-[11px] text-[hsl(var(--foreground))] whitespace-pre-wrap break-all border border-[hsl(var(--border))] max-h-40 overflow-y-auto">
                            {(() => {
                              try {
                                return JSON.stringify(JSON.parse(selectedSpan.attributes), null, 2);
                              } catch {
                                return selectedSpan.attributes;
                              }
                            })()}
                          </pre>
                        </div>
                      )}
                    </>
                  ) : (
                    <p className="text-[hsl(var(--muted-foreground))] font-sans">
                      Click a span in the waterfall above to inspect attributes.
                    </p>
                  )}
                </CardContent>
              </Card>

              {/* Correlated Logs */}
              <Card>
                <CardHeader className="p-4 pb-2">
                  <CardTitle className="text-sm font-semibold flex items-center justify-between">
                    <span className="flex items-center gap-2">
                      <FileText className="h-4 w-4 text-[hsl(var(--primary))]" />
                      Correlated Logs ({traceDetail.correlatedLogs.length})
                    </span>
                    <Link
                      to={`/logs?traceId=${selectedTraceId}`}
                      className="text-xs text-[hsl(var(--primary))] hover:underline flex items-center gap-1 font-sans"
                    >
                      Open in Log Explorer &rarr;
                    </Link>
                  </CardTitle>
                </CardHeader>
                <CardContent className="p-4 pt-2 text-xs">
                  {traceDetail.correlatedLogs.length === 0 ? (
                    <div className="flex h-36 items-center justify-center text-[hsl(var(--muted-foreground))]">
                      No logs directly attached to this trace ID.
                    </div>
                  ) : (
                    <div className="divide-y divide-[hsl(var(--border))] max-h-56 overflow-y-auto font-mono text-[11px]">
                      {traceDetail.correlatedLogs.map((log) => (
                        <div key={log.id} className="py-2 space-y-0.5">
                          <div className="flex items-center gap-2">
                            <span className="text-[hsl(var(--muted-foreground))] text-[10px]">
                              {new Date(log.timestamp).toLocaleTimeString()}
                            </span>
                            <Badge
                              variant={
                                log.severity?.toLowerCase() === "error" ||
                                log.severity?.toLowerCase() === "critical"
                                  ? "danger"
                                  : "secondary"
                              }
                              className="text-[9px] py-0 px-1"
                            >
                              {log.severity}
                            </Badge>
                            <span className="text-amber-400 font-semibold">{log.serviceName}</span>
                          </div>
                          <p className="text-[hsl(var(--foreground))] font-sans text-xs">{log.message}</p>
                        </div>
                      ))}
                    </div>
                  )}
                </CardContent>
              </Card>
            </div>
          </div>
        )}
      </div>
    );
  }

  // Trace list view
  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-2xl font-bold tracking-tight">Distributed Traces</h1>
            <Badge variant="outline" className="text-xs">
              OpenTelemetry APM
            </Badge>
          </div>
          <p className="text-sm text-[hsl(var(--muted-foreground))]">
            Analyze end-to-end distributed transaction traces, latency bottlenecks, and error propagation.
          </p>
        </div>

        <Button
          variant="outline"
          size="sm"
          onClick={() => fetchTraces(page)}
          disabled={isLoading}
          className="gap-1.5 text-xs"
        >
          <RefreshCw className={`h-3.5 w-3.5 ${isLoading ? "animate-spin" : ""}`} />
          Refresh
        </Button>
      </div>

      {/* Filter Toolbar */}
      <Card>
        <CardContent className="p-4 space-y-3">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 md:grid-cols-4">
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

            {/* Search Input */}
            <div>
              <label className="text-xs font-medium text-[hsl(var(--muted-foreground))] mb-1 block">Search</label>
              <div className="relative">
                <Search className="absolute left-2.5 top-2 h-3.5 w-3.5 text-[hsl(var(--muted-foreground))]" />
                <Input
                  placeholder="Root span, Trace ID, endpoint..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  onKeyDown={(e) => e.key === "Enter" && fetchTraces(1)}
                  className="pl-8 h-8 text-xs font-mono"
                />
              </div>
            </div>

            {/* Min Duration */}
            <div>
              <label className="text-xs font-medium text-[hsl(var(--muted-foreground))] mb-1 block">
                Min Latency (ms)
              </label>
              <Input
                type="number"
                placeholder="e.g. 500"
                value={minDurationMs}
                onChange={(e) => setMinDurationMs(e.target.value)}
                className="h-8 text-xs"
              />
            </div>
          </div>

          <div className="flex items-center justify-between pt-2 border-t border-[hsl(var(--border))]">
            <label className="flex items-center gap-2 cursor-pointer text-xs">
              <input
                type="checkbox"
                checked={errorsOnly}
                onChange={(e) => setErrorsOnly(e.target.checked)}
                className="rounded border-[hsl(var(--border))] text-[hsl(var(--primary))] focus:ring-[hsl(var(--primary))]"
              />
              <span className="font-medium text-rose-400">Show Errors Only</span>
            </label>

            <Button size="sm" onClick={() => fetchTraces(1)} className="text-xs h-8">
              Filter Traces
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Error Notice */}
      {errorMessage && (
        <ErrorState
          title="Trace Collection Query Failed"
          message={errorMessage}
          onRetry={() => fetchTraces(page)}
        />
      )}

      {/* Traces Table */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between pb-2 pt-4 px-4">
          <CardTitle className="text-sm font-semibold flex items-center gap-2">
            <GitBranch className="h-4 w-4 text-[hsl(var(--primary))]" />
            Trace Records
            <span className="text-xs font-normal text-[hsl(var(--muted-foreground))]">
              ({totalCount.toLocaleString()} total traces)
            </span>
          </CardTitle>
          <div className="text-xs text-[hsl(var(--muted-foreground))]">
            Page {page} of {Math.max(1, totalPages)}
          </div>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading && traces.length === 0 ? (
            <div className="p-4 space-y-3">
              {Array.from({ length: 6 }).map((_, i) => (
                <div key={i} className="flex items-center gap-3">
                  <Skeleton className="h-4 w-4 rounded-full" />
                  <Skeleton className="h-4 w-48" />
                  <Skeleton className="h-4 w-24" />
                  <Skeleton className="h-4 flex-1" />
                </div>
              ))}
            </div>
          ) : traces.length === 0 ? (
            <div className="p-6">
              <EmptyState
                icon={GitBranch}
                title="No trace records found"
                description="Generate requests through your instrumented applications to view distributed traces."
                actionLabel="Clear Filter Query"
                onAction={() => {
                  setSelectedAppId("");
                  setSelectedService("");
                  setErrorsOnly(false);
                  setSearchQuery("");
                  setMinDurationMs("");
                  fetchTraces(1);
                }}
              />
            </div>
          ) : (
            <div className="divide-y divide-[hsl(var(--border))] font-mono text-xs">
              {traces.map((trace) => (
                <div
                  key={trace.traceId}
                  onClick={() => handleSelectTrace(trace.traceId)}
                  className="flex items-center justify-between p-3 cursor-pointer hover:bg-[hsl(var(--accent)/0.3)] transition-colors select-none"
                >
                  <div className="flex items-center gap-3 min-w-0 flex-1">
                    {/* Status Icon */}
                    {trace.hasErrors ? (
                      <XCircle className="h-4 w-4 text-rose-400 shrink-0" />
                    ) : (
                      <CheckCircle2 className="h-4 w-4 text-emerald-400 shrink-0" />
                    )}

                    {/* Root Span Name */}
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <span className="font-semibold text-[hsl(var(--foreground))] truncate text-xs">
                          {trace.rootSpanName}
                        </span>
                        <Badge
                          variant="outline"
                          className="text-[10px] py-0 px-1.5 font-normal text-amber-400 border-amber-500/30"
                        >
                          {trace.serviceName}
                        </Badge>
                      </div>
                      <div className="text-[11px] text-[hsl(var(--muted-foreground))] font-mono mt-0.5 truncate">
                        ID: {trace.traceId} &bull; Started {new Date(trace.startedAtUtc).toLocaleTimeString()}
                      </div>
                    </div>
                  </div>

                  {/* Right Badges */}
                  <div className="flex items-center gap-3 shrink-0 ml-4">
                    {/* Spans Count */}
                    <div className="flex items-center gap-1 text-[11px] text-[hsl(var(--muted-foreground))]">
                      <Layers className="h-3 w-3" />
                      <span>{trace.spanCount} spans</span>
                    </div>

                    {/* Latency Pill */}
                    <span
                      className={`rounded border px-2 py-0.5 text-xs font-bold font-mono ${getDurationBadgeColor(
                        trace.durationMs
                      )}`}
                    >
                      {trace.durationMs.toFixed(1)} ms
                    </span>

                    <ChevronRight className="h-4 w-4 text-[hsl(var(--muted-foreground))]" />
                  </div>
                </div>
              ))}
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
                onClick={() => fetchTraces(page - 1)}
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
                onClick={() => fetchTraces(page + 1)}
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
