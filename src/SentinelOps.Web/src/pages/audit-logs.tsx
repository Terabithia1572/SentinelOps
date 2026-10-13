import { useState, useEffect, useCallback } from "react";
import {
  getAuditEvents,
  getAuditSummary,
  getAuditEventDetail,
  type AuditEventDto,
  type AuditSummaryDto,
} from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  ShieldAlert,
  Search,
  Filter,
  RefreshCw,
  User,
  Activity,
  FileCode,
  Terminal,
  AlertOctagon,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Loader2,
  X,
} from "lucide-react";

export function AuditLogsPage() {
  const [events, setEvents] = useState<AuditEventDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(25);
  const [totalPages, setTotalPages] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Summary
  const [summary, setSummary] = useState<AuditSummaryDto | null>(null);

  // Filters
  const [search, setSearch] = useState("");
  const [outcomeFilter, setOutcomeFilter] = useState("all");
  const [targetTypeFilter, setTargetTypeFilter] = useState("all");
  const [timeRange, setTimeRange] = useState("24h");

  // Selected Detail Modal
  const [selectedEvent, setSelectedEvent] = useState<AuditEventDto | null>(null);


  const fetchSummary = async () => {
    try {
      const sum = await getAuditSummary();
      setSummary(sum);
    } catch {
      // Non-blocking
    }
  };

  const fetchEvents = useCallback(async () => {
    try {
      setIsLoading(true);
      setErrorMessage(null);

      let fromUtc: string | undefined = undefined;
      const now = new Date();
      if (timeRange === "24h") {
        fromUtc = new Date(now.getTime() - 24 * 3600 * 1000).toISOString();
      } else if (timeRange === "7d") {
        fromUtc = new Date(now.getTime() - 7 * 24 * 3600 * 1000).toISOString();
      } else if (timeRange === "30d") {
        fromUtc = new Date(now.getTime() - 30 * 24 * 3600 * 1000).toISOString();
      }

      const res = await getAuditEvents({
        page,
        pageSize,
        search: search.trim() || undefined,
        outcome: outcomeFilter !== "all" ? outcomeFilter : undefined,
        targetType: targetTypeFilter !== "all" ? targetTypeFilter : undefined,
        fromUtc,
      });

      setEvents(res.items);
      setTotalCount(res.totalCount);
      setTotalPages(res.totalPages);
    } catch (err: any) {
      setErrorMessage(err.response?.data?.message || err.message || "Failed to load audit trail");
    } finally {
      setIsLoading(false);
    }
  }, [page, pageSize, search, outcomeFilter, targetTypeFilter, timeRange]);

  useEffect(() => {
    fetchEvents();
    fetchSummary();
  }, [fetchEvents]);

  const handleInspect = async (event: AuditEventDto) => {
    try {
      const detail = await getAuditEventDetail(event.id);
      setSelectedEvent(detail);
    } catch {
      setSelectedEvent(event);
    }
  };


  return (
    <div className="space-y-6">
      {/* Page Title */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-white flex items-center gap-2">
            <ShieldAlert className="w-7 h-7 text-purple-400" />
            Security & Audit Center
          </h1>
          <p className="text-sm text-slate-400">
            Immutable audit trail of security-sensitive events, authorization changes, and forensic records
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => {
              fetchEvents();
              fetchSummary();
            }}
            disabled={isLoading}
            className="flex items-center gap-1.5"
          >
            <RefreshCw className={`w-4 h-4 ${isLoading ? "animate-spin" : ""}`} />
            Refresh
          </Button>
        </div>
      </div>

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

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="bg-slate-900/60 border-slate-800">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-xs font-medium text-slate-400">Security Events (24h)</p>
              <h3 className="text-2xl font-bold text-white mt-1">{summary?.totalEvents24h ?? totalCount}</h3>
            </div>
            <div className="w-10 h-10 rounded-lg bg-purple-500/10 flex items-center justify-center text-purple-400">
              <Activity className="w-5 h-5" />
            </div>
          </CardContent>
        </Card>

        <Card className="bg-slate-900/60 border-slate-800">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-xs font-medium text-slate-400">Security Failures (24h)</p>
              <h3 className="text-2xl font-bold text-rose-400 mt-1">{summary?.failureCount24h ?? 0}</h3>
            </div>
            <div className="w-10 h-10 rounded-lg bg-rose-500/10 flex items-center justify-center text-rose-400">
              <AlertOctagon className="w-5 h-5" />
            </div>
          </CardContent>
        </Card>

        <Card className="bg-slate-900/60 border-slate-800">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-xs font-medium text-slate-400">Top Action</p>
              <h3 className="text-sm font-bold text-sky-400 mt-1 truncate max-w-[150px]">
                {summary?.topActions?.[0]?.key || "Auth.LoginSuccess"}
              </h3>
              <p className="text-[10px] text-slate-500">{summary?.topActions?.[0]?.count ?? 0} events</p>
            </div>
            <div className="w-10 h-10 rounded-lg bg-sky-500/10 flex items-center justify-center text-sky-400">
              <Terminal className="w-5 h-5" />
            </div>
          </CardContent>
        </Card>

        <Card className="bg-slate-900/60 border-slate-800">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-xs font-medium text-slate-400">Top Actor</p>
              <h3 className="text-sm font-bold text-emerald-400 mt-1 truncate max-w-[150px]">
                {summary?.topActors?.[0]?.key || "admin"}
              </h3>
              <p className="text-[10px] text-slate-500">{summary?.topActors?.[0]?.count ?? 0} actions</p>
            </div>
            <div className="w-10 h-10 rounded-lg bg-emerald-500/10 flex items-center justify-center text-emerald-400">
              <User className="w-5 h-5" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filter Bar */}
      <Card className="bg-slate-900/60 border-slate-800">
        <CardContent className="p-4">
          <div className="flex flex-col md:flex-row gap-3">
            <div className="flex-1 relative">
              <Search className="w-4 h-4 absolute left-3 top-3 text-slate-500" />
              <Input
                placeholder="Search audit trail by actor, action, details, target..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="pl-9 bg-slate-950 border-slate-800 text-slate-100"
              />
            </div>

            <div className="flex flex-wrap items-center gap-2">
              <Filter className="w-4 h-4 text-slate-400" />
              <select
                value={outcomeFilter}
                onChange={(e) => {
                  setOutcomeFilter(e.target.value);
                  setPage(1);
                }}
                className="bg-slate-950 border border-slate-800 text-sm text-slate-200 rounded-md px-3 py-2 outline-none"
              >
                <option value="all">All Outcomes</option>
                <option value="Success">Success</option>
                <option value="Failure">Failure</option>
              </select>

              <select
                value={targetTypeFilter}
                onChange={(e) => {
                  setTargetTypeFilter(e.target.value);
                  setPage(1);
                }}
                className="bg-slate-950 border border-slate-800 text-sm text-slate-200 rounded-md px-3 py-2 outline-none"
              >
                <option value="all">All Target Types</option>
                <option value="User">User</option>
                <option value="ApiKey">ApiKey</option>
                <option value="AlertRule">AlertRule</option>
                <option value="Incident">Incident</option>
                <option value="Report">Report</option>
                <option value="System">System</option>
                <option value="ApplicationMember">ApplicationMember</option>
              </select>

              <select
                value={timeRange}
                onChange={(e) => {
                  setTimeRange(e.target.value);
                  setPage(1);
                }}
                className="bg-slate-950 border border-slate-800 text-sm text-slate-200 rounded-md px-3 py-2 outline-none"
              >
                <option value="24h">Last 24 Hours</option>
                <option value="7d">Last 7 Days</option>
                <option value="30d">Last 30 Days</option>
                <option value="all">All Time</option>
              </select>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Audit Log Table */}
      <Card className="bg-slate-900/60 border-slate-800">
        <CardHeader className="pb-3 border-b border-slate-800">
          <CardTitle className="text-base font-semibold text-white">Audit Event Stream</CardTitle>
          <CardDescription className="text-xs text-slate-400">
            Append-only forensic event log with IP tracking and payload sanitization
          </CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-950/60 text-xs text-slate-400 uppercase border-b border-slate-800">
                <tr>
                  <th className="px-4 py-3">Timestamp (UTC)</th>
                  <th className="px-4 py-3">Outcome</th>
                  <th className="px-4 py-3">Action</th>
                  <th className="px-4 py-3">Actor & IP</th>
                  <th className="px-4 py-3">Target</th>
                  <th className="px-4 py-3">Details</th>
                  <th className="px-4 py-3 text-right">Inspect</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/60">
                {isLoading ? (
                  <tr>
                    <td colSpan={7} className="py-12 text-center text-slate-400">
                      <Loader2 className="w-6 h-6 animate-spin mx-auto text-purple-400 mb-2" />
                      Streaming audit logs...
                    </td>
                  </tr>
                ) : events.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="py-12 text-center text-slate-400">
                      No audit events match the query criteria.
                    </td>
                  </tr>
                ) : (
                  events.map((ev) => {
                    const isSuccess = ev.outcome.toLowerCase() === "success";
                    return (
                      <tr key={ev.id} className="hover:bg-slate-800/30 transition-colors">
                        <td className="px-4 py-3 text-xs text-slate-400 font-mono">
                          {new Date(ev.timestampUtc).toLocaleString()}
                        </td>
                        <td className="px-4 py-3">
                          {isSuccess ? (
                            <span className="inline-flex items-center gap-1 text-emerald-400 text-xs font-medium">
                              <CheckCircle2 className="w-3.5 h-3.5" />
                              Success
                            </span>
                          ) : (
                            <span className="inline-flex items-center gap-1 text-rose-400 text-xs font-medium">
                              <XCircle className="w-3.5 h-3.5" />
                              Failure
                            </span>
                          )}
                        </td>
                        <td className="px-4 py-3">
                          <Badge variant="outline" className="font-mono text-[11px] bg-slate-950 border-slate-800 text-sky-300">
                            {ev.action}
                          </Badge>
                        </td>
                        <td className="px-4 py-3">
                          <div className="text-xs font-medium text-slate-200">{ev.actorName || ev.actorId || "System"}</div>
                          <div className="text-[10px] text-slate-500 font-mono">{ev.ipAddress || "Internal"}</div>
                        </td>
                        <td className="px-4 py-3 text-xs text-slate-300">
                          <span className="text-slate-500">{ev.targetEntityType}:</span> {ev.targetEntityId}
                        </td>
                        <td className="px-4 py-3 text-xs text-slate-400 max-w-xs truncate" title={ev.details}>
                          {ev.details}
                        </td>
                        <td className="px-4 py-3 text-right">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleInspect(ev)}
                            className="h-8 px-2 text-slate-400 hover:text-white"
                          >
                            <FileCode className="w-3.5 h-3.5" />
                          </Button>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between p-4 border-t border-slate-800 text-xs text-slate-400">
              <div>Page {page} of {totalPages} ({totalCount} total events)</div>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page >= totalPages}
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                >
                  Next
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Forensic Inspection Modal */}
      {selectedEvent && (
        <div className="fixed inset-0 z-50 bg-black/70 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto p-6 space-y-4">
            <div className="flex justify-between items-start border-b border-slate-800 pb-3">
              <div>
                <h3 className="text-lg font-bold text-white flex items-center gap-2">
                  <Terminal className="w-5 h-5 text-purple-400" />
                  Forensic Event Details
                </h3>
                <p className="text-xs text-slate-400 font-mono">Event ID: {selectedEvent.id}</p>
              </div>
              <button onClick={() => setSelectedEvent(null)} className="text-slate-400 hover:text-white">
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="grid grid-cols-2 gap-4 text-xs">
              <div className="bg-slate-950 p-3 rounded-lg border border-slate-800">
                <span className="text-slate-500 block mb-0.5">Timestamp (UTC)</span>
                <span className="font-mono text-slate-200">{selectedEvent.timestampUtc}</span>
              </div>
              <div className="bg-slate-950 p-3 rounded-lg border border-slate-800">
                <span className="text-slate-500 block mb-0.5">Action & Outcome</span>
                <span className="font-semibold text-slate-200">{selectedEvent.action}</span> (
                <span className={selectedEvent.outcome === "Success" ? "text-emerald-400" : "text-rose-400"}>
                  {selectedEvent.outcome}
                </span>)
              </div>
              <div className="bg-slate-950 p-3 rounded-lg border border-slate-800">
                <span className="text-slate-500 block mb-0.5">Actor Info</span>
                <span className="text-slate-200 font-medium">{selectedEvent.actorName || "System"}</span> (ID: {selectedEvent.actorId || "N/A"})
              </div>
              <div className="bg-slate-950 p-3 rounded-lg border border-slate-800">
                <span className="text-slate-500 block mb-0.5">IP Address</span>
                <span className="font-mono text-slate-200">{selectedEvent.ipAddress || "Internal"}</span>
              </div>
              <div className="bg-slate-950 p-3 rounded-lg border border-slate-800">
                <span className="text-slate-500 block mb-0.5">Target Entity</span>
                <span className="text-slate-200">{selectedEvent.targetEntityType} / {selectedEvent.targetEntityId}</span>
              </div>
              <div className="bg-slate-950 p-3 rounded-lg border border-slate-800">
                <span className="text-slate-500 block mb-0.5">Correlation ID</span>
                <span className="font-mono text-slate-400 truncate block">{selectedEvent.correlationId || "None"}</span>
              </div>
            </div>

            {selectedEvent.userAgent && (
              <div className="text-xs bg-slate-950 p-3 rounded-lg border border-slate-800">
                <span className="text-slate-500 block mb-0.5">User Agent</span>
                <span className="font-mono text-slate-300 break-all">{selectedEvent.userAgent}</span>
              </div>
            )}

            <div className="text-xs bg-slate-950 p-3 rounded-lg border border-slate-800">
              <span className="text-slate-500 block mb-1">Human-Readable Details</span>
              <p className="text-slate-200">{selectedEvent.details}</p>
            </div>

            {/* Sanitized Metadata JSON Viewer */}
            <div className="space-y-1.5">
              <span className="text-xs font-semibold text-slate-300">Sanitized Event Metadata</span>
              <pre className="bg-slate-950 p-4 rounded-lg border border-slate-800 text-xs font-mono text-slate-300 overflow-x-auto max-h-60">
                {selectedEvent.metadataJson
                  ? JSON.stringify(JSON.parse(selectedEvent.metadataJson), null, 2)
                  : "No extended metadata payload attached."}
              </pre>
            </div>

            <div className="pt-2 flex justify-end">
              <Button variant="outline" onClick={() => setSelectedEvent(null)}>Close</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
