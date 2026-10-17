import { useState } from "react";
import { Outlet } from "react-router-dom";
import { Menu, Search, Activity, Radio } from "lucide-react";
import { Sidebar } from "@/components/layout/sidebar";
import { NotificationCenter } from "@/components/layout/notification-center";
import { CommandPalette } from "@/components/layout/command-palette";
import { Breadcrumbs } from "@/components/ui/breadcrumbs";
import { Button } from "@/components/ui/button";
import { ErrorBoundary } from "@/components/ui/error-boundary";
import { useAlertsHub } from "@/hooks/use-alerts-hub";
import { useTenant } from "@/context/tenant-context";

export function MainLayout() {
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [commandPaletteOpen, setCommandPaletteOpen] = useState(false);
  const { isConnected, status } = useAlertsHub();
  const { activeOrg } = useTenant();

  return (
    <div className="flex h-screen overflow-hidden bg-[#08090D] text-[#F0F3F9]">
      {/* Desktop Sidebar */}
      <div className="hidden md:flex md:shrink-0">
        <Sidebar />
      </div>

      {/* Mobile Drawer Backdrop & Sidebar */}
      {mobileMenuOpen && (
        <div
          className="fixed inset-0 z-50 flex md:hidden"
          role="dialog"
          aria-modal="true"
          aria-label="Mobile Navigation"
        >
          {/* Overlay */}
          <div
            className="fixed inset-0 bg-[#08090D]/80 backdrop-blur-xs transition-opacity"
            onClick={() => setMobileMenuOpen(false)}
            aria-hidden="true"
          />

          {/* Drawer container */}
          <div className="relative flex w-64 max-w-[80vw] flex-1 flex-col shadow-2xl">
            <Sidebar
              onItemClick={() => setMobileMenuOpen(false)}
              onClose={() => setMobileMenuOpen(false)}
            />
          </div>
        </div>
      )}

      {/* Main Content Area */}
      <div className="flex flex-1 flex-col overflow-hidden">
        {/* Top Navigation Bar / Operational Telemetry Bar */}
        <header className="flex h-12 shrink-0 items-center justify-between border-b border-[#1E2333] bg-[#0E1017] px-4">
          <div className="flex items-center gap-3">
            {/* Mobile Hamburger Button */}
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setMobileMenuOpen(true)}
              className="h-8 w-8 p-0 text-[#758498] hover:text-[#F0F3F9] md:hidden"
              aria-label="Open navigation menu"
            >
              <Menu className="h-4 w-4" />
            </Button>

            {/* Dynamic Breadcrumbs */}
            <Breadcrumbs />
          </div>

          {/* Center/Right Operational Console Controls */}
          <div className="flex items-center gap-2 sm:gap-3">
            {/* Command Palette Trigger */}
            <button
              onClick={() => setCommandPaletteOpen(true)}
              className="hidden lg:flex items-center gap-2 rounded bg-[#10121A] border border-[#1E2333] hover:border-[#2A324B] px-2.5 py-1 text-xs text-[#758498] hover:text-[#F0F3F9] transition-colors"
              title="Global Search & Navigation (Ctrl+K)"
            >
              <Search className="h-3 w-3 text-[#00E5FF]" />
              <span className="font-mono text-[11px]">Quick search telemetry...</span>
              <kbd className="rounded bg-[#0E1017] border border-[#1E2333] px-1 py-0.2 text-[9px] font-mono text-[#55657E]">
                Ctrl K
              </kbd>
            </button>

            {/* Environment Badge - Dynamic Tenant / Cluster Context */}
            <div className="hidden sm:flex items-center gap-1.5 rounded bg-[#10121A] border border-[#1E2333] px-2 py-0.5 text-[10px] font-mono">
              <span className={`h-1.5 w-1.5 rounded-full ${activeOrg ? "bg-cyan-400" : "bg-slate-500"}`} />
              <span className="text-[#00E5FF] font-semibold tracking-wider uppercase">
                {(activeOrg as any)?.planTier ? `${(activeOrg as any).planTier}` : activeOrg?.slug ? activeOrg.slug.slice(0, 10).toUpperCase() : "CLUSTER"}
              </span>
            </div>

            {/* Dynamic Telemetry Ingestion State */}
            <div className="hidden xl:flex items-center gap-1.5 rounded bg-[#10121A] border border-[#1E2333] px-2 py-0.5 text-[10px] font-mono text-[#758498]">
              <Activity className={`h-3 w-3 ${isConnected ? "text-[#00E5FF]" : "text-[#55657E]"}`} />
              <span>{isConnected ? "TELEMETRY SYNC" : "STANDBY"}</span>
            </div>

            {/* Real-time Push State (SignalR / WebSocket) */}
            <div className={`flex items-center gap-1.5 rounded bg-[#10121A] border border-[#1E2333] px-2 py-0.5 text-[10px] font-mono ${
              isConnected
                ? "text-emerald-400"
                : status === "connecting" || status === "reconnecting"
                ? "text-amber-400"
                : "text-[#758498]"
            }`}>
              <Radio className={`h-3 w-3 ${isConnected ? "text-emerald-400 animate-pulse" : "text-[#55657E]"}`} />
              <span className="hidden sm:inline">
                {isConnected ? "LIVE" : status === "connecting" ? "CONNECTING" : "STANDBY"}
              </span>
            </div>

            {/* Mobile Search Icon */}
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setCommandPaletteOpen(true)}
              className="h-7 w-7 p-0 text-[#758498] hover:text-[#F0F3F9] lg:hidden"
              aria-label="Open command palette"
            >
              <Search className="h-3.5 w-3.5" />
            </Button>

            {/* In-App Notification Center */}
            <NotificationCenter />
          </div>
        </header>

        {/* Scrollable Page Body */}
        <main className="flex-1 overflow-y-auto bg-[#08090D]">
          <div className="container mx-auto max-w-7xl p-3 sm:p-4 md:p-6">
            <ErrorBoundary fallbackTitle="Page View Intercepted">
              <Outlet />
            </ErrorBoundary>
          </div>
        </main>
      </div>

      {/* Global Command Palette Dialog */}
      <CommandPalette
        open={commandPaletteOpen}
        onOpenChange={setCommandPaletteOpen}
      />
    </div>
  );
}
