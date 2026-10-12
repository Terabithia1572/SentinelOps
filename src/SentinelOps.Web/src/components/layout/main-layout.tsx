import { Outlet } from "react-router-dom";
import { Sidebar } from "@/components/layout/sidebar";

export function MainLayout() {
  return (
    <div className="flex h-screen overflow-hidden">
      <Sidebar />
      <main className="flex-1 overflow-y-auto bg-[hsl(var(--background))]">
        <div className="container mx-auto max-w-7xl p-6">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
