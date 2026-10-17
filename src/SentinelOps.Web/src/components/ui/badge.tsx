import React from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

const badgeVariants = cva(
  "inline-flex items-center rounded-xs border px-2 py-0.5 text-[11px] font-mono tracking-tight transition-colors focus:outline-none focus:ring-1 focus:ring-ring",
  {
    variants: {
      variant: {
        default:
          "border-[#00E5FF]/30 bg-[#00E5FF]/10 text-[#00E5FF]",
        secondary:
          "border-[hsl(var(--border))] bg-[hsl(var(--secondary))] text-[hsl(var(--secondary-foreground))]",
        destructive:
          "border-rose-500/30 bg-rose-500/10 text-rose-400 font-medium",
        outline: "border-[hsl(var(--border))] text-[hsl(var(--foreground))]",
        success:
          "border-emerald-500/30 bg-emerald-500/10 text-emerald-400 font-medium",
        warning:
          "border-amber-500/30 bg-amber-500/10 text-amber-400 font-medium",
        danger:
          "border-rose-500/30 bg-rose-500/10 text-rose-400 font-medium",
        info:
          "border-sky-500/30 bg-sky-500/10 text-sky-400 font-medium",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
);

export interface BadgeProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof badgeVariants> {}

function Badge({ className, variant, ...props }: BadgeProps) {
  return (
    <div className={cn(badgeVariants({ variant }), className)} {...props} />
  );
}

export { Badge, badgeVariants };
