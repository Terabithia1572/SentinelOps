import { Link } from "react-router-dom";
import {
  AlertOctagon,
  ArrowRight,
  Search
} from "lucide-react";

export function ConceptCPage({ screen }: { screen: "login" | "overview" | "logs" | "incident" }) {

  return (
    <div className="min-h-screen bg-[#07090C] text-[#E2E8F0] font-mono antialiased selection:bg-[#00FF88]/20 selection:text-[#00FF88]">
      {/* Concept Navigation Bar */}
      <div className="border-b border-[#1A232E] bg-[#0C1015] px-6 py-2.5 flex items-center justify-between text-xs sticky top-0 z-50">
        <div className="flex items-center gap-3">
          <span className="text-[#00FF88] font-bold tracking-widest uppercase text-[11px] bg-[#00FF88]/10 px-2 py-0.5 border border-[#00FF88]/30">
            Concept C: Industrial Command Center
          </span>
          <span className="text-[#475569]">|</span>
          <span className="text-[#94A3B8] text-xs">MODULE: <strong className="text-white uppercase">{screen}</strong></span>
        </div>
        <div className="flex items-center gap-2">
          <Link
            to="/design-prototypes/concept-c/login"
            className={`px-2.5 py-1 text-xs border transition ${screen === "login" ? "bg-[#00FF88] text-black border-[#00FF88] font-bold" : "text-[#94A3B8] border-transparent hover:border-[#1A232E]"}`}
          >
            [1] LOGIN
          </Link>
          <Link
            to="/design-prototypes/concept-c/overview"
            className={`px-2.5 py-1 text-xs border transition ${screen === "overview" ? "bg-[#00FF88] text-black border-[#00FF88] font-bold" : "text-[#94A3B8] border-transparent hover:border-[#1A232E]"}`}
          >
            [2] OVERVIEW
          </Link>
          <Link
            to="/design-prototypes/concept-c/logs"
            className={`px-2.5 py-1 text-xs border transition ${screen === "logs" ? "bg-[#00FF88] text-black border-[#00FF88] font-bold" : "text-[#94A3B8] border-transparent hover:border-[#1A232E]"}`}
          >
            [3] LOGS
          </Link>
          <Link
            to="/design-prototypes/concept-c/incident"
            className={`px-2.5 py-1 text-xs border transition ${screen === "incident" ? "bg-[#00FF88] text-black border-[#00FF88] font-bold" : "text-[#94A3B8] border-transparent hover:border-[#1A232E]"}`}
          >
            [4] INCIDENT
          </Link>
        </div>
      </div>

      {/* Screen 1: Login */}
      {screen === "login" && (
        <div className="min-h-[calc(100vh-45px)] flex items-center justify-center p-6 bg-[#07090C] relative">
          <div className="w-full max-w-lg border-2 border-[#1E293B] bg-[#0B0F15] p-8 relative shadow-[0_0_50px_rgba(0,0,0,0.8)]">
            <div className="flex items-center justify-between border-b border-[#1E293B] pb-4 mb-6">
              <div className="flex items-center gap-2">
                <span className="h-2.5 w-2.5 bg-[#00FF88] animate-pulse" />
                <span className="text-white font-bold tracking-widest text-sm uppercase">SENTINEL-OPS // TAC-OPS-01</span>
              </div>
              <span className="text-[10px] text-[#00FF88] bg-[#00FF88]/10 border border-[#00FF88]/20 px-2 py-0.5">
                SEC_LEVEL: 04
              </span>
            </div>

            <div className="mb-6 space-y-1">
              <span className="text-xs text-[#64748B] block uppercase tracking-wider">COMMAND CONSOLE ACCESS</span>
              <p className="text-xs text-[#94A3B8]">Provide authenticated cryptographic credentials to engage telemetry link.</p>
            </div>

            <form className="space-y-4" onSubmit={e => e.preventDefault()}>
              <div>
                <label className="block text-[11px] text-[#64748B] uppercase tracking-wider mb-1">OPERATOR_IDENTIFIER</label>
                <input
                  type="text"
                  defaultValue="admin@sentinelops.local"
                  className="w-full bg-[#07090C] border border-[#1E293B] px-3 py-2 text-xs text-[#00FF88] focus:outline-none focus:border-[#00FF88] transition"
                />
              </div>

              <div>
                <label className="block text-[11px] text-[#64748B] uppercase tracking-wider mb-1">CIPHER_TOKEN_KEY</label>
                <input
                  type="password"
                  defaultValue="••••••••••••"
                  className="w-full bg-[#07090C] border border-[#1E293B] px-3 py-2 text-xs text-[#00FF88] focus:outline-none focus:border-[#00FF88] transition"
                />
              </div>

              <div className="pt-2">
                <button
                  type="button"
                  className="w-full bg-[#00FF88] hover:bg-[#33FF9F] text-black font-bold text-xs uppercase tracking-widest py-3 transition flex items-center justify-center gap-2 shadow-[0_0_20px_rgba(0,255,136,0.2)]"
                >
                  INITIALIZE TELEMETRY SESSION <ArrowRight className="h-4 w-4" />
                </button>
              </div>
            </form>

            <div className="mt-6 pt-4 border-t border-[#1E293B] flex justify-between text-[10px] text-[#475569]">
              <span>CLUSTER: PRD-CLUSTER-ALPHA</span>
              <span>CRYPTO_ENG: AES-256-GCM</span>
            </div>
          </div>
        </div>
      )}

      {/* Screen 2: Overview */}
      {screen === "overview" && (
        <div className="p-6 max-w-7xl mx-auto space-y-5">
          {/* Telemetry Header */}
          <div className="border border-[#1A232E] bg-[#0C1015] p-4 flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div className="flex items-center gap-4">
              <span className="h-3 w-3 bg-[#00FF88] animate-pulse" />
              <div>
                <span className="text-[10px] text-[#00FF88] uppercase tracking-widest block font-bold">RADAR STATUS: ACTIVE SWEEP</span>
                <h1 className="text-lg text-white font-bold tracking-tight">OPERATIONS FLIGHT DECK // PRODUCTION-EU</h1>
              </div>
            </div>
            <div className="flex items-center gap-4 text-xs">
              <div className="border border-[#1A232E] px-3 py-1.5 bg-[#07090C]">
                <span className="text-[#64748B] mr-2">INGESTION:</span>
                <span className="text-[#00FF88] font-bold">2,840.4 EPS</span>
              </div>
              <div className="border border-[#1A232E] px-3 py-1.5 bg-[#07090C]">
                <span className="text-[#64748B] mr-2">CLUSTER HEALTH:</span>
                <span className="text-emerald-400 font-bold">100% NOMINAL</span>
              </div>
            </div>
          </div>

          {/* Subsystem Telemetry Rack */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            {[
              { id: "RACK_01", title: "NODES_ACTIVE", val: "34 / 34", status: "NOMINAL", sColor: "text-[#00FF88]" },
              { id: "RACK_02", title: "THROUGHPUT_RPS", val: "2,842.1", status: "+12.4%", sColor: "text-[#38BDF8]" },
              { id: "RACK_03", title: "LATENCY_P99", val: "48.2 MS", status: "NOMINAL", sColor: "text-[#00FF88]" },
              { id: "RACK_04", title: "ACTIVE_THREATS", val: "01 CRIT", status: "BREACHED", sColor: "text-rose-500" }
            ].map(item => (
              <div key={item.id} className="border border-[#1A232E] bg-[#0B0F15] p-3.5 relative">
                <div className="flex justify-between text-[10px] text-[#64748B] mb-2">
                  <span>{item.id}</span>
                  <span className={item.sColor}>{item.status}</span>
                </div>
                <div className="text-[11px] text-[#94A3B8] mb-1">{item.title}</div>
                <div className={`text-2xl font-bold tracking-tight ${item.sColor}`}>{item.val}</div>
              </div>
            ))}
          </div>

          {/* Threat Warning Feed */}
          <div className="border border-rose-500/30 bg-[#0F0A0E] p-4">
            <div className="flex items-center justify-between mb-3 border-b border-rose-500/20 pb-2">
              <div className="flex items-center gap-2 text-rose-500 text-xs font-bold uppercase">
                <AlertOctagon className="h-4 w-4" />
                <span>ACTIVE THREAT DISPATCH // CRITICAL INCIDENT DETECTED</span>
              </div>
              <span className="text-[10px] text-rose-400">PRIORITY_01</span>
            </div>
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
              <div>
                <div className="text-white text-sm font-bold mb-1">
                  [INC-2026-0842] CHECKOUT API ELEVATED LATENCY & DB CONNECTION LEAK
                </div>
                <p className="text-xs text-[#94A3B8]">
                  order-service in production-eu-west-1 reporting 3.8s p99 latency with connection acquisition starvation.
                </p>
              </div>
              <Link
                to="/design-prototypes/concept-c/incident"
                className="px-4 py-2 bg-rose-500 hover:bg-rose-600 text-white font-bold text-xs uppercase tracking-wider shrink-0"
              >
                ENGAGE INCIDENT &gt;&gt;
              </Link>
            </div>
          </div>
        </div>
      )}

      {/* Screen 3: Log Explorer */}
      {screen === "logs" && (
        <div className="p-6 max-w-7xl mx-auto space-y-4">
          <div className="border border-[#1A232E] bg-[#0C1015] p-3 flex items-center justify-between text-xs">
            <span className="text-[#00FF88] font-bold">RAW TELEMETRY BUS // LOG_BUFFER_TAIL</span>
            <span className="text-[#64748B]">STREAM_RATE: 1,420 MSG/SEC</span>
          </div>

          <div className="border border-[#1A232E] bg-[#07090C] p-2 flex items-center gap-2">
            <Search className="h-3.5 w-3.5 text-[#64748B]" />
            <input
              type="text"
              placeholder="FILTER STRING EXPR: service == 'payment-gateway' && level >= ERROR"
              className="w-full bg-transparent text-xs text-[#00FF88] focus:outline-none placeholder-[#475569]"
            />
          </div>

          <div className="border border-[#1A232E] bg-[#0B0F15] text-xs">
            <div className="border-b border-[#1A232E] bg-[#0E131A] px-4 py-2 text-[#64748B] text-[10px] uppercase grid grid-cols-12">
              <span className="col-span-2">TIMESTAMP_UTC</span>
              <span className="col-span-1">LEVEL</span>
              <span className="col-span-2">SUBSYSTEM</span>
              <span className="col-span-6">PAYLOAD_MESSAGE</span>
              <span className="col-span-1 text-right">CORR_ID</span>
            </div>
            <div className="divide-y divide-[#151B22]">
              {[
                { time: "15:09:35.786", sev: "ERR", sColor: "text-rose-500 font-bold", svc: "payment-gateway", msg: "Downstream payment provider gateway timeout after 5000ms: HTTP 504 Gateway Timeout", corr: "0x4BF9" },
                { time: "15:08:50.786", sev: "CRIT", sColor: "text-rose-500 bg-rose-500/10 font-bold", svc: "order-service", msg: "Database connection pool saturated (100/100 active connections). Connection acquisition queue size: 48", corr: "0x4BF9" },
                { time: "15:07:35.786", sev: "WARN", sColor: "text-amber-400", svc: "inventory-cache", msg: "Redis cache memory utilization reached 84.5% (1.69 GB / 2.00 GB). Eviction policy volatile-lru active.", corr: "0x7CF8" },
                { time: "15:06:35.786", sev: "INFO", sColor: "text-[#38BDF8]", svc: "identity-api", msg: "Issued OAuth2 bearer JWT token for service account payment-worker-sa [tenant: global-fintech]", corr: "0x7CF8" }
              ].map((log, i) => (
                <div key={i} className="px-4 py-2 hover:bg-[#111720] transition grid grid-cols-12 items-center">
                  <span className="col-span-2 text-[#64748B]">{log.time}</span>
                  <span className={`col-span-1 ${log.sColor}`}>[{log.sev}]</span>
                  <span className="col-span-2 text-[#00FF88]">{log.svc}</span>
                  <span className="col-span-6 text-[#CBD5E1] truncate pr-2">{log.msg}</span>
                  <span className="col-span-1 text-right text-[#64748B] hover:text-[#00FF88] cursor-pointer underline">{log.corr}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* Screen 4: Incident Detail */}
      {screen === "incident" && (
        <div className="p-6 max-w-6xl mx-auto space-y-6">
          <div className="border-2 border-rose-500/40 bg-[#0E090D] p-5">
            <div className="flex justify-between items-start mb-3 border-b border-rose-500/30 pb-3">
              <div>
                <span className="text-[10px] text-rose-500 font-bold uppercase tracking-widest block mb-1">
                  SEVERITY_01 // ACTIVE WAR ROOM
                </span>
                <h1 className="text-xl text-white font-bold tracking-tight">
                  INC-2026-0842: CHECKOUT API ELEVATED LATENCY & POSTGRESQL SATURATION
                </h1>
              </div>
              <div className="text-right">
                <span className="text-xs text-rose-400 font-bold block">DURATION: 18M 42S</span>
                <span className="text-[10px] text-[#64748B]">COMMANDER: S. CHEN</span>
              </div>
            </div>

            <div className="grid grid-cols-3 gap-4 text-xs border border-[#2D1B24] p-3 bg-[#070507]">
              <div>
                <span className="text-[#64748B] block text-[10px]">AFFECTED_TARGET</span>
                <span className="text-white font-bold">order-service:5100</span>
              </div>
              <div>
                <span className="text-[#64748B] block text-[10px]">FAILED_TRANSACTIONS</span>
                <span className="text-rose-500 font-bold">142 SAMPLES</span>
              </div>
              <div>
                <span className="text-[#64748B] block text-[10px]">ROOT_CORRELATION</span>
                <span className="text-[#00FF88] font-bold">TRACE_ID: 4bf92f35</span>
              </div>
            </div>
          </div>

          <div className="border border-[#1A232E] bg-[#0B0F15] p-5 space-y-4 text-xs">
            <span className="text-[#00FF88] text-[11px] font-bold tracking-wider uppercase block border-b border-[#1A232E] pb-2">
              SIGNAL TELEMETRY CHRONOLOGY
            </span>
            <div className="space-y-3">
              <div className="flex items-start gap-4">
                <span className="text-[#64748B] w-24 shrink-0">15:09:00 UTC</span>
                <span className="text-rose-500 font-bold">[BREACH]</span>
                <span className="text-white">Rule p99_latency_2000ms threshold violated. Auto-escalated to tier 1 on-call.</span>
              </div>
              <div className="flex items-start gap-4">
                <span className="text-[#64748B] w-24 shrink-0">15:12:30 UTC</span>
                <span className="text-[#00FF88] font-bold">[COMMAND]</span>
                <span className="text-white">Sarah Chen assumed command. Hotfix patch PR #412 compiled for rolling container rollout.</span>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
