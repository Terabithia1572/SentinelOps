import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import apiClient, {
  type ApiResponse,
  type PagedResponse,
  type AlertRuleDto,
  type CreateAlertRuleRequest,
  type UpdateAlertRuleRequest,
  type NotificationChannelDto,
  type CreateNotificationChannelRequest,
  type ApplicationDto,
  type AlertConditionType,
} from "@/lib/api-client";
import { useAuth } from "@/context/auth-context";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  BellRing,
  Plus,
  Play,
  Trash2,
  Edit2,
  Power,
  PowerOff,
  CheckCircle2,
  AlertTriangle,
  RefreshCw,
  Search,
  Send,
  Webhook,
  Mail,
  X,
} from "lucide-react";

export function AlertRulesPage() {
  const { isOperator } = useAuth();
  const queryClient = useQueryClient();

  const [searchQuery, setSearchQuery] = useState("");
  const [selectedAppId, setSelectedAppId] = useState<string>("");
  const [testResult, setTestResult] = useState<{ id: string; message: string; isBreached: boolean } | null>(null);

  // Modals
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [editingRule, setEditingRule] = useState<AlertRuleDto | null>(null);
  const [isChannelModalOpen, setIsChannelModalOpen] = useState(false);

  // Alert Rule Form State
  const [formName, setFormName] = useState("");
  const [formDesc, setFormDesc] = useState("");
  const [formAppId, setFormAppId] = useState("");
  const [formEnv, setFormEnv] = useState("");
  const [formService, setFormService] = useState("");
  const [formConditionType, setFormConditionType] = useState<AlertConditionType>("ErrorRateAboveThreshold");
  const [formOperator, setFormOperator] = useState("GreaterThan");
  const [formMetricName, setFormMetricName] = useState("error_rate");
  const [formThreshold, setFormThreshold] = useState<number>(5);
  const [formWindow, setFormWindow] = useState<number>(5);
  const [formConsecutive, setFormConsecutive] = useState<number>(1);
  const [formCooldown, setFormCooldown] = useState<number>(15);
  const [formSeverity, setFormSeverity] = useState("Warning");
  const [formChannelIds, setFormChannelIds] = useState<string[]>([]);

  // Notification Channel Form State
  const [chName, setChName] = useState("");
  const [chDesc, setChDesc] = useState("");
  const [chType, setChType] = useState("Webhook");
  const [chDestination, setChDestination] = useState("https://hooks.example.com/alerts");
  const [chSecret, setChSecret] = useState("");

  // Applications
  const { data: applications = [] } = useQuery({
    queryKey: ["applications"],
    queryFn: async () => {
      const res = await apiClient.get<ApiResponse<ApplicationDto[]>>("/applications");
      return res.data.data || [];
    },
  });

  // Notification Channels
  const { data: channels = [] } = useQuery({
    queryKey: ["notification-channels"],
    queryFn: async () => {
      const res = await apiClient.get<ApiResponse<NotificationChannelDto[]>>("/notification-channels");
      return res.data.data || [];
    },
  });

  // Alert Rules
  const { data: rulesData, isLoading, refetch: refetchRules } = useQuery({
    queryKey: ["alert-rules", selectedAppId],
    queryFn: async () => {
      const params = selectedAppId ? `?applicationId=${selectedAppId}&pageSize=100` : "?pageSize=100";
      const res = await apiClient.get<ApiResponse<PagedResponse<AlertRuleDto>>>(`/alert-rules${params}`);
      return res.data.data;
    },
  });

  // Create Rule Mutation
  const createRuleMutation = useMutation({
    mutationFn: async (req: CreateAlertRuleRequest) => {
      await apiClient.post("/alert-rules", req);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["alert-rules"] });
      queryClient.invalidateQueries({ queryKey: ["alert-summary"] });
      setIsCreateOpen(false);
      resetRuleForm();
    },
  });

  // Update Rule Mutation
  const updateRuleMutation = useMutation({
    mutationFn: async ({ id, req }: { id: string; req: UpdateAlertRuleRequest }) => {
      await apiClient.put(`/alert-rules/${id}`, req);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["alert-rules"] });
      setEditingRule(null);
      resetRuleForm();
    },
  });

  // Delete Rule Mutation
  const deleteRuleMutation = useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/alert-rules/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["alert-rules"] });
      queryClient.invalidateQueries({ queryKey: ["alert-summary"] });
    },
  });

  // Toggle Rule Enable / Disable
  const toggleRuleMutation = useMutation({
    mutationFn: async ({ id, isEnabled }: { id: string; isEnabled: boolean }) => {
      if (isEnabled) {
        await apiClient.post(`/alert-rules/${id}/disable`);
      } else {
        await apiClient.post(`/alert-rules/${id}/enable`);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["alert-rules"] });
    },
  });

  // Test Evaluate Mutation
  const evaluateRuleMutation = useMutation({
    mutationFn: async (id: string) => {
      const res = await apiClient.post<ApiResponse<{ evaluated: boolean; isBreached: boolean; observedValue: number; message: string }>>(
        `/alert-rules/${id}/evaluate`
      );
      return { id, data: res.data.data };
    },
    onSuccess: (result) => {
      setTestResult({
        id: result.id,
        message: result.data.message || `Observed value: ${result.data.observedValue}`,
        isBreached: result.data.isBreached,
      });
      queryClient.invalidateQueries({ queryKey: ["alert-rules"] });
      queryClient.invalidateQueries({ queryKey: ["alerts"] });
    },
  });

  // Create Channel Mutation
  const createChannelMutation = useMutation({
    mutationFn: async (req: CreateNotificationChannelRequest) => {
      await apiClient.post("/notification-channels", req);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notification-channels"] });
      setIsChannelModalOpen(false);
      setChName("");
      setChDesc("");
      setChDestination("");
      setChSecret("");
    },
  });

  const resetRuleForm = () => {
    setFormName("");
    setFormDesc("");
    setFormAppId(applications[0]?.id || "");
    setFormEnv("");
    setFormService("");
    setFormConditionType("ErrorRateAboveThreshold");
    setFormOperator("GreaterThan");
    setFormMetricName("error_rate");
    setFormThreshold(5);
    setFormWindow(5);
    setFormConsecutive(1);
    setFormCooldown(15);
    setFormSeverity("Warning");
    setFormChannelIds([]);
  };

  const handleEditClick = (rule: AlertRuleDto) => {
    setEditingRule(rule);
    setFormName(rule.name);
    setFormDesc(rule.description);
    setFormAppId(rule.applicationId);
    setFormEnv(rule.environmentName || "");
    setFormService(rule.serviceName || "");
    setFormConditionType(rule.conditionType as AlertConditionType);
    setFormOperator(rule.operator);
    setFormMetricName(rule.metricName);
    setFormThreshold(rule.threshold);
    setFormWindow(rule.evaluationWindowMinutes);
    setFormConsecutive(rule.consecutiveCountRequired);
    setFormCooldown(rule.cooldownMinutes);
    setFormSeverity(rule.severity);
  };

  const rules = rulesData?.items || [];
  const filteredRules = rules.filter(
    (r) =>
      r.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      r.conditionType.toLowerCase().includes(searchQuery.toLowerCase()) ||
      r.description.toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Alert Rules & Channels</h1>
          <p className="text-sm text-[hsl(var(--muted-foreground))] mt-1">
            Configure automated metric thresholds, latency percentiles, error rate breaches, and notification dispatch.
          </p>
        </div>

        <div className="flex items-center gap-2">
          {isOperator && (
            <>
              <Button
                variant="outline"
                size="sm"
                className="gap-1.5"
                onClick={() => setIsChannelModalOpen(true)}
              >
                <Send className="h-3.5 w-3.5" />
                Notification Channels ({channels.length})
              </Button>
              <Button
                size="sm"
                className="gap-1.5"
                onClick={() => {
                  resetRuleForm();
                  setIsCreateOpen(true);
                }}
              >
                <Plus className="h-3.5 w-3.5" />
                New Alert Rule
              </Button>
            </>
          )}
        </div>
      </div>

      {/* Filter bar */}
      <Card className="border-[hsl(var(--border))]">
        <CardContent className="p-4 flex flex-col sm:flex-row items-center justify-between gap-3">
          <div className="flex items-center gap-2 w-full sm:w-auto">
            <select
              value={selectedAppId}
              onChange={(e) => setSelectedAppId(e.target.value)}
              className="h-9 text-xs rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] px-3 py-1 text-[hsl(var(--foreground))]"
            >
              <option value="">All Applications</option>
              {applications.map((app) => (
                <option key={app.id} value={app.id}>
                  {app.displayName || app.name}
                </option>
              ))}
            </select>

            <Button variant="ghost" size="sm" onClick={() => refetchRules()}>
              <RefreshCw className="h-3.5 w-3.5" />
            </Button>
          </div>

          <div className="relative w-full sm:w-64">
            <Search className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-[hsl(var(--muted-foreground))]" />
            <Input
              placeholder="Search alert rules..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="h-9 pl-8 text-xs"
            />
          </div>
        </CardContent>
      </Card>

      {/* Rules Table */}
      <Card className="border-[hsl(var(--border))]">
        <CardHeader className="p-4 border-b border-[hsl(var(--border))]">
          <CardTitle className="text-base font-semibold">Configured Alert Rules</CardTitle>
          <CardDescription className="text-xs">
            Evaluated continuously every 15 seconds by background worker
          </CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="py-16 text-center text-sm text-[hsl(var(--muted-foreground))] flex items-center justify-center gap-2">
              <RefreshCw className="h-4 w-4 animate-spin" />
              Loading alert rules...
            </div>
          ) : filteredRules.length === 0 ? (
            <div className="py-16 text-center">
              <BellRing className="mx-auto h-10 w-10 text-[hsl(var(--muted-foreground))] mb-2 opacity-60" />
              <h3 className="font-semibold text-sm">No alert rules configured</h3>
              <p className="text-xs text-[hsl(var(--muted-foreground))] mt-1">
                Create a rule to detect errors, high latency, or throughput drops.
              </p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead className="bg-[hsl(var(--muted)/0.3)] border-b border-[hsl(var(--border))] text-[hsl(var(--muted-foreground))]">
                  <tr>
                    <th className="py-3 px-4 font-semibold">Rule Name</th>
                    <th className="py-3 px-4 font-semibold">Condition & Threshold</th>
                    <th className="py-3 px-4 font-semibold">Window / Cooldown</th>
                    <th className="py-3 px-4 font-semibold">Severity</th>
                    <th className="py-3 px-4 font-semibold">Status</th>
                    <th className="py-3 px-4 font-semibold">Last Value</th>
                    <th className="py-3 px-4 font-semibold text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[hsl(var(--border))]">
                  {filteredRules.map((rule) => (
                    <tr
                      key={rule.id}
                      className={`hover:bg-[hsl(var(--accent)/0.3)] transition-colors ${
                        !rule.isEnabled ? "opacity-60" : ""
                      }`}
                    >
                      <td className="py-3 px-4">
                        <div className="font-medium text-[hsl(var(--foreground))]">{rule.name}</div>
                        <div className="text-[11px] text-[hsl(var(--muted-foreground))] mt-0.5">
                          {rule.serviceName ? `svc: ${rule.serviceName}` : "All Services"}{" "}
                          {rule.environmentName ? `(${rule.environmentName})` : ""}
                        </div>
                      </td>
                      <td className="py-3 px-4">
                        <span className="font-mono font-medium text-sky-400">
                          {rule.conditionType}
                        </span>
                        <div className="text-[11px] text-[hsl(var(--muted-foreground))] mt-0.5">
                          {rule.operator} {rule.threshold}
                        </div>
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap text-[hsl(var(--muted-foreground))]">
                        <span>{rule.evaluationWindowMinutes}m window</span>
                        <div className="text-[10px]">
                          {rule.consecutiveCountRequired} breach / {rule.cooldownMinutes}m cooldown
                        </div>
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap">
                        <Badge
                          variant={
                            rule.severity === "Critical"
                              ? "danger"
                              : rule.severity === "Warning"
                              ? "warning"
                              : "secondary"
                          }
                        >
                          {rule.severity}
                        </Badge>
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap">
                        <Badge
                          variant={
                            rule.status === "Firing"
                              ? "danger"
                              : rule.status === "Pending"
                              ? "warning"
                              : "secondary"
                          }
                        >
                          {rule.status}
                        </Badge>
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap font-mono">
                        {rule.lastMeasuredValue != null
                          ? rule.lastMeasuredValue.toFixed(2)
                          : "—"}
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap text-right">
                        <div className="flex items-center justify-end gap-1.5">
                          {/* Test evaluate button */}
                          <Button
                            variant="outline"
                            size="sm"
                            className="h-7 px-2 text-xs text-sky-400 hover:bg-sky-500/10"
                            onClick={() => evaluateRuleMutation.mutate(rule.id)}
                            disabled={evaluateRuleMutation.isPending}
                            title="Evaluate rule against current telemetry"
                          >
                            <Play className="h-3 w-3 mr-1" />
                            Test
                          </Button>

                          {/* Enable/Disable toggle */}
                          {isOperator && (
                            <Button
                              variant="ghost"
                              size="sm"
                              className={`h-7 w-7 p-0 ${
                                rule.isEnabled ? "text-emerald-400" : "text-muted-foreground"
                              }`}
                              onClick={() =>
                                toggleRuleMutation.mutate({
                                  id: rule.id,
                                  isEnabled: rule.isEnabled,
                                })
                              }
                              title={rule.isEnabled ? "Disable rule" : "Enable rule"}
                            >
                              {rule.isEnabled ? (
                                <Power className="h-3.5 w-3.5" />
                              ) : (
                                <PowerOff className="h-3.5 w-3.5" />
                              )}
                            </Button>
                          )}

                          {isOperator && (
                            <Button
                              variant="ghost"
                              size="sm"
                              className="h-7 w-7 p-0 text-muted-foreground hover:text-foreground"
                              onClick={() => handleEditClick(rule)}
                              title="Edit rule"
                            >
                              <Edit2 className="h-3.5 w-3.5" />
                            </Button>
                          )}

                          {isOperator && (
                            <Button
                              variant="ghost"
                              size="sm"
                              className="h-7 w-7 p-0 text-rose-400 hover:bg-rose-500/10"
                              onClick={() => {
                                if (confirm(`Delete alert rule "${rule.name}"?`)) {
                                  deleteRuleMutation.mutate(rule.id);
                                }
                              }}
                              title="Delete rule"
                            >
                              <Trash2 className="h-3.5 w-3.5" />
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Inline Test Result Toast */}
          {testResult && (
            <div
              className={`p-3 m-4 rounded-md border text-xs flex items-center justify-between ${
                testResult.isBreached
                  ? "bg-rose-500/10 border-rose-500/30 text-rose-300"
                  : "bg-emerald-500/10 border-emerald-500/30 text-emerald-300"
              }`}
            >
              <div className="flex items-center gap-2">
                {testResult.isBreached ? (
                  <AlertTriangle className="h-4 w-4 text-rose-400" />
                ) : (
                  <CheckCircle2 className="h-4 w-4 text-emerald-400" />
                )}
                <span>
                  <strong>Evaluation result:</strong> {testResult.message}
                </span>
              </div>
              <Button
                variant="ghost"
                size="sm"
                className="h-6 w-6 p-0 text-muted-foreground hover:text-foreground"
                onClick={() => setTestResult(null)}
              >
                <X className="h-3 w-3" />
              </Button>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Create / Edit Rule Modal */}
      {(isCreateOpen || editingRule) && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
          <div className="w-full max-w-2xl max-h-[90vh] overflow-y-auto rounded-lg border border-[hsl(var(--border))] bg-[hsl(var(--card))] p-6 shadow-xl animate-in zoom-in-95">
            <div className="flex items-center justify-between border-b border-[hsl(var(--border))] pb-3">
              <h3 className="text-base font-semibold">
                {editingRule ? "Edit Alert Rule" : "Create Alert Rule"}
              </h3>
              <Button
                variant="ghost"
                size="sm"
                className="h-7 w-7 p-0"
                onClick={() => {
                  setIsCreateOpen(false);
                  setEditingRule(null);
                }}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>

            <div className="mt-4 grid grid-cols-1 md:grid-cols-2 gap-4 text-xs">
              {/* Name */}
              <div className="space-y-1">
                <label className="font-medium">Rule Name *</label>
                <Input
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  placeholder="e.g. High HTTP 5xx Error Rate"
                />
              </div>

              {/* Application */}
              <div className="space-y-1">
                <label className="font-medium">Application *</label>
                <select
                  disabled={!!editingRule}
                  value={formAppId}
                  onChange={(e) => setFormAppId(e.target.value)}
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

              {/* Service */}
              <div className="space-y-1">
                <label className="font-medium">Service Name (optional)</label>
                <Input
                  value={formService}
                  onChange={(e) => setFormService(e.target.value)}
                  placeholder="e.g. order-api"
                />
              </div>

              {/* Environment */}
              <div className="space-y-1">
                <label className="font-medium">Environment (optional)</label>
                <Input
                  value={formEnv}
                  onChange={(e) => setFormEnv(e.target.value)}
                  placeholder="e.g. Production"
                />
              </div>

              {/* Condition Type */}
              <div className="space-y-1">
                <label className="font-medium">Condition Type *</label>
                <select
                  value={formConditionType}
                  onChange={(e) => {
                    const ct = e.target.value as AlertConditionType;
                    setFormConditionType(ct);
                    if (ct === "ErrorRateAboveThreshold") setFormMetricName("error_rate");
                    else if (ct === "P95LatencyAboveThreshold") setFormMetricName("latency_p95");
                    else if (ct === "P99LatencyAboveThreshold") setFormMetricName("latency_p99");
                    else if (ct.startsWith("Throughput")) setFormMetricName("requests_per_second");
                    else if (ct === "ExceptionCountAboveThreshold") setFormMetricName("exception_count");
                  }}
                  className="w-full h-9 rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] px-3 py-1 text-[hsl(var(--foreground))]"
                >
                  <option value="ErrorRateAboveThreshold">Error Rate Above Threshold (%)</option>
                  <option value="P95LatencyAboveThreshold">P95 Latency Above Threshold (ms)</option>
                  <option value="P99LatencyAboveThreshold">P99 Latency Above Threshold (ms)</option>
                  <option value="ThroughputBelowThreshold">Throughput Below Threshold (req/sec)</option>
                  <option value="ThroughputAboveThreshold">Throughput Above Threshold (req/sec)</option>
                  <option value="ExceptionCountAboveThreshold">Exception Count Above Threshold</option>
                  <option value="LogSeverityCountAboveThreshold">Error/Critical Log Count</option>
                  <option value="NoTelemetryReceived">No Telemetry Received (Dead Man's Snitch)</option>
                </select>
              </div>

              {/* Operator */}
              <div className="space-y-1">
                <label className="font-medium">Operator *</label>
                <select
                  value={formOperator}
                  onChange={(e) => setFormOperator(e.target.value)}
                  className="w-full h-9 rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] px-3 py-1 text-[hsl(var(--foreground))]"
                >
                  <option value="GreaterThan">Greater Than (&gt;)</option>
                  <option value="GreaterThanOrEqual">Greater Than or Equal (&gt;=)</option>
                  <option value="LessThan">Less Than (&lt;)</option>
                  <option value="LessThanOrEqual">Less Than or Equal (&lt;=)</option>
                  <option value="Equal">Equal (==)</option>
                </select>
              </div>

              {/* Threshold */}
              <div className="space-y-1">
                <label className="font-medium">Threshold Value *</label>
                <Input
                  type="number"
                  step="any"
                  value={formThreshold}
                  onChange={(e) => setFormThreshold(parseFloat(e.target.value) || 0)}
                  placeholder="e.g. 5.0"
                />
              </div>

              {/* Window Minutes */}
              <div className="space-y-1">
                <label className="font-medium">Evaluation Window (minutes)</label>
                <Input
                  type="number"
                  min="1"
                  max="1440"
                  value={formWindow}
                  onChange={(e) => setFormWindow(parseInt(e.target.value) || 5)}
                />
              </div>

              {/* Consecutive Failures */}
              <div className="space-y-1">
                <label className="font-medium">Consecutive Breaches Required</label>
                <Input
                  type="number"
                  min="1"
                  max="50"
                  value={formConsecutive}
                  onChange={(e) => setFormConsecutive(parseInt(e.target.value) || 1)}
                />
              </div>

              {/* Cooldown Minutes */}
              <div className="space-y-1">
                <label className="font-medium">Cooldown Period (minutes)</label>
                <Input
                  type="number"
                  min="1"
                  max="1440"
                  value={formCooldown}
                  onChange={(e) => setFormCooldown(parseInt(e.target.value) || 15)}
                />
              </div>

              {/* Severity */}
              <div className="space-y-1">
                <label className="font-medium">Severity *</label>
                <select
                  value={formSeverity}
                  onChange={(e) => setFormSeverity(e.target.value)}
                  className="w-full h-9 rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] px-3 py-1 text-[hsl(var(--foreground))]"
                >
                  <option value="Critical">Critical (P1 - Automatic Incident)</option>
                  <option value="Error">Error (P2)</option>
                  <option value="Warning">Warning (P3)</option>
                  <option value="Information">Information (P4)</option>
                </select>
              </div>

              {/* Description */}
              <div className="col-span-1 md:col-span-2 space-y-1">
                <label className="font-medium">Description</label>
                <Input
                  value={formDesc}
                  onChange={(e) => setFormDesc(e.target.value)}
                  placeholder="Explanation of the alert trigger and troubleshooting instructions"
                />
              </div>

              {/* Notification Channels Multi-select */}
              <div className="col-span-1 md:col-span-2 space-y-1.5">
                <label className="font-medium">Dispatch Notification Channels</label>
                {channels.length === 0 ? (
                  <p className="text-muted-foreground italic">
                    No channels registered yet. Click &quot;Notification Channels&quot; in the header to add Webhook or Email.
                  </p>
                ) : (
                  <div className="flex flex-wrap gap-2 pt-1">
                    {channels.map((ch) => {
                      const isSelected = formChannelIds.includes(ch.id);
                      return (
                        <button
                          type="button"
                          key={ch.id}
                          onClick={() => {
                            setFormChannelIds(
                              isSelected
                                ? formChannelIds.filter((id) => id !== ch.id)
                                : [...formChannelIds, ch.id]
                            );
                          }}
                          className={`px-2.5 py-1 rounded text-xs border transition-colors flex items-center gap-1.5 ${
                            isSelected
                              ? "bg-sky-500/20 border-sky-500/50 text-sky-300"
                              : "border-border text-muted-foreground hover:bg-accent"
                          }`}
                        >
                          {ch.type === "Email" ? <Mail className="h-3 w-3" /> : <Webhook className="h-3 w-3" />}
                          {ch.name}
                        </button>
                      );
                    })}
                  </div>
                )}
              </div>
            </div>

            <div className="mt-6 flex items-center justify-end gap-2 border-t border-[hsl(var(--border))] pt-4">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => {
                  setIsCreateOpen(false);
                  setEditingRule(null);
                }}
              >
                Cancel
              </Button>
              <Button
                size="sm"
                disabled={!formName.trim() || (!editingRule && !formAppId)}
                onClick={() => {
                  if (editingRule) {
                    updateRuleMutation.mutate({
                      id: editingRule.id,
                      req: {
                        name: formName,
                        description: formDesc,
                        environmentName: formEnv || null,
                        serviceName: formService || null,
                        conditionType: formConditionType,
                        operator: formOperator,
                        metricName: formMetricName,
                        threshold: formThreshold,
                        evaluationWindowMinutes: formWindow,
                        consecutiveCountRequired: formConsecutive,
                        cooldownMinutes: formCooldown,
                        severity: formSeverity,
                        notificationChannelIds: formChannelIds,
                      },
                    });
                  } else {
                    createRuleMutation.mutate({
                      name: formName,
                      description: formDesc,
                      applicationId: formAppId,
                      environmentName: formEnv || null,
                      serviceName: formService || null,
                      conditionType: formConditionType,
                      operator: formOperator,
                      metricName: formMetricName,
                      threshold: formThreshold,
                      evaluationWindowMinutes: formWindow,
                      consecutiveCountRequired: formConsecutive,
                      cooldownMinutes: formCooldown,
                      severity: formSeverity,
                      notificationChannelIds: formChannelIds,
                    });
                  }
                }}
              >
                {editingRule ? "Save Changes" : "Create Rule"}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Notification Channels Modal */}
      {isChannelModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
          <div className="w-full max-w-lg rounded-lg border border-[hsl(var(--border))] bg-[hsl(var(--card))] p-6 shadow-xl animate-in zoom-in-95">
            <div className="flex items-center justify-between border-b border-[hsl(var(--border))] pb-3">
              <h3 className="text-base font-semibold">Registered Notification Channels</h3>
              <Button
                variant="ghost"
                size="sm"
                className="h-7 w-7 p-0"
                onClick={() => setIsChannelModalOpen(false)}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>

            {/* List */}
            <div className="mt-4 space-y-2 max-h-48 overflow-y-auto">
              {channels.length === 0 ? (
                <p className="text-xs text-muted-foreground py-2 text-center">No notification channels yet.</p>
              ) : (
                channels.map((ch) => (
                  <div
                    key={ch.id}
                    className="flex items-center justify-between p-2.5 rounded border border-border bg-accent/20 text-xs"
                  >
                    <div>
                      <div className="font-semibold flex items-center gap-1.5">
                        {ch.type === "Email" ? <Mail className="h-3.5 w-3.5" /> : <Webhook className="h-3.5 w-3.5" />}
                        {ch.name}
                        <Badge variant="secondary" className="text-[10px] py-0 px-1">{ch.type}</Badge>
                      </div>
                      <p className="text-[11px] text-muted-foreground mt-0.5 font-mono">{ch.destination}</p>
                    </div>
                  </div>
                ))
              )}
            </div>

            {/* Add Channel Form */}
            <div className="mt-6 border-t border-border pt-4 space-y-3 text-xs">
              <h4 className="font-semibold text-sm">Add New Destination</h4>
              <div className="grid grid-cols-2 gap-2">
                <Input
                  placeholder="Channel Name (e.g. SRE Slack Webhook)"
                  value={chName}
                  onChange={(e) => setChName(e.target.value)}
                />
                <select
                  value={chType}
                  onChange={(e) => setChType(e.target.value)}
                  className="h-9 rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] px-3 py-1 text-[hsl(var(--foreground))]"
                >
                  <option value="Webhook">Webhook (HTTP POST with HMAC)</option>
                  <option value="Email">Email (SMTP)</option>
                  <option value="Slack">Slack Webhook</option>
                  <option value="PagerDuty">PagerDuty Event API</option>
                </select>
              </div>

              <Input
                placeholder={chType === "Email" ? "alerts@company.com" : "https://hooks.slack.com/services/..."}
                value={chDestination}
                onChange={(e) => setChDestination(e.target.value)}
              />

              {chType === "Webhook" && (
                <Input
                  type="password"
                  placeholder="Optional Webhook HMAC-SHA256 Secret"
                  value={chSecret}
                  onChange={(e) => setChSecret(e.target.value)}
                />
              )}

              <p className="text-[11px] text-muted-foreground">
                Security note: All webhook URLs are strictly validated to prevent Server-Side Request Forgery (SSRF). Loopback, private IP ranges, and AWS/Azure/GCP metadata endpoints (169.254.169.254) are rejected.
              </p>

              <div className="flex justify-end gap-2 pt-2">
                <Button
                  size="sm"
                  disabled={!chName.trim() || !chDestination.trim() || createChannelMutation.isPending}
                  onClick={() =>
                    createChannelMutation.mutate({
                      name: chName,
                      description: chDesc,
                      type: chType,
                      destination: chDestination,
                      webhookSecret: chSecret || undefined,
                    })
                  }
                >
                  Add Destination
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
