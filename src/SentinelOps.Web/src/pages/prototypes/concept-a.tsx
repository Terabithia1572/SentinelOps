import { useState } from "react";
import { Link } from "react-router-dom";
import {
  Shield,
  AlertCircle,
  Search,
  ChevronRight,
  ArrowRight
} from "lucide-react";

export function ConceptAPage({ screen }: { screen: "login" | "overview" | "logs" | "incident" }) {
  const [logFilter, setLogFilter] = useState("all");

  return (
    <div className="min-h-screen bg-[#08090D] text-[#E2E8F0] font-sans antialiased selection:bg-[#00E5FF]/20 selection:text-[#00E5FF]">
      {/* Concept Navigation Bar */}
      <div className="border-b border-[#1E2333] bg-[#0E1017]/80 backdrop-blur px-6 py-2.5 flex items-center justify-between text-xs sticky top-0 z-50">
        <div className="flex items-center gap-3">
          <span className="font-mono text-[#00E5FF] font-semibold tracking-wider uppercase text-[11px] bg-[#00E5FF]/10 px-2 py-0.5 rounded border border-[#00E5FF]/20">
            Concept A: Precision Dark
          </span>
          <span className="text-[#64748B]">|</span>
          <span className="text-[#94A3B8]">Screen: <strong className="text-white capitalize">{screen}</strong></span>
        </div>
        <div className="flex items-center gap-2">
          <Link
            to="/design-prototypes/concept-a/login"
            className={`px-2.5 py-1 rounded transition text-xs ${screen === "login" ? "bg-[#00E5FF] text-black font-semibold" : "text-[#94A3B8] hover:text-white"}`}
          >
            1. Login
          </Link>
          <Link
            to="/design-prototypes/concept-a/overview"
            className={`px-2.5 py-1 rounded transition text-xs ${screen === "overview" ? "bg-[#00E5FF] text-black font-semibold" : "text-[#94A3B8] hover:text-white"}`}
          >
            2. Overview
          </Link>
          <Link
            to="/design-prototypes/concept-a/logs"
            className={`px-2.5 py-1 rounded transition text-xs ${screen === "logs" ? "bg-[#00E5FF] text-black font-semibold" : "text-[#94A3B8] hover:text-white"}`}
          >
            3. Log Explorer
          </Link>
          <Link
            to="/design-prototypes/concept-a/incident"
            className={`px-2.5 py-1 rounded transition text-xs ${screen === "incident" ? "bg-[#00E5FF] text-black font-semibold" : "text-[#94A3B8] hover:text-white"}`}
          >
            4. Incident Detail
          </Link>
        </div>
      </div>

      {/* Screen 1: Login */}
      {screen === "login" && (
        <div className="min-h-[calc(100vh-45px)] grid lg:grid-cols-12 relative overflow-hidden">
          {/* Left Brand Architectural Column */}
          <div className="lg:col-span-7 bg-[#0B0D13] border-r border-[#1B202E] p-12 lg:p-16 flex flex-col justify-between relative">
            <div className="absolute inset-0 bg-[radial-gradient(circle_at_30%_30%,rgba(0,229,255,0.04),transparent_70%)] pointer-events-none" />
            <div>
              <div className="flex items-center gap-2.5 mb-16">
                <div className="h-8 w-8 rounded bg-[#00E5FF]/10 border border-[#00E5FF]/40 flex items-center justify-center">
                  <Shield className="h-4 w-4 text-[#00E5FF]" />
                </div>
                <span className="font-mono text-sm tracking-widest font-bold uppercase text-white">SentinelOps</span>
                <span className="text-[10px] font-mono text-[#00E5FF] bg-[#00E5FF]/10 px-1.5 py-0.5 rounded">v2.4.0</span>
              </div>

              <div className="max-w-lg">
                <h1 className="text-3xl lg:text-4xl font-semibold tracking-tight text-white mb-4 leading-tight">
                  High-throughput telemetry with surgical precision.
                </h1>
                <p className="text-[#94A3B8] text-sm leading-relaxed mb-8">
                  Centralized OTLP ingestion, distributed trace waterfall analysis, and deterministic root-cause intelligence engineered for mission-critical infrastructure.
                </p>

                <div className="grid grid-cols-2 gap-4 border-t border-[#1B202E] pt-6 font-mono text-xs">
                  <div>
                    <span className="text-[#64748B] block text-[10px] uppercase tracking-wider mb-1">Global Pipeline</span>
                    <span className="text-[#00E5FF] font-semibold flex items-center gap-1.5">
                      <span className="h-1.5 w-1.5 rounded-full bg-[#00E5FF] animate-pulse" />
                      1.84M EPS Ingested
                    </span>
                  </div>
                  <div>
                    <span className="text-[#64748B] block text-[10px] uppercase tracking-wider mb-1">Core Cluster SLA</span>
                    <span className="text-emerald-400 font-semibold">99.995% Availability</span>
                  </div>
                </div>
              </div>
            </div>

            <div className="flex items-center justify-between text-xs text-[#64748B] font-mono border-t border-[#1B202E] pt-6">
              <span>Tenant Isolation: Enforced</span>
              <span>Hardware-backed HSM: Ready</span>
            </div>
          </div>

          {/* Right Form Column */}
          <div className="lg:col-span-5 flex items-center justify-center p-8 lg:p-12 bg-[#08090D]">
            <div className="w-full max-w-sm">
              <div className="mb-8">
                <h2 className="text-xl font-semibold text-white tracking-tight">Access Control</h2>
                <p className="text-xs text-[#94A3B8] mt-1">Authenticate with authorized organization credentials.</p>
              </div>

              <form className="space-y-4" onSubmit={e => e.preventDefault()}>
                <div>
                  <label className="block text-xs font-mono uppercase tracking-wider text-[#94A3B8] mb-1.5">
                    Operator Identity
                  </label>
                  <input
                    type="text"
                    defaultValue="admin@sentinelops.local"
                    className="w-full bg-[#10121A] border border-[#222738] rounded px-3 py-2 text-sm text-white focus:outline-none focus:border-[#00E5FF] font-mono text-xs placeholder-[#475569] transition"
                  />
                </div>

                <div>
                  <div className="flex items-center justify-between mb-1.5">
                    <label className="text-xs font-mono uppercase tracking-wider text-[#94A3B8]">Secret Key / Password</label>
                    <a href="#forgot" className="text-[11px] text-[#00E5FF] hover:underline">Reset</a>
                  </div>
                  <input
                    type="password"
                    defaultValue="••••••••••••"
                    className="w-full bg-[#10121A] border border-[#222738] rounded px-3 py-2 text-sm text-white focus:outline-none focus:border-[#00E5FF] font-mono text-xs transition"
                  />
                </div>

                <div className="pt-2">
                  <button
                    type="button"
                    className="w-full bg-[#00E5FF] hover:bg-[#33EBFF] text-black font-semibold text-xs tracking-wider uppercase py-2.5 rounded transition shadow-[0_0_20px_rgba(0,229,255,0.25)] flex items-center justify-center gap-2"
                  >
                    Authenticate Operator <ArrowRight className="h-3.5 w-3.5" />
                  </button>
                </div>
              </form>

              <div className="mt-8 pt-6 border-t border-[#1B202E] text-center text-xs text-[#64748B]">
                Need corporate SSO provisioning? <a href="#sso" className="text-[#00E5FF] hover:underline">SAML 2.0 / OIDC</a>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Screen 2: Overview */}
      {screen === "overview" && (
        <div className="p-8 max-w-7xl mx-auto space-y-6">
          {/* Precision Top Bar */}
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-[#1E2333] pb-6">
            <div>
              <div className="flex items-center gap-2 text-xs font-mono text-[#64748B] mb-1">
                <span>PRODUCTION-CLUSTER-01</span>
                <span>/</span>
                <span className="text-[#00E5FF]">EU-WEST-1</span>
              </div>
              <h1 className="text-2xl font-semibold text-white tracking-tight">System Posture & Telemetry</h1>
            </div>
            <div className="flex items-center gap-3">
              <div className="bg-[#12151F] border border-[#222738] rounded px-3 py-1.5 flex items-center gap-2 font-mono text-xs">
                <span className="h-2 w-2 rounded-full bg-emerald-400 animate-pulse" />
                <span className="text-[#94A3B8]">Sync:</span>
                <span className="text-white">Live Stream</span>
              </div>
              <div className="bg-[#12151F] border border-[#222738] rounded flex p-0.5 text-xs font-mono">
                {["15m", "1h", "24h", "7d"].map(t => (
                  <button
                    key={t}
                    className={`px-2.5 py-1 rounded transition ${t === "24h" ? "bg-[#00E5FF] text-black font-semibold" : "text-[#94A3B8] hover:text-white"}`}
                  >
                    {t}
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* Surgical KPI Ribbon */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            {[
              { label: "Active Nodes", val: "34 / 34", trend: "100% Normal", status: "ok" },
              { label: "Throughput (RPS)", val: "2,842", trend: "+12.4% vs 24h", status: "ok" },
              { label: "p99 Latency", val: "48.2 ms", trend: "SLA target <120ms", status: "ok" },
              { label: "Fired Alerts", val: "3 Active", trend: "1 Critical, 2 Warn", status: "alert" }
            ].map((k, i) => (
              <div key={i} className="bg-[#10121A] border border-[#1E2333] hover:border-[#2E364F] rounded p-4 transition relative overflow-hidden group">
                <div className={`absolute top-0 left-0 right-0 h-0.5 ${k.status === "alert" ? "bg-amber-400" : "bg-[#00E5FF]/40"}`} />
                <span className="text-[11px] font-mono text-[#64748B] uppercase tracking-wider block mb-1">{k.label}</span>
                <div className="text-2xl font-bold font-mono tracking-tight text-white mb-1.5">{k.val}</div>
                <span className={`text-[11px] font-mono ${k.status === "alert" ? "text-amber-400" : "text-[#00E5FF]"}`}>{k.trend}</span>
              </div>
            ))}
          </div>

          {/* Active Incident Matrix */}
          <div className="bg-[#10121A] border border-[#1E2333] rounded overflow-hidden">
            <div className="px-5 py-3 border-b border-[#1E2333] flex items-center justify-between bg-[#12151F]">
              <div className="flex items-center gap-2">
                <AlertCircle className="h-4 w-4 text-rose-400" />
                <h3 className="text-sm font-semibold text-white tracking-wide">Active Incident Queue</h3>
                <span className="bg-rose-500/10 text-rose-400 border border-rose-500/20 text-[10px] font-mono px-2 py-0.5 rounded">
                  1 Critical
                </span>
              </div>
              <span className="text-xs font-mono text-[#64748B]">Auto-triage: Enabled</span>
            </div>
            <div className="divide-y divide-[#1A1F2C]">
              <div className="p-4 hover:bg-[#141722] transition flex items-center justify-between">
                <div className="flex items-start gap-3">
                  <div className="h-2 w-2 rounded-full bg-rose-500 mt-1.5 shadow-[0_0_8px_rgba(244,63,94,0.6)]" />
                  <div>
                    <div className="flex items-center gap-2 mb-1">
                      <span className="font-mono text-xs text-[#00E5FF] font-semibold">INC-2026-0842</span>
                      <span className="text-white text-sm font-medium">Checkout API Elevated Latency & PostgreSQL Connection Saturation</span>
                    </div>
                    <div className="flex items-center gap-4 text-xs text-[#64748B] font-mono">
                      <span>Service: <strong className="text-[#94A3B8]">order-service</strong></span>
                      <span>Env: <strong className="text-[#94A3B8]">production</strong></span>
                      <span>Assignee: <strong className="text-white">Sarah Chen (SRE)</strong></span>
                      <span>Duration: <strong className="text-rose-400">18m 42s</strong></span>
                    </div>
                  </div>
                </div>
                <Link
                  to="/design-prototypes/concept-a/incident"
                  className="px-3 py-1.5 bg-[#1B202E] hover:bg-[#00E5FF] hover:text-black text-white text-xs font-mono rounded transition flex items-center gap-1.5"
                >
                  Inspect Incident <ChevronRight className="h-3 w-3" />
                </Link>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Screen 3: Log Explorer */}
      {screen === "logs" && (
        <div className="p-8 max-w-7xl mx-auto space-y-4">
          <div className="flex items-center justify-between border-b border-[#1E2333] pb-4">
            <div>
              <h1 className="text-xl font-semibold text-white tracking-tight">Structured Log Stream</h1>
              <p className="text-xs text-[#64748B] font-mono mt-0.5">Filter by severity, trace context, and JSON payload attributes</p>
            </div>
            <div className="flex items-center gap-2">
              <span className="text-xs font-mono text-emerald-400 bg-emerald-500/10 border border-emerald-500/20 px-2.5 py-1 rounded flex items-center gap-1.5">
                <span className="h-1.5 w-1.5 rounded-full bg-emerald-400 animate-pulse" />
                Live Ingestion (142,050 entries)
              </span>
            </div>
          </div>

          {/* Search & Filter Bar */}
          <div className="grid grid-cols-12 gap-3 bg-[#10121A] border border-[#1E2333] rounded p-3">
            <div className="col-span-8 relative">
              <Search className="h-3.5 w-3.5 text-[#64748B] absolute left-3 top-3" />
              <input
                type="text"
                placeholder="search service:payment* status:error timeout > 1000..."
                className="w-full bg-[#08090D] border border-[#222738] rounded pl-8 pr-3 py-1.5 text-xs font-mono text-white placeholder-[#475569] focus:outline-none focus:border-[#00E5FF]"
              />
            </div>
            <div className="col-span-4 flex items-center gap-1.5 justify-end">
              {["all", "error", "warn", "info"].map(sev => (
                <button
                  key={sev}
                  onClick={() => setLogFilter(sev)}
                  className={`px-2.5 py-1 rounded text-xs font-mono uppercase tracking-wider transition ${
                    logFilter === sev
                      ? "bg-[#00E5FF] text-black font-semibold"
                      : "bg-[#141722] text-[#94A3B8] hover:text-white border border-[#222738]"
                  }`}
                >
                  {sev}
                </button>
              ))}
            </div>
          </div>

          {/* Log Stream Matrix */}
          <div className="bg-[#10121A] border border-[#1E2333] rounded overflow-hidden font-mono text-xs">
            <div className="bg-[#12151F] px-4 py-2 text-[#64748B] uppercase tracking-wider text-[10px] grid grid-cols-12 border-b border-[#1E2333]">
              <span className="col-span-2">Timestamp</span>
              <span className="col-span-1">Level</span>
              <span className="col-span-2">Service</span>
              <span className="col-span-6">Structured Message</span>
              <span className="col-span-1 text-right">Trace</span>
            </div>

            <div className="divide-y divide-[#181C28]">
              {[
                { time: "15:09:35.786", sev: "ERROR", sColor: "text-rose-400 bg-rose-500/10 border-rose-500/20", svc: "payment-gateway", msg: "Downstream payment provider gateway timeout after 5000ms: HTTP 504 Gateway Timeout", trace: "4bf92f35" },
                { time: "15:08:50.786", sev: "CRITICAL", sColor: "text-rose-500 bg-rose-500/20 border-rose-500/40 font-bold", svc: "order-service", msg: "Database connection pool saturated (100/100 active connections). Connection acquisition queue size: 48", trace: "4bf92f35" },
                { time: "15:07:35.786", sev: "WARN", sColor: "text-amber-400 bg-amber-500/10 border-amber-500/20", svc: "inventory-cache", msg: "Redis cache memory utilization reached 84.5% (1.69 GB / 2.00 GB). Eviction policy volatile-lru active.", trace: "7cf81e24" },
                { time: "15:06:35.786", sev: "INFO", sColor: "text-sky-400 bg-sky-500/10 border-sky-500/20", svc: "identity-api", msg: "Issued OAuth2 bearer JWT token for service account payment-worker-sa [tenant: global-fintech]", trace: "7cf81e24" }
              ].map((row, idx) => (
                <div key={idx} className="px-4 py-2.5 hover:bg-[#141824] transition grid grid-cols-12 items-center">
                  <span className="col-span-2 text-[#64748B]">{row.time}</span>
                  <span className="col-span-1">
                    <span className={`px-1.5 py-0.5 rounded text-[10px] border ${row.sColor}`}>{row.sev}</span>
                  </span>
                  <span className="col-span-2 text-[#00E5FF]">{row.svc}</span>
                  <span className="col-span-6 text-[#CBD5E1] truncate pr-2">{row.msg}</span>
                  <span className="col-span-1 text-right">
                    <span className="text-[#64748B] hover:text-[#00E5FF] cursor-pointer underline text-[11px]">{row.trace}</span>
                  </span>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* Screen 4: Incident Detail */}
      {screen === "incident" && (
        <div className="p-8 max-w-7xl mx-auto space-y-6">
          {/* Incident Banner */}
          <div className="bg-[#10121A] border border-[#1E2333] rounded p-6">
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-[#1E2333] pb-4 mb-4">
              <div className="flex items-center gap-3">
                <span className="bg-rose-500/10 border border-rose-500/30 text-rose-400 text-xs font-mono uppercase tracking-wider px-2.5 py-1 rounded font-semibold">
                  Critical Incident
                </span>
                <span className="font-mono text-[#00E5FF] font-semibold text-sm">INC-2026-0842</span>
                <span className="text-[#64748B]">•</span>
                <span className="text-xs font-mono text-[#94A3B8]">Status: <strong className="text-amber-400 uppercase">Investigating</strong></span>
              </div>
              <div className="flex items-center gap-2">
                <button className="px-3 py-1.5 bg-[#1B202E] hover:bg-[#252C3D] text-white text-xs font-mono rounded transition">
                  Reassign
                </button>
                <button className="px-3 py-1.5 bg-emerald-500 hover:bg-emerald-400 text-black font-semibold text-xs font-mono rounded transition">
                  Resolve Incident
                </button>
              </div>
            </div>

            <h1 className="text-xl font-semibold text-white tracking-tight mb-2">
              Checkout API Elevated Latency & PostgreSQL Connection Saturation
            </h1>
            <p className="text-xs text-[#94A3B8] leading-relaxed max-w-3xl">
              p99 latency spiked to 3.8s in checkout-service with 48 queued DB connection requests. Downstream payment provider timeouts reported on card tokenization endpoints.
            </p>

            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-6 pt-4 border-t border-[#1E2333] font-mono text-xs">
              <div>
                <span className="text-[#64748B] block text-[10px] uppercase">Service Impacted</span>
                <span className="text-white font-medium">order-service (prod)</span>
              </div>
              <div>
                <span className="text-[#64748B] block text-[10px] uppercase">Commander</span>
                <span className="text-white font-medium">Sarah Chen (SRE)</span>
              </div>
              <div>
                <span className="text-[#64748B] block text-[10px] uppercase">Telemetry Link</span>
                <span className="text-[#00E5FF] underline cursor-pointer">Trace: 4bf92f35</span>
              </div>
              <div>
                <span className="text-[#64748B] block text-[10px] uppercase">Duration</span>
                <span className="text-rose-400 font-semibold">22m 14s elapsed</span>
              </div>
            </div>
          </div>

          {/* Timeline & Analysis Tabs */}
          <div className="bg-[#10121A] border border-[#1E2333] rounded p-6">
            <h3 className="text-sm font-semibold text-white tracking-wide mb-4">Investigative Audit Timeline</h3>
            <div className="space-y-4 pl-4 border-l border-[#222738] ml-2 font-mono text-xs">
              <div className="relative">
                <div className="h-2 w-2 rounded-full bg-[#00E5FF] absolute -left-[21px] top-1" />
                <span className="text-[#64748B] text-[11px]">15:09:00 UTC — Automated Alert</span>
                <p className="text-white mt-0.5">Threshold breached: p99 latency &gt; 2000ms (measured 3842ms). Incident declared automatically.</p>
              </div>
              <div className="relative">
                <div className="h-2 w-2 rounded-full bg-amber-400 absolute -left-[21px] top-1" />
                <span className="text-[#64748B] text-[11px]">15:12:30 UTC — Sarah Chen</span>
                <p className="text-white mt-0.5">Acknowledged. Connection pool leak identified in OrderRepository.cs. Preparing hotfix.</p>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
