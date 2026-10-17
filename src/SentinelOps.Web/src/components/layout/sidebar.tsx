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
  Sparkles,
  CreditCard,
  X,
} from "lucide-react";
import { useTheme } from "@/components/theme-provider";
import { useAuth } from "@/context/auth-context";
import { useTenant } from "@/context/tenant-context";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

export interface NavItem {
  name: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
}

export interface NavSection {
  title: string;
  items: NavItem[];
}

export const navigationSections: NavSection[] = [
  {
    title: "Operations",
    items: [
      { name: "Overview", href: "/", icon: LayoutDashboard },
      { name: "Dashboards", href: "/dashboards", icon: LayoutGrid },
      { name: "APM & Service Map", href: "/apm", icon: Network },
      { name: "AI Observability", href: "/ai-observability", icon: Sparkles },
      { name: "Applications", href: "/applications", icon: AppWindow },
      { name: "Services & Health", href: "/services", icon: Server },
    ],
  },
  {
    title: "Telemetry",
    items: [
      { name: "Logs", href: "/logs", icon: FileText },
      { name: "Traces", href: "/traces", icon: GitBranch },
      { name: "Metrics", href: "/metrics", icon: BarChart3 },
      { name: "Exceptions", href: "/exceptions", icon: Bug },
      { name: "Analytics & Reports", href: "/analytics", icon: TrendingUp },
    ],
  },
  {
    title: "Incident Response",
    items: [
      { name: "Active Alerts", href: "/alerts", icon: Bell },
      { name: "Alert Rules", href: "/alert-rules", icon: BellRing },
      { name: "Incidents", href: "/incidents", icon: AlertTriangle },
      { name: "Status Pages", href: "/status-pages", icon: Globe },
      { name: "SLOs & Objectives", href: "/slos", icon: Target },
    ],
  },
  {
    title: "Administration",
    items: [
      { name: "Organizations", href: "/organizations", icon: Building2 },
      { name: "Team & Users", href: "/team", icon: Users },
      { name: "API Keys", href: "/api-keys", icon: Key },
      { name: "Billing & Plans", href: "/billing", icon: CreditCard },
      { name: "Audit Logs", href: "/audit-logs", icon: ScrollText },
      { name: "Settings", href: "/settings", icon: Settings },
    ],
  },
];

interface SidebarProps {
  onItemClick?: () => void;
  onClose?: () => void;
}

