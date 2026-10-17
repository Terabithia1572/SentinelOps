import { useState, useMemo } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate } from "react-router-dom";
import apiClient, {
  type ApiResponse,
  type PagedResponse,
  type IncidentDto,
  type CreateIncidentRequest,
  type ApplicationDto,
} from "@/lib/api-client";
import { useAuth } from "@/context/auth-context";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Dialog } from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import {
  ShieldAlert,
  Clock,
  CheckCircle2,
  Plus,
  Search,
  User,
  MessageSquare,
  History,
  ExternalLink,
  RefreshCw,
} from "lucide-react";
import { useAlertsHub } from "@/hooks/use-alerts-hub";

export function IncidentsPage() {
  const { isOperator, user } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [selectedStatus, setSelectedStatus] = useState<string>("ALL");
  const [selectedSeverity, setSelectedSeverity] = useState<string>("");
  const [selectedAppId, setSelectedAppId] = useState<string>("");
  const [searchQuery, setSearchQuery] = useState("");
  const [page, setPage] = useState(1);
  const pageSize = 20;

  // Real-time SignalR push notifications for incident lifecycle
  const { isConnected } = useAlertsHub({
    applicationId: selectedAppId || undefined,
  });

  // Create Modal
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [severity, setSeverity] = useState<string>("Critical");
  const [appId, setAppId] = useState("");
  const [serviceName, setServiceName] = useState("");
  const [envName, setEnvName] = useState("");
  const [relatedTraceId, setRelatedTraceId] = useState("");

  // Applications
  const { data: applications = [] } = useQuery({
    queryKey: ["applications"],
    queryFn: async () => {
      const res = await apiClient.get<ApiResponse<ApplicationDto[]>>("/applications");
      return res.data.data || [];
    },
  });

  // Incidents Query
  const { data: incidentsData, isLoading, refetch } = useQuery({
    queryKey: ["incidents", selectedAppId, selectedStatus, selectedSeverity, page, pageSize],
    queryFn: async () => {
      const params = new URLSearchParams();
      if (selectedAppId) params.append("applicationId", selectedAppId);
      if (selectedStatus && selectedStatus !== "ALL") params.append("status", selectedStatus);
      if (selectedSeverity) params.append("severity", selectedSeverity);
      params.append("page", page.toString());
      params.append("pageSize", pageSize.toString());

      const res = await apiClient.get<ApiResponse<PagedResponse<IncidentDto>>>(`/incidents?${params.toString()}`);
      return res.data.data;
    },
    refetchInterval: 15000,
  });

  // Create Mutation
  const createMutation = useMutation({
    mutationFn: async (req: CreateIncidentRequest) => {
      const res = await apiClient.post<ApiResponse<IncidentDto>>("/incidents", req);
      return res.data.data;
    },
    onSuccess: (newInc) => {
      queryClient.invalidateQueries({ queryKey: ["incidents"] });
      setIsCreateOpen(false);
      resetForm();
      if (newInc?.id) {
        navigate(`/incidents/${newInc.id}`);
      }
    },
  });

  const resetForm = () => {
    setTitle("");
    setDescription("");
    setSeverity("Critical");
    setAppId(applications[0]?.id || "");
    setServiceName("");
    setEnvName("");
    setRelatedTraceId("");
  };

  const incidents = incidentsData?.items || [];
  const filteredIncidents = useMemo(() => {
    if (!searchQuery.trim()) return incidents;
    const q = searchQuery.toLowerCase();
    return incidents.filter(
      (i) =>
        i.title.toLowerCase().includes(q) ||
        i.incidentNumber.toLowerCase().includes(q) ||
        (i.serviceName && i.serviceName.toLowerCase().includes(q))
    );
  }, [incidents, searchQuery]);

  // Counts
  const counts = useMemo(() => {
    return {
      open: incidents.filter((i) => i.status === "Open").length,
      investigating: incidents.filter((i) => i.status === "Investigating").length,
      mitigated: incidents.filter((i) => i.status === "Mitigated").length,
      resolved: incidents.filter((i) => i.status === "Resolved" || i.status === "Closed").length,
    };
  }, [incidents]);

  const getSeverityBadge = (sev: string) => {
    switch (sev.toLowerCase()) {
      case "critical":
        return <Badge variant="danger">Critical</Badge>;
      case "error":
        return <Badge variant="danger">Error</Badge>;
      case "warning":
        return <Badge variant="warning">Warning</Badge>;
      default:
        return <Badge variant="secondary">{sev}</Badge>;
    }
  };

  const getStatusBadge = (st: string) => {
    switch (st.toLowerCase()) {
      case "open":
        return <Badge variant="danger" className="animate-pulse">Open</Badge>;
      case "investigating":
        return <Badge variant="warning">Investigating</Badge>;
      case "mitigated":
        return <Badge variant="secondary" className="bg-sky-500/10 text-sky-400 border border-sky-500/20">Mitigated</Badge>;
      case "resolved":
        return <Badge variant="secondary" className="bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">Resolved</Badge>;
      case "closed":
        return <Badge variant="secondary">Closed</Badge>;
      default:
        return <Badge variant="secondary">{st}</Badge>;
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold tracking-tight">Incident Management</h1>
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
            Enterprise triage lifecycle: Open, Investigating, Mitigated, Resolved, Closed with audit timelines.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => refetch()} className="gap-1.5">
            <RefreshCw className="h-3.5 w-3.5" />
            Refresh
          </Button>

          {isOperator && (
            <Button
              size="sm"
              className="gap-1.5"
              onClick={() => {
                resetForm();
                setIsCreateOpen(true);
              }}
            >
              <Plus className="h-3.5 w-3.5" />
              Declare Incident
            </Button>
          )}
        </div>
      </div>

      {/* Summary KPI Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <Card className="border-red-500/20 bg-red-500/5">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <span className="text-xs font-medium text-red-400">Open Incidents</span>
              <div className="text-2xl font-bold text-red-400 mt-1">{counts.open}</div>
            </div>
            <ShieldAlert className="h-6 w-6 text-red-400 opacity-80" />
          </CardContent>
        </Card>

        <Card className="border-amber-500/20 bg-amber-500/5">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <span className="text-xs font-medium text-amber-400">Investigating</span>
              <div className="text-2xl font-bold text-amber-400 mt-1">{counts.investigating}</div>
            </div>
            <Clock className="h-6 w-6 text-amber-400 opacity-80" />
          </CardContent>
        </Card>

        <Card className="border-sky-500/20 bg-sky-500/5">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <span className="text-xs font-medium text-sky-400">Mitigated</span>
              <div className="text-2xl font-bold text-sky-400 mt-1">{counts.mitigated}</div>
            </div>
            <CheckCircle2 className="h-6 w-6 text-sky-400 opacity-80" />
          </CardContent>
        </Card>

        <Card className="border-emerald-500/20 bg-emerald-500/5">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <span className="text-xs font-medium text-emerald-400">Resolved / Closed</span>
              <div className="text-2xl font-bold text-emerald-400 mt-1">{counts.resolved}</div>
            </div>
            <CheckCircle2 className="h-6 w-6 text-emerald-400 opacity-80" />
          </CardContent>
        </Card>
      </div>

      {/* Filter and Search */}
      <Card className="border-[hsl(var(--border))]">
        <CardContent className="p-4 flex flex-col md:flex-row items-center justify-between gap-3">
          <div className="flex items-center gap-1.5 overflow-x-auto w-full md:w-auto">
            {["ALL", "Open", "Investigating", "Mitigated", "Resolved", "Closed"].map((st) => (
              <button
                key={st}
                onClick={() => {
                  setSelectedStatus(st);
                  setPage(1);
                }}
                className={`px-3 py-1.5 text-xs font-medium rounded-md transition-colors ${
                  selectedStatus === st
                    ? "bg-[hsl(var(--primary))] text-[hsl(var(--primary-foreground))]"
                    : "text-[hsl(var(--muted-foreground))] hover:bg-[hsl(var(--accent))] hover:text-[hsl(var(--foreground))]"
                }`}
              >
                {st === "ALL" ? "All Incidents" : st}
              </button>
            ))}
          </div>

          <div className="flex items-center gap-2 w-full md:w-auto">
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

            <div className="relative flex-1 sm:w-56">
              <Search className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-[hsl(var(--muted-foreground))]" />
              <Input
                placeholder="Search incidents..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="h-9 pl-8 text-xs"
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Incidents List */}
      <div className="space-y-3">
        {isLoading ? (
          <div className="space-y-3">
            {Array.from({ length: 4 }).map((_, i) => (
              <Card key={i} className="p-4 bg-slate-900/40 border-slate-800">
                <div className="flex items-center justify-between">
                  <div className="space-y-2">
                    <Skeleton className="h-5 w-48" />
                    <Skeleton className="h-3 w-64" />
                  </div>
                  <Skeleton className="h-6 w-20" />
                </div>
              </Card>
            ))}
          </div>
        ) : filteredIncidents.length === 0 ? (
          <EmptyState
            icon={CheckCircle2}
            title="No incidents reported"
            description="No active or historical incidents meet the current filter criteria."
            actionLabel={isOperator ? "Declare Incident" : undefined}
            onAction={isOperator ? () => setIsCreateOpen(true) : undefined}
          />
        ) : (
          filteredIncidents.map((incident) => (
            <Link
              key={incident.id}
              to={`/incidents/${incident.id}`}
              className="block group"
            >
              <Card className="border-[hsl(var(--border))] transition-all hover:border-[hsl(var(--primary)/0.5)] hover:shadow-md">
                <CardContent className="p-4 sm:p-5">
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                    <div className="space-y-1.5 flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="font-mono text-xs font-semibold text-sky-400">
                          {incident.incidentNumber}
                        </span>
                        {getSeverityBadge(incident.severity)}
                        {getStatusBadge(incident.status)}
                      </div>

                      <h3 className="text-base font-semibold group-hover:text-primary transition-colors truncate">
                        {incident.title}
                      </h3>

                      <p className="text-xs text-[hsl(var(--muted-foreground))] line-clamp-1">
                        {incident.description}
                      </p>

                      <div className="flex flex-wrap items-center gap-4 text-xs text-[hsl(var(--muted-foreground))] pt-1">
                        {incident.serviceName && (
                          <span>Service: <strong className="text-foreground">{incident.serviceName}</strong></span>
                        )}
                        {incident.environmentName && (
                          <span>Env: <strong className="text-foreground">{incident.environmentName}</strong></span>
                        )}
                        <span className="flex items-center gap-1">
                          <User className="h-3 w-3" />
                          {incident.assignedToUserName ? incident.assignedToUserName : "Unassigned"}
                        </span>
                        <span className="flex items-center gap-1">
                          <History className="h-3 w-3" />
                          {incident.timelineEventsCount} events
                        </span>
                        <span className="flex items-center gap-1">
                          <MessageSquare className="h-3 w-3" />
                          {incident.commentsCount} comments
                        </span>
                      </div>
                    </div>

                    <div className="text-right sm:self-center shrink-0">
                      <span className="text-xs text-[hsl(var(--muted-foreground))]">
                        Created {new Date(incident.createdAtUtc).toLocaleString()}
                      </span>
                      <div className="mt-1">
                        <Button variant="ghost" size="sm" className="h-7 text-xs gap-1 group-hover:bg-accent">
                          View Timeline & Triage
                          <ExternalLink className="h-3 w-3" />
                        </Button>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </Link>
          ))
        )}
      </div>

      {/* Declare Incident Modal */}
      <Dialog
        open={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        title="Declare New Incident"
        description="Initiate an active operational incident with severity assignment and service telemetry linkages."
      >
        <div className="space-y-3 text-xs">
          <div className="space-y-1">
            <label className="font-medium text-slate-300">Incident Title *</label>
            <Input
              placeholder="e.g. Checkout Service elevated 500 error rates across US-East"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="bg-slate-950/80 border-slate-800 text-xs text-slate-200"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="font-medium text-slate-300">Severity *</label>
              <select
                value={severity}
                onChange={(e) => setSeverity(e.target.value)}
                className="w-full h-9 rounded-md border border-slate-800 bg-slate-950/80 px-3 py-1 text-slate-200 text-xs"
              >
                <option value="Critical">Critical (P1 - Complete outage / data loss)</option>
                <option value="Error">Error (P2 - Major feature broken)</option>
                <option value="Warning">Warning (P3 - Minor degradation)</option>
                <option value="Information">Information (P4 - Informational)</option>
              </select>
            </div>

            <div className="space-y-1">
              <label className="font-medium text-slate-300">Affected Application *</label>
              <select
                value={appId}
                onChange={(e) => setAppId(e.target.value)}
                className="w-full h-9 rounded-md border border-slate-800 bg-slate-950/80 px-3 py-1 text-slate-200 text-xs"
              >
                <option value="">Select Application</option>
                {applications.map((app) => (
                  <option key={app.id} value={app.id}>
                    {app.displayName || app.name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="font-medium text-slate-300">Service Name (optional)</label>
              <Input
                placeholder="e.g. auth-service"
                value={serviceName}
                onChange={(e) => setServiceName(e.target.value)}
                className="bg-slate-950/80 border-slate-800 text-xs text-slate-200"
              />
            </div>
            <div className="space-y-1">
              <label className="font-medium text-slate-300">Environment (optional)</label>
              <Input
                placeholder="e.g. Production"
                value={envName}
                onChange={(e) => setEnvName(e.target.value)}
                className="bg-slate-950/80 border-slate-800 text-xs text-slate-200"
              />
            </div>
          </div>

          <div className="space-y-1">
            <label className="font-medium text-slate-300">Correlated Trace ID (optional)</label>
            <Input
              placeholder="e.g. 4bf92f3577b34da6a3ce929d0e0e4736"
              value={relatedTraceId}
              onChange={(e) => setRelatedTraceId(e.target.value)}
              className="bg-slate-950/80 border-slate-800 text-xs text-slate-200"
            />
          </div>

          <div className="space-y-1">
            <label className="font-medium text-slate-300">Description & Impact</label>
            <textarea
              rows={3}
              className="w-full rounded-md border border-slate-800 bg-slate-950/80 p-2 text-xs text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500"
              placeholder="Detailed description of symptoms, customer impact, and initial triage observations..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
          </div>

          <div className="mt-4 flex justify-end gap-2 border-t border-slate-800 pt-3">
            <Button variant="ghost" size="sm" onClick={() => setIsCreateOpen(false)} className="text-xs text-slate-300">
              Cancel
            </Button>
            <Button
              size="sm"
              className="bg-blue-600 hover:bg-blue-500 text-white text-xs"
              disabled={!title.trim() || !appId || createMutation.isPending}
              onClick={() =>
                createMutation.mutate({
                  title,
                  description,
                  severity,
                  applicationId: appId,
                  serviceName: serviceName || null,
                  environmentName: envName || null,
                  relatedTraceId: relatedTraceId || null,
                  assignedToUserId: user?.id || null,
                })
              }
            >
              {createMutation.isPending ? "Declaring..." : "Declare Incident"}
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  );
}
