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
  badge?: string;
  badgeVariant?: "destructive" | "warning" | "default";
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
      { name: "AI Observability", href: "/ai-observability", icon: Sparkles, badge: "AI", badgeVariant: "default" },
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
      { name: "Active Alerts", href: "/alerts", icon: Bell, badge: "3", badgeVariant: "destructive" },
      { name: "Alert Rules", href: "/alert-rules", icon: BellRing },
      { name: "Incidents", href: "/incidents", icon: AlertTriangle, badge: "1", badgeVariant: "warning" },
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
    <aside className="flex h-full w-64 flex-col border-r border-[#1E2333] bg-[#0A0C12] text-[#F0F3F9] select-none">
      {/* Brand Header */}
      <div className="flex h-12 items-center justify-between border-b border-[#1E2333] px-4 bg-[#0E1017]">
        <div className="flex items-center gap-2.5">
          <div className="flex h-6 w-6 items-center justify-center rounded bg-[#00E5FF]/10 border border-[#00E5FF]/30">
            <Shield className="h-3.5 w-3.5 text-[#00E5FF]" />
          </div>
          <div className="flex items-center gap-1.5">
            <span className="text-xs font-bold tracking-tight text-[#F0F3F9] font-mono">
              SentinelOps
            </span>
            <span className="text-[10px] font-mono text-[#00E5FF] bg-[#00E5FF]/10 border border-[#00E5FF]/20 px-1 py-0.2 rounded">
              v18.0
            </span>
          </div>
        </div>

        {onClose && (
          <Button
            variant="ghost"
            size="sm"
            onClick={onClose}
            className="h-7 w-7 p-0 text-[#758498] hover:text-[#F0F3F9] md:hidden"
            aria-label="Close sidebar navigation"
          >
            <X className="h-4 w-4" />
          </Button>
        )}
      </div>

      {/* Organization Switcher */}
      {user && (
        <div className="border-b border-[#1E2333] p-2 bg-[#08090D]">
          <div className="flex items-center justify-between text-[10px] text-[#758498] mb-1 px-1 font-mono uppercase tracking-wider">
            <span>Organization</span>
            <NavLink
              to="/organizations"
              onClick={onItemClick}
              className="hover:text-[#00E5FF] transition-colors"
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
              className="w-full bg-[#10121A] border border-[#1E2333] text-[#F0F3F9] text-xs rounded px-2 py-1 focus:outline-none focus:border-[#00E5FF] font-mono"
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
              className="flex items-center justify-between px-2 py-1 rounded bg-[#10121A] border border-[#1E2333] text-xs font-medium hover:border-[#2A324B] transition-colors"
            >
              <div className="flex items-center gap-2 truncate">
                <Building2 className="h-3.5 w-3.5 text-[#00E5FF] shrink-0" />
                <span className="truncate text-[#F0F3F9] font-mono text-xs">{activeOrg?.name || "Acme Corp"}</span>
              </div>
              <Badge variant="outline" className="text-[10px] px-1 py-0 h-4 border-[#1E2333] text-[#758498]">
                {activeOrg?.userRole || "Admin"}
              </Badge>
            </NavLink>
          )}
        </div>
      )}

      {/* Navigation Sections */}
      <nav className="flex-1 overflow-y-auto px-2 py-2 space-y-3" aria-label="Main Navigation">
        {navigationSections.map((section) => (
          <div key={section.title} className="space-y-0.5">
            <h2 className="px-2 text-[10px] font-mono font-medium uppercase tracking-wider text-[#55657E]">
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
                        "group flex items-center justify-between rounded px-2.5 py-1.5 text-xs font-medium transition-all",
                        isActive
                          ? "bg-[#141722] text-[#F0F3F9] border-l-2 border-[#00E5FF] font-semibold"
                          : "text-[#A6B4C9] hover:bg-[#10121A] hover:text-[#F0F3F9] border-l-2 border-transparent"
                      )
                    }
                    end={item.href === "/"}
                  >
                    <div className="flex items-center gap-2 truncate">
                      <item.icon className="h-3.5 w-3.5 shrink-0 text-[#758498] group-hover:text-[#F0F3F9] transition-colors" />
                      <span className="truncate">{item.name}</span>
                    </div>
                    {item.badge && (
                      <span
                        className={cn(
                          "ml-auto text-[10px] font-mono px-1 py-0.2 rounded border",
                          item.badgeVariant === "destructive"
                            ? "border-rose-500/30 bg-rose-500/10 text-rose-400"
                            : item.badgeVariant === "warning"
                            ? "border-amber-500/30 bg-amber-500/10 text-amber-400"
                            : "border-[#00E5FF]/30 bg-[#00E5FF]/10 text-[#00E5FF]"
                        )}
                      >
                        {item.badge}
                      </span>
                    )}
                  </NavLink>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </nav>

      {/* User & Settings Footer */}
      <div className="border-t border-[#1E2333] p-2 space-y-1.5 bg-[#0E1017]">
        {user && (
          <div className="flex items-center gap-2 rounded bg-[#10121A] border border-[#1E2333] p-1.5">
            <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded bg-[#00E5FF]/10 text-[#00E5FF] border border-[#00E5FF]/20 font-mono text-xs">
              <UserIcon className="h-3.5 w-3.5" />
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-center justify-between gap-1">
                <p className="truncate text-xs font-medium text-[#F0F3F9] leading-none">
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
              <p className="truncate text-[10px] font-mono text-[#758498] mt-0.5">{user.email}</p>
            </div>
          </div>
        )}

        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="sm"
            className="flex-1 justify-start gap-1.5 text-xs text-[#758498] hover:text-[#F0F3F9] hover:bg-[#141722] h-7 px-2"
            onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
          >
            {theme === "dark" ? (
              <Sun className="h-3 w-3 text-amber-400" />
            ) : (
              <Moon className="h-3 w-3 text-[#00E5FF]" />
            )}
            <span>{theme === "dark" ? "Light" : "Dark"}</span>
          </Button>

          {user && (
            <Button
              variant="ghost"
              size="sm"
              className="gap-1 text-xs text-rose-400 hover:bg-rose-500/10 hover:text-rose-300 h-7 px-2"
              onClick={logout}
              title="Sign Out"
            >
              <LogOut className="h-3 w-3" />
              <span>Exit</span>
            </Button>
          )}
        </div>
      </div>
    </aside>
  );
}