export function Sidebar({ onItemClick, onClose }: SidebarProps) {
  const { theme, setTheme } = useTheme();
  const { user, logout } = useAuth();
  const { activeOrg, organizations, setActiveOrg } = useTenant();

  const primaryRole = user?.roles?.[0] || "Viewer";

  return (
    <aside className="flex h-full w-64 flex-col border-r border-[hsl(var(--border))] bg-[hsl(var(--card))]">
      {/* Brand & Optional Close Button */}
      <div className="flex h-16 items-center justify-between border-b border-[hsl(var(--border))] px-5">
        <div className="flex items-center gap-2.5">
          <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-600/10 border border-blue-500/20">
            <Shield className="h-5 w-5 text-blue-500" />
          </div>
          <div>
            <span className="text-base font-bold tracking-tight text-white block leading-none">
              SentinelOps
            </span>
            <span className="text-[10px] text-slate-400 block leading-tight mt-0.5">
              Observability Suite
            </span>
          </div>
        </div>

        {onClose && (
          <Button
            variant="ghost"
            size="sm"
            onClick={onClose}
            className="h-8 w-8 p-0 text-slate-400 hover:text-white md:hidden"
            aria-label="Close sidebar navigation"
          >
            <X className="h-5 w-5" />
          </Button>
        )}
      </div>

      {/* Active Organization Switcher */}
      {user && (
        <div className="border-b border-[hsl(var(--border))] px-3 py-2.5 bg-slate-900/40">
          <div className="flex items-center justify-between text-xs text-slate-400 mb-1.5 px-1">
            <span className="font-semibold uppercase tracking-wider text-[10px]">Organization</span>
            <NavLink
              to="/organizations"
              onClick={onItemClick}
              className="hover:text-blue-400 transition-colors"
              title="Manage Organizations"
            >
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
              className="w-full bg-slate-950/80 border border-slate-800 text-slate-200 text-xs rounded px-2 py-1.5 focus:outline-none focus:ring-1 focus:ring-blue-500"
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
              onClick={onItemClick}
              className="flex items-center justify-between px-2 py-1.5 rounded bg-slate-950/80 border border-slate-800 text-xs font-medium hover:border-slate-700 transition-colors"
            >
              <div className="flex items-center gap-2 truncate">
                <Building2 className="h-3.5 w-3.5 text-blue-400 shrink-0" />
                <span className="truncate text-slate-200">{activeOrg?.name || "Default Org"}</span>
              </div>
              <Badge variant="outline" className="text-[10px] px-1 py-0 h-4 border-slate-700 text-slate-400">
                {activeOrg?.userRole || "Member"}
              </Badge>
            </NavLink>
          )}
        </div>
      )}

      {/* Navigation Sections */}
      <nav className="flex-1 overflow-y-auto px-3 py-3 space-y-4" aria-label="Main Navigation">
        {navigationSections.map((section) => (
          <div key={section.title} className="space-y-1">
            <h2 className="px-3 text-[10px] font-semibold uppercase tracking-wider text-slate-400 select-none">
              {section.title}
            </h2>
            <ul className="space-y-0.5">
              {section.items.map((item) => (
                <li key={item.name}>
                  <NavLink
                    to={item.href}
                    onClick={onItemClick}
                    className={({ isActive }) =>
                      cn(
                        "flex items-center gap-2.5 rounded-lg px-3 py-1.5 text-xs font-medium transition-colors",
                        isActive
                          ? "bg-blue-600 text-white shadow-sm shadow-blue-600/30"
                          : "text-slate-400 hover:bg-slate-800/60 hover:text-slate-200"
                      )
                    }
                    end={item.href === "/"}
                  >
                    <item.icon className="h-4 w-4 shrink-0" />
                    <span className="truncate">{item.name}</span>
                  </NavLink>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </nav>

      {/* User & Settings Footer */}
      <div className="border-t border-[hsl(var(--border))] p-3 space-y-2 bg-slate-900/20">
        {user && (
          <div className="flex items-center gap-2.5 rounded-lg bg-slate-800/40 border border-slate-800 p-2">
            <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-blue-500/10 text-blue-400 border border-blue-500/20">
              <UserIcon className="h-4 w-4" />
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-center justify-between gap-1">
                <p className="truncate text-xs font-semibold text-slate-200 leading-none">
                  {user.fullName || user.userName}
                </p>
                <Badge
                  variant={
                    primaryRole === "Admin"
                      ? "danger"
                      : primaryRole === "Operator"
                      ? "warning"
                      : "secondary"
                  }
                  className="text-[9px] px-1 py-0 h-3.5"
                >
                  {primaryRole}
                </Badge>
              </div>
              <p className="truncate text-[10px] text-slate-400 mt-1">{user.email}</p>
            </div>
          </div>
        )}

        <div className="flex items-center gap-1.5">
          <Button
            variant="ghost"
            size="sm"
            className="flex-1 justify-start gap-2 text-xs text-slate-400 hover:text-slate-200 hover:bg-slate-800/60 h-8"
            onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
          >
            {theme === "dark" ? (
              <Sun className="h-3.5 w-3.5 text-amber-400" />
            ) : (
              <Moon className="h-3.5 w-3.5 text-blue-400" />
            )}
            <span>{theme === "dark" ? "Light" : "Dark"}</span>
          </Button>

          {user && (
            <Button
              variant="ghost"
              size="sm"
              className="gap-1.5 text-xs text-rose-400 hover:bg-rose-500/10 hover:text-rose-300 h-8"
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
