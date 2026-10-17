import React, { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/context/auth-context";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Shield, Eye, EyeOff, Loader2, AlertCircle, CheckCircle2 } from "lucide-react";

export function RegisterPage() {
  const { register, isAuthenticated } = useAuth();
  const navigate = useNavigate();

  const [fullName, setFullName] = useState("");
  const [userName, setUserName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  React.useEffect(() => {
    if (isAuthenticated) {
      navigate("/", { replace: true });
    }
  }, [isAuthenticated, navigate]);

  const hasLength = password.length >= 8;
  const hasUpper = /[A-Z]/.test(password);
  const hasNumber = /[0-9]/.test(password);
  const hasSpecial = /[^a-zA-Z0-9]/.test(password);
  const passwordsMatch = password && confirmPassword && password === confirmPassword;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!fullName.trim() || !userName.trim() || !email.trim() || !password) {
      setError("Please fill in all required fields.");
      return;
    }

    if (!hasLength || !hasUpper || !hasNumber || !hasSpecial) {
      setError("Password does not meet complexity requirements.");
      return;
    }

    if (password !== confirmPassword) {
      setError("Passwords do not match.");
      return;
    }

    try {
      setIsLoading(true);
      await register(fullName.trim(), userName.trim(), email.trim(), password);
      navigate("/", { replace: true });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Failed to register account.";
      setError(msg);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-[#08090D] px-4 py-12 text-[#F0F3F9]">
      <div className="w-full max-w-lg space-y-6">
        <div className="flex flex-col items-center text-center space-y-2">
          <div className="flex h-10 w-10 items-center justify-center rounded bg-[#00E5FF]/10 border border-[#00E5FF]/30 mb-1">
            <Shield className="h-6 w-6 text-[#00E5FF]" />
          </div>
          <h1 className="text-xl font-bold font-mono tracking-tight text-[#F0F3F9]">SentinelOps</h1>
          <p className="text-xs text-[#758498]">
            Create your account to start managing telemetry & incidents
          </p>
        </div>

        <div className="rounded border border-[#1E2333] bg-[#10121A] p-6 shadow-2xl">
          <div className="space-y-1 mb-5">
            <h2 className="text-lg font-bold tracking-tight text-[#F0F3F9] font-mono">Register Account</h2>
            <p className="text-xs text-[#758498]">
              Set up your SentinelOps user profile and initial role assignment
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div className="flex items-start gap-2 rounded border border-rose-500/30 bg-rose-500/10 p-2.5 text-xs text-rose-400 font-mono">
                <AlertCircle className="h-4 w-4 shrink-0 mt-0.5" />
                <span>{error}</span>
              </div>
            )}

            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div className="space-y-1.5">
                <label className="text-xs font-mono font-medium text-[#A6B4C9]" htmlFor="fullName">
                  Full Name
                </label>
                <Input
                  id="fullName"
                  type="text"
                  placeholder="Jane Doe"
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  disabled={isLoading}
                  required
                />
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-mono font-medium text-[#A6B4C9]" htmlFor="userName">
                  Username
                </label>
                <Input
                  id="userName"
                  type="text"
                  placeholder="jdoe"
                  value={userName}
                  onChange={(e) => setUserName(e.target.value)}
                  disabled={isLoading}
                  required
                />
              </div>
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-mono font-medium text-[#A6B4C9]" htmlFor="email">
                Work Email
              </label>
              <Input
                id="email"
                type="email"
                placeholder="jane@company.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={isLoading}
                required
              />
            </div>

            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div className="space-y-1.5">
                <label className="text-xs font-mono font-medium text-[#A6B4C9]" htmlFor="password">
                  Password
                </label>
                <div className="relative">
                  <Input
                    id="password"
                    type={showPassword ? "text" : "password"}
                    placeholder="••••••••••••"
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

              <div className="space-y-1.5">
                <label className="text-xs font-mono font-medium text-[#A6B4C9]" htmlFor="confirmPassword">
                  Confirm Password
                </label>
                <Input
                  id="confirmPassword"
                  type={showPassword ? "text" : "password"}
                  placeholder="••••••••••••"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  disabled={isLoading}
                  required
                />
              </div>
            </div>

            {/* Password Requirements Guidance */}
            <div className="rounded bg-[#0E1017] p-3 text-xs space-y-1.5 border border-[#1E2333] font-mono">
              <p className="font-semibold text-[#F0F3F9] text-[11px]">Password requirements:</p>
              <div className="grid grid-cols-2 gap-1 text-[#758498] text-[11px]">
                <span className={`flex items-center gap-1 ${hasLength ? "text-[#34D399] font-medium" : ""}`}>
                  <CheckCircle2 className="h-3 w-3" /> Min 8 characters
                </span>
                <span className={`flex items-center gap-1 ${hasUpper ? "text-[#34D399] font-medium" : ""}`}>
                  <CheckCircle2 className="h-3 w-3" /> Uppercase letter
                </span>
                <span className={`flex items-center gap-1 ${hasNumber ? "text-[#34D399] font-medium" : ""}`}>
                  <CheckCircle2 className="h-3 w-3" /> Number (0-9)
                </span>
                <span className={`flex items-center gap-1 ${hasSpecial ? "text-[#34D399] font-medium" : ""}`}>
                  <CheckCircle2 className="h-3 w-3" /> Special character (!@#)
                </span>
              </div>
              {confirmPassword && (
                <p className={passwordsMatch ? "text-[#34D399] font-medium mt-1 text-[11px]" : "text-[#F43F5E] font-medium mt-1 text-[11px]"}>
                  {passwordsMatch ? "✓ Passwords match" : "✕ Passwords do not match"}
                </p>
              )}
            </div>

            <div className="pt-2">
              <Button
                type="submit"
                className="w-full h-9 bg-[#00E5FF] hover:bg-[#33EBFF] text-[#08090D] font-bold font-mono text-xs rounded transition-all cursor-pointer"
                disabled={isLoading}
              >
                {isLoading && <Loader2 className="h-3.5 w-3.5 mr-2 animate-spin" />}
                {isLoading ? "Creating Account..." : "Create Account"}
              </Button>
            </div>

            <div className="text-center text-xs text-[#758498] pt-2 border-t border-[#1E2333]/60">
              Already have an account?{" "}
              <Link to="/login" className="font-semibold text-[#00E5FF] hover:underline font-mono">
                Sign in
              </Link>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
