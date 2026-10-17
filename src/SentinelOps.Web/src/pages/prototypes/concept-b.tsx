import { Link } from "react-router-dom";
import {
  ArrowRight,
  CheckCircle
} from "lucide-react";

export function ConceptBPage({ screen }: { screen: "login" | "overview" | "logs" | "incident" }) {

  return (
    <div className="min-h-screen bg-[#111315] text-[#EDECE8] font-sans antialiased selection:bg-[#E5A93C]/20 selection:text-[#E5A93C]">
      {/* Concept Navigation Bar */}
      <div className="border-b border-[#282C33] bg-[#16181B] px-8 py-3 flex items-center justify-between text-xs sticky top-0 z-50">
        <div className="flex items-center gap-3">
          <span className="font-serif italic text-[#E5A93C] font-semibold text-sm">
            Concept B: Editorial Enterprise
          </span>
          <span className="text-[#4E5460]">|</span>
          <span className="text-[#9CA3AF] text-xs">Screen: <strong className="text-white capitalize">{screen}</strong></span>
        </div>
        <div className="flex items-center gap-2">
          <Link
            to="/design-prototypes/concept-b/login"
            className={`px-3 py-1 rounded transition text-xs ${screen === "login" ? "bg-[#E5A93C] text-[#111315] font-semibold" : "text-[#9CA3AF] hover:text-white"}`}
          >
            1. Login
          </Link>
          <Link
            to="/design-prototypes/concept-b/overview"
            className={`px-3 py-1 rounded transition text-xs ${screen === "overview" ? "bg-[#E5A93C] text-[#111315] font-semibold" : "text-[#9CA3AF] hover:text-white"}`}
          >
            2. Overview
          </Link>
          <Link
            to="/design-prototypes/concept-b/logs"
            className={`px-3 py-1 rounded transition text-xs ${screen === "logs" ? "bg-[#E5A93C] text-[#111315] font-semibold" : "text-[#9CA3AF] hover:text-white"}`}
          >
            3. Log Explorer
          </Link>
          <Link
            to="/design-prototypes/concept-b/incident"
            className={`px-3 py-1 rounded transition text-xs ${screen === "incident" ? "bg-[#E5A93C] text-[#111315] font-semibold" : "text-[#9CA3AF] hover:text-white"}`}
          >
            4. Incident Detail
          </Link>
        </div>
      </div>

      {/* Screen 1: Login */}
      {screen === "login" && (
        <div className="min-h-[calc(100vh-50px)] max-w-5xl mx-auto px-8 py-16 flex flex-col justify-center">
          <div className="border border-[#282C33] bg-[#17191D] p-12 lg:p-16 rounded-md shadow-2xl relative">
            <div className="border-b border-[#282C33] pb-8 mb-10 flex items-baseline justify-between">
              <div>
                <span className="text-[#E5A93C] text-xs uppercase tracking-widest font-semibold block mb-2">SentinelOps Observability Platform</span>
                <h1 className="font-serif text-3xl lg:text-4xl text-white font-normal tracking-tight">Enterprise Access & Governance</h1>
              </div>
              <span className="font-mono text-xs text-[#7A8290]">Edition 2026.4</span>
            </div>

            <div className="grid lg:grid-cols-12 gap-12">
              <div className="lg:col-span-7 space-y-4 text-sm text-[#A5ACB8] leading-relaxed pr-6 border-r border-[#282C33]">
                <p>
                  SentinelOps coordinates enterprise-scale telemetry ingestion, audit trail integrity, and root-cause diagnostic intelligence across distributed workloads.
                </p>
                <p className="text-xs text-[#7A8290]">
                  By signing in, you confirm adherence to organizational multi-tenant authorization policies and confidentiality standards. Session activities are recorded to the immutable audit ledger.
                </p>
                <div className="pt-6">
                  <div className="flex items-center gap-4 text-xs font-mono text-[#9CA3AF]">
                    <span className="flex items-center gap-1.5"><CheckCircle className="h-3.5 w-3.5 text-[#E5A93C]" /> SOC-2 Type II</span>
                    <span className="flex items-center gap-1.5"><CheckCircle className="h-3.5 w-3.5 text-[#E5A93C]" /> ISO 27001 Certified</span>
                    <span className="flex items-center gap-1.5"><CheckCircle className="h-3.5 w-3.5 text-[#E5A93C]" /> Multi-Tenant RBAC</span>
                  </div>
                </div>
              </div>

              <div className="lg:col-span-5 space-y-4">
                <div>
                  <label className="block text-xs uppercase tracking-wider text-[#A5ACB8] font-medium mb-1.5">Work Email Address</label>
                  <input
                    type="email"
                    defaultValue="admin@sentinelops.local"
                    className="w-full bg-[#111315] border border-[#313640] rounded px-3.5 py-2.5 text-sm text-white focus:outline-none focus:border-[#E5A93C] transition"
                  />
                </div>
                <div>
                  <div className="flex justify-between items-center mb-1.5">
                    <label className="text-xs uppercase tracking-wider text-[#A5ACB8] font-medium">Password</label>
                    <a href="#reset" className="text-xs text-[#E5A93C] hover:underline">Forgot?</a>
                  </div>
                  <input
                    type="password"
                    defaultValue="••••••••••••"
                    className="w-full bg-[#111315] border border-[#313640] rounded px-3.5 py-2.5 text-sm text-white focus:outline-none focus:border-[#E5A93C] transition"
                  />
                </div>
                <div className="pt-2">
                  <button
                    type="button"
                    className="w-full bg-[#E5A93C] hover:bg-[#F0B84D] text-[#111315] font-semibold text-sm py-2.5 rounded transition flex items-center justify-center gap-2"
                  >
                    Enter Workspace <ArrowRight className="h-4 w-4" />
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Screen 2: Overview */}
      {screen === "overview" && (
        <div className="max-w-6xl mx-auto px-8 py-10 space-y-8">
          <div className="border-b border-[#2B2F36] pb-6 flex items-baseline justify-between">
            <div>
              <span className="text-[#E5A93C] text-xs uppercase tracking-widest font-medium block mb-1">Infrastructure Health Report</span>
              <h1 className="font-serif text-3xl text-white font-normal tracking-tight">Executive Telemetry Overview</h1>
            </div>
            <div className="text-xs font-mono text-[#8C929D]">
              Reporting window: <strong className="text-white font-sans">Last 24 Hours</strong> (00:00 - 23:59 UTC)
            </div>
          </div>

          {/* Editorial Key Indicators */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-8 py-4 border-b border-[#2B2F36]">
            <div>
              <span className="text-xs text-[#8C929D] uppercase tracking-wider block mb-1">Fleet Availability</span>
              <div className="font-serif text-4xl text-white font-normal mb-1">99.98%</div>
              <span className="text-xs text-emerald-400 font-mono">0.02% error budget used</span>
            </div>
            <div>
              <span className="text-xs text-[#8C929D] uppercase tracking-wider block mb-1">Throughput Rate</span>
              <div className="font-serif text-4xl text-white font-normal mb-1">2,840<span className="text-sm font-sans text-[#8C929D] ml-1">rps</span></div>
              <span className="text-xs text-[#E5A93C] font-mono">+8.4% above median</span>
            </div>
            <div>
              <span className="text-xs text-[#8C929D] uppercase tracking-wider block mb-1">p99 Service Latency</span>
              <div className="font-serif text-4xl text-white font-normal mb-1">48.2<span className="text-sm font-sans text-[#8C929D] ml-1">ms</span></div>
              <span className="text-xs text-emerald-400 font-mono">Well within SLA (&lt;120ms)</span>
            </div>
            <div>
              <span className="text-xs text-[#8C929D] uppercase tracking-wider block mb-1">Active Incidents</span>
              <div className="font-serif text-4xl text-amber-400 font-normal mb-1">01</div>
              <span className="text-xs text-amber-400 font-mono">1 under investigation</span>
            </div>
          </div>

          {/* Editorial Incident Digest */}
          <div>
            <h3 className="text-xs uppercase tracking-widest text-[#8C929D] font-semibold mb-4">Priority Incident Digest</h3>
            <div className="border border-[#2B2F36] bg-[#16181B] rounded p-6">
              <div className="flex items-start justify-between">
                <div>
                  <div className="flex items-center gap-2 mb-1.5">
                    <span className="font-mono text-xs text-[#E5A93C] font-bold">INC-2026-0842</span>
                    <span className="text-xs bg-rose-500/10 text-rose-400 border border-rose-500/20 px-2 py-0.5 rounded">Critical</span>
                    <span className="text-xs text-[#8C929D]">Started 18m ago</span>
                  </div>
                  <h4 className="text-lg text-white font-medium mb-2">Checkout API Latency & PostgreSQL Connection Saturation</h4>
                  <p className="text-sm text-[#A5ACB8] leading-relaxed max-w-3xl mb-4">
                    The payment authorization pipeline experienced connection queue starvation due to an unhandled transaction scope leak during third-party gateway timeouts.
                  </p>
                  <div className="flex items-center gap-6 text-xs text-[#8C929D] font-mono">
                    <span>Assigned: <strong className="text-white">Sarah Chen</strong></span>
                    <span>Service: <strong className="text-white">order-service</strong></span>
                    <span>Environment: <strong className="text-white">Production (eu-west-1)</strong></span>
                  </div>
                </div>
                <Link
                  to="/design-prototypes/concept-b/incident"
                  className="px-4 py-2 border border-[#E5A93C] text-[#E5A93C] hover:bg-[#E5A93C] hover:text-[#111315] text-xs font-semibold rounded transition"
                >
                  Review Case Dossier
                </Link>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Screen 3: Log Explorer */}
      {screen === "logs" && (
        <div className="max-w-6xl mx-auto px-8 py-10 space-y-6">
          <div className="border-b border-[#2B2F36] pb-4 flex items-baseline justify-between">
            <div>
              <span className="text-[#E5A93C] text-xs uppercase tracking-widest font-medium block mb-1">Forensic Ledger</span>
              <h1 className="font-serif text-3xl text-white font-normal tracking-tight">Structured Log Journal</h1>
            </div>
            <span className="text-xs text-[#8C929D] font-mono">142,050 entries recorded</span>
          </div>

          <div className="border border-[#2B2F36] bg-[#16181B] rounded divide-y divide-[#2B2F36]">
            {[
              { time: "15:09:35.786", sev: "ERROR", sColor: "text-rose-400", svc: "payment-gateway", msg: "Downstream payment provider gateway timeout after 5000ms: HTTP 504 Gateway Timeout" },
              { time: "15:08:50.786", sev: "CRITICAL", sColor: "text-rose-500 font-bold", svc: "order-service", msg: "Database connection pool saturated (100/100 active connections). Connection acquisition queue size: 48" },
              { time: "15:07:35.786", sev: "WARNING", sColor: "text-[#E5A93C]", svc: "inventory-cache", msg: "Redis cache memory utilization reached 84.5% (1.69 GB / 2.00 GB). Eviction policy volatile-lru active." },
              { time: "15:06:35.786", sev: "INFO", sColor: "text-[#8C929D]", svc: "identity-api", msg: "Issued OAuth2 bearer JWT token for service account payment-worker-sa [tenant: global-fintech]" }
            ].map((entry, idx) => (
              <div key={idx} className="p-4 hover:bg-[#1A1D22] transition flex items-start gap-4">
                <span className="font-mono text-xs text-[#7A8290] w-28 shrink-0">{entry.time}</span>
                <span className={`font-mono text-xs uppercase w-20 shrink-0 ${entry.sColor}`}>{entry.sev}</span>
                <span className="font-mono text-xs text-[#E5A93C] w-36 shrink-0">{entry.svc}</span>
                <p className="text-sm text-[#D1D5DB] leading-snug">{entry.msg}</p>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Screen 4: Incident Detail */}
      {screen === "incident" && (
        <div className="max-w-5xl mx-auto px-8 py-10 space-y-8">
          <div className="border-b border-[#2B2F36] pb-6 flex items-start justify-between">
            <div>
              <div className="flex items-center gap-2 mb-2 font-mono text-xs text-[#E5A93C]">
                <span>INCIDENT DOSSIER</span>
                <span>/</span>
                <span className="text-white font-bold">INC-2026-0842</span>
              </div>
              <h1 className="font-serif text-3xl text-white font-normal tracking-tight mb-2">
                Checkout API Elevated Latency & PostgreSQL Connection Saturation
              </h1>
              <p className="text-sm text-[#A5ACB8] leading-relaxed max-w-2xl">
                Formal incident postmortem and root-cause analysis ledger for the October 17th checkout degradation.
              </p>
            </div>
            <span className="px-3 py-1 bg-amber-500/10 border border-amber-500/30 text-amber-400 font-mono text-xs uppercase rounded">
              Status: Investigating
            </span>
          </div>

          <div className="grid grid-cols-12 gap-8">
            <div className="col-span-8 space-y-6">
              <div>
                <h3 className="font-serif text-lg text-white mb-2">Root Cause Summary</h3>
                <p className="text-sm text-[#CBD5E1] leading-relaxed bg-[#16181B] border border-[#2B2F36] p-4 rounded">
                  Transaction scopes opened in <code className="text-[#E5A93C] font-mono">OrderRepository.cs</code> failed to abort connections upon receiving downstream HTTP 504 gateway timeouts from the payment vendor, causing connection pool exhaustion within 90 seconds.
                </p>
              </div>

              <div>
                <h3 className="font-serif text-lg text-white mb-3">Chronological Record</h3>
                <div className="space-y-4 border-l-2 border-[#2B2F36] pl-4 ml-1">
                  <div>
                    <span className="font-mono text-xs text-[#7A8290]">15:09:00 UTC — Evaluation Engine</span>
                    <p className="text-sm text-[#E2E8F0] mt-0.5">Automated detection fired rule: p99 latency threshold exceeded 2000ms.</p>
                  </div>
                  <div>
                    <span className="font-mono text-xs text-[#7A8290]">15:12:30 UTC — Sarah Chen</span>
                    <p className="text-sm text-[#E2E8F0] mt-0.5">Assigned incident commander. Hotfix PR #412 prepared with explicit connection disposal timeout handler.</p>
                  </div>
                </div>
              </div>
            </div>

            <div className="col-span-4 border-l border-[#2B2F36] pl-6 space-y-6 text-xs font-mono">
              <div>
                <span className="text-[#7A8290] block uppercase mb-1">Assigned SRE Lead</span>
                <span className="text-white text-sm">Sarah Chen</span>
              </div>
              <div>
                <span className="text-[#7A8290] block uppercase mb-1">Target Application</span>
                <span className="text-white text-sm">Checkout Services</span>
              </div>
              <div>
                <span className="text-[#7A8290] block uppercase mb-1">Calculated Downtime</span>
                <span className="text-rose-400 text-sm font-semibold">18m 42s elapsed</span>
              </div>
              <div className="pt-4 border-t border-[#2B2F36]">
                <button className="w-full bg-[#E5A93C] hover:bg-[#F0B84D] text-[#111315] font-semibold text-xs py-2 rounded transition">
                  Publish Case Postmortem
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
