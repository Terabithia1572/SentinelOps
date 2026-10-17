import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import apiClient, {
  type ApiResponse,
  type SloDto,
  type SloComplianceHistoryDto,
  type CreateSloRequest,
  type ApplicationDto,
  type SloIndicatorType,
  type SloEvaluationWindow,
} from "@/lib/api-client";
import { useAuth } from "@/context/auth-context";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  Target,
  Plus,
  RefreshCw,
  Play,
  Trash2,
  Activity,
  Flame,
  ShieldCheck,
  X,
} from "lucide-react";
import {
  ResponsiveContainer,
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
} from "recharts";

export function SlosPage() {
  const { isOperator } = useAuth();
  const queryClient = useQueryClient();

  const [selectedAppId, setSelectedAppId] = useState<string>("");
  const [selectedHistorySloId, setSelectedHistorySloId] = useState<string | null>(null);

  // Create Modal
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [name, setName] = useState("");
  const [desc, setDesc] = useState("");
  const [appId, setAppId] = useState("");
  const [serviceName, setServiceName] = useState("");
  const [envName, setEnvName] = useState("");
  const [indicatorType, setIndicatorType] = useState<SloIndicatorType>("Availability");
  const [targetPercentage, setTargetPercentage] = useState<number>(99.9);
  const [evaluationWindow, setEvaluationWindow] = useState<SloEvaluationWindow>("Rolling24Hours");
  const [latencyThresholdMs, setLatencyThresholdMs] = useState<number>(250);

  // Applications
  const { data: applications = [] } = useQuery({
    queryKey: ["applications"],
    queryFn: async () => {
      const res = await apiClient.get<ApiResponse<ApplicationDto[]>>("/applications");
      return res.data.data || [];
    },
  });

  // SLOs Query
  const { data: slos = [], isLoading, refetch } = useQuery({
    queryKey: ["slos", selectedAppId],
    queryFn: async () => {
      const params = selectedAppId ? `?applicationId=${selectedAppId}` : "";
      const res = await apiClient.get<ApiResponse<SloDto[]>>(`/slos${params}`);
      return res.data.data || [];
    },
    refetchInterval: 30000,
  });

  // SLO Compliance History for Selected SLO
  const { data: historyData, isLoading: isHistoryLoading } = useQuery({
    queryKey: ["slo-history", selectedHistorySloId],
    queryFn: async () => {
      if (!selectedHistorySloId) return null;
      const res = await apiClient.get<ApiResponse<SloComplianceHistoryDto>>(
        `/slos/${selectedHistorySloId}/compliance`
      );
      return res.data.data;
    },
    enabled: !!selectedHistorySloId,
  });

  // Create Mutation
  const createMutation = useMutation({
    mutationFn: async (req: CreateSloRequest) => {
      await apiClient.post("/slos", req);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["slos"] });
      setIsCreateOpen(false);
      resetForm();
    },
  });

  // Re-evaluate Mutation
  const evaluateMutation = useMutation({
    mutationFn: async (id: string) => {
      await apiClient.post(`/slos/${id}/evaluate`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["slos"] });
      if (selectedHistorySloId) {
        queryClient.invalidateQueries({ queryKey: ["slo-history", selectedHistorySloId] });
      }
    },
  });

  // Delete Mutation
  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/slos/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["slos"] });
      if (selectedHistorySloId) {
        setSelectedHistorySloId(null);
      }
    },
  });

  const resetForm = () => {
    setName("");
    setDesc("");
    setAppId(applications[0]?.id || "");
    setServiceName("");
    setEnvName("");
    setIndicatorType("Availability");
    setTargetPercentage(99.9);
    setEvaluationWindow("Rolling24Hours");
    setLatencyThresholdMs(250);
  };

  // KPI Calculations
  const totalSlos = slos.length;
  const meetingTarget = slos.filter((s) => !s.isBreached).length;
  const breachedSlos = slos.filter((s) => s.isBreached).length;
  const avgCompliance =
    totalSlos > 0
      ? (
          slos.reduce((acc, s) => acc + (s.currentCompliancePercentage ?? 100), 0) / totalSlos
        ).toFixed(2)
      : "100.00";

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">SLO & Error Budget Monitoring</h1>
          <p className="text-sm text-[hsl(var(--muted-foreground))] mt-1">
            Site Reliability Engineering (SRE) availability & latency objectives, burn rate formulas, and error budget depletion tracking.
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
              Define Objective
            </Button>
          )}
        </div>
      </div>

      {/* Summary KPI Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <Card className="border-[hsl(var(--border))]">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <span className="text-xs font-medium text-[hsl(var(--muted-foreground))]">Active SLOs</span>
              <div className="text-2xl font-bold mt-1">{totalSlos}</div>
            </div>
            <Target className="h-6 w-6 text-sky-400 opacity-80" />
          </CardContent>
        </Card>

        <Card className="border-emerald-500/20 bg-emerald-500/5">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <span className="text-xs font-medium text-emerald-400">Meeting Target</span>
              <div className="text-2xl font-bold text-emerald-400 mt-1">{meetingTarget}</div>
            </div>
            <ShieldCheck className="h-6 w-6 text-emerald-400 opacity-80" />
          </CardContent>
        </Card>

        <Card className="border-red-500/20 bg-red-500/5">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <span className="text-xs font-medium text-red-400">Breached / In Fast Burn</span>
              <div className="text-2xl font-bold text-red-400 mt-1">{breachedSlos}</div>
            </div>
            <Flame className="h-6 w-6 text-red-400 opacity-80" />
          </CardContent>
        </Card>

        <Card className="border-[hsl(var(--border))]">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <span className="text-xs font-medium text-[hsl(var(--muted-foreground))]">Avg Compliance</span>
              <div className="text-2xl font-bold mt-1 font-mono">{avgCompliance}%</div>
            </div>
            <Activity className="h-6 w-6 text-purple-400 opacity-80" />
          </CardContent>
        </Card>
      </div>

      {/* Filter by Application */}
      <Card className="border-[hsl(var(--border))]">
        <CardContent className="p-3 flex items-center gap-3">
          <span className="text-xs font-medium text-[hsl(var(--muted-foreground))]">Application Filter:</span>
          <select
            value={selectedAppId}
            onChange={(e) => setSelectedAppId(e.target.value)}
            className="h-8 text-xs rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] px-3 text-[hsl(var(--foreground))]"
          >
            <option value="">All Applications</option>
            {applications.map((app) => (
              <option key={app.id} value={app.id}>
                {app.displayName || app.name}
              </option>
            ))}
          </select>
        </CardContent>
      </Card>

      {/* SLO Cards Grid */}
      {isLoading ? (
        <div className="py-24 text-center text-sm text-[hsl(var(--muted-foreground))] flex items-center justify-center gap-2">
          <RefreshCw className="h-4 w-4 animate-spin" />
          Loading SLO performance metrics...
        </div>
      ) : slos.length === 0 ? (
        <Card className="border-[hsl(var(--border))]">
          <CardContent className="py-16 text-center">
            <Target className="mx-auto h-10 w-10 text-[hsl(var(--muted-foreground))] mb-2 opacity-60" />
            <h3 className="font-semibold text-sm">No SLOs defined yet</h3>
            <p className="text-xs text-[hsl(var(--muted-foreground))] mt-1">
              Create an availability or latency objective to begin tracking SRE error budgets.
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {slos.map((slo) => {
            const compliance = slo.currentCompliancePercentage ?? 100;
            const errorBudget = slo.currentErrorBudgetRemaining ?? 100;
            const burnRate = slo.burnRate ?? 0;
            const isBreached = slo.isBreached;

            return (
              <Card
                key={slo.id}
                className={`border transition-all ${
                  isBreached
                    ? "border-rose-500/40 bg-rose-500/5"
                    : "border-[hsl(var(--border))] hover:border-[hsl(var(--primary)/0.5)]"
                }`}
              >
                <CardHeader className="p-4 pb-2">
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <div className="flex items-center gap-1.5">
                        <Badge
                          variant={slo.indicatorType === "Availability" ? "default" : "secondary"}
                          className="text-[10px] px-1.5 py-0"
                        >
                          {slo.indicatorType}
                        </Badge>
                        <span className="text-[11px] font-mono text-[hsl(var(--muted-foreground))]">
                          {slo.serviceName}
                        </span>
                      </div>
                      <CardTitle className="text-sm font-semibold mt-1">{slo.name}</CardTitle>
                    </div>

                    <Badge variant={isBreached ? "danger" : "secondary"}>
                      {isBreached ? "Breached" : "Healthy"}
                    </Badge>
                  </div>
                  <CardDescription className="text-xs line-clamp-1 mt-0.5">
                    {slo.description || `Target: ${slo.targetPercentage}% over ${slo.evaluationWindow}`}
                  </CardDescription>
                </CardHeader>

                <CardContent className="p-4 pt-2 space-y-3">
                  {/* Compliance vs Target */}
                  <div className="flex items-baseline justify-between pt-1">
                    <div>
                      <span className="text-xs text-[hsl(var(--muted-foreground))]">Compliance</span>
                      <div className="text-2xl font-bold font-mono">
                        <span className={isBreached ? "text-rose-400" : "text-emerald-400"}>
                          {compliance.toFixed(2)}%
                        </span>
                      </div>
                    </div>

                    <div className="text-right">
                      <span className="text-xs text-[hsl(var(--muted-foreground))]">Target Objective</span>
                      <div className="text-sm font-bold font-mono text-[hsl(var(--muted-foreground))]">
                        {slo.targetPercentage.toFixed(2)}%
                      </div>
                    </div>
                  </div>

                  {/* Error Budget Remaining Bar */}
                  <div className="space-y-1">
                    <div className="flex justify-between text-xs">
                      <span className="text-[hsl(var(--muted-foreground))]">Error Budget Remaining</span>
                      <span className={`font-mono font-semibold ${errorBudget <= 0 ? "text-rose-400" : "text-foreground"}`}>
                        {errorBudget.toFixed(1)}%
                      </span>
                    </div>
                    <div className="h-2 w-full rounded-full bg-[hsl(var(--secondary))] overflow-hidden">
                      <div
                        className={`h-full transition-all rounded-full ${
                          errorBudget <= 0
                            ? "bg-rose-500"
                            : errorBudget < 20
                            ? "bg-amber-500"
                            : "bg-emerald-500"
                        }`}
                        style={{ width: `${Math.min(100, Math.max(0, errorBudget))}%` }}
                      />
                    </div>
                  </div>

                  {/* Burn Rate Indicator */}
                  <div className="flex items-center justify-between text-xs pt-1 border-t border-[hsl(var(--border))]">
                    <span className="flex items-center gap-1 text-[hsl(var(--muted-foreground))]">
                      <Flame className="h-3.5 w-3.5 text-amber-400" />
                      Burn Rate:
                    </span>
                    <span
                      className={`font-mono font-semibold ${
                        burnRate > 2.0
                          ? "text-rose-400"
                          : burnRate > 1.0
                          ? "text-amber-400"
                          : "text-emerald-400"
                      }`}
                    >
                      {burnRate.toFixed(2)}x
                    </span>
                  </div>

                  {/* Actions */}
                  <div className="flex items-center justify-between pt-2 border-t border-[hsl(var(--border))] text-xs">
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-7 px-2 text-xs text-sky-400 hover:bg-sky-500/10"
                      onClick={() => setSelectedHistorySloId(slo.id)}
                    >
                      <Activity className="h-3 w-3 mr-1" />
                      History Chart
                    </Button>

                    <div className="flex items-center gap-1">
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-7 px-2 text-xs"
                        onClick={() => evaluateMutation.mutate(slo.id)}
                        disabled={evaluateMutation.isPending}
                        title="Calculate compliance over PostgreSQL telemetry"
                      >
                        <Play className="h-3 w-3 mr-1" />
                        Re-eval
                      </Button>

                      {isOperator && (
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-7 w-7 p-0 text-rose-400 hover:bg-rose-500/10"
                          onClick={() => {
                            if (confirm(`Delete objective "${slo.name}"?`)) {
                              deleteMutation.mutate(slo.id);
                            }
                          }}
                          title="Delete objective"
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </Button>
                      )}
                    </div>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}

      {/* Compliance History Drawer / Chart Modal */}
      {selectedHistorySloId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
          <div className="w-full max-w-3xl rounded-lg border border-[hsl(var(--border))] bg-[hsl(var(--card))] p-6 shadow-xl animate-in zoom-in-95">
            <div className="flex items-center justify-between border-b border-[hsl(var(--border))] pb-3">
              <div>
                <h3 className="text-base font-semibold">
                  SLO Compliance Trend & Error Budget Consumption
                </h3>
                <p className="text-xs text-[hsl(var(--muted-foreground))] mt-0.5">
                  {historyData?.name || "Historical telemetry calculation"}
                </p>
              </div>
              <Button
                variant="ghost"
                size="sm"
                className="h-7 w-7 p-0"
                onClick={() => setSelectedHistorySloId(null)}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>

            <div className="mt-4">
              {isHistoryLoading ? (
                <div className="py-20 text-center text-sm text-[hsl(var(--muted-foreground))] flex items-center justify-center gap-2">
                  <RefreshCw className="h-4 w-4 animate-spin" />
                  Generating historical compliance points...
                </div>
              ) : historyData && historyData.history && historyData.history.length > 0 ? (
                <div className="space-y-4">
                  <div className="h-64 w-full">
                    <ResponsiveContainer width="100%" height="100%">
                      <AreaChart
                        data={historyData.history.map((pt) => ({
                          time: new Date(pt.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
                          compliance: pt.compliancePercentage,
                          errorBudget: pt.errorBudgetRemaining,
                          target: historyData.targetPercentage,
                        }))}
                        margin={{ top: 10, right: 10, left: -20, bottom: 0 }}
                      >
                        <defs>
                          <linearGradient id="colorCompliance" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="5%" stopColor="#34D399" stopOpacity={0.3} />
                            <stop offset="95%" stopColor="#34D399" stopOpacity={0.0} />
                          </linearGradient>
                        </defs>
                        <CartesianGrid strokeDasharray="3 3" stroke="#1E2333" opacity={0.8} />
                        <XAxis dataKey="time" stroke="#758498" textAnchor="end" tick={{ fontSize: 10 }} />
                        <YAxis domain={[95, 100]} stroke="#758498" tick={{ fontSize: 10 }} />
                        <Tooltip
                          contentStyle={{
                            backgroundColor: "#10121A",
                            borderColor: "#1E2333",
                            borderRadius: "4px",
                            fontSize: "11px",
                            color: "#F0F3F9",
                            fontFamily: "monospace",
                          }}
                        />
                        <Area
                          type="monotone"
                          dataKey="compliance"
                          name="Compliance %"
                          stroke="#10b981"
                          fillOpacity={1}
                          fill="url(#colorCompliance)"
                        />
                      </AreaChart>
                    </ResponsiveContainer>
                  </div>

                  <div className="grid grid-cols-3 gap-2 text-center text-xs">
                    <div className="p-2 rounded bg-accent/30 border border-border">
                      <span className="text-muted-foreground">Current Compliance</span>
                      <p className="font-bold font-mono text-emerald-400 mt-0.5">
                        {historyData.currentCompliance.toFixed(2)}%
                      </p>
                    </div>
                    <div className="p-2 rounded bg-accent/30 border border-border">
                      <span className="text-muted-foreground">Target</span>
                      <p className="font-bold font-mono mt-0.5">{historyData.targetPercentage}%</p>
                    </div>
                    <div className="p-2 rounded bg-accent/30 border border-border">
                      <span className="text-muted-foreground">Error Budget Left</span>
                      <p className="font-bold font-mono text-sky-400 mt-0.5">
                        {historyData.errorBudgetRemaining.toFixed(1)}%
                      </p>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="py-16 text-center text-xs text-muted-foreground">
                  No evaluation points recorded for this SLO yet. Click &quot;Re-eval&quot; to perform initial evaluation.
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Define Objective Modal */}
      {isCreateOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
          <div className="w-full max-w-lg rounded-lg border border-[hsl(var(--border))] bg-[hsl(var(--card))] p-6 shadow-xl animate-in zoom-in-95">
            <div className="flex items-center justify-between border-b border-[hsl(var(--border))] pb-3">
              <h3 className="text-base font-semibold">Define Service Level Objective</h3>
              <Button variant="ghost" size="sm" className="h-7 w-7 p-0" onClick={() => setIsCreateOpen(false)}>
                <X className="h-4 w-4" />
              </Button>
            </div>

            <div className="mt-4 space-y-3 text-xs">
              <div className="space-y-1">
                <label className="font-medium">Objective Name *</label>
                <Input
                  placeholder="e.g. API Gateway 99.9% Availability"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <label className="font-medium">Application *</label>
                  <select
                    value={appId}
                    onChange={(e) => setAppId(e.target.value)}
                    className="w-full h-9 rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] px-3 py-1 text-[hsl(var(--foreground))]"
                  >
                    <option value="">Select Application</option>
                    {applications.map((app) => (
                      <option key={app.id} value={app.id}>
                        {app.displayName || app.name}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="space-y-1">
                  <label className="font-medium">Service Name *</label>
                  <Input
                    placeholder="e.g. api-service"
                    value={serviceName}
                    onChange={(e) => setServiceName(e.target.value)}
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <label className="font-medium">Indicator Type *</label>
                  <select
                    value={indicatorType}
                    onChange={(e) => setIndicatorType(e.target.value as SloIndicatorType)}
                    className="w-full h-9 rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] px-3 py-1 text-[hsl(var(--foreground))]"
                  >
                    <option value="Availability">Availability (% Successful Requests)</option>
                    <option value="Latency">Latency (% Requests Below Threshold)</option>
                  </select>
                </div>

                <div className="space-y-1">
                  <label className="font-medium">Evaluation Window *</label>
                  <select
                    value={evaluationWindow}
                    onChange={(e) => setEvaluationWindow(e.target.value as SloEvaluationWindow)}
                    className="w-full h-9 rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] px-3 py-1 text-[hsl(var(--foreground))]"
                  >
                    <option value="Rolling24Hours">Rolling 24 Hours</option>
                    <option value="Rolling7Days">Rolling 7 Days</option>
                    <option value="Rolling30Days">Rolling 30 Days</option>
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <label className="font-medium">Target Compliance % *</label>
                  <Input
                    type="number"
                    step="0.01"
                    min="50"
                    max="100"
                    value={targetPercentage}
                    onChange={(e) => setTargetPercentage(parseFloat(e.target.value) || 99.9)}
                  />
                </div>

                {indicatorType === "Latency" ? (
                  <div className="space-y-1">
                    <label className="font-medium">Latency Threshold (ms) *</label>
                    <Input
                      type="number"
                      min="1"
                      value={latencyThresholdMs}
                      onChange={(e) => setLatencyThresholdMs(parseFloat(e.target.value) || 200)}
                    />
                  </div>
                ) : (
                  <div className="space-y-1">
                    <label className="font-medium">Environment (optional)</label>
                    <Input
                      placeholder="e.g. Production"
                      value={envName}
                      onChange={(e) => setEnvName(e.target.value)}
                    />
                  </div>
                )}
              </div>

              <div className="space-y-1">
                <label className="font-medium">Description</label>
                <Input
                  placeholder="SRE rationale and customer SLO expectation"
                  value={desc}
                  onChange={(e) => setDesc(e.target.value)}
                />
              </div>
            </div>

            <div className="mt-6 flex justify-end gap-2 border-t border-[hsl(var(--border))] pt-3">
              <Button variant="ghost" size="sm" onClick={() => setIsCreateOpen(false)}>
                Cancel
              </Button>
              <Button
                size="sm"
                disabled={!name.trim() || !appId || !serviceName.trim() || createMutation.isPending}
                onClick={() =>
                  createMutation.mutate({
                    name,
                    description: desc,
                    applicationId: appId,
                    serviceName,
                    environmentName: envName || null,
                    indicatorType,
                    targetPercentage,
                    evaluationWindow,
                    latencyThresholdMs: indicatorType === "Latency" ? latencyThresholdMs : null,
                  })
                }
              >
                {createMutation.isPending ? "Creating..." : "Save Objective"}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
