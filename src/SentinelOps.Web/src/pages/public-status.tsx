import { useParams, Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  getPublicStatusPage,
  type PublicStatusPageDto,
  type StatusPageComponentDto,
  type StatusPageIncidentDto,
} from "@/lib/api-client";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  CheckCircle2,
  AlertTriangle,
  AlertOctagon,
  Clock,
  ExternalLink,
  ShieldCheck,
  RefreshCw,
  Calendar,
  Layers,
} from "lucide-react";

export function PublicStatusPage() {
  const { slug } = useParams<{ slug: string }>();

  const { data: statusPage, isLoading, error, refetch, isFetching } = useQuery<PublicStatusPageDto>({
    queryKey: ["public-status", slug],
    queryFn: () => getPublicStatusPage(slug || ""),
    enabled: !!slug,
    refetchInterval: 30000,
  });

  if (isLoading) {
    return (
      <div className="min-h-screen bg-slate-950 text-slate-100 flex items-center justify-center">
        <div className="flex items-center gap-3 text-sm text-slate-400">
          <RefreshCw className="h-5 w-5 animate-spin text-sky-400" />
          <span>Loading live service status...</span>
        </div>
      </div>
    );
  }

  if (error || !statusPage) {
    return (
      <div className="min-h-screen bg-slate-950 text-slate-100 flex items-center justify-center p-4">
        <div className="max-w-md w-full text-center space-y-4">
          <AlertOctagon className="h-12 w-12 text-rose-500 mx-auto" />
          <h1 className="text-xl font-bold">Status Page Unavailable</h1>
          <p className="text-sm text-slate-400">
            The requested status page &quot;{slug}&quot; could not be found or is set to private.
          </p>
          <Link to="/">
            <Button variant="outline" className="mt-2 text-xs">
              Return to SentinelOps Home
            </Button>
          </Link>
        </div>
      </div>
    );
  }

  const getOverallStatusBanner = () => {
    switch (statusPage.overallStatus) {
      case "All Systems Operational":
        return {
          bg: "bg-emerald-950/40 border-emerald-500/30 text-emerald-300",
          icon: <CheckCircle2 className="h-6 w-6 text-emerald-400" />,
          title: "All Systems Operational",
          desc: "All monitored services and backend infrastructure are running normally.",
        };
      case "Degraded System Performance":
        return {
          bg: "bg-amber-950/40 border-amber-500/30 text-amber-300",
          icon: <AlertTriangle className="h-6 w-6 text-amber-400" />,
          title: "Degraded System Performance",
          desc: "Some components are experiencing minor latency or elevated error rates.",
        };
      case "Partial System Outage":
        return {
          bg: "bg-orange-950/40 border-orange-500/30 text-orange-300",
          icon: <AlertTriangle className="h-6 w-6 text-orange-400" />,
          title: "Partial System Outage",
          desc: "One or more core services are experiencing an outage. Responders are active.",
        };
      case "Major System Outage":
        return {
          bg: "bg-rose-950/40 border-rose-500/30 text-rose-300",
          icon: <AlertOctagon className="h-6 w-6 text-rose-400" />,
          title: "Major System Outage",
          desc: "Critical services are down. Emergency incident response is in progress.",
        };
      default:
        return {
          bg: "bg-blue-950/40 border-blue-500/30 text-blue-300",
          icon: <Clock className="h-6 w-6 text-blue-400" />,
          title: statusPage.overallStatus,
          desc: "Scheduled maintenance or status evaluation is active.",
        };
    }
  };

  const getComponentStatusBadge = (status: string) => {
    switch (status) {
      case "Operational":
        return (
          <span className="flex items-center gap-1.5 text-xs text-emerald-400 font-medium">
            <span className="h-2 w-2 rounded-full bg-emerald-500 animate-pulse" />
            Operational
          </span>
        );
      case "Degraded Performance":
        return (
          <span className="flex items-center gap-1.5 text-xs text-amber-400 font-medium">
            <span className="h-2 w-2 rounded-full bg-amber-500" />
            Degraded Performance
          </span>
        );
      case "Partial Outage":
        return (
          <span className="flex items-center gap-1.5 text-xs text-orange-400 font-medium">
            <span className="h-2 w-2 rounded-full bg-orange-500" />
            Partial Outage
          </span>
        );
      case "Major Outage":
        return (
          <span className="flex items-center gap-1.5 text-xs text-rose-400 font-medium">
            <span className="h-2 w-2 rounded-full bg-rose-500" />
            Major Outage
          </span>
        );
      default:
        return (
          <span className="flex items-center gap-1.5 text-xs text-sky-400 font-medium">
            <span className="h-2 w-2 rounded-full bg-sky-500" />
            {status}
          </span>
        );
    }
  };

  const banner = getOverallStatusBanner();

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 font-sans selection:bg-sky-500 selection:text-white">
      {/* Top Navigation Bar */}
      <header className="border-b border-slate-800 bg-slate-900/60 backdrop-blur sticky top-0 z-40">
        <div className="max-w-4xl mx-auto px-4 h-16 flex items-center justify-between">
          <div className="flex items-center gap-3">
            {statusPage.logoUrl ? (
              <img src={statusPage.logoUrl} alt={statusPage.name} className="h-7 w-7 rounded object-cover" />
            ) : (
              <ShieldCheck className="h-6 w-6 text-sky-400" />
            )}
            <div>
              <h1 className="font-bold text-sm sm:text-base text-white tracking-tight">{statusPage.name}</h1>
              {statusPage.description && (
                <p className="text-[11px] text-slate-400 truncate max-w-xs sm:max-w-md">{statusPage.description}</p>
              )}
            </div>
          </div>

          <div className="flex items-center gap-3">
            {statusPage.supportUrl && (
              <a
                href={statusPage.supportUrl}
                target="_blank"
                rel="noreferrer"
                className="hidden sm:inline-flex items-center gap-1 text-xs text-sky-400 hover:text-sky-300"
              >
                <span>Support Portal</span>
                <ExternalLink className="h-3 w-3" />
              </a>
            )}
            <Button
              size="sm"
              variant="ghost"
              className="h-8 w-8 p-0 text-slate-400 hover:text-white"
              onClick={() => refetch()}
              title="Refresh status"
            >
              <RefreshCw className={`h-3.5 w-3.5 ${isFetching ? "animate-spin" : ""}`} />
            </Button>
          </div>
        </div>
      </header>

      {/* Main Container */}
      <main className="max-w-4xl mx-auto px-4 py-8 space-y-8">
        {/* Header Notice Banner if present */}
        {statusPage.headerNotice && (
          <div className="p-3.5 rounded-lg border border-sky-500/30 bg-sky-950/30 text-xs text-sky-200 flex items-start gap-2.5">
            <span className="font-semibold text-sky-400 uppercase tracking-wider text-[10px] mt-0.5">Notice:</span>
            <span>{statusPage.headerNotice}</span>
          </div>
        )}

        {/* Overall Status Hero Banner */}
        <div className={`p-6 rounded-xl border ${banner.bg} flex items-center gap-4 shadow-lg transition-all`}>
          <div className="p-3 rounded-lg bg-slate-950/60 border border-white/5">{banner.icon}</div>
          <div className="flex-1">
            <h2 className="text-lg font-bold tracking-tight text-white">{banner.title}</h2>
            <p className="text-xs text-slate-300 mt-0.5">{banner.desc}</p>
          </div>
          <div className="text-right text-[11px] text-slate-400 hidden sm:block">
            <span>Last checked: {new Date(statusPage.generatedAtUtc).toLocaleTimeString()}</span>
          </div>
        </div>

        {/* Active Incidents */}
        {(statusPage.activeIncidents || []).length > 0 && (
          <section className="space-y-4">
            <h3 className="text-sm font-semibold uppercase tracking-wider text-rose-400 flex items-center gap-2">
              <AlertTriangle className="h-4 w-4" />
              Active Incidents ({(statusPage.activeIncidents || []).length})
            </h3>

            <div className="space-y-4">
              {(statusPage.activeIncidents || []).map((incident) => (
                <Card key={incident.id} className="border-rose-500/30 bg-rose-950/10">
                  <CardHeader className="p-4 border-b border-rose-500/20 flex flex-row items-center justify-between">
                    <div>
                      <div className="flex items-center gap-2">
                        <Badge variant="danger" className="text-[10px]">
                          {incident.status}
                        </Badge>
                        <Badge variant="outline" className="text-[10px] border-rose-500/40 text-rose-300">
                          Severity: {incident.severity}
                        </Badge>
                      </div>
                      <CardTitle className="text-base font-bold mt-1.5 text-white">{incident.title}</CardTitle>
                    </div>
                    <span className="text-xs text-slate-400">
                      Started: {incident.createdAtUtc ? new Date(incident.createdAtUtc).toLocaleTimeString() : "—"}
                    </span>
                  </CardHeader>
                  <CardContent className="p-4 space-y-3">
                    {(incident.updates || []).map((update) => (
                      <div key={update.id} className="pl-3 border-l-2 border-rose-500/40 space-y-1">
                        <div className="flex items-center justify-between text-[11px] text-slate-400">
                          <span className="font-semibold text-rose-300">{update.status}</span>
                          <span>{update.createdAtUtc ? new Date(update.createdAtUtc).toLocaleString() : "—"}</span>
                        </div>
                        <p className="text-xs text-slate-200 whitespace-pre-wrap">{update.message}</p>
                      </div>
                    ))}
                  </CardContent>
                </Card>
              ))}
            </div>
          </section>
        )}

        {/* Scheduled Maintenance */}
        {(statusPage.scheduledMaintenance || []).length > 0 && (
          <section className="space-y-4">
            <h3 className="text-sm font-semibold uppercase tracking-wider text-sky-400 flex items-center gap-2">
              <Calendar className="h-4 w-4" />
              Scheduled Maintenance
            </h3>

            <div className="space-y-3">
              {(statusPage.scheduledMaintenance || []).map((m) => (
                <div key={m.id} className="p-4 rounded-lg bg-slate-900 border border-slate-800 space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="font-bold text-sm text-white">{m.title}</span>
                    <Badge variant="outline" className="text-[10px] text-sky-300 border-sky-500/40">
                      {m.status}
                    </Badge>
                  </div>
                  {m.description && <p className="text-xs text-slate-300">{m.description}</p>}
                  <div className="text-[11px] text-slate-400 flex items-center gap-2">
                    <Clock className="h-3.5 w-3.5 text-sky-400" />
                    <span>
                      {m.startTimeUtc ? new Date(m.startTimeUtc).toLocaleString() : "—"} — {m.endTimeUtc ? new Date(m.endTimeUtc).toLocaleString() : "—"}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </section>
        )}

        {/* Components Status Grid */}
        <section className="space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-semibold uppercase tracking-wider text-slate-400 flex items-center gap-2">
              <Layers className="h-4 w-4" />
              System Components ({(statusPage.components || []).length})
            </h3>
            <span className="text-xs text-slate-500">Live Telemetry Rollup</span>
          </div>

          <Card className="border-slate-800 bg-slate-900/60 divide-y divide-slate-800 overflow-hidden">
            {(statusPage.components || []).map((comp: StatusPageComponentDto) => (
              <div key={comp.id} className="p-4 flex items-center justify-between hover:bg-slate-800/30 transition-colors">
                <div>
                  <div className="font-semibold text-xs sm:text-sm text-white">{comp.name}</div>
                  {comp.description && (
                    <div className="text-[11px] text-slate-400 mt-0.5">{comp.description}</div>
                  )}
                </div>
                <div>{getComponentStatusBadge(comp.computedStatus)}</div>
              </div>
            ))}
          </Card>
        </section>

        {/* Past Incidents Archive */}
        <section className="space-y-4 pt-4 border-t border-slate-800">
          <h3 className="text-sm font-semibold uppercase tracking-wider text-slate-400">
            Past Incident History
          </h3>

          {(statusPage.pastIncidents || []).length === 0 ? (
            <div className="p-6 rounded-lg bg-slate-900/40 border border-slate-800 text-center text-xs text-slate-400">
              No incidents reported in the last 30 days. System reliability is 100%.
            </div>
          ) : (
            <div className="space-y-3">
              {(statusPage.pastIncidents || []).map((incident: StatusPageIncidentDto) => (
                <div key={incident.id} className="p-4 rounded-lg bg-slate-900/50 border border-slate-800 space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="font-semibold text-xs sm:text-sm text-slate-200">{incident.title}</span>
                    <Badge variant="secondary" className="text-[10px] bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
                      Resolved
                    </Badge>
                  </div>
                  <div className="text-[11px] text-slate-400 flex items-center justify-between">
                    <span>
                      Resolved: {incident.resolvedAtUtc ? new Date(incident.resolvedAtUtc).toLocaleString() : "Recently"}
                    </span>
                    <span>{(incident.updates || []).length} updates logged</span>
                  </div>
                  {(incident.updates || []).length > 0 && (
                    <p className="text-xs text-slate-300 pl-3 border-l border-slate-700 mt-2">
                      {incident.updates[0].message}
                    </p>
                  )}
                </div>
              ))}
            </div>
          )}
        </section>

        {/* Footer */}
        <footer className="pt-8 pb-12 border-t border-slate-800 text-center text-xs text-slate-500 space-y-2">
          <div className="flex items-center justify-center gap-2">
            <ShieldCheck className="h-4 w-4 text-sky-400" />
            <span className="font-semibold text-slate-400">Powered by SentinelOps</span>
          </div>
          <p>Automated real-time observability, incident intelligence, and SLA monitoring platform.</p>
        </footer>
      </main>
    </div>
  );
}
