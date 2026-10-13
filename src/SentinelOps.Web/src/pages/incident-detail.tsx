import { useState } from "react";
import { useParams, Link } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import apiClient, {
  type ApiResponse,
  type IncidentDetailDto,
  type TransitionIncidentStatusRequest,
} from "@/lib/api-client";
import { useAuth } from "@/context/auth-context";
import { useAlertsHub } from "@/hooks/use-alerts-hub";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  ArrowLeft,
  AlertTriangle,
  User,
  History,
  MessageSquare,
  Send,
  ExternalLink,
  RefreshCw,
} from "lucide-react";

export function IncidentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user, isOperator } = useAuth();
  const queryClient = useQueryClient();

  const [commentText, setCommentText] = useState("");
  const [showResolveModal, setShowResolveModal] = useState(false);
  const [rootCauseInput, setRootCauseInput] = useState("");
  const [resolutionInput, setResolutionInput] = useState("");

  // Live WebSocket updates
  useAlertsHub({
    onIncidentUpdated: (inc) => {
      if (inc.id === id) {
        queryClient.invalidateQueries({ queryKey: ["incident", id] });
      }
    },
  });

  // Incident Query
  const { data: incident, isLoading, error } = useQuery({
    queryKey: ["incident", id],
    queryFn: async () => {
      if (!id) throw new Error("Incident ID is required");
      const res = await apiClient.get<ApiResponse<IncidentDetailDto>>(`/incidents/${id}`);
      return res.data.data;
    },
    enabled: !!id,
    refetchInterval: 10000,
  });

  // Transition Status Mutation
  const transitionMutation = useMutation({
    mutationFn: async (req: TransitionIncidentStatusRequest) => {
      if (!id) return;
      await apiClient.post(`/incidents/${id}/transition`, req);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["incident", id] });
      queryClient.invalidateQueries({ queryKey: ["incidents"] });
      setShowResolveModal(false);
    },
  });

  // Assign Mutation
  const assignMutation = useMutation({
    mutationFn: async ({ userId, notes }: { userId?: string | null; notes?: string }) => {
      if (!id) return;
      await apiClient.post(`/incidents/${id}/assign`, {
        assignedToUserId: userId,
        notes,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["incident", id] });
      queryClient.invalidateQueries({ queryKey: ["incidents"] });
    },
  });

  // Add Comment Mutation
  const commentMutation = useMutation({
    mutationFn: async (text: string) => {
      if (!id) return;
      await apiClient.post(`/incidents/${id}/comments`, { commentText: text });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["incident", id] });
      setCommentText("");
    },
  });

  if (isLoading) {
    return (
      <div className="py-24 text-center text-sm text-[hsl(var(--muted-foreground))] flex items-center justify-center gap-2">
        <RefreshCw className="h-4 w-4 animate-spin" />
        Loading incident details...
      </div>
    );
  }

  if (error || !incident) {
    return (
      <div className="py-24 text-center">
        <AlertTriangle className="mx-auto h-10 w-10 text-rose-400 mb-2" />
        <h3 className="font-semibold text-base">Incident Not Found</h3>
        <p className="text-xs text-[hsl(var(--muted-foreground))] mt-1">
          The requested incident does not exist or you do not have permission to view it.
        </p>
        <Link to="/incidents" className="mt-4 inline-block">
          <Button size="sm">Back to Incidents</Button>
        </Link>
      </div>
    );
  }

  const getSeverityBadge = (sev: string) => {
    switch (sev.toLowerCase()) {
      case "critical":
        return <Badge variant="danger">Critical (P1)</Badge>;
      case "error":
        return <Badge variant="danger">Error (P2)</Badge>;
      case "warning":
        return <Badge variant="warning">Warning (P3)</Badge>;
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
      {/* Top Breadcrumb & Status Action Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <Link to="/incidents">
            <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
              <ArrowLeft className="h-4 w-4" />
            </Button>
          </Link>
          <div>
            <div className="flex items-center gap-2">
              <span className="font-mono text-xs font-semibold text-sky-400">
                {incident.incidentNumber}
              </span>
              {getSeverityBadge(incident.severity)}
              {getStatusBadge(incident.status)}
            </div>
            <h1 className="text-xl font-bold tracking-tight mt-0.5">{incident.title}</h1>
          </div>
        </div>

        {/* Status transition action buttons */}
        {isOperator && (
          <div className="flex items-center gap-2">
            {incident.status === "Open" && (
              <Button
                size="sm"
                className="bg-amber-600 hover:bg-amber-700 text-white"
                onClick={() =>
                  transitionMutation.mutate({
                    status: "Investigating",
                    note: "Assigned for immediate investigation",
                  })
                }
                disabled={transitionMutation.isPending}
              >
                Acknowledge & Investigate
              </Button>
            )}

            {incident.status === "Investigating" && (
              <Button
                size="sm"
                className="bg-sky-600 hover:bg-sky-700 text-white"
                onClick={() =>
                  transitionMutation.mutate({
                    status: "Mitigated",
                    note: "Service mitigation applied",
                  })
                }
                disabled={transitionMutation.isPending}
              >
                Mark Mitigated
              </Button>
            )}

            {(incident.status === "Investigating" || incident.status === "Mitigated") && (
              <Button
                size="sm"
                className="bg-emerald-600 hover:bg-emerald-700 text-white"
                onClick={() => {
                  setRootCauseInput(incident.rootCauseSummary || "");
                  setResolutionInput(incident.resolutionNotes || "");
                  setShowResolveModal(true);
                }}
                disabled={transitionMutation.isPending}
              >
                Resolve Incident
              </Button>
            )}

            {incident.status === "Resolved" && (
              <Button
                size="sm"
                variant="outline"
                onClick={() =>
                  transitionMutation.mutate({
                    status: "Closed",
                    note: "Incident postmortem verified and closed",
                  })
                }
                disabled={transitionMutation.isPending}
              >
                Close Incident
              </Button>
            )}

            {(incident.status === "Resolved" || incident.status === "Closed") && (
              <Button
                size="sm"
                variant="outline"
                className="text-amber-400"
                onClick={() =>
                  transitionMutation.mutate({
                    status: "Investigating",
                    note: "Incident reopened due to recurring telemetry breach",
                  })
                }
                disabled={transitionMutation.isPending}
              >
                Reopen
              </Button>
            )}
          </div>
        )}
      </div>

      {/* Main Grid: Details (Left) + Timeline & Discussion (Right) */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left Column: Details & Postmortem */}
        <div className="space-y-6 lg:col-span-1">
          {/* Metadata Card */}
          <Card className="border-[hsl(var(--border))]">
            <CardHeader className="p-4 border-b border-[hsl(var(--border))]">
              <CardTitle className="text-sm font-semibold">Incident Overview</CardTitle>
            </CardHeader>
            <CardContent className="p-4 space-y-3 text-xs">
              <div>
                <span className="text-[hsl(var(--muted-foreground))]">Description</span>
                <p className="font-normal mt-0.5 whitespace-pre-wrap">{incident.description}</p>
              </div>

              <div className="grid grid-cols-2 gap-2 pt-2 border-t border-[hsl(var(--border))]">
                <div>
                  <span className="text-[hsl(var(--muted-foreground))]">Service</span>
                  <p className="font-semibold text-foreground mt-0.5">{incident.serviceName || "—"}</p>
                </div>
                <div>
                  <span className="text-[hsl(var(--muted-foreground))]">Environment</span>
                  <p className="font-semibold text-foreground mt-0.5">{incident.environmentName || "—"}</p>
                </div>
              </div>

              <div className="pt-2 border-t border-[hsl(var(--border))]">
                <span className="text-[hsl(var(--muted-foreground))]">Lead Responder</span>
                <div className="flex items-center justify-between mt-1">
                  <div className="flex items-center gap-1.5">
                    <User className="h-4 w-4 text-[hsl(var(--muted-foreground))]" />
                    <span className="font-semibold text-foreground">
                      {incident.assignedToUserName || "Unassigned"}
                    </span>
                  </div>
                  {isOperator && user && incident.assignedToUserId !== user.id && (
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-6 text-[11px] text-sky-400 hover:text-sky-300 px-1.5"
                      onClick={() => assignMutation.mutate({ userId: user.id, notes: "Self-assigned" })}
                    >
                      Assign to me
                    </Button>
                  )}
                </div>
              </div>

              {/* Related Trace ID */}
              {incident.relatedTraceId && (
                <div className="pt-2 border-t border-[hsl(var(--border))]">
                  <span className="text-[hsl(var(--muted-foreground))]">Correlated Distributed Trace</span>
                  <div className="mt-1 flex items-center justify-between">
                    <span className="font-mono text-[11px] truncate text-sky-400 max-w-[200px]">
                      {incident.relatedTraceId}
                    </span>
                    <Link to={`/traces?traceId=${incident.relatedTraceId}`}>
                      <Button variant="ghost" size="sm" className="h-6 text-xs p-1">
                        <ExternalLink className="h-3 w-3" />
                      </Button>
                    </Link>
                  </div>
                </div>
              )}

              {/* Linked Alert Rules */}
              {incident.linkedAlerts && incident.linkedAlerts.length > 0 && (
                <div className="pt-2 border-t border-[hsl(var(--border))]">
                  <span className="text-[hsl(var(--muted-foreground))]">Triggering Alert Rules</span>
                  <div className="mt-1 space-y-1">
                    {incident.linkedAlerts.map((rule) => (
                      <Link
                        key={rule.id}
                        to="/alert-rules"
                        className="flex items-center justify-between text-[11px] p-1.5 rounded bg-accent/30 hover:bg-accent/60 transition-colors"
                      >
                        <span className="font-medium text-foreground truncate">{rule.name}</span>
                        <Badge variant="secondary" className="text-[10px] py-0 px-1">{rule.conditionType}</Badge>
                      </Link>
                    ))}
                  </div>
                </div>
              )}

              {/* Lifecycle Timestamps */}
              <div className="pt-2 border-t border-[hsl(var(--border))] space-y-1.5 text-[11px] text-[hsl(var(--muted-foreground))]">
                <div className="flex justify-between">
                  <span>Created:</span>
                  <span className="text-foreground">{new Date(incident.createdAtUtc).toLocaleString()}</span>
                </div>
                {incident.acknowledgedAtUtc && (
                  <div className="flex justify-between">
                    <span>Acknowledged by {incident.acknowledgedBy}:</span>
                    <span className="text-foreground">{new Date(incident.acknowledgedAtUtc).toLocaleString()}</span>
                  </div>
                )}
                {incident.mitigatedAtUtc && (
                  <div className="flex justify-between">
                    <span>Mitigated by {incident.mitigatedBy}:</span>
                    <span className="text-foreground">{new Date(incident.mitigatedAtUtc).toLocaleString()}</span>
                  </div>
                )}
                {incident.resolvedAtUtc && (
                  <div className="flex justify-between">
                    <span>Resolved by {incident.resolvedBy}:</span>
                    <span className="text-foreground">{new Date(incident.resolvedAtUtc).toLocaleString()}</span>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Root Cause & Postmortem */}
          {(incident.rootCauseSummary || incident.resolutionNotes) && (
            <Card className="border-[hsl(var(--border))]">
              <CardHeader className="p-4 border-b border-[hsl(var(--border))]">
                <CardTitle className="text-sm font-semibold">Postmortem & Resolution Notes</CardTitle>
              </CardHeader>
              <CardContent className="p-4 space-y-3 text-xs">
                {incident.rootCauseSummary && (
                  <div>
                    <span className="font-medium text-[hsl(var(--muted-foreground))]">Root Cause Summary</span>
                    <p className="mt-0.5 text-foreground whitespace-pre-wrap">{incident.rootCauseSummary}</p>
                  </div>
                )}
                {incident.resolutionNotes && (
                  <div className="pt-2 border-t border-[hsl(var(--border))]">
                    <span className="font-medium text-[hsl(var(--muted-foreground))]">Resolution Details</span>
                    <p className="mt-0.5 text-foreground whitespace-pre-wrap">{incident.resolutionNotes}</p>
                  </div>
                )}
              </CardContent>
            </Card>
          )}
        </div>

        {/* Right Column: Timeline & Discussion */}
        <div className="space-y-6 lg:col-span-2">
          {/* Timeline */}
          <Card className="border-[hsl(var(--border))]">
            <CardHeader className="p-4 border-b border-[hsl(var(--border))]">
              <CardTitle className="text-sm font-semibold flex items-center gap-2">
                <History className="h-4 w-4 text-sky-400" />
                Audit Trail & Incident Timeline ({incident.timelineEvents?.length || 0})
              </CardTitle>
              <CardDescription className="text-xs">
                Immutable event stream of all status changes, severity updates, and triage steps
              </CardDescription>
            </CardHeader>
            <CardContent className="p-4">
              <div className="relative pl-6 space-y-4 before:absolute before:left-2 before:top-2 before:bottom-2 before:w-0.5 before:bg-[hsl(var(--border))]">
                {incident.timelineEvents && incident.timelineEvents.length > 0 ? (
                  incident.timelineEvents.map((evt) => (
                    <div key={evt.id} className="relative flex items-start gap-3">
                      <div className="absolute -left-6 top-1 h-2.5 w-2.5 rounded-full bg-sky-400 border-2 border-[hsl(var(--card))]" />
                      <div className="flex-1 rounded-md bg-[hsl(var(--accent)/0.3)] p-3 border border-[hsl(var(--border))]">
                        <div className="flex items-center justify-between text-xs">
                          <span className="font-semibold text-foreground">{evt.title}</span>
                          <span className="text-[10px] text-[hsl(var(--muted-foreground))]">
                            {new Date(evt.timestampUtc).toLocaleString()}
                          </span>
                        </div>
                        <p className="text-xs text-[hsl(var(--muted-foreground))] mt-1">
                          {evt.description}
                        </p>
                        <span className="text-[10px] text-sky-400/80 mt-1 block">
                          Actor: {evt.actorName}
                        </span>
                      </div>
                    </div>
                  ))
                ) : (
                  <p className="text-xs text-[hsl(var(--muted-foreground))] italic">No timeline events recorded.</p>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Discussion Thread */}
          <Card className="border-[hsl(var(--border))]">
            <CardHeader className="p-4 border-b border-[hsl(var(--border))]">
              <CardTitle className="text-sm font-semibold flex items-center gap-2">
                <MessageSquare className="h-4 w-4 text-amber-400" />
                Responder Communication & Notes ({incident.comments?.length || 0})
              </CardTitle>
            </CardHeader>
            <CardContent className="p-4 space-y-4">
              {/* Existing comments */}
              <div className="space-y-3">
                {incident.comments && incident.comments.length > 0 ? (
                  incident.comments.map((comment) => (
                    <div
                      key={comment.id}
                      className="p-3 rounded-md bg-[hsl(var(--card))] border border-[hsl(var(--border))] text-xs"
                    >
                      <div className="flex items-center justify-between text-[11px] text-[hsl(var(--muted-foreground))] pb-1 border-b border-border/40">
                        <span className="font-semibold text-foreground">{comment.userName}</span>
                        <span>{new Date(comment.createdAtUtc).toLocaleString()}</span>
                      </div>
                      <p className="mt-2 text-foreground whitespace-pre-wrap">{comment.commentText}</p>
                    </div>
                  ))
                ) : (
                  <p className="text-xs text-[hsl(var(--muted-foreground))] italic py-2">
                    No responder notes yet. Add thoughts, commands executed, or diagnostic observations below.
                  </p>
                )}
              </div>

              {/* Add comment input */}
              <div className="pt-2 border-t border-[hsl(var(--border))] space-y-2">
                <textarea
                  rows={3}
                  value={commentText}
                  onChange={(e) => setCommentText(e.target.value)}
                  placeholder="Add responder triage note, mitigation update, or diagnostic runbook log..."
                  className="w-full rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] p-2.5 text-xs text-[hsl(var(--foreground))]"
                />
                <div className="flex justify-end">
                  <Button
                    size="sm"
                    className="gap-1.5"
                    disabled={!commentText.trim() || commentMutation.isPending}
                    onClick={() => commentMutation.mutate(commentText)}
                  >
                    <Send className="h-3.5 w-3.5" />
                    {commentMutation.isPending ? "Posting..." : "Post Note"}
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Resolve Incident Modal */}
      {showResolveModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
          <div className="w-full max-w-lg rounded-lg border border-[hsl(var(--border))] bg-[hsl(var(--card))] p-6 shadow-xl animate-in zoom-in-95">
            <h3 className="text-base font-semibold">Resolve Incident</h3>
            <p className="text-xs text-[hsl(var(--muted-foreground))] mt-1">
              Provide resolution notes and root cause summary for auditing and SLA tracking.
            </p>

            <div className="mt-4 space-y-3 text-xs">
              <div className="space-y-1">
                <label className="font-medium">Root Cause Summary *</label>
                <textarea
                  rows={2}
                  value={rootCauseInput}
                  onChange={(e) => setRootCauseInput(e.target.value)}
                  placeholder="e.g. Memory leak in background worker cache caused OOM pod restart"
                  className="w-full rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] p-2 text-xs"
                />
              </div>

              <div className="space-y-1">
                <label className="font-medium">Resolution Notes *</label>
                <textarea
                  rows={3}
                  value={resolutionInput}
                  onChange={(e) => setResolutionInput(e.target.value)}
                  placeholder="e.g. Rolled back deployment to v1.4.2; cache expiration verified in Redis"
                  className="w-full rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] p-2 text-xs"
                />
              </div>
            </div>

            <div className="mt-6 flex justify-end gap-2 border-t border-[hsl(var(--border))] pt-3">
              <Button variant="ghost" size="sm" onClick={() => setShowResolveModal(false)}>
                Cancel
              </Button>
              <Button
                size="sm"
                className="bg-emerald-600 hover:bg-emerald-700 text-white"
                disabled={!rootCauseInput.trim() || !resolutionInput.trim() || transitionMutation.isPending}
                onClick={() =>
                  transitionMutation.mutate({
                    status: "Resolved",
                    rootCauseSummary: rootCauseInput,
                    resolutionNotes: resolutionInput,
                  })
                }
              >
                {transitionMutation.isPending ? "Resolving..." : "Confirm Resolution"}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
