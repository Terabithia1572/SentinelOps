import React, { useState, useEffect, useCallback } from "react";
import { useSearchParams } from "react-router-dom";
import apiClient, {
  type ApiResponse,
  type ApplicationDto,
  type ApiKeyDto,
  type ApiKeyCreatedResponse,
} from "@/lib/api-client";
import { useAuth } from "@/context/auth-context";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  Key,
  Plus,
  RefreshCw,
  Trash2,
  Copy,
  Check,
  AlertTriangle,
  Loader2,
  ShieldAlert,
  X,
  Clock,
  CheckCircle2,
} from "lucide-react";

export function ApiKeysPage() {
  const { isOperator } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();

  const [applications, setApplications] = useState<ApplicationDto[]>([]);
  const [selectedAppId, setSelectedAppId] = useState<string>(searchParams.get("app") || "");
  const [apiKeys, setApiKeys] = useState<ApiKeyDto[]>([]);
  const [isLoadingApps, setIsLoadingApps] = useState(true);
  const [isLoadingKeys, setIsLoadingKeys] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Generate modal
  const [showGenerateModal, setShowGenerateModal] = useState(false);
  const [keyName, setKeyName] = useState("");
  const [expiryOption, setExpiryOption] = useState("90"); // days
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Raw key revelation modal
  const [revealedKey, setRevealedKey] = useState<ApiKeyCreatedResponse | null>(null);
  const [isCopied, setIsCopied] = useState(false);

  // Load applications
  useEffect(() => {
    const loadApps = async () => {
      try {
        setIsLoadingApps(true);
        const res = await apiClient.get<ApiResponse<ApplicationDto[]>>("/applications");
        if (res.data.success && res.data.data) {
          setApplications(res.data.data);
          // If no app selected or param not in list, select the first
          if (res.data.data.length > 0) {
            const initialId = searchParams.get("app") || res.data.data[0].id;
            setSelectedAppId(initialId);
          }
        }
      } catch (err: unknown) {
        setErrorMessage(err instanceof Error ? err.message : "Failed to load applications.");
      } finally {
        setIsLoadingApps(false);
      }
    };
    loadApps();
  }, [searchParams]);

  // Load API keys whenever selected application changes
  const fetchKeys = useCallback(async (appId: string) => {
    if (!appId) return;
    try {
      setIsLoadingKeys(true);
      setErrorMessage(null);
      const res = await apiClient.get<ApiResponse<ApiKeyDto[]>>(`/applications/${appId}/keys`);
      if (res.data.success) {
        setApiKeys(res.data.data || []);
      }
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : "Failed to load API keys.");
    } finally {
      setIsLoadingKeys(false);
    }
  }, []);

  useEffect(() => {
    if (selectedAppId) {
      fetchKeys(selectedAppId);
    }
  }, [selectedAppId, fetchKeys]);

  const handleAppChange = (appId: string) => {
    setSelectedAppId(appId);
    setSearchParams({ app: appId });
  };

  const handleGenerateKey = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedAppId || !keyName.trim()) return;

    try {
      setIsSubmitting(true);
      const days = parseInt(expiryOption, 10);
      const expiresAt =
        days > 0 ? new Date(Date.now() + days * 24 * 60 * 60 * 1000).toISOString() : undefined;

      const res = await apiClient.post<ApiResponse<ApiKeyCreatedResponse>>(
        `/applications/${selectedAppId}/keys`,
        {
          name: keyName.trim(),
          expiresAtUtc: expiresAt,
        }
      );

      if (res.data.success && res.data.data) {
        setShowGenerateModal(false);
        setKeyName("");
        setRevealedKey(res.data.data);
        await fetchKeys(selectedAppId);
      }
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Failed to create API key");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleRotateKey = async (keyId: string, currentName: string) => {
    if (
      !window.confirm(
        `Are you sure you want to rotate key "${currentName}"? The existing key will be permanently invalidated.`
      )
    ) {
      return;
    }

    try {
      const res = await apiClient.post<ApiResponse<ApiKeyCreatedResponse>>(`/keys/${keyId}/rotate`, {});
      if (res.data.success && res.data.data) {
        setRevealedKey(res.data.data);
        await fetchKeys(selectedAppId);
      }
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Failed to rotate API key");
    }
  };

  const handleRevokeKey = async (keyId: string, currentName: string) => {
    if (!window.confirm(`Revoke API key "${currentName}" immediately? Telemetry ingestion with this key will fail.`)) {
      return;
    }

    try {
      const res = await apiClient.post<ApiResponse<boolean>>(`/keys/${keyId}/revoke`, {});
      if (res.data.success) {
        await fetchKeys(selectedAppId);
      }
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Failed to revoke API key");
    }
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    setIsCopied(true);
    setTimeout(() => setIsCopied(false), 2500);
  };

  const selectedApp = applications.find((a) => a.id === selectedAppId);

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">API Keys</h1>
          <p className="text-sm text-[hsl(var(--muted-foreground))]">
            Manage cryptographically secure ingestion tokens for OpenTelemetry SDKs and agents
          </p>
        </div>
        {isOperator && selectedAppId && (
          <Button onClick={() => setShowGenerateModal(true)} className="gap-2">
            <Plus className="h-4 w-4" />
            Generate Ingestion Key
          </Button>
        )}
      </div>

      {/* Application Selector */}
      <Card className="border-[hsl(var(--border))]">
        <CardContent className="p-4 sm:p-6 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <Key className="h-5 w-5 text-[hsl(var(--primary))]" />
            <div>
              <span className="text-sm font-semibold">Select Application</span>
              <p className="text-xs text-[hsl(var(--muted-foreground))]">
                API keys are scoped to specific monitored applications
              </p>
            </div>
          </div>
          <select
            value={selectedAppId}
            onChange={(e) => handleAppChange(e.target.value)}
            disabled={isLoadingApps || applications.length === 0}
            className="flex h-10 w-full sm:w-72 rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--background))] px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[hsl(var(--ring))]"
          >
            {applications.length === 0 ? (
              <option value="">No applications found</option>
            ) : (
              applications.map((app) => (
                <option key={app.id} value={app.id}>
                  {app.displayName} ({app.name})
                </option>
              ))
            )}
          </select>
        </CardContent>
      </Card>

      {/* Error Message */}
      {errorMessage && (
        <div className="flex items-center gap-2 rounded-md border border-rose-500/20 bg-rose-500/10 p-4 text-sm text-rose-400">
          <AlertTriangle className="h-5 w-5 shrink-0" />
          <span>{errorMessage}</span>
        </div>
      )}

      {/* Keys List */}
      {isLoadingKeys ? (
        <div className="flex h-64 items-center justify-center">
          <Loader2 className="h-8 w-8 animate-spin text-[hsl(var(--primary))]" />
        </div>
      ) : !selectedAppId ? (
        <Card className="flex flex-col items-center justify-center py-16 text-center border-dashed">
          <ShieldAlert className="h-12 w-12 text-[hsl(var(--muted-foreground))] mb-3" />
          <CardTitle className="text-lg">No Application Selected</CardTitle>
          <CardDescription className="mt-1">
            Please register or select an application first.
          </CardDescription>
        </Card>
      ) : apiKeys.length === 0 ? (
        <Card className="flex flex-col items-center justify-center py-16 text-center border-dashed">
          <Key className="h-12 w-12 text-[hsl(var(--muted-foreground))] mb-3" />
          <CardTitle className="text-lg">No API Keys for {selectedApp?.displayName}</CardTitle>
          <CardDescription className="max-w-md mt-1">
            Generate an ingestion key to authorize this application to send logs, traces, and metrics.
          </CardDescription>
          {isOperator && (
            <Button onClick={() => setShowGenerateModal(true)} className="mt-4 gap-2">
              <Plus className="h-4 w-4" />
              Generate API Key
            </Button>
          )}
        </Card>
      ) : (
        <div className="overflow-x-auto rounded-lg border border-[hsl(var(--border))] bg-[hsl(var(--card))]">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-[hsl(var(--border))] bg-[hsl(var(--muted)/0.5)] text-xs text-[hsl(var(--muted-foreground))] uppercase">
              <tr>
                <th className="px-6 py-3 font-semibold">Name</th>
                <th className="px-6 py-3 font-semibold">Key Prefix</th>
                <th className="px-6 py-3 font-semibold">Status</th>
                <th className="px-6 py-3 font-semibold">Created</th>
                <th className="px-6 py-3 font-semibold">Expires</th>
                <th className="px-6 py-3 font-semibold">Last Used</th>
                {isOperator && <th className="px-6 py-3 text-right font-semibold">Actions</th>}
              </tr>
            </thead>
            <tbody className="divide-y divide-[hsl(var(--border))]">
              {apiKeys.map((k) => (
                <tr key={k.id} className="hover:bg-[hsl(var(--accent)/0.3)] transition-colors">
                  <td className="px-6 py-4 font-medium">{k.name}</td>
                  <td className="px-6 py-4">
                    <code className="rounded bg-[hsl(var(--accent))] px-2 py-1 font-mono text-xs">
                      {k.keyPrefix}...
                    </code>
                  </td>
                  <td className="px-6 py-4">
                    {k.revokedAtUtc ? (
                      <Badge variant="danger">Revoked</Badge>
                    ) : !k.isActive ? (
                      <Badge variant="warning">Expired</Badge>
                    ) : (
                      <Badge variant="success">Active</Badge>
                    )}
                  </td>
                  <td className="px-6 py-4 text-xs text-[hsl(var(--muted-foreground))]">
                    {new Date(k.createdAtUtc).toLocaleDateString()}
                  </td>
                  <td className="px-6 py-4 text-xs text-[hsl(var(--muted-foreground))]">
                    {k.expiresAtUtc ? new Date(k.expiresAtUtc).toLocaleDateString() : "Never"}
                  </td>
                  <td className="px-6 py-4 text-xs text-[hsl(var(--muted-foreground))]">
                    {k.lastUsedAtUtc ? new Date(k.lastUsedAtUtc).toLocaleDateString() : "Never"}
                  </td>
                  {isOperator && (
                    <td className="px-6 py-4 text-right">
                      {!k.revokedAtUtc && (
                        <div className="flex items-center justify-end gap-2">
                          <Button
                            variant="ghost"
                            size="sm"
                            className="h-8 gap-1 text-xs"
                            onClick={() => handleRotateKey(k.id, k.name)}
                            title="Rotate Key"
                          >
                            <RefreshCw className="h-3.5 w-3.5" /> Rotate
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            className="h-8 gap-1 text-xs text-rose-400 hover:bg-rose-500/10 hover:text-rose-300"
                            onClick={() => handleRevokeKey(k.id, k.name)}
                            title="Revoke Key"
                          >
                            <Trash2 className="h-3.5 w-3.5" /> Revoke
                          </Button>
                        </div>
                      )}
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Modal: Generate Key */}
      {showGenerateModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-xs">
          <Card className="w-full max-w-md border-[hsl(var(--border))] shadow-2xl">
            <CardHeader className="flex flex-row items-center justify-between">
              <div>
                <CardTitle className="text-xl">Generate Ingestion Key</CardTitle>
                <CardDescription>
                  For {selectedApp?.displayName || "Application"}
                </CardDescription>
              </div>
              <Button
                variant="ghost"
                size="sm"
                className="h-8 w-8 p-0"
                onClick={() => setShowGenerateModal(false)}
              >
                <X className="h-4 w-4" />
              </Button>
            </CardHeader>
            <form onSubmit={handleGenerateKey}>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <label className="text-sm font-medium" htmlFor="keyNameInput">
                    Key Description / Name
                  </label>
                  <Input
                    id="keyNameInput"
                    placeholder="e.g. Production OTel Ingestion"
                    value={keyName}
                    onChange={(e) => setKeyName(e.target.value)}
                    required
                  />
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-medium" htmlFor="expirySelect">
                    Expiration Period
                  </label>
                  <select
                    id="expirySelect"
                    value={expiryOption}
                    onChange={(e) => setExpiryOption(e.target.value)}
                    className="flex h-10 w-full rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--background))] px-3 py-2 text-sm"
                  >
                    <option value="30">30 days</option>
                    <option value="90">90 days (Recommended)</option>
                    <option value="365">1 year</option>
                    <option value="0">Never expires</option>
                  </select>
                </div>
              </CardContent>
              <div className="flex justify-end gap-2 border-t border-[hsl(var(--border))] px-6 py-4">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setShowGenerateModal(false)}
                  disabled={isSubmitting}
                >
                  Cancel
                </Button>
                <Button type="submit" disabled={isSubmitting || !keyName.trim()}>
                  {isSubmitting && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
                  Generate Key
                </Button>
              </div>
            </form>
          </Card>
        </div>
      )}

      {/* Modal: Reveal Raw Key (Once Only!) */}
      {revealedKey && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4 backdrop-blur-xs">
          <Card className="w-full max-w-lg border-emerald-500/30 shadow-2xl bg-[hsl(var(--card))]">
            <CardHeader className="space-y-1">
              <div className="flex items-center gap-2 text-emerald-400">
                <CheckCircle2 className="h-6 w-6" />
                <CardTitle className="text-xl">API Key Generated Successfully</CardTitle>
              </div>
              <CardDescription>
                Copy this key now. For security purposes, it will never be displayed again.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="rounded-md border border-amber-500/20 bg-amber-500/10 p-3 text-xs text-amber-300 flex items-start gap-2">
                <AlertTriangle className="h-4 w-4 shrink-0 mt-0.5" />
                <span>
                  Store this secret securely in your environment variables or key vault. If lost, you must rotate the key.
                </span>
              </div>

              <div className="space-y-2">
                <label className="text-xs font-semibold uppercase tracking-wider text-[hsl(var(--muted-foreground))]">
                  Raw API Key Secret
                </label>
                <div className="flex items-center gap-2">
                  <Input
                    readOnly
                    value={revealedKey.rawApiKey}
                    className="font-mono text-xs select-all bg-[hsl(var(--accent))]"
                  />
                  <Button
                    type="button"
                    variant="outline"
                    className="shrink-0 gap-1.5"
                    onClick={() => copyToClipboard(revealedKey.rawApiKey)}
                  >
                    {isCopied ? (
                      <>
                        <Check className="h-4 w-4 text-emerald-400" />
                        <span className="text-emerald-400 text-xs">Copied</span>
                      </>
                    ) : (
                      <>
                        <Copy className="h-4 w-4" />
                        <span className="text-xs">Copy</span>
                      </>
                    )}
                  </Button>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4 rounded-md bg-[hsl(var(--accent)/0.5)] p-3 text-xs">
                <div>
                  <span className="text-[hsl(var(--muted-foreground))]">Key Name:</span>
                  <p className="font-semibold">{revealedKey.name}</p>
                </div>
                <div>
                  <span className="text-[hsl(var(--muted-foreground))]">Expires:</span>
                  <p className="font-semibold flex items-center gap-1">
                    <Clock className="h-3 w-3" />
                    {revealedKey.expiresAtUtc
                      ? new Date(revealedKey.expiresAtUtc).toLocaleDateString()
                      : "Never"}
                  </p>
                </div>
              </div>
            </CardContent>
            <div className="flex justify-end border-t border-[hsl(var(--border))] px-6 py-4">
              <Button onClick={() => setRevealedKey(null)}>
                I Have Copied the Key
              </Button>
            </div>
          </Card>
        </div>
      )}
    </div>
  );
}
