import { Link, useNavigate } from "react-router-dom";
import { Compass, ArrowLeft, Home, AlertTriangle, FileText } from "lucide-react";
import { Button } from "@/components/ui/button";

export function NotFoundPage() {
  const navigate = useNavigate();

  return (
    <div className="flex min-h-[70vh] flex-col items-center justify-center text-center px-4 py-12">
      <div className="mb-6 flex h-20 w-20 items-center justify-center rounded-2xl bg-slate-900 border border-slate-800 shadow-xl shadow-blue-950/20">
        <Compass className="h-10 w-10 text-blue-500 animate-pulse" />
      </div>

      <div className="inline-flex items-center gap-2 rounded-full border border-slate-800 bg-slate-900/60 px-3 py-1 text-xs font-semibold text-slate-400 mb-4">
        <span>HTTP 404</span>
        <span className="h-1 w-1 rounded-full bg-slate-600" />
        <span>Resource Not Found</span>
      </div>

      <h1 className="text-3xl sm:text-4xl font-extrabold tracking-tight text-white mb-3">
        Page Not Located in Topology
      </h1>

      <p className="max-w-md text-sm sm:text-base text-slate-400 mb-8">
        The requested path does not match any active SentinelOps dashboard, telemetry service, or configuration view. It may have been decommissioned or relocated.
      </p>

      <div className="flex flex-wrap items-center justify-center gap-3 mb-10">
        <Button
          variant="outline"
          className="border-slate-800 hover:bg-slate-800 text-slate-200"
          onClick={() => navigate(-1)}
        >
          <ArrowLeft className="mr-2 h-4 w-4" />
          Previous Screen
        </Button>

        <Button
          className="bg-blue-600 hover:bg-blue-500 text-white shadow-lg shadow-blue-600/20"
          onClick={() => navigate("/")}
        >
          <Home className="mr-2 h-4 w-4" />
          Operations Overview
        </Button>
      </div>

      <div className="w-full max-w-lg rounded-xl border border-slate-800/80 bg-slate-900/40 p-4">
        <p className="text-xs font-medium uppercase tracking-wider text-slate-400 mb-3 text-left">
          Quick Telemetry Navigation
        </p>
        <div className="grid grid-cols-2 gap-2 text-left">
          <Link
            to="/incidents"
            className="flex items-center gap-2.5 rounded-lg border border-slate-800/60 bg-slate-950/40 p-2.5 text-xs text-slate-300 hover:border-slate-700 hover:bg-slate-800/40 hover:text-white transition-colors"
          >
            <AlertTriangle className="h-4 w-4 text-amber-400 shrink-0" />
            <div className="truncate">
              <span className="font-medium block">Active Incidents</span>
              <span className="text-[10px] text-slate-400">Triage firing alerts</span>
            </div>
          </Link>

          <Link
            to="/logs"
            className="flex items-center gap-2.5 rounded-lg border border-slate-800/60 bg-slate-950/40 p-2.5 text-xs text-slate-300 hover:border-slate-700 hover:bg-slate-800/40 hover:text-white transition-colors"
          >
            <FileText className="h-4 w-4 text-blue-400 shrink-0" />
            <div className="truncate">
              <span className="font-medium block">Log Explorer</span>
              <span className="text-[10px] text-slate-400">Structured log streams</span>
            </div>
          </Link>
        </div>
      </div>
    </div>
  );
}
