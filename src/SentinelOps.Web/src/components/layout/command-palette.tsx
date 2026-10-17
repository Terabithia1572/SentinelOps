import { useState, useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import {
  Search,
  LayoutDashboard,
  FileText,
  GitBranch,
  BarChart3,
  AlertTriangle,
  Sparkles,
  Network,
  Bell,
  Settings,
  CreditCard,
  Key,
  ShieldAlert,
  RotateCw,
  Layers,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface CommandItem {
  id: string;
  name: string;
  category: "Navigation" | "Telemetry" | "Incidents" | "Actions";
  icon: React.ComponentType<{ className?: string }>;
  shortcut?: string;
  action: () => void;
}

interface CommandPaletteProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CommandPalette({ open, onOpenChange }: CommandPaletteProps) {
  const [query, setQuery] = useState("");
  const [selectedIndex, setSelectedIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const navigate = useNavigate();

  const commands: CommandItem[] = [
    {
      id: "nav-overview",
      name: "Go to System Overview",
      category: "Navigation",
      icon: LayoutDashboard,
      shortcut: "G O",
      action: () => {
        navigate("/");
        onOpenChange(false);
      },
    },
    {
      id: "nav-logs",
      name: "Explore Live Logs",
      category: "Telemetry",
      icon: FileText,
      shortcut: "G L",
      action: () => {
        navigate("/logs");
        onOpenChange(false);
      },
    },
    {
      id: "nav-traces",
      name: "Inspect Distributed Traces",
      category: "Telemetry",
      icon: GitBranch,
      shortcut: "G T",
      action: () => {
        navigate("/traces");
        onOpenChange(false);
      },
    },
    {
      id: "nav-metrics",
      name: "Analyze Service Metrics",
      category: "Telemetry",
      icon: BarChart3,
      shortcut: "G M",
      action: () => {
        navigate("/metrics");
        onOpenChange(false);
      },
    },
    {
      id: "nav-apm",
      name: "APM & Service Map",
      category: "Telemetry",
      icon: Network,
      action: () => {
        navigate("/apm");
        onOpenChange(false);
      },
    },
    {
      id: "nav-incidents",
      name: "Incident Response Console",
      category: "Incidents",
      icon: AlertTriangle,
      shortcut: "G I",
      action: () => {
        navigate("/incidents");
        onOpenChange(false);
      },
    },
    {
      id: "nav-alerts",
      name: "Active Alerts Queue",
      category: "Incidents",
      icon: Bell,
      action: () => {
        navigate("/alerts");
        onOpenChange(false);
      },
    },
    {
      id: "nav-ai",
      name: "AI Anomaly & Root Cause Analysis",
      category: "Navigation",
      icon: Sparkles,
      action: () => {
        navigate("/ai-observability");
        onOpenChange(false);
      },
    },
    {
      id: "nav-billing",
      name: "Subscription & Usage Billing",
      category: "Navigation",
      icon: CreditCard,
      action: () => {
        navigate("/billing");
        onOpenChange(false);
      },
    },
    {
      id: "nav-api-keys",
      name: "API Keys & Ingestion Tokens",
      category: "Navigation",
      icon: Key,
      action: () => {
        navigate("/api-keys");
        onOpenChange(false);
      },
    },
    {
      id: "nav-settings",
      name: "Platform Settings",
      category: "Navigation",
      icon: Settings,
      action: () => {
        navigate("/settings");
        onOpenChange(false);
      },
    },
    {
      id: "act-refresh",
      name: "Refresh Telemetry Stream",
      category: "Actions",
      icon: RotateCw,
      action: () => {
        window.location.reload();
        onOpenChange(false);
      },
    },
    {
      id: "act-simulate-alert",
      name: "Simulate Test Alert Incident",
      category: "Actions",
      icon: ShieldAlert,
      action: () => {
        navigate("/incidents");
        onOpenChange(false);
      },
    },
    {
      id: "act-switch-env",
      name: "Switch Monitored Environment",
      category: "Actions",
      icon: Layers,
      action: () => {
        navigate("/applications");
        onOpenChange(false);
      },
    },
  ];

  const filteredCommands = commands.filter((cmd) =>
    cmd.name.toLowerCase().includes(query.toLowerCase()) ||
    cmd.category.toLowerCase().includes(query.toLowerCase())
  );

  useEffect(() => {
    setSelectedIndex(0);
  }, [query]);

  useEffect(() => {
    if (open) {
      setTimeout(() => inputRef.current?.focus(), 50);
    } else {
      setQuery("");
    }
  }, [open]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === "k") {
        e.preventDefault();
        onOpenChange(!open);
      }
      if (!open) return;

      if (e.key === "Escape") {
        e.preventDefault();
        onOpenChange(false);
      } else if (e.key === "ArrowDown") {
        e.preventDefault();
        setSelectedIndex((prev) =>
          prev < filteredCommands.length - 1 ? prev + 1 : 0
        );
      } else if (e.key === "ArrowUp") {
        e.preventDefault();
        setSelectedIndex((prev) =>
          prev > 0 ? prev - 1 : filteredCommands.length - 1
        );
      } else if (e.key === "Enter") {
        e.preventDefault();
        if (filteredCommands[selectedIndex]) {
          filteredCommands[selectedIndex].action();
        }
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [open, onOpenChange, filteredCommands, selectedIndex]);

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center pt-20 p-4"
      role="dialog"
      aria-modal="true"
      aria-label="Command Palette"
    >
      <div
        className="fixed inset-0 bg-[#08090D]/85 backdrop-blur-xs transition-opacity"
        onClick={() => onOpenChange(false)}
        aria-hidden="true"
      />

      <div className="relative z-50 w-full max-w-xl rounded border border-[#1E2333] bg-[#10121A] shadow-2xl overflow-hidden animate-in fade-in zoom-in-95 duration-100">
        {/* Search Input Bar */}
        <div className="flex items-center px-4 py-3 border-b border-[#1E2333] bg-[#0E1017]">
          <Search className="h-4 w-4 text-[#00E5FF] shrink-0 mr-3" />
          <input
            ref={inputRef}
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search telemetry, pages, or run operational commands... (e.g. 'logs')"
            className="w-full bg-transparent text-xs text-[#F0F3F9] placeholder:text-[#55657E] focus:outline-none font-mono"
            role="combobox"
            aria-expanded="true"
          />
          <kbd className="hidden sm:inline-flex items-center gap-1 rounded bg-[#141722] border border-[#1E2333] px-1.5 py-0.5 text-[10px] font-mono text-[#758498]">
            ESC
          </kbd>
        </div>

        {/* Results List */}
        <div className="max-h-80 overflow-y-auto p-2 divide-y divide-[#1E2333]/30">
          {filteredCommands.length === 0 ? (
            <div className="py-8 text-center text-xs text-[#758498] font-mono">
              No matching commands or routes found for &quot;{query}&quot;
            </div>
          ) : (
            <div className="space-y-0.5">
              {filteredCommands.map((cmd, index) => {
                const isSelected = index === selectedIndex;
                const Icon = cmd.icon;
                return (
                  <button
                    key={cmd.id}
                    onClick={() => cmd.action()}
                    onMouseEnter={() => setSelectedIndex(index)}
                    className={cn(
                      "flex w-full items-center justify-between px-3 py-2 text-xs rounded transition-colors text-left",
                      isSelected
                        ? "bg-[#141722] text-[#F0F3F9] border-l-2 border-[#00E5FF]"
                        : "text-[#A6B4C9] hover:bg-[#141722]/50 hover:text-[#F0F3F9] border-l-2 border-transparent"
                    )}
                  >
                    <div className="flex items-center gap-2.5">
                      <Icon
                        className={cn(
                          "h-3.5 w-3.5 shrink-0",
                          isSelected ? "text-[#00E5FF]" : "text-[#758498]"
                        )}
                      />
                      <span className="font-medium">{cmd.name}</span>
                      <span className="text-[10px] font-mono uppercase tracking-wider text-[#55657E] bg-[#0E1017] px-1.5 py-0.2 rounded border border-[#1E2333]">
                        {cmd.category}
                      </span>
                    </div>

                    {cmd.shortcut && (
                      <kbd className="text-[10px] font-mono text-[#55657E] bg-[#0E1017] px-1.5 py-0.5 rounded border border-[#1E2333]">
                        {cmd.shortcut}
                      </kbd>
                    )}
                  </button>
                );
              })}
            </div>
          )}
        </div>

        {/* Footer Hints */}
        <div className="flex items-center justify-between px-3 py-2 border-t border-[#1E2333] bg-[#0E1017] text-[10px] font-mono text-[#55657E]">
          <div className="flex items-center gap-2">
            <span>↑↓ Navigate</span>
            <span>↵ Select</span>
            <span>Esc Close</span>
          </div>
          <span className="text-[#00E5FF]">SentinelOps Command Line</span>
        </div>
      </div>
    </div>
  );
}
