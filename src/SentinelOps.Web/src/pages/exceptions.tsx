import React, { useState, useEffect, useCallback } from "react";
import { Link } from "react-router-dom";
import apiClient, {
  type ApiResponse,
  type ExceptionGroupDto,
  type ApplicationDto,
  type LogMetadataDto,
} from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  Bug,
  RefreshCw,
  GitBranch,
  FileText,
  AlertTriangle,
  Search,
} from "lucide-react";

export function ExceptionsPage() {
  const [applications, setApplications] = useState<ApplicationDto[]>([]);
  const [selectedAppId, setSelectedAppId] = useState<string>("");
  const [selectedService, setSelectedService] = useState<string>("");
  const [selectedEnv, setSelectedEnv] = useState<string>("");
  const [servicesList, setServicesList] = useState<string[]>([]);
  const [envsList, setEnvsList] = useState<string[]>([]);
  const [timeWindow, setTimeWindow] = useState<string>("24h");

  const [exceptions, setExceptions] = useState<ExceptionGroupDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [filterText, setFilterText] = useState<string>("");

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

  const getTimeRangeUtc = useCallback(() => {
    const toUtc = new Date();
    let fromUtc: Date;
    switch (timeWindow) {
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
        fromUtc = new Date(toUtc.getTime() - 24 * 60 * 60 * 1000);
    }
    return { fromUtc: fromUtc.toISOString(), toUtc: toUtc.toISOString() };
  }, [timeWindow]);

  const fetchExceptions = useCallback(async () => {
    try {
      setIsLoading(true);
      setErrorMessage(null);

      const { fromUtc, toUtc } = getTimeRangeUtc();
      const params: Record<string, string | number> = {
        fromUtc,
        toUtc,
        limit: 50,
      };

      if (selectedAppId) params.applicationId = selectedAppId;
      if (selectedService) params.serviceName = selectedService;
      if (selectedEnv) params.environmentName = selectedEnv;

      const res = await apiClient.get<ApiResponse<ExceptionGroupDto[]>>("/exceptions", { params });
      if (res.data.success && res.data.data) {
        setExceptions(res.data.data);
      }
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : "Failed to load exceptions.");
    } finally {
      setIsLoading(false);
    }
  }, [selectedAppId, selectedService, selectedEnv, getTimeRangeUtc]);

  useEffect(() => {
    fetchExceptions();
  }, [fetchExceptions]);

  const filtered = React.useMemo(() => {
    if (!filterText.trim()) return exceptions;
    const q = filterText.toLowerCase();
    return exceptions.filter(
      (e) =>
        e.exceptionType.toLowerCase().includes(q) ||
        e.message.toLowerCase().includes(q) ||
        e.serviceName.toLowerCase().includes(q) ||
        e.fingerprint.toLowerCase().includes(q)
    );
  }, [exceptions, filterText]);

  const totalOccurrences = exceptions.reduce((sum, e) => sum + e.count, 0);

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-2xl font-bold tracking-tight">Exceptions Explorer</h1>
            <Badge variant="outline" className="text-xs">
              Normalized Fingerprints
            </Badge>
          </div>
          <p className="text-sm text-[hsl(var(--muted-foreground))]">
            Grouped error tracking, exception deduplication, and crash diagnostics with trace correlation.
          </p>
        </div>

        <Button
          variant="outline"
          size="sm"
          onClick={fetchExceptions}
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

            <div>
              <label className="text-xs font-medium text-[hsl(var(--muted-foreground))] mb-1 block">Environment</label>
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

            <div>
              <label className="text-xs font-medium text-[hsl(var(--muted-foreground))] mb-1 block">Time Window</label>
              <select
                value={timeWindow}
                onChange={(e) => setTimeWindow(e.target.value)}
                className="w-full rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--background))] px-3 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-[hsl(var(--primary))]"
              >
                <option value="1h">Last 1 hour</option>
                <option value="6h">Last 6 hours</option>
                <option value="24h">Last 24 hours</option>
                <option value="7d">Last 7 days</option>
              </select>
            </div>

            <div>
              <label className="text-xs font-medium text-[hsl(var(--muted-foreground))] mb-1 block">Search Filter</label>
              <div className="relative">
                <Search className="absolute left-2.5 top-2 h-3.5 w-3.5 text-[hsl(var(--muted-foreground))]" />
                <input
                  type="text"
                  placeholder="Filter by exception type, message, fingerprint..."
                  value={filterText}
                  onChange={(e) => setFilterText(e.target.value)}
                  className="w-full rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--background))] pl-8 pr-3 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-[hsl(var(--primary))]"
                />
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Error Message */}
      {errorMessage && (
        <div className="rounded-md border border-rose-500/20 bg-rose-500/10 p-3 text-sm text-rose-400 flex items-center gap-2">
          <AlertTriangle className="h-4 w-4 shrink-0" />
          <span>{errorMessage}</span>
        </div>
      )}

      {/* Exceptions List */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between pb-2 pt-4 px-4">
          <CardTitle className="text-sm font-semibold flex items-center gap-2">
            <Bug className="h-4 w-4 text-rose-400" />
            Grouped Exception Fingerprints
            <span className="text-xs font-normal text-[hsl(var(--muted-foreground))]">
              ({exceptions.length} unique issues, {totalOccurrences.toLocaleString()} occurrences)
            </span>
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading && exceptions.length === 0 ? (
            <div className="flex h-48 items-center justify-center text-sm text-[hsl(var(--muted-foreground))]">
              <RefreshCw className="h-4 w-4 animate-spin mr-2" /> Aggregating exception groups...
            </div>
          ) : filtered.length === 0 ? (
            <div className="flex h-48 flex-col items-center justify-center text-center p-6">
              <Bug className="h-10 w-10 text-[hsl(var(--muted-foreground))] mb-2 opacity-50" />
              <p className="text-sm font-medium">No exceptions recorded</p>
              <p className="text-xs text-[hsl(var(--muted-foreground))] max-w-sm mt-1">
                Your services have zero uncaught exceptions in the selected time range.
              </p>
            </div>
          ) : (
            <div className="divide-y divide-[hsl(var(--border))] text-xs">
              {filtered.map((item) => (
                <div
                  key={item.fingerprint}
                  className="p-4 hover:bg-[hsl(var(--accent)/0.3)] transition-colors flex flex-col sm:flex-row sm:items-center justify-between gap-4"
                >
                  <div className="space-y-1.5 min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <Badge variant="danger" className="text-[10px] font-mono py-0 h-4">
                        #{item.fingerprint}
                      </Badge>
                      <span className="font-bold text-rose-400 text-xs font-mono">
                        {item.exceptionType}
                      </span>
                      <Badge variant="outline" className="text-[10px] py-0 h-4 text-amber-400 border-amber-500/30">
                        {item.serviceName}
                      </Badge>
                      <span className="text-[10px] text-[hsl(var(--muted-foreground))]">
                        [{item.environmentName}]
                      </span>
                    </div>

                    <p className="text-[hsl(var(--foreground))] font-normal break-words">
                      {item.message}
                    </p>

                    <div className="flex items-center gap-4 text-[11px] text-[hsl(var(--muted-foreground))] pt-1 font-mono">
                      <span>First: {new Date(item.firstSeenAtUtc).toLocaleTimeString()}</span>
                      <span>&bull;</span>
                      <span>Last: {new Date(item.lastSeenAtUtc).toLocaleTimeString()}</span>
                    </div>
                  </div>

                  {/* Occurrence badge & links */}
                  <div className="flex sm:flex-col items-end justify-between sm:justify-center gap-2 shrink-0">
                    <div className="rounded bg-rose-500/10 border border-rose-500/20 px-2.5 py-1 text-center">
                      <span className="text-base font-bold font-mono text-rose-400 block leading-none">
                        {item.count.toLocaleString()}
                      </span>
                      <span className="text-[9px] text-[hsl(var(--muted-foreground))] uppercase font-semibold">
                        events
                      </span>
                    </div>

                    <div className="flex items-center gap-1.5">
                      {item.sampleTraceId && (
                        <Link
                          to={`/traces?traceId=${item.sampleTraceId}`}
                          className="flex items-center gap-1 rounded bg-[hsl(var(--secondary))] hover:bg-[hsl(var(--accent))] px-2 py-1 text-[11px] font-medium transition-colors"
                          title="Inspect Sample Trace Waterfall"
                        >
                          <GitBranch className="h-3 w-3 text-[hsl(var(--primary))]" />
                          Trace
                        </Link>
                      )}
                      <Link
                        to={`/logs?q=${encodeURIComponent(item.exceptionType)}`}
                        className="flex items-center gap-1 rounded bg-[hsl(var(--secondary))] hover:bg-[hsl(var(--accent))] px-2 py-1 text-[11px] font-medium transition-colors"
                        title="Search in Logs Explorer"
                      >
                        <FileText className="h-3 w-3" />
                        Logs
                      </Link>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
