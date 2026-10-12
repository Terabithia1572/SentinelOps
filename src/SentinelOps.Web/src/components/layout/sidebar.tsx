import { NavLink } from "react-router-dom";
import {
  LayoutDashboard,
  AppWindow,
  Server,
  FileText,
  GitBranch,
  BarChart3,
  Bug,
  Bell,
  AlertTriangle,
  HeartPulse,
  TrendingUp,
  Key,
  Users,
  ScrollText,
  Settings,
  Shield,
  Moon,
  Sun,
} from "lucide-react";
import { useTheme } from "@/components/theme-provider";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

const navigation = [
  { name: "Overview", href: "/", icon: LayoutDashboard },
  { name: "Applications", href: "/applications", icon: AppWindow },
  { name: "Services", href: "/services", icon: Server },
  { name: "Logs", href: "/logs", icon: FileText },
  { name: "Traces", href: "/traces", icon: GitBranch },
  { name: "Metrics", href: "/metrics", icon: BarChart3 },
  { name: "Exceptions", href: "/exceptions", icon: Bug },
  { name: "Alerts", href: "/alerts", icon: Bell },
  { name: "Incidents", href: "/incidents", icon: AlertTriangle },
  { name: "Health", href: "/health", icon: HeartPulse },
  { name: "Analytics", href: "/analytics", icon: TrendingUp },
  { name: "API Keys", href: "/api-keys", icon: Key },
  { name: "Team", href: "/team", icon: Users },
  { name: "Audit Logs", href: "/audit-logs", icon: ScrollText },
  { name: "Settings", href: "/settings", icon: Settings },
];

export function Sidebar() {
  const { theme, setTheme } = useTheme();

  return (
    <aside className="flex h-screen w-64 flex-col border-r border-[hsl(var(--border))] bg-[hsl(var(--card))]">
      {/* Logo */}
      <div className="flex h-16 items-center gap-2 border-b border-[hsl(var(--border))] px-6">
        <Shield className="h-7 w-7 text-[hsl(var(--primary))]" />
        <span className="text-lg font-bold tracking-tight">SentinelOps</span>
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto px-3 py-4">
        <ul className="space-y-1">
          {navigation.map((item) => (
            <li key={item.name}>
              <NavLink
                to={item.href}
                className={({ isActive }) =>
                  cn(
                    "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                    isActive
                      ? "bg-[hsl(var(--primary))] text-[hsl(var(--primary-foreground))]"
                      : "text-[hsl(var(--muted-foreground))] hover:bg-[hsl(var(--accent))] hover:text-[hsl(var(--accent-foreground))]"
                  )
                }
                end={item.href === "/"}
              >
                <item.icon className="h-4 w-4" />
                {item.name}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>

      {/* Theme Toggle */}
      <div className="border-t border-[hsl(var(--border))] p-3">
        <Button
          variant="ghost"
          size="sm"
          className="w-full justify-start gap-2"
          onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
        >
          {theme === "dark" ? (
            <Sun className="h-4 w-4" />
          ) : (
            <Moon className="h-4 w-4" />
          )}
          {theme === "dark" ? "Light Mode" : "Dark Mode"}
        </Button>
      </div>
    </aside>
  );
}
