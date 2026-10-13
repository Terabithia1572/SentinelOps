import { useState, useEffect, useCallback } from "react";
import {
  getSystemHealth,
  getSystemSettings,
  updateSystemSettings,
  getRetentionPolicy,
  updateRetentionPolicy,
  previewRetentionCleanup,
  executeRetentionCleanup,
  type SystemHealthDto,
  type SystemSettingsDto,
  type RetentionPolicyDto,
  type RetentionDryRunResultDto,
} from "@/lib/api-client";
import { useAuth } from "@/context/auth-context";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  Settings,
  Server,
  Database,
  Radio,
  BellRing,
  Shield,
  Trash2,
  Eye,
  AlertTriangle,
  CheckCircle,
  RefreshCw,
  Loader2,
  HardDrive,
  Activity,
  X,
} from "lucide-react";

export function SettingsPage() {
  const { isAdmin } = useAuth();

  // Health
  const [health, setHealth] = useState<SystemHealthDto | null>(null);

  // System Settings
  const [settings, setSettings] = useState<SystemSettingsDto | null>(null);
  const [isSavingSettings, setIsSavingSettings] = useState(false);

  // Retention Policy
  const [retention, setRetention] = useState<RetentionPolicyDto | null>(null);
  const [isSavingRetention, setIsSavingRetention] = useState(false);

  // Dry Run Modal
  const [dryRunResult, setDryRunResult] = useState<RetentionDryRunResultDto | null>(null);
  const [isPreviewingRetention, setIsPreviewingRetention] = useState(false);
  const [confirmPurge, setConfirmPurge] = useState(false);
  const [isExecutingPurge, setIsExecutingPurge] = useState(false);

  // Feedback Messages
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const fetchHealth = useCallback(async () => {
    try {
      const res = await getSystemHealth();
      setHealth(res);
    } catch {
      // Non-blocking
    }
  }, []);

  const fetchSettingsAndRetention = useCallback(async () => {
    try {
      const [sett, ret] = await Promise.all([getSystemSettings(), getRetentionPolicy()]);
      setSettings(sett);
      setRetention(ret);
    } catch (err: any) {
      setErrorMessage(err.message || "Failed to load platform configuration");
    }
  }, []);


  useEffect(() => {
    fetchHealth();
    fetchSettingsAndRetention();
  }, [fetchHealth, fetchSettingsAndRetention]);

  const handleSaveSettings = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!settings) return;
    try {
      setIsSavingSettings(true);
      setErrorMessage(null);
      await updateSystemSettings(settings);
      setSuccessMessage("Global platform settings updated successfully");
    } catch (err: any) {
      setErrorMessage(err.response?.data?.message || err.message || "Failed to save settings");
    } finally {
      setIsSavingSettings(false);
    }
  };

  const handleSaveRetention = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!retention) return;
    try {
      setIsSavingRetention(true);
      setErrorMessage(null);
      await updateRetentionPolicy(retention);
      setSuccessMessage("Data retention policy updated successfully");
    } catch (err: any) {
      setErrorMessage(err.response?.data?.message || err.message || "Failed to save retention policy");
    } finally {
      setIsSavingRetention(false);
    }
  };

  const handlePreviewDryRun = async () => {
    try {
      setIsPreviewingRetention(true);
      setErrorMessage(null);
      const res = await previewRetentionCleanup();
      setDryRunResult(res);
      setConfirmPurge(false);
    } catch (err: any) {
      setErrorMessage(err.response?.data?.message || err.message || "Failed to preview retention cleanup");
    } finally {
      setIsPreviewingRetention(false);
    }
  };

  const handleExecutePurge = async () => {
    if (!confirmPurge) return;
    try {
      setIsExecutingPurge(true);
      setErrorMessage(null);
      const res = await executeRetentionCleanup(true);
      setSuccessMessage(res.message);
      setDryRunResult(null);
      fetchHealth();
    } catch (err: any) {
      setErrorMessage(err.response?.data?.message || err.message || "Failed to execute retention cleanup");
    } finally {
      setIsExecutingPurge(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-white flex items-center gap-2">
            <Settings className="w-7 h-7 text-sky-400" />
            Platform Settings & Administration
          </h1>
          <p className="text-sm text-slate-400">
            System configuration, data retention lifecycles, and live infrastructure health
          </p>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => {
            fetchHealth();
            fetchSettingsAndRetention();
          }}
          className="flex items-center gap-1.5"
        >
          <RefreshCw className="w-4 h-4" />
          Refresh Status
        </Button>
      </div>

      {/* Notifications */}
      {successMessage && (
        <div className="bg-emerald-950/60 border border-emerald-800 text-emerald-300 p-3.5 rounded-lg flex items-center justify-between">
          <div className="flex items-center gap-2">
            <CheckCircle className="w-5 h-5 text-emerald-400" />
            <span>{successMessage}</span>
          </div>
          <button onClick={() => setSuccessMessage(null)} className="text-emerald-400 hover:text-emerald-200">
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {errorMessage && (
        <div className="bg-rose-950/60 border border-rose-800 text-rose-300 p-3.5 rounded-lg flex items-center justify-between">
          <div className="flex items-center gap-2">
            <AlertTriangle className="w-5 h-5 text-rose-400" />
            <span>{errorMessage}</span>
          </div>
          <button onClick={() => setErrorMessage(null)} className="text-rose-400 hover:text-rose-200">
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Live System Health Grid */}
      <div className="space-y-3">
        <h3 className="text-base font-semibold text-white flex items-center gap-2">
          <Activity className="w-5 h-5 text-emerald-400" />
          Live Platform Infrastructure Health
        </h3>

        {health && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
            {[
              { component: health.database, icon: <Database className="w-5 h-5 text-sky-400" /> },
              { component: health.redis, icon: <HardDrive className="w-5 h-5 text-amber-400" /> },
              { component: health.ingestionEngine, icon: <Radio className="w-5 h-5 text-emerald-400" /> },
              { component: health.alertEvaluator, icon: <BellRing className="w-5 h-5 text-purple-400" /> },
              { component: health.notificationDispatcher, icon: <Shield className="w-5 h-5 text-teal-400" /> },
            ].map(({ component, icon }, idx) => (
              <Card key={idx} className="bg-slate-900/60 border-slate-800">
                <CardContent className="p-4 space-y-2">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2 font-medium text-white text-sm">
                      {icon}
                      {component.name}
                    </div>
                    <Badge
                      variant="outline"
                      className={`text-xs ${
                        component.status === "Healthy"
                          ? "bg-emerald-950/80 text-emerald-400 border-emerald-800"
                          : "bg-amber-950/80 text-amber-400 border-amber-800"
                      }`}
                    >
                      {component.status}
                    </Badge>
                  </div>
                  <p className="text-xs text-slate-400 leading-snug">{component.message}</p>
                  <div className="flex items-center justify-between pt-1 border-t border-slate-800/60 text-[11px] text-slate-500">
                    <span>Latency: {component.latencyMs}ms</span>
                    <span>Verified Live</span>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Data Retention Policies Card */}
        <Card className="bg-slate-900/60 border-slate-800">
          <CardHeader className="pb-3 border-b border-slate-800">
            <CardTitle className="text-base font-semibold text-white flex items-center gap-2">
              <Trash2 className="w-5 h-5 text-rose-400" />
              Telemetry & Data Retention Lifecycles
            </CardTitle>
            <CardDescription className="text-xs text-slate-400">
              Configure automated retention periods in days. Records older than the cutoff are purged.
            </CardDescription>
          </CardHeader>
          <CardContent className="p-4">
            {retention ? (
              <form onSubmit={handleSaveRetention} className="space-y-4">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
                  <div>
                    <label className="text-slate-300 font-medium block mb-1">Structured Logs (Days)</label>
                    <Input
                      type="number"
                      min={1}
                      max={365}
                      value={retention.logsRetentionDays}
                      onChange={(e) => setRetention({ ...retention, logsRetentionDays: parseInt(e.target.value) || 1 })}
                      className="bg-slate-950 border-slate-800"
                    />
                  </div>

                  <div>
                    <label className="text-slate-300 font-medium block mb-1">Distributed Traces (Days)</label>
                    <Input
                      type="number"
                      min={1}
                      max={365}
                      value={retention.tracesRetentionDays}
                      onChange={(e) => setRetention({ ...retention, tracesRetentionDays: parseInt(e.target.value) || 1 })}
                      className="bg-slate-950 border-slate-800"
                    />
                  </div>

                  <div>
                    <label className="text-slate-300 font-medium block mb-1">Metric Time-Series (Days)</label>
                    <Input
                      type="number"
                      min={1}
                      max={365}
                      value={retention.metricsRetentionDays}
                      onChange={(e) => setRetention({ ...retention, metricsRetentionDays: parseInt(e.target.value) || 1 })}
                      className="bg-slate-950 border-slate-800"
                    />
                  </div>

                  <div>
                    <label className="text-slate-300 font-medium block mb-1">Alert Evaluations (Days)</label>
                    <Input
                      type="number"
                      min={1}
                      max={365}
                      value={retention.alertEvaluationsRetentionDays}
                      onChange={(e) => setRetention({ ...retention, alertEvaluationsRetentionDays: parseInt(e.target.value) || 1 })}
                      className="bg-slate-950 border-slate-800"
                    />
                  </div>

                  <div>
                    <label className="text-slate-300 font-medium block mb-1">Resolved Incidents (Days)</label>
                    <Input
                      type="number"
                      min={1}
                      max={730}
                      value={retention.resolvedIncidentsRetentionDays}
                      onChange={(e) => setRetention({ ...retention, resolvedIncidentsRetentionDays: parseInt(e.target.value) || 1 })}
                      className="bg-slate-950 border-slate-800"
                    />
                  </div>

                  <div>
                    <label className="text-slate-300 font-medium block mb-1">Security Audit Trail (Days)</label>
                    <Input
                      type="number"
                      min={30}
                      max={1825}
                      value={retention.auditLogsRetentionDays}
                      onChange={(e) => setRetention({ ...retention, auditLogsRetentionDays: parseInt(e.target.value) || 30 })}
                      className="bg-slate-950 border-slate-800"
                    />
                  </div>
                </div>

                <div className="pt-3 border-t border-slate-800 flex flex-wrap items-center justify-between gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={handlePreviewDryRun}
                    disabled={isPreviewingRetention}
                    className="flex items-center gap-1.5"
                  >
                    <Eye className="w-4 h-4 text-sky-400" />
                    {isPreviewingRetention ? "Estimating..." : "Preview Deletion (Dry Run)"}
                  </Button>

                  {isAdmin && (
                    <Button type="submit" size="sm" disabled={isSavingRetention}>
                      {isSavingRetention ? <Loader2 className="w-4 h-4 animate-spin mr-1" /> : null}
                      Save Retention Policy
                    </Button>
                  )}
                </div>
              </form>
            ) : (
              <div className="py-8 text-center text-slate-500">Loading retention settings...</div>
            )}
          </CardContent>
        </Card>

        {/* Global Platform Configuration Card */}
        <Card className="bg-slate-900/60 border-slate-800">
          <CardHeader className="pb-3 border-b border-slate-800">
            <CardTitle className="text-base font-semibold text-white flex items-center gap-2">
              <Server className="w-5 h-5 text-sky-400" />
              Platform & Operating Settings
            </CardTitle>
            <CardDescription className="text-xs text-slate-400">
              Global instance branding, evaluation intervals, and operating switches
            </CardDescription>
          </CardHeader>
          <CardContent className="p-4">
            {settings ? (
              <form onSubmit={handleSaveSettings} className="space-y-4">
                <div className="space-y-3 text-xs">
                  <div>
                    <label className="text-slate-300 font-medium block mb-1">Platform Instance Name</label>
                    <Input
                      value={settings.platformName}
                      onChange={(e) => setSettings({ ...settings, platformName: e.target.value })}
                      className="bg-slate-950 border-slate-800"
                    />
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="text-slate-300 font-medium block mb-1">Evaluation Interval (sec)</label>
                      <Input
                        type="number"
                        min={5}
                        max={300}
                        value={settings.alertEvaluationIntervalSeconds}
                        onChange={(e) => setSettings({ ...settings, alertEvaluationIntervalSeconds: parseInt(e.target.value) || 30 })}
                        className="bg-slate-950 border-slate-800"
                      />
                    </div>

                    <div>
                      <label className="text-slate-300 font-medium block mb-1">Webhook Timeout (sec)</label>
                      <Input
                        type="number"
                        min={1}
                        max={60}
                        value={settings.webhookTimeoutSeconds}
                        onChange={(e) => setSettings({ ...settings, webhookTimeoutSeconds: parseInt(e.target.value) || 10 })}
                        className="bg-slate-950 border-slate-800"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="text-slate-300 font-medium block mb-1">System Notice Banner (Optional)</label>
                    <Input
                      placeholder="e.g. Scheduled maintenance this Sunday at 02:00 UTC"
                      value={settings.systemNoticeBanner}
                      onChange={(e) => setSettings({ ...settings, systemNoticeBanner: e.target.value })}
                      className="bg-slate-950 border-slate-800"
                    />
                  </div>

                  <div className="flex items-center justify-between p-3 bg-slate-950 border border-slate-800 rounded-lg">
                    <div>
                      <div className="font-medium text-slate-200">Public Self-Registration</div>
                      <div className="text-[11px] text-slate-500">Allow new visitors to register viewer accounts</div>
                    </div>
                    <input
                      type="checkbox"
                      checked={settings.publicRegistrationEnabled}
                      onChange={(e) => setSettings({ ...settings, publicRegistrationEnabled: e.target.checked })}
                      className="w-4 h-4 accent-sky-500 rounded cursor-pointer"
                    />
                  </div>
                </div>

                <div className="pt-3 border-t border-slate-800 flex justify-end">
                  {isAdmin && (
                    <Button type="submit" size="sm" disabled={isSavingSettings}>
                      {isSavingSettings ? <Loader2 className="w-4 h-4 animate-spin mr-1" /> : null}
                      Save Operating Settings
                    </Button>
                  )}
                </div>
              </form>
            ) : (
              <div className="py-8 text-center text-slate-500">Loading settings...</div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Dry Run Modal */}
      {dryRunResult && (
        <div className="fixed inset-0 z-50 bg-black/70 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-xl max-w-xl w-full max-h-[90vh] overflow-y-auto p-6 space-y-4">
            <div className="flex justify-between items-start border-b border-slate-800 pb-3">
              <div>
                <h3 className="text-lg font-bold text-white flex items-center gap-2">
                  <Eye className="w-5 h-5 text-sky-400" />
                  Retention Cleanup Dry-Run Estimate
                </h3>
                <p className="text-xs text-slate-400">
                  Estimated records eligible for deletion based on current retention policies
                </p>
              </div>
              <button onClick={() => setDryRunResult(null)} className="text-slate-400 hover:text-white">
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="p-3 bg-sky-950/40 border border-sky-800/60 rounded-lg text-xs text-sky-300 flex items-center justify-between">
              <span>Total Estimated Records Eligible for Deletion:</span>
              <span className="font-bold text-base text-white font-mono">
                {dryRunResult.totalEstimatedRecordsToDelete.toLocaleString()}
              </span>
            </div>

            <div className="space-y-2 text-xs">
              {[
                dryRunResult.logs,
                dryRunResult.traces,
                dryRunResult.spans,
                dryRunResult.metrics,
                dryRunResult.alertEvaluations,
                dryRunResult.resolvedIncidents,
                dryRunResult.notifications,
                dryRunResult.auditLogs,
              ].map((item, idx) => (
                <div key={idx} className="p-2.5 bg-slate-950 border border-slate-800 rounded-lg flex items-center justify-between">
                  <div>
                    <span className="font-medium text-slate-200">{item.tableName}</span>
                    <div className="text-[10px] text-slate-500">
                      Cutoff: {new Date(item.cutoffUtc).toLocaleDateString()} ({item.retentionDays}d policy)
                    </div>
                  </div>
                  <div className="text-right">
                    <span className="font-mono text-slate-300 font-semibold">{item.estimatedCount.toLocaleString()} rows</span>
                    {item.oldestRecordUtc && (
                      <div className="text-[10px] text-slate-500">
                        Oldest: {new Date(item.oldestRecordUtc).toLocaleDateString()}
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>

            {isAdmin && (
              <div className="p-3 bg-rose-950/30 border border-rose-800/50 rounded-lg space-y-2 text-xs">
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="confirmPurge"
                    checked={confirmPurge}
                    onChange={(e) => setConfirmPurge(e.target.checked)}
                    className="w-4 h-4 accent-rose-600 rounded cursor-pointer"
                  />
                  <label htmlFor="confirmPurge" className="text-rose-200 cursor-pointer font-medium">
                    I confirm immediate purge of expired records in bounded batches
                  </label>
                </div>
              </div>
            )}

            <div className="pt-2 flex justify-end gap-2">
              <Button variant="ghost" onClick={() => setDryRunResult(null)}>Close</Button>
              {isAdmin && (
                <Button
                  variant="destructive"
                  onClick={handleExecutePurge}
                  disabled={!confirmPurge || isExecutingPurge}
                >
                  {isExecutingPurge ? <Loader2 className="w-4 h-4 animate-spin mr-1" /> : <Trash2 className="w-4 h-4 mr-1" />}
                  Execute Bounded Purge
                </Button>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
