import { useParams, Link } from "react-router-dom";
import { ConceptAPage } from "./concept-a";
import { ConceptBPage } from "./concept-b";
import { ConceptCPage } from "./concept-c";

export function DesignPrototypesPage() {
  const { concept, screen } = useParams<{ concept?: string; screen?: string }>();

  const validScreen = (screen as "login" | "overview" | "logs" | "incident") || "overview";

  if (concept === "concept-a") {
    return <ConceptAPage screen={validScreen} />;
  }

  if (concept === "concept-b") {
    return <ConceptBPage screen={validScreen} />;
  }

  if (concept === "concept-c") {
    return <ConceptCPage screen={validScreen} />;
  }

  return (
    <div className="min-h-screen bg-[#08090D] text-white p-8 max-w-6xl mx-auto space-y-10 font-sans">
      <div className="border-b border-[#1E2333] pb-6">
        <span className="text-xs font-mono uppercase tracking-widest text-[#00E5FF] block mb-2">Phase 18.1 Design Explorations</span>
        <h1 className="text-3xl font-bold tracking-tight mb-2">SentinelOps Visual Design Concepts</h1>
        <p className="text-sm text-[#94A3B8] max-w-2xl leading-relaxed">
          Three distinctive, human-crafted design directions developed in accordance with the official Anthropic Frontend Design skill to replace generic SaaS templates.
        </p>
      </div>

      <div className="grid md:grid-cols-3 gap-6">
        {/* Concept A Card */}
        <div className="border border-[#1E2333] bg-[#0E1017] rounded-lg p-6 flex flex-col justify-between hover:border-[#00E5FF]/40 transition group">
          <div>
            <div className="flex items-center justify-between mb-4">
              <span className="h-2 w-2 rounded-full bg-[#00E5FF] shadow-[0_0_8px_#00E5FF]" />
              <span className="text-[10px] font-mono text-[#00E5FF] bg-[#00E5FF]/10 px-2 py-0.5 rounded border border-[#00E5FF]/20">
                Direction A
              </span>
            </div>
            <h2 className="text-xl font-bold text-white mb-2 group-hover:text-[#00E5FF] transition">Precision Dark</h2>
            <p className="text-xs text-[#94A3B8] leading-relaxed mb-6">
              Obsidian black foundation, surgical 1px borders, cold electric-cyan accents, and compact technical typography. Engineered for high-speed developer and SRE workflows.
            </p>
            <div className="space-y-1.5 font-mono text-xs border-t border-[#1E2333] pt-4 text-[#CBD5E1]">
              <div className="text-[10px] uppercase text-[#64748B] mb-1 font-sans">Screens:</div>
              <Link to="/design-prototypes/concept-a/login" className="block hover:text-[#00E5FF]">→ 1. Login Page</Link>
              <Link to="/design-prototypes/concept-a/overview" className="block hover:text-[#00E5FF]">→ 2. Main Dashboard</Link>
              <Link to="/design-prototypes/concept-a/logs" className="block hover:text-[#00E5FF]">→ 3. Log Explorer</Link>
              <Link to="/design-prototypes/concept-a/incident" className="block hover:text-[#00E5FF]">→ 4. Incident Detail</Link>
            </div>
          </div>
          <Link
            to="/design-prototypes/concept-a/overview"
            className="mt-6 w-full py-2 bg-[#00E5FF] hover:bg-[#33EBFF] text-black font-semibold text-xs tracking-wider uppercase text-center rounded transition"
          >
            Launch Concept A
          </Link>
        </div>

        {/* Concept B Card */}
        <div className="border border-[#282C33] bg-[#16181B] rounded-lg p-6 flex flex-col justify-between hover:border-[#E5A93C]/40 transition group">
          <div>
            <div className="flex items-center justify-between mb-4">
              <span className="h-2 w-2 rounded-full bg-[#E5A93C] shadow-[0_0_8px_#E5A93C]" />
              <span className="text-[10px] font-mono text-[#E5A93C] bg-[#E5A93C]/10 px-2 py-0.5 rounded border border-[#E5A93C]/20">
                Direction B
              </span>
            </div>
            <h2 className="text-xl font-serif text-white mb-2 group-hover:text-[#E5A93C] transition">Editorial Enterprise</h2>
            <p className="text-xs text-[#9CA3AF] leading-relaxed mb-6">
              Warm dark graphite foundation, refined warm amber accents, authoritative typography, generous whitespace, and continuous hairline rules. Designed for executive trust.
            </p>
            <div className="space-y-1.5 font-mono text-xs border-t border-[#282C33] pt-4 text-[#D1D5DB]">
              <div className="text-[10px] uppercase text-[#7A8290] mb-1 font-sans">Screens:</div>
              <Link to="/design-prototypes/concept-b/login" className="block hover:text-[#E5A93C]">→ 1. Login Page</Link>
              <Link to="/design-prototypes/concept-b/overview" className="block hover:text-[#E5A93C]">→ 2. Main Dashboard</Link>
              <Link to="/design-prototypes/concept-b/logs" className="block hover:text-[#E5A93C]">→ 3. Log Explorer</Link>
              <Link to="/design-prototypes/concept-b/incident" className="block hover:text-[#E5A93C]">→ 4. Incident Detail</Link>
            </div>
          </div>
          <Link
            to="/design-prototypes/concept-b/overview"
            className="mt-6 w-full py-2 bg-[#E5A93C] hover:bg-[#F0B84D] text-[#111315] font-semibold text-xs tracking-wider uppercase text-center rounded transition"
          >
            Launch Concept B
          </Link>
        </div>

        {/* Concept C Card */}
        <div className="border border-[#1A232E] bg-[#0C1015] rounded-lg p-6 flex flex-col justify-between hover:border-[#00FF88]/40 transition group">
          <div>
            <div className="flex items-center justify-between mb-4">
              <span className="h-2 w-2 rounded-full bg-[#00FF88] shadow-[0_0_8px_#00FF88]" />
              <span className="text-[10px] font-mono text-[#00FF88] bg-[#00FF88]/10 px-2 py-0.5 rounded border border-[#00FF88]/20">
                Direction C
              </span>
            </div>
            <h2 className="text-xl font-mono text-white mb-2 group-hover:text-[#00FF88] transition">Industrial Command</h2>
            <p className="text-xs text-[#8FA3B8] leading-relaxed mb-6">
              Matte carbon black foundation, phosphor emerald and radar cyan signals, zero-radius chamfers, rack-mounted modules, and dense flight-recorder telemetry streams.
            </p>
            <div className="space-y-1.5 font-mono text-xs border-t border-[#1A232E] pt-4 text-[#CBD5E1]">
              <div className="text-[10px] uppercase text-[#64748B] mb-1 font-sans">Screens:</div>
              <Link to="/design-prototypes/concept-c/login" className="block hover:text-[#00FF88]">→ 1. Login Page</Link>
              <Link to="/design-prototypes/concept-c/overview" className="block hover:text-[#00FF88]">→ 2. Main Dashboard</Link>
              <Link to="/design-prototypes/concept-c/logs" className="block hover:text-[#00FF88]">→ 3. Log Explorer</Link>
              <Link to="/design-prototypes/concept-c/incident" className="block hover:text-[#00FF88]">→ 4. Incident Detail</Link>
            </div>
          </div>
          <Link
            to="/design-prototypes/concept-c/overview"
            className="mt-6 w-full py-2 bg-[#00FF88] hover:bg-[#33FF9F] text-black font-semibold text-xs tracking-wider uppercase text-center rounded transition"
          >
            Launch Concept C
          </Link>
        </div>
      </div>
    </div>
  );
}
