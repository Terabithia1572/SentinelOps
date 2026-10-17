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
}

const statusColorMap: Record<StatusType, { dot: string; pulse: string; badge: string }> = {
  healthy: {
    dot: "bg-emerald-500",
    pulse: "bg-emerald-400",
    badge: "border-emerald-500/20 bg-emerald-500/10 text-emerald-400",
  },
  operational: {
    dot: "bg-emerald-500",
    pulse: "bg-emerald-400",
    badge: "border-emerald-500/20 bg-emerald-500/10 text-emerald-400",
  },
  degraded: {
    dot: "bg-amber-500",
    pulse: "bg-amber-400",
    badge: "border-amber-500/20 bg-amber-500/10 text-amber-400",
  },
  warning: {
    dot: "bg-amber-500",
    pulse: "bg-amber-400",
    badge: "border-amber-500/20 bg-amber-500/10 text-amber-400",
  },
  outage: {
    dot: "bg-rose-500",
    pulse: "bg-rose-400",
    badge: "border-rose-500/20 bg-rose-500/10 text-rose-400",
  },
  critical: {
    dot: "bg-rose-500",
    pulse: "bg-rose-400",
    badge: "border-rose-500/20 bg-rose-500/10 text-rose-400",
  },
  unknown: {
    dot: "bg-slate-400",
    pulse: "bg-slate-300",
    badge: "border-slate-500/20 bg-slate-500/10 text-slate-400",
  },
  info: {
    dot: "bg-sky-500",
    pulse: "bg-sky-400",
    badge: "border-sky-500/20 bg-sky-500/10 text-sky-400",
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
      {...props}
    >
      <span className="relative flex shrink-0 items-center justify-center">
        {shouldPulse && (status === "healthy" || status === "operational" || status === "degraded" || status === "critical") && (
          <span
            className={cn(
              "absolute inline-flex h-full w-full animate-ping rounded-full opacity-60",
              config.pulse
            )}
          />
        )}
        <span className={cn("relative inline-flex rounded-full", dotSize, config.dot)} />
      </span>
      {label && <span>{label}</span>}
    </span>
  );
}
