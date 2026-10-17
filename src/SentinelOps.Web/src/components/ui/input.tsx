import React, { forwardRef } from "react";
import { cn } from "@/lib/utils";

export type InputProps = React.InputHTMLAttributes<HTMLInputElement>;

const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ className, type, ...props }, ref) => {
    return (
      <input
        type={type}
        className={cn(
          "flex h-9 w-full rounded border border-[hsl(var(--border))] bg-[#0A0C11] px-3 py-1.5 text-xs text-[hsl(var(--foreground))] transition-colors placeholder:text-[#55657E] focus-visible:outline-none focus-visible:border-[#00E5FF] focus-visible:ring-1 focus-visible:ring-[#00E5FF] disabled:cursor-not-allowed disabled:opacity-40",
          className
        )}
        ref={ref}
        {...props}
      />
    );
  }
);
Input.displayName = "Input";

export { Input };
