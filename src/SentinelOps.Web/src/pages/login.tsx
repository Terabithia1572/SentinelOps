import React, { useState } from "react";
import { Link, useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "@/context/auth-context";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Shield, Eye, EyeOff, Loader2, AlertCircle, Activity, Radio, Cpu, Lock } from "lucide-react";

export function LoginPage() {
  const { login, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [emailOrUser, setEmailOrUser] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const from = (location.state as { from?: { pathname: string } })?.from?.pathname || "/";

  React.useEffect(() => {
    if (isAuthenticated) {
      navigate(from, { replace: true });
    }
  }, [isAuthenticated, navigate, from]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!emailOrUser.trim()) {
      setError("Please enter your email or username.");
      return;
    }
    if (!password) {
      setError("Please enter your password.");
      return;
    }

    try {
      setIsLoading(true);
      await login(emailOrUser.trim(), password);
      navigate(from, { replace: true });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Invalid credentials or service unavailable.";
      setError(msg);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen bg-[#08090D] text-[#F0F3F9]">
      {/* Left Column: Architectural Telemetry Vitals (Visible on large screens) */}
      <div className="hidden lg:flex lg:w-1/2 flex-col justify-between border-r border-[#1E2333] bg-[#0A0C12] p-12 relative overflow-hidden">
        {/* Subtle grid pattern background */}
        <div className="absolute inset-0 bg-[radial-gradient(#1E2333_1px,transparent_1px)] [background-size:24px_24px] opacity-30 pointer-events-none" />

        {/* Top Branding */}
        <div className="relative z-10">
          <div className="flex items-center gap-3">
            <div className="flex h-9 w-9 items-center justify-center rounded bg-[#00E5FF]/10 border border-[#00E5FF]/30">
              <Shield className="h-5 w-5 text-[#00E5FF]" />
            </div>
            <div>
              <h1 className="text-base font-bold font-mono tracking-tight text-[#F0F3F9] block leading-none">
                SentinelOps
              </h1>
              <span className="text-[10px] font-mono text-[#758498] mt-1 block">
                Enterprise Telemetry & Incident Intelligence
              </span>
            </div>
          </div>
        </div>

        {/* Middle Value Proposition & Telemetry Feed */}
        <div className="relative z-10 space-y-6 my-auto max-w-md">
          <div className="space-y-2">
            <Badge variant="outline" className="border-[#00E5FF]/30 bg-[#00E5FF]/10 text-[#00E5FF] font-mono text-[11px]">
              PRECISION WORKBENCH
            </Badge>
            <h2 className="text-2xl font-bold tracking-tight text-[#F0F3F9] leading-snug">
              Sub-millisecond telemetry ingestion and AI-assisted root cause triage.
            </h2>
            <p className="text-xs text-[#758498] leading-relaxed">
              Engineered for Site Reliability Engineers and DevOps architects managing high-throughput distributed systems.
            </p>
          </div>

          {/* Real-time Telemetry Readout Cards */}
          <div className="grid grid-cols-2 gap-3 pt-2">
            <div className="rounded border border-[#1E2333] bg-[#10121A] p-3">
              <div className="flex items-center justify-between text-[10px] font-mono text-[#758498]">
                <span>THROUGHPUT</span>
                <Activity className="h-3 w-3 text-[#00E5FF]" />
              </div>
              <div className="text-lg font-bold font-mono text-[#F0F3F9] mt-1 tabular-nums">
                14.2k eps
              </div>
              <div className="text-[10px] font-mono text-[#34D399] mt-0.5">
                ● Ingestion nominal
              </div>
            </div>

            <div className="rounded border border-[#1E2333] bg-[#10121A] p-3">
              <div className="flex items-center justify-between text-[10px] font-mono text-[#758498]">
                <span>UPTIME SLA</span>
                <Radio className="h-3 w-3 text-[#34D399]" />
              </div>
              <div className="text-lg font-bold font-mono text-[#F0F3F9] mt-1 tabular-nums">
                99.99%
              </div>
              <div className="text-[10px] font-mono text-[#758498] mt-0.5">
                Zero fatal faults
              </div>
            </div>

            <div className="rounded border border-[#1E2333] bg-[#10121A] p-3">
              <div className="flex items-center justify-between text-[10px] font-mono text-[#758498]">
                <span>ACTIVE AGENTS</span>
                <Cpu className="h-3 w-3 text-[#00E5FF]" />
              </div>
              <div className="text-lg font-bold font-mono text-[#F0F3F9] mt-1 tabular-nums">
                3 Nodes
              </div>
              <div className="text-[10px] font-mono text-[#758498] mt-0.5">
                OTel collector mesh
              </div>
            </div>

            <div className="rounded border border-[#1E2333] bg-[#10121A] p-3">
              <div className="flex items-center justify-between text-[10px] font-mono text-[#758498]">
                <span>ISOLATION</span>
                <Lock className="h-3 w-3 text-[#FBBF24]" />
              </div>
              <div className="text-lg font-bold font-mono text-[#F0F3F9] mt-1 tabular-nums">
                Multi-Tenant
              </div>
              <div className="text-[10px] font-mono text-[#758498] mt-0.5">
                Row-level security
              </div>
            </div>
          </div>
        </div>

        {/* Bottom Compliance Ledger */}
        <div className="relative z-10 flex items-center gap-4 text-[11px] font-mono text-[#55657E] border-t border-[#1E2333] pt-4">
          <span>SOC-2 TYPE II</span>
          <span>•</span>
          <span>ISO 27001</span>
          <span>•</span>
          <span>ENCRYPTED IN TRANSIT</span>
        </div>
      </div>

      {/* Right Column: Surgical Authentication Console */}
      <div className="flex flex-1 flex-col justify-center items-center p-6 sm:p-12 lg:p-16">
        <div className="w-full max-w-sm space-y-6">
          {/* Mobile Brand Heading */}
          <div className="flex flex-col items-center text-center lg:hidden space-y-2 mb-4">
            <div className="flex h-10 w-10 items-center justify-center rounded bg-[#00E5FF]/10 border border-[#00E5FF]/30">
              <Shield className="h-6 w-6 text-[#00E5FF]" />
            </div>
            <h1 className="text-xl font-bold font-mono text-[#F0F3F9]">SentinelOps</h1>
            <p className="text-xs text-[#758498]">Enterprise Observability Suite</p>
          </div>

          {/* Form Card */}
          <div className="rounded border border-[#1E2333] bg-[#10121A] p-6 shadow-2xl">
            <div className="space-y-1 mb-5">
              <h2 className="text-lg font-bold tracking-tight text-[#F0F3F9] font-mono">Sign In</h2>
              <p className="text-xs text-[#758498]">
                Enter your credentials to access your telemetry platform
              </p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-4">
              {error && (
                <div role="alert" className="flex items-start gap-2 rounded border border-rose-500/20 bg-rose-500/10 p-2.5 text-xs text-rose-400 font-mono">
                  <AlertCircle className="h-4 w-4 shrink-0 mt-0.5" />
                  <span>{error}</span>
                </div>
              )}

              <div className="space-y-1.5">
                <label className="text-xs font-mono font-medium text-[#A6B4C9]" htmlFor="emailOrUser">
                  Email or Username
                </label>
                <Input
                  id="emailOrUser"
                  type="text"
                  placeholder="admin@sentinelops.local"
                  autoComplete="username"
                  value={emailOrUser}
                  onChange={(e) => setEmailOrUser(e.target.value)}
                  disabled={isLoading}
                  required
                />
              </div>

              <div className="space-y-1.5">
                <div className="flex items-center justify-between">
                  <label className="text-xs font-mono font-medium text-[#A6B4C9]" htmlFor="password">
                    Password
                  </label>
                </div>
                <div className="relative">
                  <Input
                    id="password"
                    type={showPassword ? "text" : "password"}
                    placeholder="••••••••••••"
                    autoComplete="current-password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    disabled={isLoading}
                    required
                  />
                  <button
                    type="button"
                    className="absolute right-2.5 top-1/2 -translate-y-1/2 text-[#758498] hover:text-[#F0F3F9]"
                    onClick={() => setShowPassword(!showPassword)}
                    tabIndex={-1}
                  >
                    {showPassword ? <EyeOff className="h-3.5 w-3.5" /> : <Eye className="h-3.5 w-3.5" />}
                  </button>
                </div>
              </div>

              <div className="pt-2">
                <Button
                  type="submit"
                  className="w-full h-9 bg-[#00E5FF] hover:bg-[#33EBFF] text-[#08090D] font-bold font-mono text-xs rounded transition-all cursor-pointer"
                  disabled={isLoading}
                >
                  {isLoading && <Loader2 className="h-3.5 w-3.5 mr-2 animate-spin" />}
                  {isLoading ? "Authenticating..." : "Sign In"}
                </Button>
              </div>

              <div className="text-center text-xs text-[#758498] pt-2 border-t border-[#1E2333]/60">
                Don&apos;t have an account?{" "}
                <Link to="/register" className="font-semibold text-[#00E5FF] hover:underline font-mono">
                  Create one
                </Link>
              </div>
            </form>
          </div>

          {/* Quick Demo Credentials Helper */}
          <div className="rounded border border-[#1E2333]/80 bg-[#0E1017] p-3 text-[11px] font-mono text-[#758498] space-y-1">
            <div className="text-[#A6B4C9] font-medium flex items-center justify-between">
              <span>Demo Credentials:</span>
              <button
                type="button"
                onClick={() => {
                  setEmailOrUser("admin@sentinelops.local");
                  setPassword("Admin123!");
                }}
                className="text-[10px] text-[#00E5FF] hover:underline cursor-pointer"
              >
                Auto-fill
              </button>
            </div>
            <div>User: admin@sentinelops.local</div>
            <div>Pass: Admin123!</div>
          </div>
        </div>
      </div>
    </div>
  );
}
