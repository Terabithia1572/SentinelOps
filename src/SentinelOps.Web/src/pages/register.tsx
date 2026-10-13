import React, { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/context/auth-context";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
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
    <div className="flex min-h-screen items-center justify-center bg-[hsl(var(--background))] px-4 py-12">
      <div className="w-full max-w-lg space-y-6">
        <div className="flex flex-col items-center text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-[hsl(var(--primary)/0.15)] ring-1 ring-[hsl(var(--primary)/0.3)] mb-3">
            <Shield className="h-7 w-7 text-[hsl(var(--primary))]" />
          </div>
          <h1 className="text-2xl font-bold tracking-tight">SentinelOps</h1>
          <p className="text-sm text-[hsl(var(--muted-foreground))]">
            Create your account to start managing telemetry & incidents
          </p>
        </div>

        <Card className="border-[hsl(var(--border))] shadow-lg">
          <CardHeader className="space-y-1">
            <CardTitle className="text-xl">Register Account</CardTitle>
            <CardDescription>
              Set up your SentinelOps user profile and initial role assignment
            </CardDescription>
          </CardHeader>
          <form onSubmit={handleSubmit}>
            <CardContent className="space-y-4">
              {error && (
                <div className="flex items-start gap-2 rounded-md border border-rose-500/20 bg-rose-500/10 p-3 text-sm text-rose-400">
                  <AlertCircle className="h-4 w-4 shrink-0 mt-0.5" />
                  <span>{error}</span>
                </div>
              )}

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <label className="text-sm font-medium leading-none" htmlFor="fullName">
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

                <div className="space-y-2">
                  <label className="text-sm font-medium leading-none" htmlFor="userName">
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

              <div className="space-y-2">
                <label className="text-sm font-medium leading-none" htmlFor="email">
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

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <label className="text-sm font-medium leading-none" htmlFor="password">
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
                      className="absolute right-3 top-1/2 -translate-y-1/2 text-[hsl(var(--muted-foreground))] hover:text-[hsl(var(--foreground))]"
                      onClick={() => setShowPassword(!showPassword)}
                      tabIndex={-1}
                    >
                      {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                    </button>
                  </div>
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-medium leading-none" htmlFor="confirmPassword">
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
              <div className="rounded-md bg-[hsl(var(--accent)/0.5)] p-3 text-xs space-y-1.5 border border-[hsl(var(--border))]">
                <p className="font-semibold text-[hsl(var(--foreground))]">Password requirements:</p>
                <div className="grid grid-cols-2 gap-1 text-[hsl(var(--muted-foreground))]">
                  <span className={`flex items-center gap-1 ${hasLength ? "text-emerald-400 font-medium" : ""}`}>
                    <CheckCircle2 className="h-3 w-3" /> Min 8 characters
                  </span>
                  <span className={`flex items-center gap-1 ${hasUpper ? "text-emerald-400 font-medium" : ""}`}>
                    <CheckCircle2 className="h-3 w-3" /> Uppercase letter
                  </span>
                  <span className={`flex items-center gap-1 ${hasNumber ? "text-emerald-400 font-medium" : ""}`}>
                    <CheckCircle2 className="h-3 w-3" /> Number (0-9)
                  </span>
                  <span className={`flex items-center gap-1 ${hasSpecial ? "text-emerald-400 font-medium" : ""}`}>
                    <CheckCircle2 className="h-3 w-3" /> Special character (!@#)
                  </span>
                </div>
                {confirmPassword && (
                  <p className={passwordsMatch ? "text-emerald-400 font-medium mt-1" : "text-rose-400 font-medium mt-1"}>
                    {passwordsMatch ? "✓ Passwords match" : "✕ Passwords do not match"}
                  </p>
                )}
              </div>
            </CardContent>

            <CardFooter className="flex flex-col space-y-4">
              <Button type="submit" className="w-full gap-2" disabled={isLoading}>
                {isLoading && <Loader2 className="h-4 w-4 animate-spin" />}
                {isLoading ? "Creating Account..." : "Create Account"}
              </Button>

              <div className="text-center text-xs text-[hsl(var(--muted-foreground))]">
                Already have an account?{" "}
                <Link to="/login" className="font-semibold text-[hsl(var(--primary))] hover:underline">
                  Sign in
                </Link>
              </div>
            </CardFooter>
          </form>
        </Card>
      </div>
    </div>
  );
}
