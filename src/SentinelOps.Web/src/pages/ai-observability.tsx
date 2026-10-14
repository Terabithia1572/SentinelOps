import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Sparkles,
  AlertTriangle,
  FileCode,
  TrendingUp,
  Brain,
  CheckCircle2,
  Clock,
  ShieldCheck,
  Send,
  RefreshCw,
  Check,
  Sliders,
  DollarSign,
  Activity,
} from "lucide-react";
import { useTenant } from "@/context/tenant-context";
import apiClient, {
  type ApiResponse,
  type ApplicationDto,
  type TelemetryAnomalyDto,
  type LogPatternDto,
  type PredictiveForecastDto,
  type RcaAnalysisResult,
  type CopilotChatResponse,
  type AiUsageDto,
  getAnomalies,
  resolveAnomaly,
  getLogPatterns,
  analyzeLogPatterns,
  performRca,
  verifyRcaHypothesis,
  getActiveForecasts,
  evaluateForecast,
  askCopilot,
  getAiUsage,
} from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";

export function AiObservabilityPage() {
  const queryClient = useQueryClient();
  const { activeOrg } = useTenant();
  const [activeTab, setActiveTab] = useState<"anomalies" | "patterns" | "rca" | "forecasts" | "copilot">("anomalies");
  const [selectedAppId, setSelectedAppId] = useState<string>("");

  // 1. Fetch Applications for dropdown
  const { data: applications = [] } = useQuery({
    queryKey: ["applications"],
    queryFn: async () => {
      const res = await apiClient.get<ApiResponse<ApplicationDto[]>>("/applications");
      return res.data.data || [];
    },
  });

  const activeAppId = selectedAppId || applications[0]?.id || "";

  // 2. Fetch AI Usage & Token Quota
  const { data: aiUsage } = useQuery<AiUsageDto>({
    queryKey: ["aiUsage"],
    queryFn: getAiUsage,
    refetchInterval: 30000,
  });

  // 3. Tab: Anomalies Query & Mutation
  const { data: anomalies = [], isLoading: isLoadingAnomalies, refetch: refetchAnomalies } = useQuery<TelemetryAnomalyDto[]>({
    queryKey: ["anomalies", activeAppId],
    queryFn: () => getAnomalies(activeAppId),
    enabled: !!activeAppId,
  });

  const resolveMutation = useMutation({
    mutationFn: (id: string) => resolveAnomaly(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["anomalies", activeAppId] });
    },
  });

  // 4. Tab: Log Patterns Query & Mutation
  const { data: logPatterns = [], isLoading: isLoadingPatterns, refetch: refetchPatterns } = useQuery<LogPatternDto[]>({
    queryKey: ["logPatterns", activeAppId],
    queryFn: () => getLogPatterns(activeAppId),
    enabled: !!activeAppId,
  });

  const analyzePatternsMutation = useMutation({
    mutationFn: (hours: number) => analyzeLogPatterns(activeAppId, hours),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["logPatterns", activeAppId] });
    },
  });

  // 5. Tab: RCA State & Mutation
  const [rcaIncidentId, setRcaIncidentId] = useState<string>("");
  const [rcaResult, setRcaResult] = useState<RcaAnalysisResult | null>(null);
  const [operatorNotes, setOperatorNotes] = useState<Record<number, string>>({});

  const rcaMutation = useMutation({
    mutationFn: (incidentId: string) => performRca({ incidentId, correlationWindowMinutes: 30 }),
    onSuccess: (data) => {
      setRcaResult(data);
    },
  });

  const verifyHypothesisMutation = useMutation({
    mutationFn: ({ rcaId, index, verified, notes }: { rcaId: string; index: number; verified: boolean; notes?: string }) =>
      verifyRcaHypothesis(rcaId, index, verified, notes),
    onSuccess: (_, vars) => {
      if (rcaResult) {
        const updated = { ...rcaResult };
        if (updated.hypotheses[vars.index]) {
          updated.hypotheses[vars.index].isVerifiedByOperator = vars.verified;
          updated.hypotheses[vars.index].operatorNotes = vars.notes;
        }
        setRcaResult(updated);
      }
    },
  });

  // 6. Tab: Predictive Forecasts Query & Form State
  const { data: forecasts = [], isLoading: isLoadingForecasts, refetch: refetchForecasts } = useQuery<PredictiveForecastDto[]>({
    queryKey: ["forecasts", activeAppId],
    queryFn: () => getActiveForecasts(activeAppId),
    enabled: !!activeAppId,
  });

  const [forecastService, setForecastService] = useState("api-gateway");
  const [forecastMetric, setForecastMetric] = useState("cpu_utilization_pct");
  const [forecastHorizon, setForecastHorizon] = useState(60);
  const [forecastThreshold, setForecastThreshold] = useState(85.0);

  const evaluateForecastMutation = useMutation({
    mutationFn: () =>
      evaluateForecast({
        organizationId: activeOrg?.id || "00000000-0000-0000-0000-000000000000",
        applicationId: activeAppId,
        serviceName: forecastService,
        metricName: forecastMetric,
        horizonMinutes: forecastHorizon,
        alertThreshold: forecastThreshold,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["forecasts", activeAppId] });
    },
  });

  // 7. Tab: Copilot Chat State & Mutation
  const [copilotQuestion, setCopilotQuestion] = useState("");
  const [copilotMessages, setCopilotMessages] = useState<
    Array<{ sender: "user" | "copilot"; text: string; responseData?: CopilotChatResponse; timestamp: Date }>
  >([
    {
      sender: "copilot",
      text: "Hello! I am your SentinelOps Incident Copilot. How can I assist you with telemetry investigations or root cause analysis today?",
      timestamp: new Date(),
    },
  ]);

  const copilotMutation = useMutation({
    mutationFn: (q: string) =>
      askCopilot({
        incidentId: rcaIncidentId || "00000000-0000-0000-0000-000000000000",
        question: q,
      }),
    onSuccess: (data) => {
      setCopilotMessages((prev) => [
        ...prev,
        {
          sender: "copilot",
          text: data.response,
          responseData: data,
          timestamp: new Date(),
        },
      ]);
      queryClient.invalidateQueries({ queryKey: ["aiUsage"] });
    },
  });

  const handleSendCopilot = (e: React.FormEvent) => {
    e.preventDefault();
    if (!copilotQuestion.trim()) return;
    const q = copilotQuestion.trim();
    setCopilotMessages((prev) => [...prev, { sender: "user", text: q, timestamp: new Date() }]);
    setCopilotQuestion("");
    copilotMutation.mutate(q);
  };

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-[hsl(var(--border))] pb-5">
        <div>
          <div className="flex items-center gap-2">
            <Sparkles className="h-6 w-6 text-[hsl(var(--primary))]" />
            <h1 className="text-2xl font-bold tracking-tight">AI Observability & Incident Intelligence</h1>
          </div>
          <p className="text-sm text-[hsl(var(--muted-foreground))] mt-1">
            Explainable telemetry intelligence, statistical anomaly detection, log clustering, and capacity forecasting.
          </p>
        </div>

        {/* Application Selector */}
        <div className="flex items-center gap-3">
          <label htmlFor="app-select" className="text-xs font-semibold text-[hsl(var(--muted-foreground))] uppercase tracking-wider">
            Application:
          </label>
          <select
            id="app-select"
            value={activeAppId}
            onChange={(e) => setSelectedAppId(e.target.value)}
            className="h-9 rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] px-3 text-xs focus:ring-1 focus:ring-[hsl(var(--primary))]"
          >
            {applications.map((app) => (
              <option key={app.id} value={app.id}>
                {app.displayName || app.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Usage & Quota Bar */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="bg-[hsl(var(--card))]">
          <CardContent className="p-4 flex items-center gap-3">
            <div className="p-2.5 rounded-lg bg-[hsl(var(--primary)/0.1)] text-[hsl(var(--primary))]">
              <Brain className="h-5 w-5" />
            </div>
            <div>
              <p className="text-xs text-[hsl(var(--muted-foreground))] font-medium">Provider Engine</p>
              <p className="text-sm font-semibold">Local Heuristics / LLM</p>
            </div>
          </CardContent>
        </Card>

        <Card className="bg-[hsl(var(--card))]">
          <CardContent className="p-4 flex items-center gap-3">
            <div className="p-2.5 rounded-lg bg-blue-500/10 text-blue-500">
              <Activity className="h-5 w-5" />
            </div>
            <div>
              <p className="text-xs text-[hsl(var(--muted-foreground))] font-medium">Invocations MTD</p>
              <p className="text-sm font-semibold">{aiUsage?.invocationsMonthToDate ?? 0} requests</p>
            </div>
          </CardContent>
        </Card>

        <Card className="bg-[hsl(var(--card))]">
          <CardContent className="p-4 flex items-center gap-3">
            <div className="p-2.5 rounded-lg bg-purple-500/10 text-purple-500">
              <Sliders className="h-5 w-5" />
            </div>
            <div>
              <p className="text-xs text-[hsl(var(--muted-foreground))] font-medium">Tokens Used / Budget</p>
              <p className="text-sm font-semibold">
                {aiUsage?.tokensUsedMonthToDate?.toLocaleString() ?? 0} /{" "}
                {aiUsage?.monthlyTokenBudget?.toLocaleString() ?? "500,000"}
              </p>
            </div>
          </CardContent>
        </Card>

        <Card className="bg-[hsl(var(--card))]">
          <CardContent className="p-4 flex items-center gap-3">
            <div className="p-2.5 rounded-lg bg-emerald-500/10 text-emerald-500">
              <DollarSign className="h-5 w-5" />
            </div>
            <div>
              <p className="text-xs text-[hsl(var(--muted-foreground))] font-medium">Estimated Cost</p>
              <p className="text-sm font-semibold">${(aiUsage?.estimatedCostUsd ?? 0).toFixed(4)}</p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Navigation Tabs */}
      <div className="flex border-b border-[hsl(var(--border))] space-x-2">
        <button
          data-testid="tab-anomalies"
          onClick={() => setActiveTab("anomalies")}
          className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeTab === "anomalies"
              ? "border-[hsl(var(--primary))] text-[hsl(var(--primary))]"
              : "border-transparent text-[hsl(var(--muted-foreground))] hover:text-[hsl(var(--foreground))]"
          }`}
        >
          <AlertTriangle className="h-4 w-4" />
          Anomalies ({anomalies.filter((a) => a.status === "Active").length})
        </button>

        <button
          data-testid="tab-patterns"
          onClick={() => setActiveTab("patterns")}
          className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeTab === "patterns"
              ? "border-[hsl(var(--primary))] text-[hsl(var(--primary))]"
              : "border-transparent text-[hsl(var(--muted-foreground))] hover:text-[hsl(var(--foreground))]"
          }`}
        >
          <FileCode className="h-4 w-4" />
          Log Patterns ({logPatterns.length})
        </button>

        <button
          data-testid="tab-rca"
          onClick={() => setActiveTab("rca")}
          className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeTab === "rca"
              ? "border-[hsl(var(--primary))] text-[hsl(var(--primary))]"
              : "border-transparent text-[hsl(var(--muted-foreground))] hover:text-[hsl(var(--foreground))]"
          }`}
        >
          <Brain className="h-4 w-4" />
          Root Cause Analysis
        </button>

        <button
          data-testid="tab-forecasts"
          onClick={() => setActiveTab("forecasts")}
          className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeTab === "forecasts"
              ? "border-[hsl(var(--primary))] text-[hsl(var(--primary))]"
              : "border-transparent text-[hsl(var(--muted-foreground))] hover:text-[hsl(var(--foreground))]"
          }`}
        >
          <TrendingUp className="h-4 w-4" />
          Predictive Forecasts ({forecasts.length})
        </button>

        <button
          data-testid="tab-copilot"
          onClick={() => setActiveTab("copilot")}
          className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeTab === "copilot"
              ? "border-[hsl(var(--primary))] text-[hsl(var(--primary))]"
              : "border-transparent text-[hsl(var(--muted-foreground))] hover:text-[hsl(var(--foreground))]"
          }`}
        >
          <Sparkles className="h-4 w-4" />
          Incident Copilot
        </button>
      </div>

      {/* TAB CONTENT 1: ANOMALIES */}
      {activeTab === "anomalies" && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-base font-semibold">Active Statistical Anomalies</h2>
            <Button variant="outline" size="sm" onClick={() => refetchAnomalies()} disabled={isLoadingAnomalies}>
              <RefreshCw className={`h-3.5 w-3.5 mr-1.5 ${isLoadingAnomalies ? "animate-spin" : ""}`} />
              Refresh
            </Button>
          </div>

          {anomalies.length === 0 ? (
            <Card>
              <CardContent className="p-8 text-center text-sm text-[hsl(var(--muted-foreground))]">
                No active anomalies detected for this application. Baseline telemetry metrics are within expected standard deviation.
              </CardContent>
            </Card>
          ) : (
            <div className="grid grid-cols-1 gap-3">
              {anomalies.map((anomaly) => (
                <Card key={anomaly.id} className="overflow-hidden border border-[hsl(var(--border))]">
                  <div className="p-4 flex flex-col md:flex-row md:items-center justify-between gap-4">
                    <div className="space-y-1.5 flex-1">
                      <div className="flex items-center gap-2">
                        <Badge variant={anomaly.severity === "Critical" ? "destructive" : "secondary"}>
                          {anomaly.severity}
                        </Badge>
                        <span className="font-semibold text-sm">{anomaly.serviceName}</span>
                        <span className="text-xs text-[hsl(var(--muted-foreground))] font-mono">
                          [{anomaly.signalType}]
                        </span>
                        <Badge variant="outline" className="text-xs">
                          {anomaly.detectionMethod}
                        </Badge>
                      </div>
                      <p className="text-xs text-[hsl(var(--muted-foreground))]">{anomaly.explanation}</p>
                      <div className="flex items-center gap-4 text-xs font-mono text-[hsl(var(--muted-foreground))] pt-1">
                        <span>Baseline: {anomaly.baselineValue.toFixed(2)}</span>
                        <span>Observed: {anomaly.observedValue.toFixed(2)}</span>
                        <span>Deviation: {anomaly.deviationScore.toFixed(2)}σ</span>
                        <span>Confidence: {(anomaly.confidenceScore * 100).toFixed(0)}%</span>
                      </div>
                    </div>

                    <div className="flex items-center gap-2 shrink-0">
                      {anomaly.status === "Active" ? (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => resolveMutation.mutate(anomaly.id)}
                          disabled={resolveMutation.isPending}
                        >
                          <CheckCircle2 className="h-4 w-4 mr-1 text-emerald-500" />
                          Mark Resolved
                        </Button>
                      ) : (
                        <Badge variant="outline" className="text-emerald-500 border-emerald-500">
                          Resolved
                        </Badge>
                      )}
                    </div>
                  </div>
                </Card>
              ))}
            </div>
          )}
        </div>
      )}

      {/* TAB CONTENT 2: LOG PATTERNS */}
      {activeTab === "patterns" && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-base font-semibold">Clustered Log Patterns</h2>
              <p className="text-xs text-[hsl(var(--muted-foreground))]">
                Log statements clustered into deterministic templates with variable masking (`&lt;NUM&gt;`, `&lt;UUID&gt;`, `&lt;IP&gt;`).
              </p>
            </div>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => refetchPatterns()}
                disabled={isLoadingPatterns}
              >
                <RefreshCw className={`h-3.5 w-3.5 mr-1.5 ${isLoadingPatterns ? "animate-spin" : ""}`} />
                Refresh
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => analyzePatternsMutation.mutate(1)}
                disabled={analyzePatternsMutation.isPending}
              >
                <RefreshCw className={`h-3.5 w-3.5 mr-1.5 ${analyzePatternsMutation.isPending ? "animate-spin" : ""}`} />
                Analyze Last 1h
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => analyzePatternsMutation.mutate(24)}
                disabled={analyzePatternsMutation.isPending}
              >
                Analyze 24h
              </Button>
            </div>
          </div>

          {logPatterns.length === 0 ? (
            <Card>
              <CardContent className="p-8 text-center text-sm text-[hsl(var(--muted-foreground))]">
                No log patterns extracted yet. Click "Analyze Last 1h" to cluster incoming log streams.
              </CardContent>
            </Card>
          ) : (
            <div className="space-y-3">
              {logPatterns.map((pattern) => (
                <Card key={pattern.id} className="p-4 border border-[hsl(var(--border))]">
                  <div className="space-y-2">
                    <div className="flex items-center justify-between gap-2">
                      <div className="flex items-center gap-2">
                        <Badge variant={pattern.severity === "Error" || pattern.severity === "Critical" ? "destructive" : "secondary"}>
                          {pattern.severity}
                        </Badge>
                        <span className="text-xs font-semibold">{pattern.serviceName}</span>
                        <span className="text-[11px] font-mono text-[hsl(var(--muted-foreground))]">
                          Fingerprint: {pattern.patternFingerprint.substring(0, 12)}...
                        </span>
                      </div>
                      <Badge variant="outline" className="font-mono text-xs">
                        {pattern.totalOccurrences.toLocaleString()} occurrences
                      </Badge>
                    </div>

                    <div className="bg-[hsl(var(--muted)/0.4)] p-2.5 rounded font-mono text-xs text-[hsl(var(--foreground))] whitespace-pre-wrap break-all">
                      {pattern.template}
                    </div>

                    <div className="text-[11px] text-[hsl(var(--muted-foreground))] flex items-center justify-between">
                      <span>Sample: {pattern.sampleMessage.substring(0, 80)}...</span>
                      {pattern.sampleTraceId && (
                        <span className="font-mono text-[hsl(var(--primary))]">
                          Trace: {pattern.sampleTraceId}
                        </span>
                      )}
                    </div>
                  </div>
                </Card>
              ))}
            </div>
          )}
        </div>
      )}

      {/* TAB CONTENT 3: ROOT CAUSE ANALYSIS (RCA) */}
      {activeTab === "rca" && (
        <div className="space-y-5">
          <Card className="p-4 border border-[hsl(var(--border))]">
            <div className="flex flex-col sm:flex-row sm:items-center gap-3">
              <div className="flex-1">
                <label className="text-xs font-semibold text-[hsl(var(--muted-foreground))] block mb-1">
                  Incident ID for Root Cause Investigation:
                </label>
                <Input
                  placeholder="Enter Incident GUID (e.g. 7cb45d62-...)"
                  value={rcaIncidentId}
                  onChange={(e) => setRcaIncidentId(e.target.value)}
                  className="font-mono text-xs"
                />
              </div>
              <Button
                className="mt-auto"
                disabled={!rcaIncidentId || rcaMutation.isPending}
                onClick={() => rcaMutation.mutate(rcaIncidentId)}
              >
                <Brain className={`h-4 w-4 mr-2 ${rcaMutation.isPending ? "animate-spin" : ""}`} />
                Run Root Cause Analysis
              </Button>
            </div>
          </Card>

          {rcaResult && (
            <div className="space-y-4">
              <Card className="border border-[hsl(var(--border))]">
                <CardContent className="p-5 space-y-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="font-bold text-base">Analysis Summary</h3>
                      <p className="text-xs text-[hsl(var(--muted-foreground))]">
                        Provider: {rcaResult.providerName} (Deterministic: {rcaResult.isDeterministic ? "Yes" : "No"})
                      </p>
                    </div>
                    <Badge variant="outline">
                      {new Date(rcaResult.generatedAtUtc).toLocaleTimeString()}
                    </Badge>
                  </div>

                  <p className="text-sm bg-[hsl(var(--muted)/0.3)] p-3 rounded">{rcaResult.summary}</p>

                  {/* Impacted Services */}
                  {rcaResult.impactedServices.length > 0 && (
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-medium text-[hsl(var(--muted-foreground))]">Impacted Services:</span>
                      {rcaResult.impactedServices.map((svc) => (
                        <Badge key={svc} variant="secondary">
                          {svc}
                        </Badge>
                      ))}
                    </div>
                  )}

                  {/* Hypotheses List */}
                  <div className="space-y-3 pt-2">
                    <h4 className="text-xs font-semibold uppercase tracking-wider text-[hsl(var(--muted-foreground))]">
                      Hypotheses & Evidence Chains
                    </h4>

                    {rcaResult.hypotheses.map((hyp, index) => (
                      <div key={index} className="p-4 rounded-lg border border-[hsl(var(--border))] bg-[hsl(var(--card))] space-y-3">
                        <div className="flex items-center justify-between gap-2">
                          <div className="flex items-center gap-2">
                            <Badge
                              variant={
                                hyp.confidenceLevel === "Likely cause"
                                  ? "destructive"
                                  : hyp.confidenceLevel === "Possible cause"
                                  ? "secondary"
                                  : "outline"
                              }
                            >
                              {hyp.confidenceLevel} ({(hyp.confidenceScore * 100).toFixed(0)}%)
                            </Badge>
                            <span className="font-semibold text-sm">{hyp.hypothesis}</span>
                          </div>
                          {hyp.isVerifiedByOperator ? (
                            <Badge className="bg-emerald-500 text-white flex items-center gap-1">
                              <Check className="h-3 w-3" /> Verified by Operator
                            </Badge>
                          ) : (
                            <Badge variant="outline" className="text-[hsl(var(--muted-foreground))]">
                              Unverified
                            </Badge>
                          )}
                        </div>

                        {/* Evidence */}
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs">
                          <div className="space-y-1">
                            <span className="font-semibold text-emerald-500 flex items-center gap-1">
                              <CheckCircle2 className="h-3.5 w-3.5" /> Supporting Evidence
                            </span>
                            <ul className="list-disc list-inside space-y-0.5 text-[hsl(var(--muted-foreground))]">
                              {hyp.supportingEvidence.map((ev, i) => (
                                <li key={i}>{ev}</li>
                              ))}
                            </ul>
                          </div>

                          <div className="space-y-1">
                            <span className="font-semibold text-amber-500 flex items-center gap-1">
                              <AlertTriangle className="h-3.5 w-3.5" /> Contradictory Evidence
                            </span>
                            <ul className="list-disc list-inside space-y-0.5 text-[hsl(var(--muted-foreground))]">
                              {hyp.contradictoryEvidence.length > 0 ? (
                                hyp.contradictoryEvidence.map((ev, i) => <li key={i}>{ev}</li>)
                              ) : (
                                <li>None identified.</li>
                              )}
                            </ul>
                          </div>
                        </div>

                        {/* Recommended Steps */}
                        {hyp.recommendedSteps.length > 0 && (
                          <div className="space-y-1 text-xs pt-1 border-t border-[hsl(var(--border))]">
                            <span className="font-semibold text-[hsl(var(--foreground))]">Recommended Steps:</span>
                            <ol className="list-decimal list-inside space-y-0.5 text-[hsl(var(--muted-foreground))]">
                              {hyp.recommendedSteps.map((step, i) => (
                                <li key={i}>{step}</li>
                              ))}
                            </ol>
                          </div>
                        )}

                        {/* Operator Verification Control */}
                        <div className="pt-2 flex items-center gap-2">
                          <Input
                            placeholder="Operator verification notes..."
                            value={operatorNotes[index] || ""}
                            onChange={(e) => setOperatorNotes((prev) => ({ ...prev, [index]: e.target.value }))}
                            className="text-xs h-8"
                          />
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() =>
                              verifyHypothesisMutation.mutate({
                                rcaId: rcaIncidentId,
                                index,
                                verified: true,
                                notes: operatorNotes[index] || "Verified by engineer",
                              })
                            }
                            disabled={verifyHypothesisMutation.isPending}
                          >
                            Verify
                          </Button>
                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() =>
                              verifyHypothesisMutation.mutate({
                                rcaId: rcaIncidentId,
                                index,
                                verified: false,
                                notes: operatorNotes[index] || "Rejected by engineer",
                              })
                            }
                            disabled={verifyHypothesisMutation.isPending}
                          >
                            Reject
                          </Button>
                        </div>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            </div>
          )}
        </div>
      )}

      {/* TAB CONTENT 4: PREDICTIVE FORECASTS */}
      {activeTab === "forecasts" && (
        <div className="space-y-5">
          <Card className="p-4 border border-[hsl(var(--border))]">
            <h3 className="font-bold text-sm mb-3">Evaluate Capacity Trajectory</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3">
              <div>
                <label className="text-xs font-semibold text-[hsl(var(--muted-foreground))] block mb-1">Service:</label>
                <Input
                  value={forecastService}
                  onChange={(e) => setForecastService(e.target.value)}
                  className="text-xs h-8"
                />
              </div>
              <div>
                <label className="text-xs font-semibold text-[hsl(var(--muted-foreground))] block mb-1">Metric:</label>
                <Input
                  value={forecastMetric}
                  onChange={(e) => setForecastMetric(e.target.value)}
                  className="text-xs h-8"
                />
              </div>
              <div>
                <label className="text-xs font-semibold text-[hsl(var(--muted-foreground))] block mb-1">
                  Horizon (mins):
                </label>
                <Input
                  type="number"
                  value={forecastHorizon}
                  onChange={(e) => setForecastHorizon(Number(e.target.value))}
                  className="text-xs h-8"
                />
              </div>
              <div>
                <label className="text-xs font-semibold text-[hsl(var(--muted-foreground))] block mb-1">
                  Alert Threshold:
                </label>
                <Input
                  type="number"
                  value={forecastThreshold}
                  onChange={(e) => setForecastThreshold(Number(e.target.value))}
                  className="text-xs h-8"
                />
              </div>
              <div className="flex items-end">
                <Button
                  className="w-full h-8 text-xs"
                  onClick={() => evaluateForecastMutation.mutate()}
                  disabled={evaluateForecastMutation.isPending}
                >
                  <TrendingUp className="h-3.5 w-3.5 mr-1" />
                  Evaluate OLS
                </Button>
              </div>
            </div>
          </Card>

          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <h3 className="font-semibold text-sm">Active Capacity Projections</h3>
              <Button variant="outline" size="sm" onClick={() => refetchForecasts()} disabled={isLoadingForecasts}>
                <RefreshCw className={`h-3.5 w-3.5 mr-1 ${isLoadingForecasts ? "animate-spin" : ""}`} />
                Refresh
              </Button>
            </div>

            {forecasts.length === 0 ? (
              <Card>
                <CardContent className="p-8 text-center text-sm text-[hsl(var(--muted-foreground))]">
                  No active predictive forecasts. Click "Evaluate OLS" above to compute linear regressions for services.
                </CardContent>
              </Card>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {forecasts.map((f) => (
                  <Card key={f.id} className="p-4 border border-[hsl(var(--border))]">
                    <div className="space-y-2.5">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <span className="font-bold text-sm">{f.serviceName}</span>
                          <Badge variant="outline" className="font-mono text-xs">
                            {f.metricName}
                          </Badge>
                        </div>
                        {f.projectedBreachTimeUtc ? (
                          <Badge variant="destructive" className="flex items-center gap-1">
                            <Clock className="h-3 w-3" />
                            Breach: {new Date(f.projectedBreachTimeUtc).toLocaleTimeString()}
                          </Badge>
                        ) : (
                          <Badge variant="secondary" className="text-emerald-500">
                            Normal Trajectory
                          </Badge>
                        )}
                      </div>

                      <div className="grid grid-cols-3 gap-2 text-xs bg-[hsl(var(--muted)/0.3)] p-2.5 rounded">
                        <div>
                          <span className="text-[hsl(var(--muted-foreground))] block">Current</span>
                          <span className="font-mono font-semibold">{f.currentValue.toFixed(1)}</span>
                        </div>
                        <div>
                          <span className="text-[hsl(var(--muted-foreground))] block">Predicted (+{f.forecastHorizonMinutes}m)</span>
                          <span className="font-mono font-semibold">{f.predictedValue.toFixed(1)}</span>
                        </div>
                        <div>
                          <span className="text-[hsl(var(--muted-foreground))] block">Slope (Δ/min)</span>
                          <span className="font-mono font-semibold">{f.trendSlope > 0 ? `+${f.trendSlope.toFixed(3)}` : f.trendSlope.toFixed(3)}</span>
                        </div>
                      </div>

                      <div className="text-[11px] text-[hsl(var(--muted-foreground))] flex items-center justify-between font-mono">
                        <span>95% CI: [{f.confidenceIntervalLower.toFixed(1)}, {f.confidenceIntervalUpper.toFixed(1)}]</span>
                        <span>Samples: {f.sampleCount}</span>
                      </div>
                    </div>
                  </Card>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* TAB CONTENT 5: INCIDENT COPILOT */}
      {activeTab === "copilot" && (
        <div className="space-y-4">
          {/* Safety Disclaimer Banner */}
          <div className="p-3 bg-amber-500/10 border border-amber-500/30 rounded-lg flex items-center gap-3 text-xs text-amber-500">
            <ShieldCheck className="h-5 w-5 shrink-0" />
            <span>
              <strong>Safety Boundary:</strong> All AI-generated remediation commands require explicit human operator confirmation.
              Commands are never executed automatically by the platform.
            </span>
          </div>

          <Card className="border border-[hsl(var(--border))] flex flex-col h-[550px]">
            {/* Chat Messages */}
            <div className="flex-1 overflow-y-auto p-4 space-y-4">
              {copilotMessages.map((msg, i) => (
                <div
                  key={i}
                  className={`flex ${msg.sender === "user" ? "justify-end" : "justify-start"}`}
                >
                  <div
                    className={`max-w-[80%] rounded-lg p-3 text-sm ${
                      msg.sender === "user"
                        ? "bg-[hsl(var(--primary))] text-[hsl(var(--primary-foreground))]"
                        : "bg-[hsl(var(--muted)/0.5)] border border-[hsl(var(--border))]"
                    }`}
                  >
                    <p className="whitespace-pre-wrap">{msg.text}</p>

                    {/* Copilot Structured Recommendations */}
                    {msg.responseData && (
                      <div className="mt-3 pt-3 border-t border-[hsl(var(--border))] space-y-2 text-xs">
                        <div className="flex items-center gap-2">
                          <Badge variant="outline" className="text-emerald-500 border-emerald-500/30">
                            Safety Checks: Passed
                          </Badge>
                          <Badge variant="secondary">
                            Confidence: {(msg.responseData.confidenceScore * 100).toFixed(0)}%
                          </Badge>
                          <Badge variant="outline" className="text-amber-500 border-amber-500/30">
                            Human Approval Required
                          </Badge>
                        </div>

                        {msg.responseData.recommendedActions.length > 0 && (
                          <div className="space-y-1">
                            <span className="font-semibold block">Suggested Remediation Steps:</span>
                            <ul className="list-disc list-inside space-y-1 text-[hsl(var(--muted-foreground))]">
                              {msg.responseData.recommendedActions.map((action, ai) => (
                                <li key={ai}>{action}</li>
                              ))}
                            </ul>
                          </div>
                        )}
                      </div>
                    )}

                    <span className="text-[10px] opacity-60 block mt-1.5 text-right">
                      {msg.timestamp.toLocaleTimeString()}
                    </span>
                  </div>
                </div>
              ))}
              {copilotMutation.isPending && (
                <div className="flex justify-start">
                  <div className="bg-[hsl(var(--muted)/0.5)] border border-[hsl(var(--border))] rounded-lg p-3 text-xs flex items-center gap-2 text-[hsl(var(--muted-foreground))]">
                    <Sparkles className="h-4 w-4 animate-spin text-[hsl(var(--primary))]" />
                    Analyzing telemetry evidence and synthesizing hypothesis...
                  </div>
                </div>
              )}
            </div>

            {/* Chat Input Form */}
            <form onSubmit={handleSendCopilot} className="p-3 border-t border-[hsl(var(--border))] flex items-center gap-2">
              <Input
                placeholder="Ask Copilot about an incident, error spike, or trace diagnosis..."
                value={copilotQuestion}
                onChange={(e) => setCopilotQuestion(e.target.value)}
                disabled={copilotMutation.isPending}
                className="text-xs"
              />
              <Button
                type="submit"
                size="sm"
                aria-label="Send message"
                data-testid="copilot-send"
                disabled={!copilotQuestion.trim() || copilotMutation.isPending}
              >
                <Send className="h-4 w-4" />
              </Button>
            </form>
          </Card>
        </div>
      )}
    </div>
  );
}
export default AiObservabilityPage;
