import React, { useState } from "react";
import { cn } from "@/lib/utils";
import { AlertTriangle, RefreshCw, ChevronDown, ChevronUp } from "lucide-react";
import { Button } from "./button";

export interface ErrorStateProps extends React.HTMLAttributes<HTMLDivElement> {
  title?: string;
  message?: string;
  error?: Error | unknown;
  onRetry?: () => void;
  isRetrying?: boolean;
}

export function ErrorState({
  title = "An unexpected error occurred",
  message = "Failed to communicate with SentinelOps API or load telemetry records.",
  error,
  onRetry,
  isRetrying = false,
  className,
  ...props
}: ErrorStateProps) {
  const [showDetails, setShowDetails] = useState(false);
  const technicalMessage =
    error instanceof Error
      ? error.stack || error.message
      : typeof error === "string"
      ? error
      : error
      ? JSON.stringify(error, null, 2)
      : null;

  return (
    <div
      className={cn(
        "flex min-h-[260px] flex-col items-center justify-center rounded-lg border border-rose-500/30 bg-rose-500/5 p-8 text-center",
        className
      )}
      role="alert"
      {...props}
    >
      <div className="flex h-12 w-12 items-center justify-center rounded-full bg-rose-500/10 text-rose-400 mb-4">
        <AlertTriangle className="h-6 w-6" aria-hidden="true" />
      </div>
      <h3 className="text-base font-semibold text-[hsl(var(--foreground))]">{title}</h3>
      <p className="mt-1.5 max-w-md text-sm text-[hsl(var(--muted-foreground))]">{message}</p>

      {onRetry && (
        <Button
          size="sm"
          variant="outline"
          className="mt-5 gap-2 border-rose-500/30 hover:bg-rose-500/10 text-rose-300"
          onClick={onRetry}
          disabled={isRetrying}
        >
          <RefreshCw className={cn("h-3.5 w-3.5", isRetrying && "animate-spin")} />
          <span>{isRetrying ? "Retrying..." : "Try Again"}</span>
        </Button>
      )}

      {technicalMessage && (
        <div className="mt-4 w-full max-w-lg text-left">
          <button
            type="button"
            className="flex items-center gap-1 text-xs text-[hsl(var(--muted-foreground))] hover:text-[hsl(var(--foreground))] mx-auto mb-2"
            onClick={() => setShowDetails(!showDetails)}
          >
            <span>{showDetails ? "Hide technical details" : "Show technical details"}</span>
            {showDetails ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />}
          </button>
          {showDetails && (
            <pre className="overflow-x-auto rounded bg-black/60 p-3 text-[11px] font-mono text-rose-300/90 border border-rose-500/20">
              {technicalMessage}
            </pre>
          )}
        </div>
      )}
    </div>
  );
}
