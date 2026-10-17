import { Component, type ErrorInfo, type ReactNode } from "react";
import { AlertTriangle, RefreshCw, Home } from "lucide-react";
import { Button } from "./button";

interface Props {
  children: ReactNode;
  fallbackTitle?: string;
  onReset?: () => void;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

export class ErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
    error: null,
    errorInfo: null,
  };

  public static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error, errorInfo: null };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error("ErrorBoundary caught an unhandled component error:", error, errorInfo);
    this.setState({ errorInfo });
  }

  private handleReset = () => {
    this.setState({ hasError: false, error: null, errorInfo: null });
    if (this.props.onReset) {
      this.props.onReset();
    }
  };

  public render() {
    if (this.state.hasError) {
      return (
        <div className="flex min-h-[360px] w-full flex-col items-center justify-center p-6 text-center">
          <div className="w-full max-w-lg rounded border border-[#1E2333] bg-[#10121A] p-6 shadow-xl">
            <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-rose-500/10 border border-rose-500/20 text-rose-400">
              <AlertTriangle className="h-6 w-6" />
            </div>

            <h2 className="text-base font-bold text-[#F0F3F9]">
              {this.props.fallbackTitle || "Render Diagnostic Intercepted"}
            </h2>

            <p className="mt-1 text-xs text-[#758498]">
              An unexpected client error occurred while rendering this view. State has been safely isolated.
            </p>

            {this.state.error && (
              <div className="mt-4 rounded border border-[#1E2333] bg-[#08090D] p-3 text-left">
                <span className="font-mono text-[11px] text-rose-400 font-semibold block">
                  {this.state.error.name}: {this.state.error.message}
                </span>
                {this.state.errorInfo && (
                  <details className="mt-2 text-[10px] text-[#55657E]">
                    <summary className="cursor-pointer font-mono hover:text-[#758498]">View Component Stack</summary>
                    <pre className="mt-1 max-h-32 overflow-auto font-mono text-[9px] text-[#758498]">
                      {this.state.errorInfo.componentStack}
                    </pre>
                  </details>
                )}
              </div>
            )}

            <div className="mt-6 flex items-center justify-center gap-3">
              <Button
                variant="outline"
                size="sm"
                onClick={this.handleReset}
                className="h-8 text-xs border-[#1E2333] hover:border-[#00E5FF]/40 text-[#F0F3F9] gap-1.5"
              >
                <RefreshCw className="h-3.5 w-3.5 text-[#00E5FF]" />
                Retry View
              </Button>

              <Button
                size="sm"
                onClick={() => { window.location.href = "/"; }}
                className="h-8 text-xs bg-[#00E5FF]/10 text-[#00E5FF] hover:bg-[#00E5FF]/20 border border-[#00E5FF]/30 gap-1.5"
              >
                <Home className="h-3.5 w-3.5" />
                Back to Overview
              </Button>
            </div>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
