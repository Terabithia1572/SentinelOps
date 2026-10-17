import { useState } from "react";
import { useParams, Link } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import apiClient, {
  type ApiResponse,
  type IncidentDetailDto,
  type TransitionIncidentStatusRequest,
  analyzeIncident,
  generateIncidentPostmortem,
  suggestIncidentRemediation,
  submitIncidentFeedback,
  type IncidentAnalysisResult,
  type IncidentPostmortemResult,
  type RemediationSuggestionsResult,
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
  Sparkles,
  ThumbsUp,
  ThumbsDown,
  Terminal,
  FileText,
  Copy,
  Check,
} from "lucide-react";

export function IncidentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user, isOperator } = useAuth();
  const queryClient = useQueryClient();

  const [commentText, setCommentText] = useState("");
  const [showResolveModal, setShowResolveModal] = useState(false);
  const [rootCauseInput, setRootCauseInput] = useState("");
  const [resolutionInput, setResolutionInput] = useState("");

  // AI Intelligence State
  const [aiAnalysis, setAiAnalysis] = useState<IncidentAnalysisResult | null>(null);
  const [aiLoading, setAiLoading] = useState(false);
  const [aiRemediation, setAiRemediation] = useState<RemediationSuggestionsResult | null>(null);
  const [remediationLoading, setRemediationLoading] = useState(false);
  const [aiPostmortem, setAiPostmortem] = useState<IncidentPostmortemResult | null>(null);
  const [postmortemLoading, setPostmortemLoading] = useState(false);
  const [showPostmortemModal, setShowPostmortemModal] = useState(false);
  const [copiedPostmortem, setCopiedPostmortem] = useState(false);
  const [feedbackState, setFeedbackState] = useState<Record<string, "up" | "down">>({});

  // Live WebSocket updates
  const { isConnected } = useAlertsHub({
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

  const handleAnalyze = async () => {
    if (!id) return;
    try {
      setAiLoading(true);
      const res = await analyzeIncident(id);
      setAiAnalysis(res);
    } catch {
      // Error
    } finally {
      setAiLoading(false);
    }
  };

  const handleRemediate = async () => {
    if (!id) return;
    try {
      setRemediationLoading(true);
      const res = await suggestIncidentRemediation(id);
      setAiRemediation(res);
    } catch {
      // Error
    } finally {
      setRemediationLoading(false);
    }
  };

  const handlePostmortem = async () => {
    if (!id) return;
    try {
      setPostmortemLoading(true);
      const res = await generateIncidentPostmortem(id);
      setAiPostmortem(res);
      setShowPostmortemModal(true);
    } catch {
      // Error
    } finally {
      setPostmortemLoading(false);
    }
  };

  const handleFeedback = async (hypId: string, isHelpful: boolean) => {
    if (!id) return;
    setFeedbackState((prev) => ({ ...prev, [hypId]: isHelpful ? "up" : "down" }));
    await submitIncidentFeedback(id, { hypothesisId: hypId, isHelpful }).catch(() => {});
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
              {isConnected && (
                <span className="inline-flex items-center gap-1 text-[11px] font-medium text-emerald-400 ml-1">
                  <span className="h-1.5 w-1.5 rounded-full bg-emerald-400 animate-pulse" />
                  Live Sync
                </span>
              )}
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
          {/* AI Incident Intelligence Card */}
          <Card className="border-purple-500/30 bg-purple-950/10 shadow-sm overflow-hidden">
            <CardHeader className="p-4 border-b border-purple-500/20 bg-purple-950/20 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div>
                <CardTitle className="text-sm font-bold flex items-center gap-2 text-purple-300">
                  <Sparkles className="h-4 w-4 text-purple-400" />
                  AI Incident Intelligence & Diagnostic Assistant
                  <Badge variant="outline" className="text-[10px] bg-purple-500/10 text-purple-300 border-purple-500/30">
                    AI-Generated Heuristic Insights
                  </Badge>
                </CardTitle>
                <CardDescription className="text-xs text-purple-200/70 mt-0.5">
                  Automated telemetry correlation, root cause hypotheses, remediation playbooks, and postmortems.
                </CardDescription>
              </div>

              <div className="flex items-center gap-2">
                <Button
                  size="sm"
                  variant="outline"
                  className="h-7 text-xs gap-1.5 border-purple-500/40 text-purple-300 hover:bg-purple-500/20"
                  onClick={handleAnalyze}
                  disabled={aiLoading}
                >
                  <RefreshCw className={`h-3 w-3 ${aiLoading ? "animate-spin" : ""}`} />
                  {aiAnalysis ? "Re-Analyze" : "Analyze Incident"}
                </Button>

                <Button
                  size="sm"
                  variant="outline"
                  className="h-7 text-xs gap-1.5 border-purple-500/40 text-purple-300 hover:bg-purple-500/20"
                  onClick={handleRemediate}
                  disabled={remediationLoading}
                >
                  <Terminal className="h-3 w-3" />
                  Playbook
                </Button>

                <Button
                  size="sm"
                  className="h-7 text-xs gap-1.5 bg-purple-600 hover:bg-purple-700 text-white"
                  onClick={handlePostmortem}
                  disabled={postmortemLoading}
                >
                  <FileText className="h-3 w-3" />
                  Postmortem
                </Button>
              </div>
            </CardHeader>

            <CardContent className="p-4 space-y-4">
              {aiLoading ? (
                <div className="text-center py-6 text-xs text-purple-300 flex flex-col items-center gap-2">
                  <RefreshCw className="h-5 w-5 animate-spin text-purple-400" />
                  <span>Synthesizing error logs, trace waterfalls, and metric deviations...</span>
                </div>
              ) : !aiAnalysis && !aiRemediation ? (
                <div className="text-center py-5 text-xs text-[hsl(var(--muted-foreground))] space-y-2">
                  <p>Click &quot;Analyze Incident&quot; to execute automated AI heuristic correlation.</p>
                  <Button
                    size="sm"
                    variant="outline"
                    className="text-xs text-purple-300 border-purple-500/40"
                    onClick={handleAnalyze}
                  >
                    Run Automated Analysis
                  </Button>
                </div>
              ) : (
                <div className="space-y-4">
                  {/* Summary */}
                  {aiAnalysis && (
                    <>
                      <div className="p-3 rounded-lg bg-slate-900/60 border border-slate-800 text-xs text-slate-200">
                        <span className="font-semibold text-purple-300 block mb-1">Executive Diagnostic Summary:</span>
                        {aiAnalysis.summary}
                      </div>

                      {/* Root Cause Hypotheses */}
                      <div className="space-y-2">
                        <span className="text-xs font-semibold uppercase tracking-wider text-purple-300/80">
                          Root Cause Hypotheses ({aiAnalysis.hypotheses.length})
                        </span>

                        {aiAnalysis.hypotheses.map((hyp) => (
                          <div key={hyp.id} className="p-3 rounded-lg bg-slate-950 border border-purple-500/20 space-y-2">
                            <div className="flex items-center justify-between">
                              <span className="font-semibold text-xs text-white">{hyp.title}</span>
                              <div className="flex items-center gap-2">
                                <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-purple-900/50 text-purple-300 border border-purple-700/50">
                                  Confidence: {hyp.confidenceScore}%
                                </span>
                                <button
                                  onClick={() => handleFeedback(hyp.id, true)}
                                  className={`p-1 rounded hover:bg-slate-800 ${
                                    feedbackState[hyp.id] === "up" ? "text-emerald-400" : "text-slate-400"
                                  }`}
                                  title="Helpful"
                                >
                                  <ThumbsUp className="h-3 w-3" />
                                </button>
                                <button
                                  onClick={() => handleFeedback(hyp.id, false)}
                                  className={`p-1 rounded hover:bg-slate-800 ${
                                    feedbackState[hyp.id] === "down" ? "text-rose-400" : "text-slate-400"
                                  }`}
                                  title="Not helpful"
                                >
                                  <ThumbsDown className="h-3 w-3" />
                                </button>
                              </div>
                            </div>

                            {/* Confidence bar */}
                            <div className="w-full bg-slate-800 rounded-full h-1.5 overflow-hidden">
                              <div
                                className="bg-purple-500 h-1.5 rounded-full"
                                style={{ width: `${hyp.confidenceScore}%` }}
                              />
                            </div>

                            <p className="text-xs text-slate-300">{hyp.description}</p>

                            {/* Evidence */}
                            {hyp.evidence.length > 0 && (
                              <div className="space-y-1 pt-1">
                                <span className="text-[10px] font-semibold text-slate-400 uppercase tracking-wider">
                                  Supporting Evidence:
                                </span>
                                <ul className="list-disc pl-4 text-[11px] text-slate-400 space-y-0.5">
                                  {hyp.evidence.map((ev, i) => (
                                    <li key={i}>{ev}</li>
                                  ))}
                                </ul>
                              </div>
                            )}
                          </div>
                        ))}
                      </div>

                      {/* Correlated Telemetry Badges */}
                      <div className="p-3 rounded-lg bg-slate-900/60 border border-slate-800 flex flex-wrap items-center gap-3 text-xs">
                        <span className="text-slate-400 font-medium">Correlated Signals:</span>
                        <Badge variant="secondary" className="text-[10px]">
                          {aiAnalysis.correlatedTelemetry.logCount} Correlated Logs
                        </Badge>
                        {aiAnalysis.correlatedTelemetry.anomalousMetrics.map((m, i) => (
                          <Badge key={i} variant="warning" className="text-[10px]">
                            {m}
                          </Badge>
                        ))}
                      </div>
                    </>
                  )}

                  {/* Remediation Playbook (if fetched) */}
                  {aiRemediation && (
                    <div className="space-y-2 pt-2 border-t border-purple-500/20">
                      <span className="text-xs font-semibold uppercase tracking-wider text-purple-300/80">
                        Remediation Playbook Checklist ({aiRemediation.actions.length} Steps)
                      </span>
                      <div className="space-y-2">
                        {aiRemediation.actions.map((act) => (
                          <div key={act.stepNumber} className="p-3 rounded-lg bg-slate-950 border border-slate-800 space-y-1.5">
                            <div className="flex items-center justify-between">
                              <span className="font-semibold text-xs text-white">
                                Step {act.stepNumber}: {act.title}
                              </span>
                              <Badge
                                variant={act.riskLevel === "Low" ? "secondary" : "warning"}
                                className="text-[10px] px-1.5 py-0"
                              >
                                Risk: {act.riskLevel}
                              </Badge>
                            </div>
                            <div className="p-2 rounded bg-slate-900 font-mono text-[11px] text-emerald-400 flex items-center justify-between">
                              <code>{act.commandOrAction}</code>
                            </div>
                            <p className="text-[11px] text-slate-400">{act.rationale}</p>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}

                  {/* Similar Past Incidents */}
                  {aiAnalysis && aiAnalysis.similarIncidents.length > 0 && (
                    <div className="space-y-2 pt-2 border-t border-purple-500/20">
                      <span className="text-xs font-semibold uppercase tracking-wider text-purple-300/80">
                        Similar Historical Incidents ({aiAnalysis.similarIncidents.length})
                      </span>
                      <div className="space-y-2">
                        {aiAnalysis.similarIncidents.map((sim) => (
                          <div key={sim.incidentId} className="p-2.5 rounded bg-slate-950 border border-slate-800 text-xs flex items-center justify-between">
                            <div>
                              <span className="font-semibold text-slate-200 block">{sim.title}</span>
                              <span className="text-[10px] text-slate-400">{sim.resolutionNotes}</span>
                            </div>
                            <Badge variant="outline" className="text-[10px] whitespace-nowrap">
                              MTTR: {sim.mttrMinutes}m
                            </Badge>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              )}
            </CardContent>
          </Card>

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

      {/* AI Generated Postmortem Modal */}
      {showPostmortemModal && aiPostmortem && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
          <div className="w-full max-w-3xl max-h-[85vh] flex flex-col rounded-lg border border-purple-500/30 bg-[hsl(var(--card))] shadow-2xl animate-in zoom-in-95">
            <div className="flex items-center justify-between border-b border-[hsl(var(--border))] p-4">
              <div className="flex items-center gap-2">
                <FileText className="h-5 w-5 text-purple-400" />
                <div>
                  <h3 className="text-base font-semibold text-foreground">
                    Incident Postmortem Report
                  </h3>
                  <p className="text-xs text-[hsl(var(--muted-foreground))]">
                    Generated: {new Date(aiPostmortem.generatedAtUtc).toLocaleString()} • AI Diagnostic Engine
                  </p>
                </div>
              </div>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setShowPostmortemModal(false)}
                className="h-8 w-8 p-0"
              >
                ✕
              </Button>
            </div>

            <div className="flex-1 overflow-y-auto p-4 space-y-4">
              <div className="rounded-md border border-[hsl(var(--border))] bg-slate-950 p-4 font-mono text-xs text-slate-200 whitespace-pre-wrap selection:bg-purple-800">
                {aiPostmortem.markdownContent}
              </div>
            </div>

            <div className="flex items-center justify-between border-t border-[hsl(var(--border))] p-4">
              <div className="text-[11px] text-[hsl(var(--muted-foreground))]">
                Ready for export to GitHub, Jira, or Confluence.
              </div>
              <div className="flex items-center gap-2">
                <Button
                  size="sm"
                  variant="outline"
                  className="gap-1.5 text-xs"
                  onClick={() => {
                    navigator.clipboard.writeText(aiPostmortem.markdownContent);
                    setCopiedPostmortem(true);
                    setTimeout(() => setCopiedPostmortem(false), 2000);
                  }}
                >
                  {copiedPostmortem ? <Check className="h-3.5 w-3.5 text-emerald-400" /> : <Copy className="h-3.5 w-3.5" />}
                  {copiedPostmortem ? "Copied!" : "Copy Markdown"}
                </Button>
                <Button
                  size="sm"
                  variant="default"
                  onClick={() => setShowPostmortemModal(false)}
                >
                  Close
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
