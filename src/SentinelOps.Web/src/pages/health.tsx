import React, { useState, useEffect, useCallback } from "react";
import { Link } from "react-router-dom";
import apiClient, {
  type ApiResponse,
  type ServiceHealthSummaryDto,
  type ApplicationDto,
} from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  HeartPulse,
  RefreshCw,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  Layers,
  FileText,
  GitBranch,
  Search,
} from "lucide-react";

export function ServiceHealthPage() {
  const [applications, setApplications] = useState<ApplicationDto[]>([]);
  const [selectedAppId, setSelectedAppId] = useState<string>("");
  const [services, setServices] = useState<ServiceHealthSummaryDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [searchFilter, setSearchFilter] = useState<string>("");

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

  const fetchHealth = useCallback(async () => {
    try {
      setIsLoading(true);
      setErrorMessage(null);
      const params: Record<string, string> = {};
      if (selectedAppId) params.applicationId = selectedAppId;

      const res = await apiClient.get<ApiResponse<ServiceHealthSummaryDto[]>>("/services/health", { params });
      if (res.data.success && res.data.data) {
        setServices(res.data.data);
      }
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : "Failed to load service health status.");
    } finally {
      setIsLoading(false);
    }
  }, [selectedAppId]);

  useEffect(() => {
    fetchHealth();
  }, [fetchHealth]);

  const filteredServices = React.useMemo(() => {
    if (!searchFilter.trim()) return services;
    const q = searchFilter.toLowerCase();
    return services.filter(
      (s) =>
        s.serviceName.toLowerCase().includes(q) ||
        s.applicationName.toLowerCase().includes(q) ||
        s.environmentName.toLowerCase().includes(q)
    );
  }, [services, searchFilter]);

  const healthyCount = services.filter((s) => s.status === "Healthy").length;
  const degradedCount = services.filter((s) => s.status === "Degraded").length;
  const unhealthyCount = services.filter((s) => s.status === "Unhealthy").length;

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-2xl font-bold tracking-tight">Service Health Monitoring</h1>
            <Badge variant="outline" className="text-xs">
              SLA / SLO Matrix
            </Badge>
          </div>
          <p className="text-sm text-[hsl(var(--muted-foreground))]">
            Real-time operational health, error budgets, and latency performance for all registered services.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <Button
            variant="outline"
            size="sm"
            onClick={fetchHealth}
            disabled={isLoading}
            className="gap-1.5 text-xs"
          >
            <RefreshCw className={`h-3.5 w-3.5 ${isLoading ? "animate-spin" : ""}`} />
            Refresh
          </Button>
        </div>
      </div>

      {/* Health Overview Counters */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-xs font-medium text-[hsl(var(--muted-foreground))]">
              Total Monitored
            </CardTitle>
            <Layers className="h-4 w-4 text-[hsl(var(--primary))]" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold font-mono">{services.length}</div>
            <p className="text-[11px] text-[hsl(var(--muted-foreground))] mt-1">active services</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-xs font-medium text-[hsl(var(--muted-foreground))]">
              Healthy
            </CardTitle>
            <CheckCircle2 className="h-4 w-4 text-emerald-400" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold font-mono text-emerald-400">{healthyCount}</div>
            <p className="text-[11px] text-[hsl(var(--muted-foreground))] mt-1">&lt; 2% error rate</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-xs font-medium text-[hsl(var(--muted-foreground))]">
              Degraded
            </CardTitle>
            <AlertTriangle className="h-4 w-4 text-amber-400" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold font-mono text-amber-400">{degradedCount}</div>
            <p className="text-[11px] text-[hsl(var(--muted-foreground))] mt-1"> elevated latency or errors</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-xs font-medium text-[hsl(var(--muted-foreground))]">
              Unhealthy
            </CardTitle>
            <XCircle className="h-4 w-4 text-rose-400" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold font-mono text-rose-400">{unhealthyCount}</div>
            <p className="text-[11px] text-[hsl(var(--muted-foreground))] mt-1">&ge; 10% errors or high p95</p>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="p-4 flex flex-col sm:flex-row items-center gap-4 justify-between">
          <div className="w-full sm:w-64">
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

          <div className="relative w-full sm:w-72">
            <Search className="absolute left-2.5 top-2 h-3.5 w-3.5 text-[hsl(var(--muted-foreground))]" />
            <input
              type="text"
              placeholder="Filter by service name..."
              value={searchFilter}
              onChange={(e) => setSearchFilter(e.target.value)}
              className="w-full rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--background))] pl-8 pr-3 py-1.5 text-xs focus:outline-none focus:ring-1 focus:ring-[hsl(var(--primary))]"
            />
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

      {/* Services Health Grid */}
      {isLoading && services.length === 0 ? (
        <div className="flex h-48 items-center justify-center text-sm text-[hsl(var(--muted-foreground))]">
          <RefreshCw className="h-4 w-4 animate-spin mr-2" /> Evaluating service health...
        </div>
      ) : filteredServices.length === 0 ? (
        <Card>
          <CardContent className="flex h-40 flex-col items-center justify-center p-6 text-center">
            <HeartPulse className="h-10 w-10 text-[hsl(var(--muted-foreground))] mb-2 opacity-50" />
            <p className="text-sm font-medium">No service telemetry found</p>
            <p className="text-xs text-[hsl(var(--muted-foreground))] mt-1">
              Instrument your microservices or generate telemetry using the sample app.
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
          {filteredServices.map((svc) => {
            const isDegraded = svc.status === "Degraded";
            const isUnhealthy = svc.status === "Unhealthy";

            return (
              <Card
                key={`${svc.applicationId}-${svc.serviceName}-${svc.environmentName}`}
                className={`overflow-hidden transition-all ${
                  isUnhealthy
                    ? "border-rose-500/30"
                    : isDegraded
                    ? "border-amber-500/30"
                    : "border-[hsl(var(--border))]"
                }`}
              >
                <div
                  className={`h-1.5 w-full ${
                    isUnhealthy ? "bg-rose-500" : isDegraded ? "bg-amber-500" : "bg-emerald-500"
                  }`}
                />
                <CardHeader className="p-4 pb-2">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <CardTitle className="text-base font-bold text-[hsl(var(--foreground))]">
                        {svc.serviceName}
                      </CardTitle>
                      <CardDescription className="text-xs text-[hsl(var(--muted-foreground))]">
                        {svc.applicationName} &bull; {svc.environmentName}
                      </CardDescription>
                    </div>

                    <Badge
                      variant={isUnhealthy ? "danger" : isDegraded ? "warning" : "success"}
                      className="text-[10px] uppercase font-bold py-0.5 px-2"
                    >
                      {svc.status}
                    </Badge>
                  </div>
                </CardHeader>

                <CardContent className="p-4 pt-2 space-y-3 text-xs">
                  {/* Metrics Row */}
                  <div className="grid grid-cols-3 gap-2 border-y border-[hsl(var(--border))] py-2.5 font-mono">
                    <div>
                      <span className="text-[10px] text-[hsl(var(--muted-foreground))] block font-sans">
                        Error Rate
                      </span>
                      <span
                        className={`text-sm font-bold ${
                          svc.errorRatePercent > 5
                            ? "text-rose-400"
                            : svc.errorRatePercent > 1
                            ? "text-amber-400"
                            : "text-emerald-400"
                        }`}
                      >
                        {svc.errorRatePercent.toFixed(1)}%
                      </span>
                    </div>

                    <div>
                      <span className="text-[10px] text-[hsl(var(--muted-foreground))] block font-sans">
                        p95 Latency
                      </span>
                      <span className="text-sm font-bold text-[hsl(var(--foreground))]">
                        {svc.p95DurationMs.toFixed(0)} ms
                      </span>
                    </div>

                    <div>
                      <span className="text-[10px] text-[hsl(var(--muted-foreground))] block font-sans">
                        Total Reqs
                      </span>
                      <span className="text-sm font-bold text-[hsl(var(--foreground))]">
                        {svc.totalRequests.toLocaleString()}
                      </span>
                    </div>
                  </div>

                  {/* Activity Timestamp */}
                  <div className="flex items-center justify-between text-[11px] text-[hsl(var(--muted-foreground))]">
                    <span>Last activity:</span>
                    <span className="font-mono">
                      {svc.lastSeenAtUtc ? new Date(svc.lastSeenAtUtc).toLocaleTimeString() : "Never"}
                    </span>
                  </div>

                  {/* Action Links */}
                  <div className="flex items-center gap-2 pt-1 border-t border-[hsl(var(--border))]">
                    <Link
                      to={`/logs?service=${encodeURIComponent(svc.serviceName)}`}
                      className="flex-1 text-center py-1 rounded bg-[hsl(var(--secondary))] hover:bg-[hsl(var(--accent))] text-[11px] font-medium transition-colors flex items-center justify-center gap-1"
                    >
                      <FileText className="h-3 w-3" /> Logs
                    </Link>
                    <Link
                      to={`/traces?service=${encodeURIComponent(svc.serviceName)}`}
                      className="flex-1 text-center py-1 rounded bg-[hsl(var(--secondary))] hover:bg-[hsl(var(--accent))] text-[11px] font-medium transition-colors flex items-center justify-center gap-1"
                    >
                      <GitBranch className="h-3 w-3" /> Traces
                    </Link>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
