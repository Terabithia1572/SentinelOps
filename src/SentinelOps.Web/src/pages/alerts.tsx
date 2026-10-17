import { useState, useMemo } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import apiClient, {
  type ApiResponse,
  type PagedResponse,
  type AlertSummaryDto,
  type AlertEventDto,
  type ApplicationDto,
} from "@/lib/api-client";
import { useAlertsHub } from "@/hooks/use-alerts-hub";
import { useAuth } from "@/context/auth-context";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Dialog } from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import {
  Bell,
  AlertTriangle,
  ShieldAlert,
  CheckCircle2,
  Clock,
  RefreshCw,
  Search,
  ExternalLink,
  Sliders,
} from "lucide-react";

export function AlertsPage() {
  const { isOperator } = useAuth();
  const queryClient = useQueryClient();

  const [selectedAppId, setSelectedAppId] = useState<string>("");
  const [selectedStatus, setSelectedStatus] = useState<string>("Firing");
  const [selectedSeverity, setSelectedSeverity] = useState<string>("");
  const [searchQuery, setSearchQuery] = useState("");
  const [page, setPage] = useState(1);
  const pageSize = 20;

  // Modals
  const [ackModalAlert, setAckModalAlert] = useState<AlertEventDto | null>(null);
  const [resolveModalAlert, setResolveModalAlert] = useState<AlertEventDto | null>(null);
  const [modalNotes, setModalNotes] = useState("");

  // Real-time SignalR connection
  const { isConnected } = useAlertsHub({
    applicationId: selectedAppId || undefined,
  });

  // Applications for filter dropdown
  const { data: applications = [] } = useQuery({
    queryKey: ["applications"],
    queryFn: async () => {
      const res = await apiClient.get<ApiResponse<ApplicationDto[]>>("/applications");
      return res.data.data || [];
    },
  });

  // Summary counts
  const { data: summary, refetch: refetchSummary } = useQuery({
    queryKey: ["alert-summary", selectedAppId],
    queryFn: async () => {
      const params = selectedAppId ? `?applicationId=${selectedAppId}` : "";
      const res = await apiClient.get<ApiResponse<AlertSummaryDto>>(`/alerts/summary${params}`);
      return res.data.data;
    },
    refetchInterval: 15000,
  });

  // Alert events query
  const {
    data: alertsData,
    isLoading,
    refetch: refetchAlerts,
  } = useQuery({
    queryKey: ["alerts", selectedAppId, selectedStatus, selectedSeverity, page, pageSize],
    queryFn: async () => {
      const params = new URLSearchParams();
      if (selectedAppId) params.append("applicationId", selectedAppId);
      if (selectedStatus && selectedStatus !== "ALL") params.append("status", selectedStatus);
      if (selectedSeverity) params.append("severity", selectedSeverity);
      params.append("page", page.toString());
      params.append("pageSize", pageSize.toString());

      const res = await apiClient.get<ApiResponse<PagedResponse<AlertEventDto>>>(`/alerts?${params.toString()}`);
      return res.data.data;
    },
    refetchInterval: 15000,
  });

  // Acknowledge mutation
  const acknowledgeMutation = useMutation({
    mutationFn: async ({ id, notes }: { id: string; notes?: string }) => {
      await apiClient.post(`/alerts/${id}/acknowledge`, { notes });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["alerts"] });
      queryClient.invalidateQueries({ queryKey: ["alert-summary"] });
      setAckModalAlert(null);
      setModalNotes("");
    },
  });

  // Resolve mutation
  const resolveMutation = useMutation({
    mutationFn: async ({ id, reason }: { id: string; reason: string }) => {
      await apiClient.post(`/alerts/${id}/resolve`, { reason });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["alerts"] });
      queryClient.invalidateQueries({ queryKey: ["alert-summary"] });
      setResolveModalAlert(null);
      setModalNotes("");
    },
  });

  const alerts = alertsData?.items || [];
  const filteredAlerts = useMemo(() => {
    if (!searchQuery.trim()) return alerts;
    const q = searchQuery.toLowerCase();
    return alerts.filter(
      (a) =>
        a.alertRuleName.toLowerCase().includes(q) ||
        a.message.toLowerCase().includes(q) ||
        a.severity.toLowerCase().includes(q)
    );
  }, [alerts, searchQuery]);

  const getSeverityBadgeVariant = (severity: string): "danger" | "warning" | "default" | "secondary" => {
    switch (severity.toLowerCase()) {
      case "critical":
        return "danger";
      case "error":
        return "danger";
      case "warning":
        return "warning";
      case "information":
      case "info":
        return "default";
      default:
        return "secondary";
    }
  };

  const getStatusBadge = (status: string) => {
    switch (status.toLowerCase()) {
      case "firing":
        return <Badge variant="danger" className="animate-pulse">Firing</Badge>;
      case "pending":
        return <Badge variant="warning">Pending</Badge>;
      case "acknowledged":
        return <Badge variant="secondary" className="bg-amber-500/10 text-amber-400 border border-amber-500/20">Acknowledged</Badge>;
      case "resolved":
        return <Badge variant="secondary" className="bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">Resolved</Badge>;
      case "suppressed":
        return <Badge variant="secondary">Suppressed</Badge>;
      default:
        return <Badge variant="secondary">{status}</Badge>;
    }
  };

  return (
    <div className="space-y-6">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold tracking-tight">Alerts & Incidents</h1>
            {isConnected ? (
              <span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-500/10 px-2.5 py-0.5 text-xs font-medium text-emerald-400 border border-emerald-500/20">
                <span className="h-1.5 w-1.5 rounded-full bg-emerald-400 animate-pulse" />
                Live WebSocket
              </span>
            ) : (
              <span className="inline-flex items-center gap-1.5 rounded-full bg-muted/40 px-2.5 py-0.5 text-xs font-medium text-muted-foreground border border-border">
                <span className="h-1.5 w-1.5 rounded-full bg-muted-foreground" />
                Polling
              </span>
            )}
          </div>
          <p className="text-sm text-[hsl(var(--muted-foreground))] mt-1">
            Real-time alerting engine evaluating telemetry thresholds, incident dispatch, and notification outbox.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => {
              refetchSummary();
              refetchAlerts();
            }}
            className="gap-1.5"
          >
            <RefreshCw className="h-3.5 w-3.5" />
            Refresh
          </Button>

          <Link to="/alert-rules">
            <Button size="sm" className="gap-1.5">
              <Sliders className="h-3.5 w-3.5" />
              Manage Rules
            </Button>
          </Link>
        </div>
      </div>

      {/* Summary KPI Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3">
        <Card className="border-[hsl(var(--border))]">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium text-[hsl(var(--muted-foreground))]">Total Rules</span>
              <Bell className="h-4 w-4 text-[hsl(var(--muted-foreground))]" />
            </div>
            <div className="mt-2 text-2xl font-bold">{summary?.totalRules ?? 0}</div>
            <p className="text-[11px] text-[hsl(var(--muted-foreground))] mt-0.5">
              {summary?.enabledRules ?? 0} active
            </p>
          </CardContent>
        </Card>

        <Card className="border-red-500/20 bg-red-500/5">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium text-red-400">Firing Alerts</span>
              <ShieldAlert className="h-4 w-4 text-red-400" />
            </div>
            <div className="mt-2 text-2xl font-bold text-red-400">{summary?.firingAlerts ?? 0}</div>
            <p className="text-[11px] text-red-400/80 mt-0.5">Requiring attention</p>
          </CardContent>
        </Card>

        <Card className="border-amber-500/20 bg-amber-500/5">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium text-amber-400">Acknowledged</span>
              <Clock className="h-4 w-4 text-amber-400" />
            </div>
            <div className="mt-2 text-2xl font-bold text-amber-400">{summary?.acknowledgedAlerts ?? 0}</div>
            <p className="text-[11px] text-amber-400/80 mt-0.5">In triage</p>
          </CardContent>
        </Card>

        <Card className="border-yellow-500/20 bg-yellow-500/5">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium text-yellow-400">Pending</span>
              <AlertTriangle className="h-4 w-4 text-yellow-400" />
            </div>
            <div className="mt-2 text-2xl font-bold text-yellow-400">{summary?.pendingAlerts ?? 0}</div>
            <p className="text-[11px] text-yellow-400/80 mt-0.5">Consecutive breaches</p>
          </CardContent>
        </Card>

        <Card className="border-[hsl(var(--border))]">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium text-[hsl(var(--muted-foreground))]">Critical Breaches</span>
              <ShieldAlert className="h-4 w-4 text-rose-400" />
            </div>
            <div className="mt-2 text-2xl font-bold text-rose-400">{summary?.criticalAlerts ?? 0}</div>
            <p className="text-[11px] text-[hsl(var(--muted-foreground))] mt-0.5">P1 Priority</p>
          </CardContent>
        </Card>

        <Card className="border-[hsl(var(--border))]">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium text-[hsl(var(--muted-foreground))]">Warnings</span>
              <AlertTriangle className="h-4 w-4 text-amber-400" />
            </div>
            <div className="mt-2 text-2xl font-bold text-amber-400">{summary?.warningAlerts ?? 0}</div>
            <p className="text-[11px] text-[hsl(var(--muted-foreground))] mt-0.5">P2 Priority</p>
          </CardContent>
        </Card>
      </div>

      {/* Filter & Search Bar */}
      <Card className="border-[hsl(var(--border))]">
        <CardContent className="p-4">
          <div className="flex flex-col md:flex-row items-center gap-3 justify-between">
            {/* Status tabs */}
            <div className="flex items-center gap-1.5 overflow-x-auto w-full md:w-auto">
              {[
                { label: "Firing", value: "Firing" },
                { label: "Acknowledged", value: "Acknowledged" },
                { label: "Resolved", value: "Resolved" },
                { label: "All Events", value: "ALL" },
              ].map((tab) => (
                <button
                  key={tab.value}
                  onClick={() => {
                    setSelectedStatus(tab.value);
                    setPage(1);
                  }}
                  className={`px-3 py-1.5 text-xs font-medium rounded-md transition-colors ${
                    selectedStatus === tab.value
                      ? "bg-[hsl(var(--primary))] text-[hsl(var(--primary-foreground))]"
                      : "text-[hsl(var(--muted-foreground))] hover:bg-[hsl(var(--accent))] hover:text-[hsl(var(--foreground))]"
                  }`}
                >
                  {tab.label}
                </button>
              ))}
            </div>

            {/* Application & Severity & Search */}
            <div className="flex flex-wrap items-center gap-2.5 w-full md:w-auto">
              <select
                value={selectedAppId}
                onChange={(e) => {
                  setSelectedAppId(e.target.value);
                  setPage(1);
                }}
                className="h-9 text-xs rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] px-3 py-1 text-[hsl(var(--foreground))]"
              >
                <option value="">All Applications</option>
                {applications.map((app) => (
                  <option key={app.id} value={app.id}>
                    {app.displayName || app.name}
                  </option>
                ))}
              </select>

              <select
                value={selectedSeverity}
                onChange={(e) => {
                  setSelectedSeverity(e.target.value);
                  setPage(1);
                }}
                className="h-9 text-xs rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] px-3 py-1 text-[hsl(var(--foreground))]"
              >
                <option value="">All Severities</option>
                <option value="Critical">Critical</option>
                <option value="Error">Error</option>
                <option value="Warning">Warning</option>
                <option value="Information">Information</option>
              </select>

              <div className="relative flex-1 sm:w-48">
                <Search className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-[hsl(var(--muted-foreground))]" />
                <Input
                  placeholder="Search alerts..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="h-9 pl-8 text-xs"
                />
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Alert Events Table */}
      <Card className="border-[hsl(var(--border))]">
        <CardHeader className="p-4 border-b border-[hsl(var(--border))]">
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="text-base font-semibold">Active & Historical Alert Events</CardTitle>
              <CardDescription className="text-xs">
                Audited telemetry evaluation triggers and state machine transitions
              </CardDescription>
            </div>
            <span className="text-xs text-[hsl(var(--muted-foreground))]">
              {alertsData?.totalCount ?? 0} total events
            </span>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-4 space-y-3">
              {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="flex items-center gap-3">
                  <Skeleton className="h-4 w-16" />
                  <Skeleton className="h-4 w-48" />
                  <Skeleton className="h-4 w-32" />
                  <Skeleton className="h-4 flex-1" />
                </div>
              ))}
            </div>
          ) : filteredAlerts.length === 0 ? (
            <div className="p-6">
              <EmptyState
                icon={CheckCircle2}
                title="No alerts found"
                description={
                  selectedStatus === "Firing"
                    ? "All systems operational. No active firing alerts for the selected criteria."
                    : "No alert events match your filter."
                }
              />
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead className="bg-[hsl(var(--muted)/0.3)] border-b border-[hsl(var(--border))] text-[hsl(var(--muted-foreground))]">
                  <tr>
                    <th className="py-3 px-4 font-semibold">Severity</th>
                    <th className="py-3 px-4 font-semibold">Alert Rule</th>
                    <th className="py-3 px-4 font-semibold">Measured vs Threshold</th>
                    <th className="py-3 px-4 font-semibold">Status</th>
                    <th className="py-3 px-4 font-semibold">Triggered At (UTC)</th>
                    <th className="py-3 px-4 font-semibold text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[hsl(var(--border))]">
                  {filteredAlerts.map((alert) => (
                    <tr
                      key={alert.id}
                      className="hover:bg-[hsl(var(--accent)/0.3)] transition-colors"
                    >
                      <td className="py-3 px-4">
                        <Badge variant={getSeverityBadgeVariant(alert.severity)}>
                          {alert.severity}
                        </Badge>
                      </td>
                      <td className="py-3 px-4">
                        <div className="font-medium text-[hsl(var(--foreground))]">
                          {alert.alertRuleName}
                        </div>
                        <div className="text-[11px] text-[hsl(var(--muted-foreground))] mt-0.5 line-clamp-1">
                          {alert.message}
                        </div>
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap font-mono text-[11px]">
                        <span className="font-semibold text-rose-400">
                          {alert.measuredValue.toFixed(2)}
                        </span>{" "}
                        <span className="text-[hsl(var(--muted-foreground))]">
                          / thr: {alert.threshold.toFixed(2)}
                        </span>
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap">
                        {getStatusBadge(alert.newStatus)}
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap text-[hsl(var(--muted-foreground))]">
                        {new Date(alert.triggeredAtUtc).toLocaleString()}
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap text-right">
                        <div className="flex items-center justify-end gap-1.5">
                          {isOperator && alert.newStatus === "Firing" && (
                            <Button
                              variant="outline"
                              size="sm"
                              className="h-7 px-2 text-xs text-amber-400 hover:bg-amber-500/10"
                              onClick={() => {
                                setAckModalAlert(alert);
                                setModalNotes("");
                              }}
                            >
                              Ack
                            </Button>
                          )}
                          {isOperator && (alert.newStatus === "Firing" || alert.newStatus === "Acknowledged") && (
                            <Button
                              variant="outline"
                              size="sm"
                              className="h-7 px-2 text-xs text-emerald-400 hover:bg-emerald-500/10"
                              onClick={() => {
                                setResolveModalAlert(alert);
                                setModalNotes("Resolved by operator");
                              }}
                            >
                              Resolve
                            </Button>
                          )}
                          <Link to="/alert-rules">
                            <Button variant="ghost" size="sm" className="h-7 w-7 p-0" title="View rule">
                              <ExternalLink className="h-3.5 w-3.5" />
                            </Button>
                          </Link>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Pagination */}
          {alertsData && alertsData.totalPages > 1 && (
            <div className="flex items-center justify-between border-t border-[hsl(var(--border))] px-4 py-3">
              <span className="text-xs text-[hsl(var(--muted-foreground))]">
                Page {alertsData.page} of {alertsData.totalPages}
              </span>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  className="h-7 text-xs"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  className="h-7 text-xs"
                  disabled={page >= alertsData.totalPages}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Next
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Acknowledge Modal */}
      <Dialog
        open={Boolean(ackModalAlert)}
        onClose={() => setAckModalAlert(null)}
        title="Acknowledge Alert"
        description={ackModalAlert ? `Mark alert "${ackModalAlert.alertRuleName}" as acknowledged.` : ""}
      >
        <div className="space-y-4">
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-300">Notes (optional)</label>
            <Input
              placeholder="e.g. Investigating cache cluster spike"
              value={modalNotes}
              onChange={(e) => setModalNotes(e.target.value)}
              className="bg-slate-950/80 border-slate-800 text-xs text-slate-200"
            />
          </div>
          <div className="flex items-center justify-end gap-2 pt-3 border-t border-slate-800">
            <Button variant="ghost" size="sm" onClick={() => setAckModalAlert(null)}>
              Cancel
            </Button>
            <Button
              size="sm"
              className="bg-amber-600 hover:bg-amber-700 text-white text-xs"
              onClick={() =>
                ackModalAlert &&
                acknowledgeMutation.mutate({
                  id: ackModalAlert.id,
                  notes: modalNotes,
                })
              }
              disabled={acknowledgeMutation.isPending}
            >
              {acknowledgeMutation.isPending ? "Acknowledging..." : "Acknowledge"}
            </Button>
          </div>
        </div>
      </Dialog>

      {/* Resolve Modal */}
      <Dialog
        open={Boolean(resolveModalAlert)}
        onClose={() => setResolveModalAlert(null)}
        title="Resolve Alert"
        description={resolveModalAlert ? `Mark alert "${resolveModalAlert.alertRuleName}" as resolved.` : ""}
      >
        <div className="space-y-4">
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-300">Resolution reason</label>
            <Input
              placeholder="e.g. Auto-scaling pod deployed, error rate returned to normal"
              value={modalNotes}
              onChange={(e) => setModalNotes(e.target.value)}
              className="bg-slate-950/80 border-slate-800 text-xs text-slate-200"
            />
          </div>
          <div className="flex items-center justify-end gap-2 pt-3 border-t border-slate-800">
            <Button variant="ghost" size="sm" onClick={() => setResolveModalAlert(null)}>
              Cancel
            </Button>
            <Button
              size="sm"
              className="bg-emerald-600 hover:bg-emerald-700 text-white text-xs"
              onClick={() =>
                resolveModalAlert &&
                resolveMutation.mutate({
                  id: resolveModalAlert.id,
                  reason: modalNotes || "Manually resolved",
                })
              }
              disabled={resolveMutation.isPending}
            >
              {resolveMutation.isPending ? "Resolving..." : "Resolve Alert"}
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  );
}
