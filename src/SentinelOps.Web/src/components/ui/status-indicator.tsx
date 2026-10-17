import React from "react";
import { cn } from "@/lib/utils";

export type StatusType =
  | "healthy"
  | "operational"
  | "degraded"
  | "warning"
  | "outage"
  | "critical"
  | "unknown"
  | "info";

export interface StatusIndicatorProps extends React.HTMLAttributes<HTMLSpanElement> {
  status: StatusType;
  label?: string;
  showPulse?: boolean;
  pulse?: boolean;
  size?: "sm" | "default" | "lg";
  showSymbol?: boolean;
}

const statusColorMap: Record<StatusType, { dot: string; pulse: string; badge: string; symbol: string; shapeClass: string }> = {
  healthy: {
    dot: "bg-emerald-400",
    pulse: "bg-emerald-400",
    badge: "border-emerald-500/30 bg-emerald-500/10 text-emerald-400",
    symbol: "●",
    shapeClass: "rounded-full",
  },
  operational: {
    dot: "bg-emerald-400",
    pulse: "bg-emerald-400",
    badge: "border-emerald-500/30 bg-emerald-500/10 text-emerald-400",
    symbol: "●",
    shapeClass: "rounded-full",
  },
  degraded: {
    dot: "bg-amber-400",
    pulse: "bg-amber-400",
    badge: "border-amber-500/30 bg-amber-500/10 text-amber-400",
    symbol: "▲",
    shapeClass: "rounded-xs rotate-45 scale-75",
  },
  warning: {
    dot: "bg-amber-400",
    pulse: "bg-amber-400",
    badge: "border-amber-500/30 bg-amber-500/10 text-amber-400",
    symbol: "▲",
    shapeClass: "rounded-xs rotate-45 scale-75",
  },
  outage: {
    dot: "bg-rose-500",
    pulse: "bg-rose-400",
    badge: "border-rose-500/30 bg-rose-500/10 text-rose-400",
    symbol: "◆",
    shapeClass: "rotate-45 scale-90",
  },
  critical: {
    dot: "bg-rose-500",
    pulse: "bg-rose-400",
    badge: "border-rose-500/30 bg-rose-500/10 text-rose-400",
    symbol: "◆",
    shapeClass: "rotate-45 scale-90",
  },
  unknown: {
    dot: "bg-slate-400",
    pulse: "bg-slate-400",
    badge: "border-slate-500/30 bg-slate-500/10 text-slate-400",
    symbol: "○",
    shapeClass: "rounded-full border border-slate-400 !bg-transparent",
  },
  info: {
    dot: "bg-[#00E5FF]",
    pulse: "bg-[#00E5FF]",
    badge: "border-[#00E5FF]/30 bg-[#00E5FF]/10 text-[#00E5FF]",
    symbol: "■",
    shapeClass: "rounded-xs",
  },
};

const dotSizes = {
  sm: "h-1.5 w-1.5",
  default: "h-2 w-2",
  lg: "h-2.5 w-2.5",
};

export function StatusIndicator({
  status,
  label,
  showPulse = true,
  pulse,
  size = "default",
  showSymbol = false,
  className,
  ...props
}: StatusIndicatorProps) {
  const config = statusColorMap[status] || statusColorMap.unknown;
  const dotSize = dotSizes[size];
  const shouldPulse = pulse !== undefined ? pulse : showPulse;

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 font-medium",
        label && "text-xs",
        className
      )}
      role="status"
      aria-label={`${status}: ${label || status}`}
      {...props}
    >
      <span className="relative flex shrink-0 items-center justify-center">
        {shouldPulse && (status === "healthy" || status === "operational" || status === "degraded" || status === "critical") && (
          <span
            className={cn(
              "absolute inline-flex h-full w-full animate-ping opacity-60",
              config.shapeClass,
              config.pulse
            )}
          />
        )}
        <span className={cn("relative inline-flex", dotSize, config.shapeClass, config.dot)} />
      </span>
      {showSymbol && <span className="text-[10px] opacity-75 font-mono select-none" aria-hidden="true">{config.symbol}</span>}
      {label && <span className="tabular-nums tracking-tight">{label}</span>}
    </span>
  );
}
