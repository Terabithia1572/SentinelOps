import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import {
  getStatusPages,
  createStatusPage,
  deleteStatusPage,
  postStatusPageIncident,
  type StatusPageDto,
  type CreateStatusPageRequest,
  type CreateStatusPageIncidentRequest,
} from "@/lib/api-client";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import {
  Globe,
  Plus,
  ExternalLink,
  Trash2,
  AlertTriangle,
  Send,
  RefreshCw,
} from "lucide-react";

export function StatusPagesPage() {
  const queryClient = useQueryClient();

  // Dialog States
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [selectedPageForIncident, setSelectedPageForIncident] = useState<StatusPageDto | null>(null);

  // Create Form State
  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [description, setDescription] = useState("");
  const [logoUrl, setLogoUrl] = useState("");
  const [supportUrl, setSupportUrl] = useState("");
  const [headerNotice, setHeaderNotice] = useState("");
  const [isPublic, setIsPublic] = useState(true);
  const [autoRollup, setAutoRollup] = useState(true);

  // Incident Form State
  const [incTitle, setIncTitle] = useState("");
  const [incStatus, setIncStatus] = useState("Investigating");
  const [incSeverity, setIncSeverity] = useState("Major");
  const [incMessage, setIncMessage] = useState("");
  const [affectedComponentIds, setAffectedComponentIds] = useState<string[]>([]);

  const { data: statusPages = [], isLoading, refetch } = useQuery({
    queryKey: ["status-pages"],
    queryFn: getStatusPages,
  });

  const createMutation = useMutation({
    mutationFn: (req: CreateStatusPageRequest) => createStatusPage(req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["status-pages"] });
      setShowCreateModal(false);
      resetCreateForm();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteStatusPage(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["status-pages"] });
    },
  });

  const postIncidentMutation = useMutation({
    mutationFn: ({ pageId, req }: { pageId: string; req: CreateStatusPageIncidentRequest }) =>
      postStatusPageIncident(pageId, req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["status-pages"] });
      setSelectedPageForIncident(null);
      resetIncidentForm();
    },
  });

  const resetCreateForm = () => {
    setName("");
    setSlug("");
    setDescription("");
    setLogoUrl("");
    setSupportUrl("");
    setHeaderNotice("");
    setIsPublic(true);
    setAutoRollup(true);
  };

  const resetIncidentForm = () => {
    setIncTitle("");
    setIncStatus("Investigating");
    setIncSeverity("Major");
    setIncMessage("");
    setAffectedComponentIds([]);
  };

  const handleNameChange = (val: string) => {
    setName(val);
    if (!slug || slug === name.toLowerCase().replace(/[^a-z0-9]+/g, "-")) {
      setSlug(val.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, ""));
    }
  };

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-xl font-bold tracking-tight">Public Status Pages</h1>
            <Badge variant="outline" className="text-xs">
              SaaS Multi-Tenant
            </Badge>
          </div>
          <p className="text-xs text-[hsl(var(--muted-foreground))] mt-0.5">
            Branded customer-facing status portals with live telemetry rollup and incident communication timelines.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => refetch()} className="h-8 text-xs gap-1.5">
            <RefreshCw className="h-3 w-3" />
            Refresh
          </Button>
          <Button
            size="sm"
            onClick={() => setShowCreateModal(true)}
            className="h-8 text-xs gap-1.5 bg-sky-600 hover:bg-sky-700 text-white"
          >
            <Plus className="h-3.5 w-3.5" />
            New Status Page
          </Button>
        </div>
      </div>

      {/* Status Pages List */}
      {isLoading ? (
        <div className="py-24 text-center text-xs text-[hsl(var(--muted-foreground))] flex items-center justify-center gap-2">
          <RefreshCw className="h-4 w-4 animate-spin text-sky-400" />
          Loading status pages...
        </div>
      ) : statusPages.length === 0 ? (
        <Card className="border-dashed border-[hsl(var(--border))]">
          <CardContent className="py-16 text-center space-y-3">
            <Globe className="h-10 w-10 text-[hsl(var(--muted-foreground))] mx-auto opacity-40" />
            <h3 className="font-semibold text-sm">No Status Pages Configured</h3>
            <p className="text-xs text-[hsl(var(--muted-foreground))] max-w-sm mx-auto">
              Create a branded public status page to communicate system health, active incidents, and maintenance schedules to your users.
            </p>
            <Button size="sm" onClick={() => setShowCreateModal(true)} className="text-xs gap-1.5">
              <Plus className="h-3.5 w-3.5" />
              Create First Status Page
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {statusPages.map((page: StatusPageDto) => (
            <Card key={page.id} className="border-[hsl(var(--border))] flex flex-col justify-between hover:border-sky-500/50 transition-colors">
              <CardHeader className="p-4 pb-2">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Globe className="h-4 w-4 text-sky-400" />
                    <CardTitle className="text-sm font-bold truncate">{page.name}</CardTitle>
                  </div>
                  <Badge variant={page.isPublic ? "default" : "secondary"} className="text-[10px] py-0">
                    {page.isPublic ? "Public" : "Private"}
                  </Badge>
                </div>
                <CardDescription className="text-xs text-[hsl(var(--muted-foreground))] mt-1 line-clamp-2">
                  {page.description || "No description provided."}
                </CardDescription>
              </CardHeader>

              <CardContent className="p-4 pt-2 space-y-4">
                <div className="p-2.5 rounded bg-accent/20 border border-[hsl(var(--border))] space-y-1.5 text-xs">
                  <div className="flex items-center justify-between">
                    <span className="text-[hsl(var(--muted-foreground))]">URL Slug:</span>
                    <span className="font-mono text-sky-400 text-[11px]">/status/{page.slug}</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-[hsl(var(--muted-foreground))]">Monitored Components:</span>
                    <span className="font-medium text-foreground">{page.components.length} Services</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-[hsl(var(--muted-foreground))]">Active Incidents:</span>
                    <span className={page.activeIncidentsCount > 0 ? "text-rose-400 font-semibold" : "text-emerald-400 font-medium"}>
                      {page.activeIncidentsCount > 0 ? `${page.activeIncidentsCount} Active` : "None (Healthy)"}
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-[hsl(var(--muted-foreground))]">Telemetry Rollup:</span>
                    <span className="text-foreground">{page.autoRollupEnabled ? "Enabled" : "Manual"}</span>
                  </div>
                </div>

                <div className="flex items-center justify-between gap-2 pt-2 border-t border-[hsl(var(--border))]">
                  <div className="flex items-center gap-1.5">
                    <Link to={`/status/${page.slug}`} target="_blank">
                      <Button size="sm" variant="outline" className="h-7 text-xs gap-1 border-sky-500/30 text-sky-400 hover:bg-sky-500/10">
                        <ExternalLink className="h-3 w-3" />
                        View Live
                      </Button>
                    </Link>
                    <Button
                      size="sm"
                      variant="outline"
                      className="h-7 text-xs gap-1 border-amber-500/30 text-amber-400 hover:bg-amber-500/10"
                      onClick={() => {
                        setSelectedPageForIncident(page);
                        resetIncidentForm();
                      }}
                    >
                      <AlertTriangle className="h-3 w-3" />
                      Post Incident
                    </Button>
                  </div>

                  <Button
                    size="sm"
                    variant="ghost"
                    className="h-7 w-7 p-0 text-rose-400 hover:text-rose-300 hover:bg-rose-500/10"
                    onClick={() => {
                      if (confirm(`Delete status page "${page.name}"?`)) {
                        deleteMutation.mutate(page.id);
                      }
                    }}
                    disabled={deleteMutation.isPending}
                    title="Delete status page"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Create Status Page Modal */}
      {showCreateModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
          <div className="w-full max-w-lg rounded-lg border border-[hsl(var(--border))] bg-[hsl(var(--card))] p-6 shadow-2xl animate-in zoom-in-95 space-y-4">
            <div>
              <h3 className="text-base font-semibold">Create Public Status Page</h3>
              <p className="text-xs text-[hsl(var(--muted-foreground))] mt-0.5">
                Configure a customer-facing portal to communicate service uptime and incident progress.
              </p>
            </div>

            <div className="space-y-3 text-xs">
              <div className="space-y-1">
                <label className="font-medium">Page Title *</label>
                <Input
                  value={name}
                  onChange={(e) => handleNameChange(e.target.value)}
                  placeholder="e.g. Acme Cloud Platform Status"
                  className="h-8 text-xs"
                />
              </div>

              <div className="space-y-1">
                <label className="font-medium">URL Slug *</label>
                <div className="flex items-center gap-1.5">
                  <span className="text-[hsl(var(--muted-foreground))] font-mono">/status/</span>
                  <Input
                    value={slug}
                    onChange={(e) => setSlug(e.target.value)}
                    placeholder="acme-status"
                    className="h-8 text-xs font-mono"
                  />
                </div>
              </div>

              <div className="space-y-1">
                <label className="font-medium">Description</label>
                <textarea
                  rows={2}
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Real-time uptime metrics, scheduled maintenance, and incident updates for Acme Cloud."
                  className="w-full rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] p-2 text-xs"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <label className="font-medium">Support URL</label>
                  <Input
                    value={supportUrl}
                    onChange={(e) => setSupportUrl(e.target.value)}
                    placeholder="https://support.example.com"
                    className="h-8 text-xs"
                  />
                </div>
                <div className="space-y-1">
                  <label className="font-medium">Logo URL</label>
                  <Input
                    value={logoUrl}
                    onChange={(e) => setLogoUrl(e.target.value)}
                    placeholder="https://example.com/logo.png"
                    className="h-8 text-xs"
                  />
                </div>
              </div>

              <div className="space-y-1">
                <label className="font-medium">Header Notice Banner (Optional)</label>
                <Input
                  value={headerNotice}
                  onChange={(e) => setHeaderNotice(e.target.value)}
                  placeholder="e.g. Scheduled database maintenance on Saturday Oct 18, 02:00-04:00 UTC."
                  className="h-8 text-xs"
                />
              </div>

              <div className="flex items-center gap-4 pt-2">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={isPublic}
                    onChange={(e) => setIsPublic(e.target.checked)}
                    className="rounded border-[hsl(var(--border))]"
                  />
                  <span>Publicly Accessible (No Login)</span>
                </label>

                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={autoRollup}
                    onChange={(e) => setAutoRollup(e.target.checked)}
                    className="rounded border-[hsl(var(--border))]"
                  />
                  <span>Live Telemetry Health Rollup</span>
                </label>
              </div>
            </div>

            <div className="flex justify-end gap-2 pt-3 border-t border-[hsl(var(--border))]">
              <Button variant="ghost" size="sm" onClick={() => setShowCreateModal(false)}>
                Cancel
              </Button>
              <Button
                size="sm"
                className="bg-sky-600 hover:bg-sky-700 text-white text-xs"
                disabled={!name.trim() || !slug.trim() || createMutation.isPending}
                onClick={() =>
                  createMutation.mutate({
                    name,
                    slug,
                    description,
                    supportUrl,
                    logoUrl,
                    headerNotice,
                    isPublic,
                    autoRollupEnabled: autoRollup,
                  })
                }
              >
                {createMutation.isPending ? "Creating..." : "Create Status Page"}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Post Status Incident Modal */}
      {selectedPageForIncident && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
          <div className="w-full max-w-lg rounded-lg border border-amber-500/30 bg-[hsl(var(--card))] p-6 shadow-2xl animate-in zoom-in-95 space-y-4">
            <div>
              <h3 className="text-base font-semibold flex items-center gap-2 text-white">
                <AlertTriangle className="h-4 w-4 text-amber-400" />
                Publish Incident Update: {selectedPageForIncident.name}
              </h3>
              <p className="text-xs text-[hsl(var(--muted-foreground))] mt-0.5">
                Broadcast an incident notice to external customers viewing this status page.
              </p>
            </div>

            <div className="space-y-3 text-xs">
              <div className="space-y-1">
                <label className="font-medium">Incident Title *</label>
                <Input
                  value={incTitle}
                  onChange={(e) => setIncTitle(e.target.value)}
                  placeholder="e.g. Elevated API Gateway Latency & 504 Timeouts"
                  className="h-8 text-xs"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <label className="font-medium">Status *</label>
                  <select
                    value={incStatus}
                    onChange={(e) => setIncStatus(e.target.value)}
                    className="w-full rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] h-8 px-2 text-xs"
                  >
                    <option value="Investigating">Investigating</option>
                    <option value="Identified">Identified</option>
                    <option value="Monitoring">Monitoring</option>
                    <option value="Resolved">Resolved</option>
                  </select>
                </div>
                <div className="space-y-1">
                  <label className="font-medium">Impact Severity *</label>
                  <select
                    value={incSeverity}
                    onChange={(e) => setIncSeverity(e.target.value)}
                    className="w-full rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] h-8 px-2 text-xs"
                  >
                    <option value="Degraded">Degraded Performance</option>
                    <option value="Minor">Minor Outage</option>
                    <option value="Major">Major Outage</option>
                    <option value="Critical">Critical Outage</option>
                  </select>
                </div>
              </div>

              {/* Affected Component Selection */}
              {selectedPageForIncident.components.length > 0 && (
                <div className="space-y-1">
                  <label className="font-medium">Affected Components</label>
                  <div className="grid grid-cols-2 gap-2 p-2 rounded border border-[hsl(var(--border))] bg-slate-950">
                    {selectedPageForIncident.components.map((comp) => (
                      <label key={comp.id} className="flex items-center gap-2 cursor-pointer text-[11px]">
                        <input
                          type="checkbox"
                          checked={affectedComponentIds.includes(comp.id)}
                          onChange={(e) => {
                            if (e.target.checked) {
                              setAffectedComponentIds([...affectedComponentIds, comp.id]);
                            } else {
                              setAffectedComponentIds(affectedComponentIds.filter((id) => id !== comp.id));
                            }
                          }}
                          className="rounded border-[hsl(var(--border))]"
                        />
                        <span className="truncate">{comp.name}</span>
                      </label>
                    ))}
                  </div>
                </div>
              )}

              <div className="space-y-1">
                <label className="font-medium">Initial Status Update Message *</label>
                <textarea
                  rows={3}
                  value={incMessage}
                  onChange={(e) => setIncMessage(e.target.value)}
                  placeholder="We are investigating elevated response times on our core API endpoints. Our telemetry team is actively troubleshooting the root cause."
                  className="w-full rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--card))] p-2 text-xs"
                />
              </div>
            </div>

            <div className="flex justify-end gap-2 pt-3 border-t border-[hsl(var(--border))]">
              <Button variant="ghost" size="sm" onClick={() => setSelectedPageForIncident(null)}>
                Cancel
              </Button>
              <Button
                size="sm"
                className="bg-amber-600 hover:bg-amber-700 text-white text-xs gap-1.5"
                disabled={!incTitle.trim() || !incMessage.trim() || postIncidentMutation.isPending}
                onClick={() =>
                  postIncidentMutation.mutate({
                    pageId: selectedPageForIncident.id,
                    req: {
                      title: incTitle,
                      status: incStatus,
                      severity: incSeverity,
                      affectedComponentIds,
                      initialMessage: incMessage,
                    },
                  })
                }
              >
                <Send className="h-3 w-3" />
                {postIncidentMutation.isPending ? "Publishing..." : "Publish Incident"}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
