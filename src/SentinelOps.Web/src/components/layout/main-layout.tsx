import { Outlet } from "react-router-dom";
import { Sidebar } from "@/components/layout/sidebar";
import { NotificationCenter } from "@/components/layout/notification-center";

export function MainLayout() {
  return (
    <div className="flex h-screen overflow-hidden">
      <Sidebar />
      <div className="flex flex-1 flex-col overflow-hidden">
        <header className="flex h-16 shrink-0 items-center justify-between border-b border-[hsl(var(--border))] bg-[hsl(var(--card))] px-6">
          <div className="flex items-center gap-2">
            <span className="text-xs font-semibold text-[hsl(var(--muted-foreground))] uppercase tracking-wider">
              Enterprise Observability & Incident Platform
            </span>
          </div>
          <div className="flex items-center gap-3">
            <NotificationCenter />
          </div>
        </header>
        <main className="flex-1 overflow-y-auto bg-[hsl(var(--background))]">
          <div className="container mx-auto max-w-7xl p-6">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
