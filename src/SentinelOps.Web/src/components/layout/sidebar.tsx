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
  BellRing,
  AlertTriangle,
  HeartPulse,
  Target,
  TrendingUp,
  Key,
  Users,
  ScrollText,
  Settings,
  Shield,
  Moon,
  Sun,
  LogOut,
  User as UserIcon,
  Building2,
  LayoutGrid,
  Network,
  Globe,
} from "lucide-react";
import { useTheme } from "@/components/theme-provider";
import { useAuth } from "@/context/auth-context";
import { useTenant } from "@/context/tenant-context";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

const navigation = [
  { name: "Overview", href: "/", icon: LayoutDashboard },
  { name: "Dashboards", href: "/dashboards", icon: LayoutGrid },
  { name: "APM & Service Map", href: "/apm", icon: Network },
  { name: "Organizations", href: "/organizations", icon: Building2 },
  { name: "Applications", href: "/applications", icon: AppWindow },
  { name: "Services", href: "/services", icon: Server },
  { name: "Logs", href: "/logs", icon: FileText },
  { name: "Traces", href: "/traces", icon: GitBranch },
  { name: "Metrics", href: "/metrics", icon: BarChart3 },
  { name: "Exceptions", href: "/exceptions", icon: Bug },
  { name: "Alerts", href: "/alerts", icon: Bell },
  { name: "Alert Rules", href: "/alert-rules", icon: BellRing },
  { name: "Incidents", href: "/incidents", icon: AlertTriangle },
  { name: "Status Pages", href: "/status-pages", icon: Globe },
  { name: "SLOs & Objectives", href: "/slos", icon: Target },
  { name: "Health", href: "/health", icon: HeartPulse },
  { name: "Analytics", href: "/analytics", icon: TrendingUp },
  { name: "API Keys", href: "/api-keys", icon: Key },
  { name: "Team", href: "/team", icon: Users },
  { name: "Audit Logs", href: "/audit-logs", icon: ScrollText },
  { name: "Settings", href: "/settings", icon: Settings },
];

export function Sidebar() {
  const { theme, setTheme } = useTheme();
  const { user, logout } = useAuth();
  const { activeOrg, organizations, setActiveOrg } = useTenant();

  const primaryRole = user?.roles?.[0] || "Viewer";

  return (
    <aside className="flex h-screen w-64 flex-col border-r border-[hsl(var(--border))] bg-[hsl(var(--card))]">
      {/* Logo */}
      <div className="flex h-16 items-center gap-2 border-b border-[hsl(var(--border))] px-6">
        <Shield className="h-7 w-7 text-[hsl(var(--primary))]" />
        <span className="text-lg font-bold tracking-tight">SentinelOps</span>
      </div>

      {/* Active Organization Switcher */}
      {user && (
        <div className="border-b border-[hsl(var(--border))] px-3 py-2.5 bg-[hsl(var(--muted)/0.3)]">
          <div className="flex items-center justify-between text-xs text-[hsl(var(--muted-foreground))] mb-1.5 px-1">
            <span className="font-semibold uppercase tracking-wider text-[10px]">Organization</span>
            <NavLink to="/organizations" className="hover:text-[hsl(var(--primary))] transition-colors" title="Manage Organizations">
              <Settings className="h-3 w-3" />
            </NavLink>
          </div>
          {organizations.length > 1 ? (
            <select
              aria-label="Active Organization"
              value={activeOrg?.id || ""}
              onChange={(e) => {
                const found = organizations.find((o) => o.id === e.target.value);
                if (found) setActiveOrg(found);
              }}
              className="w-full bg-[hsl(var(--background))] border border-[hsl(var(--border))] text-[hsl(var(--foreground))] text-xs rounded px-2 py-1.5 focus:outline-none focus:ring-1 focus:ring-[hsl(var(--primary))]"
            >
              {organizations.map((org) => (
                <option key={org.id} value={org.id}>
                  {org.name} ({org.userRole})
                </option>
              ))}
            </select>
          ) : (
            <NavLink
              to="/organizations"
              className="flex items-center justify-between px-2 py-1.5 rounded bg-[hsl(var(--background))] border border-[hsl(var(--border))] text-xs font-medium hover:border-[hsl(var(--primary)/0.5)] transition-colors"
            >
              <div className="flex items-center gap-2 truncate">
                <Building2 className="h-3.5 w-3.5 text-[hsl(var(--primary))]" />
                <span className="truncate">{activeOrg?.name || "Default Org"}</span>
              </div>
              <Badge variant="outline" className="text-[10px] px-1 py-0 h-4">
                {activeOrg?.userRole || "Member"}
              </Badge>
            </NavLink>
          )}
        </div>
      )}

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

      {/* User & Settings Footer */}
      <div className="border-t border-[hsl(var(--border))] p-3 space-y-2">
        {user && (
          <div className="flex items-center gap-3 rounded-md bg-[hsl(var(--accent)/0.5)] p-2">
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-[hsl(var(--primary)/0.2)] text-[hsl(var(--primary))]">
              <UserIcon className="h-4 w-4" />
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-center justify-between gap-1">
                <p className="truncate text-xs font-semibold leading-none">{user.fullName || user.userName}</p>
                <Badge
                  variant={
                    primaryRole === "Admin"
                      ? "danger"
                      : primaryRole === "Operator"
                      ? "warning"
                      : "secondary"
                  }
                  className="text-[10px] px-1.5 py-0 h-4"
                >
                  {primaryRole}
                </Badge>
              </div>
              <p className="truncate text-[11px] text-[hsl(var(--muted-foreground))] mt-0.5">{user.email}</p>
            </div>
          </div>
        )}

        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="sm"
            className="flex-1 justify-start gap-2 text-xs"
            onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
          >
            {theme === "dark" ? (
              <Sun className="h-3.5 w-3.5" />
            ) : (
              <Moon className="h-3.5 w-3.5" />
            )}
            {theme === "dark" ? "Light" : "Dark"}
          </Button>

          {user && (
            <Button
              variant="ghost"
              size="sm"
              className="gap-1.5 text-xs text-rose-400 hover:bg-rose-500/10 hover:text-rose-300"
              onClick={logout}
              title="Sign Out"
            >
              <LogOut className="h-3.5 w-3.5" />
              <span>Exit</span>
            </Button>
          )}
        </div>
      </div>
    </aside>
  );
}
