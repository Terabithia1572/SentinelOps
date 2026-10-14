import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useTenant } from "../context/tenant-context";
import {
  getOnboardingStatus,
  provisionOnboardingWorkspace,
  verifyFirstTelemetry,
  completeOnboarding,
  type OnboardingStatusDto,
  type OnboardingProvisionedResponse,
  type FirstTelemetryVerificationResponse
} from "../lib/api-client";
import {
  Building2,
  Server,
  Key,
  Activity,
  CheckCircle2,
  ArrowRight,
  ArrowLeft,
  Copy,
  Check,
  RefreshCw,
  Sparkles,
  ShieldAlert,
  Code2
} from "lucide-react";

export const OnboardingPage: React.FC = () => {
  const navigate = useNavigate();
  const { refreshOrganizations, setActiveOrg } = useTenant();

  const [currentStep, setCurrentStep] = useState<number>(1);
  const [loading, setLoading] = useState<boolean>(true);
  const [submitting, setSubmitting] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  // Form Fields
  const [orgName, setOrgName] = useState("");
  const [orgSlug, setOrgSlug] = useState("");
  const [planTier, setPlanTier] = useState<"Starter" | "Pro" | "Enterprise">("Starter");
  const [appName, setAppName] = useState("");
  const [appDisplayName, setAppDisplayName] = useState("");
  const [envName, setEnvName] = useState("Production");

  // Provisioned Data
  const [provisioned, setProvisioned] = useState<OnboardingProvisionedResponse | null>(null);
  const [copiedKey, setCopiedKey] = useState(false);
  const [activeSnippetTab, setActiveSnippetTab] = useState<"csharp" | "nodejs" | "python" | "collector">("csharp");

  // Telemetry Verification State
  const [verifying, setVerifying] = useState(false);
  const [telemetryResult, setTelemetryResult] = useState<FirstTelemetryVerificationResponse | null>(null);

  useEffect(() => {
    loadOnboardingState();
  }, []);

  const loadOnboardingState = async () => {
    try {
      setLoading(true);
      setError(null);
      const status: OnboardingStatusDto = await getOnboardingStatus();
      if (status.isCompleted) {
        // Already completed
        navigate("/overview");
        return;
      }
      if (status.organizationName) {
        setOrgName(status.organizationName);
      }
      if (status.applicationName) {
        setAppName(status.applicationName);
      }
    } catch (err: any) {
      console.warn("Could not load previous onboarding state:", err);
    } finally {
      setLoading(false);
    }
  };

  const handleOrgSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!orgName.trim()) {
      setError("Please enter an organization name.");
      return;
    }
    setError(null);
    setCurrentStep(2);
  };

  const handleAppSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!appName.trim()) {
      setError("Please enter a service / application name.");
      return;
    }

    try {
      setSubmitting(true);
      setError(null);

      const res = await provisionOnboardingWorkspace({
        organizationName: orgName.trim(),
        organizationSlug: orgSlug.trim() || undefined,
        planTier,
        applicationName: appName.trim(),
        applicationDisplayName: appDisplayName.trim() || undefined,
        environmentName: envName.trim() || "Production"
      });

      setProvisioned(res);
      await refreshOrganizations();
      setActiveOrg({
        id: res.organizationId,
        name: res.organizationName,
        slug: res.organizationSlug,
        description: null,
        isActive: true,
        userRole: "Owner",
        membersCount: 1,
        applicationsCount: 1,
        createdAtUtc: new Date().toISOString()
      });

      setCurrentStep(3);
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || "Failed to provision workspace.");
    } finally {
      setSubmitting(false);
    }
  };

  const handleCopyApiKey = () => {
    if (!provisioned?.rawApiKey) return;
    navigator.clipboard.writeText(provisioned.rawApiKey);
    setCopiedKey(true);
    setTimeout(() => setCopiedKey(false), 3000);
  };

  const handleCheckTelemetry = async () => {
    if (!provisioned?.applicationId) return;
    try {
      setVerifying(true);
      setError(null);
      const result = await verifyFirstTelemetry(provisioned.applicationId);
      setTelemetryResult(result);
      if (result.hasReceivedTelemetry) {
        setTimeout(() => {
          setCurrentStep(5);
        }, 1500);
      }
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || "Failed to check telemetry status.");
    } finally {
      setVerifying(false);
    }
  };

  const handleComplete = async () => {
    try {
      setSubmitting(true);
      await completeOnboarding();
      await refreshOrganizations();
      navigate("/overview");
    } catch (err: any) {
      navigate("/overview");
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="flex h-screen items-center justify-center bg-slate-950 text-slate-100">
        <RefreshCw className="h-8 w-8 animate-spin text-indigo-500" />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100">
      {/* Top Header */}
      <header className="border-b border-slate-800 bg-slate-900/60 px-6 py-4 backdrop-blur">
        <div className="mx-auto flex max-w-5xl items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-tr from-indigo-600 to-purple-600 text-white font-bold shadow-lg shadow-indigo-500/20">
              SO
            </div>
            <div>
              <h1 className="text-lg font-bold tracking-tight">SentinelOps Setup</h1>
              <p className="text-xs text-slate-400">Enterprise Observability Self-Service Onboarding</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <span className="rounded-full bg-indigo-500/10 px-3 py-1 text-xs font-medium text-indigo-400 border border-indigo-500/20">
              Step {currentStep} of 5
            </span>
          </div>
        </div>
      </header>

      {/* Main Container */}
      <main className="mx-auto max-w-4xl px-4 py-8">
        {/* Step Progress Bar */}
        <div className="mb-8">
          <div className="grid grid-cols-5 gap-2 text-center text-xs font-medium">
            <div className={`flex flex-col items-center gap-1.5 ${currentStep >= 1 ? "text-indigo-400" : "text-slate-500"}`}>
              <div className={`flex h-8 w-8 items-center justify-center rounded-full border text-xs font-semibold ${
                currentStep > 1 ? "border-indigo-500 bg-indigo-500 text-white" : currentStep === 1 ? "border-indigo-500 bg-indigo-500/20 text-indigo-300" : "border-slate-800 bg-slate-900 text-slate-500"
              }`}>
                {currentStep > 1 ? <Check className="h-4 w-4" /> : "1"}
              </div>
              <span>Workspace</span>
            </div>

            <div className={`flex flex-col items-center gap-1.5 ${currentStep >= 2 ? "text-indigo-400" : "text-slate-500"}`}>
              <div className={`flex h-8 w-8 items-center justify-center rounded-full border text-xs font-semibold ${
                currentStep > 2 ? "border-indigo-500 bg-indigo-500 text-white" : currentStep === 2 ? "border-indigo-500 bg-indigo-500/20 text-indigo-300" : "border-slate-800 bg-slate-900 text-slate-500"
              }`}>
                {currentStep > 2 ? <Check className="h-4 w-4" /> : "2"}
              </div>
              <span>Service</span>
            </div>

            <div className={`flex flex-col items-center gap-1.5 ${currentStep >= 3 ? "text-indigo-400" : "text-slate-500"}`}>
              <div className={`flex h-8 w-8 items-center justify-center rounded-full border text-xs font-semibold ${
                currentStep > 3 ? "border-indigo-500 bg-indigo-500 text-white" : currentStep === 3 ? "border-indigo-500 bg-indigo-500/20 text-indigo-300" : "border-slate-800 bg-slate-900 text-slate-500"
              }`}>
                {currentStep > 3 ? <Check className="h-4 w-4" /> : "3"}
              </div>
              <span>API Key</span>
            </div>

            <div className={`flex flex-col items-center gap-1.5 ${currentStep >= 4 ? "text-indigo-400" : "text-slate-500"}`}>
              <div className={`flex h-8 w-8 items-center justify-center rounded-full border text-xs font-semibold ${
                currentStep > 4 ? "border-indigo-500 bg-indigo-500 text-white" : currentStep === 4 ? "border-indigo-500 bg-indigo-500/20 text-indigo-300" : "border-slate-800 bg-slate-900 text-slate-500"
              }`}>
                {currentStep > 4 ? <Check className="h-4 w-4" /> : "4"}
              </div>
              <span>Verify Telemetry</span>
            </div>

            <div className={`flex flex-col items-center gap-1.5 ${currentStep >= 5 ? "text-indigo-400" : "text-slate-500"}`}>
              <div className={`flex h-8 w-8 items-center justify-center rounded-full border text-xs font-semibold ${
                currentStep === 5 ? "border-emerald-500 bg-emerald-500 text-white" : "border-slate-800 bg-slate-900 text-slate-500"
              }`}>
                5
              </div>
              <span>Ready</span>
            </div>
          </div>
        </div>

        {error && (
          <div className="mb-6 flex items-center gap-3 rounded-lg border border-rose-500/30 bg-rose-500/10 p-4 text-sm text-rose-300">
            <ShieldAlert className="h-5 w-5 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        {/* STEP 1: Workspace & Plan Selection */}
        {currentStep === 1 && (
          <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-6 shadow-xl backdrop-blur">
            <div className="mb-6">
              <h2 className="text-xl font-bold text-white flex items-center gap-2">
                <Building2 className="h-5 w-5 text-indigo-400" />
                Name your Organization & Select a Plan
              </h2>
              <p className="text-sm text-slate-400 mt-1">
                An organization creates an isolated multi-tenant boundary for your applications, team members, and data retention policies.
              </p>
            </div>

            <form onSubmit={handleOrgSubmit} className="space-y-6">
              <div>
                <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-2">
                  Organization Name *
                </label>
                <input
                  type="text"
                  value={orgName}
                  onChange={(e) => {
                    setOrgName(e.target.value);
                    if (!orgSlug) {
                      setOrgSlug(e.target.value.toLowerCase().replace(/\s+/g, "-"));
                    }
                  }}
                  placeholder="Acme Operations, TechCorp, DevSquad"
                  className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-2.5 text-sm text-slate-100 placeholder:text-slate-500 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-2">
                  Organization Slug (URL identifier)
                </label>
                <div className="flex rounded-lg border border-slate-700 bg-slate-950 text-sm">
                  <span className="flex items-center px-3 text-slate-500 border-r border-slate-800 select-none">
                    sentinelops.local/org/
                  </span>
                  <input
                    type="text"
                    value={orgSlug}
                    onChange={(e) => setOrgSlug(e.target.value.toLowerCase().replace(/\s+/g, "-"))}
                    placeholder="acme-ops"
                    className="w-full bg-transparent px-3 py-2.5 text-slate-100 placeholder:text-slate-500 focus:outline-none"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-3">
                  Choose Initial Subscription Tier (14-day trial)
                </label>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  {/* Starter Plan Card */}
                  <div
                    onClick={() => setPlanTier("Starter")}
                    className={`cursor-pointer rounded-lg border p-4 transition-all ${
                      planTier === "Starter"
                        ? "border-indigo-500 bg-indigo-950/20 shadow-md ring-1 ring-indigo-500"
                        : "border-slate-800 bg-slate-950/60 hover:border-slate-700"
                    }`}
                  >
                    <div className="flex items-center justify-between">
                      <span className="font-semibold text-white">Starter</span>
                      <span className="text-xs font-semibold text-emerald-400 bg-emerald-500/10 px-2 py-0.5 rounded">Free</span>
                    </div>
                    <p className="text-xs text-slate-400 mt-2">Perfect for side projects and local evaluation.</p>
                    <ul className="text-xs text-slate-300 mt-3 space-y-1">
                      <li>• 3 Applications</li>
                      <li>• 100k Logs / mo</li>
                      <li>• 7 Days Retention</li>
                    </ul>
                  </div>

                  {/* Pro Plan Card */}
                  <div
                    onClick={() => setPlanTier("Pro")}
                    className={`cursor-pointer rounded-lg border p-4 transition-all ${
                      planTier === "Pro"
                        ? "border-indigo-500 bg-indigo-950/20 shadow-md ring-1 ring-indigo-500"
                        : "border-slate-800 bg-slate-950/60 hover:border-slate-700"
                    }`}
                  >
                    <div className="flex items-center justify-between">
                      <span className="font-semibold text-white">Professional</span>
                      <span className="text-xs font-semibold text-indigo-400 bg-indigo-500/10 px-2 py-0.5 rounded">Demo $49/mo</span>
                    </div>
                    <p className="text-xs text-slate-400 mt-2">Distributed tracing and AI anomaly detection.</p>
                    <ul className="text-xs text-slate-300 mt-3 space-y-1">
                      <li>• 20 Applications</li>
                      <li>• 1M Logs & 500k Traces</li>
                      <li>• 30 Days Retention</li>
                    </ul>
                  </div>

                  {/* Enterprise Plan Card */}
                  <div
                    onClick={() => setPlanTier("Enterprise")}
                    className={`cursor-pointer rounded-lg border p-4 transition-all ${
                      planTier === "Enterprise"
                        ? "border-indigo-500 bg-indigo-950/20 shadow-md ring-1 ring-indigo-500"
                        : "border-slate-800 bg-slate-950/60 hover:border-slate-700"
                    }`}
                  >
                    <div className="flex items-center justify-between">
                      <span className="font-semibold text-white">Enterprise</span>
                      <span className="text-xs font-semibold text-purple-400 bg-purple-500/10 px-2 py-0.5 rounded">Demo $249/mo</span>
                    </div>
                    <p className="text-xs text-slate-400 mt-2">Unlimited scaling with predictive ops & SLA.</p>
                    <ul className="text-xs text-slate-300 mt-3 space-y-1">
                      <li>• 100 Applications</li>
                      <li>• 10M Logs & 5M Traces</li>
                      <li>• 90 Days Retention</li>
                    </ul>
                  </div>
                </div>
              </div>

              <div className="flex justify-end pt-4">
                <button
                  type="submit"
                  className="flex items-center gap-2 rounded-lg bg-indigo-600 px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-indigo-600/30 hover:bg-indigo-500 transition-colors"
                >
                  Continue to Service Setup
                  <ArrowRight className="h-4 w-4" />
                </button>
              </div>
            </form>
          </div>
        )}

        {/* STEP 2: Application / Service Setup */}
        {currentStep === 2 && (
          <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-6 shadow-xl backdrop-blur">
            <div className="mb-6">
              <h2 className="text-xl font-bold text-white flex items-center gap-2">
                <Server className="h-5 w-5 text-indigo-400" />
                Register your First Application / Microservice
              </h2>
              <p className="text-sm text-slate-400 mt-1">
                Tell us which service or API you want to monitor. You can add more services later at any time.
              </p>
            </div>

            <form onSubmit={handleAppSubmit} className="space-y-6">
              <div>
                <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-2">
                  Service Technical Name *
                </label>
                <input
                  type="text"
                  value={appName}
                  onChange={(e) => setAppName(e.target.value.toLowerCase().replace(/\s+/g, "-"))}
                  placeholder="order-service, auth-api, billing-worker"
                  className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-2.5 text-sm text-slate-100 placeholder:text-slate-500 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  required
                />
                <span className="text-xs text-slate-500 mt-1 block">Used as the OpenTelemetry `service.name` resource attribute.</span>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-2">
                  Display Name
                </label>
                <input
                  type="text"
                  value={appDisplayName}
                  onChange={(e) => setAppDisplayName(e.target.value)}
                  placeholder="Order Processing Gateway"
                  className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-2.5 text-sm text-slate-100 placeholder:text-slate-500 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-300 uppercase tracking-wider mb-2">
                  Initial Environment
                </label>
                <select
                  value={envName}
                  onChange={(e) => setEnvName(e.target.value)}
                  className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-2.5 text-sm text-slate-100 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                >
                  <option value="Production">Production</option>
                  <option value="Staging">Staging</option>
                  <option value="Development">Development</option>
                </select>
              </div>

              <div className="flex items-center justify-between pt-4">
                <button
                  type="button"
                  onClick={() => setCurrentStep(1)}
                  className="flex items-center gap-2 rounded-lg border border-slate-700 px-4 py-2 text-sm text-slate-300 hover:bg-slate-800 transition-colors"
                >
                  <ArrowLeft className="h-4 w-4" />
                  Back
                </button>

                <button
                  type="submit"
                  disabled={submitting}
                  className="flex items-center gap-2 rounded-lg bg-indigo-600 px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-indigo-600/30 hover:bg-indigo-500 transition-colors disabled:opacity-50"
                >
                  {submitting ? (
                    <>
                      <RefreshCw className="h-4 w-4 animate-spin" />
                      Provisioning Workspace...
                    </>
                  ) : (
                    <>
                      Provision Workspace & Key
                      <ArrowRight className="h-4 w-4" />
                    </>
                  )}
                </button>
              </div>
            </form>
          </div>
        )}

        {/* STEP 3: API Key & SDK Snippets */}
        {currentStep === 3 && provisioned && (
          <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-6 shadow-xl backdrop-blur">
            <div className="mb-6">
              <h2 className="text-xl font-bold text-white flex items-center gap-2">
                <Key className="h-5 w-5 text-indigo-400" />
                Copy your Telemetry Ingestion Key
              </h2>
              <p className="text-sm text-slate-400 mt-1">
                This API key authorizes telemetry ingestion for <strong>{provisioned.applicationName}</strong> into <strong>{provisioned.organizationName}</strong>.
              </p>
            </div>

            {/* Amber Safety Disclaimer */}
            <div className="mb-6 flex items-start gap-3 rounded-lg border border-amber-500/30 bg-amber-500/10 p-4 text-xs text-amber-200">
              <ShieldAlert className="h-5 w-5 shrink-0 text-amber-400" />
              <div>
                <p className="font-semibold text-amber-300">Security Warning: Copy-Once Display</p>
                <p className="mt-0.5">
                  For tenant security, SentinelOps hashes API keys at rest. You will not be able to view this raw key again after leaving this page.
                </p>
              </div>
            </div>

            {/* Copy Key Box */}
            <div className="mb-6 rounded-lg border border-indigo-500/30 bg-indigo-950/20 p-4">
              <div className="flex items-center justify-between">
                <div>
                  <span className="text-xs uppercase font-semibold text-indigo-300 tracking-wider">Raw Ingestion Key</span>
                  <p className="mt-1 font-mono text-sm font-bold text-white break-all">{provisioned.rawApiKey}</p>
                </div>
                <button
                  onClick={handleCopyApiKey}
                  className="flex items-center gap-1.5 rounded-lg bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-500 transition-colors shrink-0 ml-4"
                >
                  {copiedKey ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                  {copiedKey ? "Copied!" : "Copy Key"}
                </button>
              </div>
            </div>

            {/* SDK Code Snippets */}
            <div className="mb-6">
              <div className="flex items-center justify-between border-b border-slate-800 pb-2 mb-3">
                <span className="text-xs font-semibold text-slate-300 uppercase tracking-wider flex items-center gap-1.5">
                  <Code2 className="h-4 w-4 text-indigo-400" />
                  Integration Snippets
                </span>
                <div className="flex gap-1 text-xs">
                  <button
                    onClick={() => setActiveSnippetTab("csharp")}
                    className={`rounded px-2.5 py-1 ${activeSnippetTab === "csharp" ? "bg-indigo-600 text-white font-medium" : "text-slate-400 hover:text-slate-200"}`}
                  >
                    .NET 10 / C#
                  </button>
                  <button
                    onClick={() => setActiveSnippetTab("nodejs")}
                    className={`rounded px-2.5 py-1 ${activeSnippetTab === "nodejs" ? "bg-indigo-600 text-white font-medium" : "text-slate-400 hover:text-slate-200"}`}
                  >
                    Node.js
                  </button>
                  <button
                    onClick={() => setActiveSnippetTab("python")}
                    className={`rounded px-2.5 py-1 ${activeSnippetTab === "python" ? "bg-indigo-600 text-white font-medium" : "text-slate-400 hover:text-slate-200"}`}
                  >
                    Python
                  </button>
                  <button
                    onClick={() => setActiveSnippetTab("collector")}
                    className={`rounded px-2.5 py-1 ${activeSnippetTab === "collector" ? "bg-indigo-600 text-white font-medium" : "text-slate-400 hover:text-slate-200"}`}
                  >
                    OTel Collector
                  </button>
                </div>
              </div>

              <div className="rounded-lg border border-slate-800 bg-slate-950 p-4 font-mono text-xs text-slate-300 overflow-x-auto max-h-64">
                <pre>
                  {activeSnippetTab === "csharp" && provisioned.sdkSnippetCSharp}
                  {activeSnippetTab === "nodejs" && provisioned.sdkSnippetNodeJs}
                  {activeSnippetTab === "python" && provisioned.sdkSnippetPython}
                  {activeSnippetTab === "collector" && provisioned.otelCollectorYaml}
                </pre>
              </div>
            </div>

            <div className="flex justify-end pt-4">
              <button
                type="button"
                onClick={() => setCurrentStep(4)}
                className="flex items-center gap-2 rounded-lg bg-indigo-600 px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-indigo-600/30 hover:bg-indigo-500 transition-colors"
              >
                Proceed to Telemetry Verification
                <ArrowRight className="h-4 w-4" />
              </button>
            </div>
          </div>
        )}

        {/* STEP 4: First Telemetry Verification */}
        {currentStep === 4 && provisioned && (
          <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-6 shadow-xl backdrop-blur">
            <div className="mb-6">
              <h2 className="text-xl font-bold text-white flex items-center gap-2">
                <Activity className="h-5 w-5 text-indigo-400" />
                Listening for Telemetry from {provisioned.applicationName}
              </h2>
              <p className="text-sm text-slate-400 mt-1">
                Start your instrumented service and perform an API request. SentinelOps will automatically detect inbound logs, traces, or metrics.
              </p>
            </div>

            <div className="my-8 flex flex-col items-center justify-center rounded-xl border border-dashed border-slate-700 bg-slate-950/60 p-8 text-center">
              {telemetryResult?.hasReceivedTelemetry ? (
                <div className="flex flex-col items-center gap-3">
                  <div className="flex h-14 w-14 items-center justify-center rounded-full bg-emerald-500/10 text-emerald-400 border border-emerald-500/30">
                    <CheckCircle2 className="h-8 w-8" />
                  </div>
                  <h3 className="text-lg font-bold text-white">Telemetry Received Successfully!</h3>
                  <div className="flex gap-4 text-xs text-slate-300 mt-2">
                    <span className="rounded bg-slate-800 px-3 py-1 font-mono">Logs: {telemetryResult.logsCount}</span>
                    <span className="rounded bg-slate-800 px-3 py-1 font-mono">Traces: {telemetryResult.tracesCount}</span>
                    <span className="rounded bg-slate-800 px-3 py-1 font-mono">Metrics: {telemetryResult.metricsCount}</span>
                  </div>
                </div>
              ) : (
                <div className="flex flex-col items-center gap-3">
                  <div className="flex h-14 w-14 items-center justify-center rounded-full bg-indigo-500/10 text-indigo-400 border border-indigo-500/30 animate-pulse">
                    <Activity className="h-8 w-8" />
                  </div>
                  <h3 className="text-base font-semibold text-white">Awaiting Inbound Telemetry</h3>
                  <p className="max-w-md text-xs text-slate-400">
                    Run your application locally or send synthetic telemetry using the TelemetryGenerator CLI tool:
                  </p>
                  <code className="rounded bg-slate-900 px-3 py-1.5 font-mono text-xs text-indigo-300 border border-slate-800">
                    dotnet run --project tools/SentinelOps.TelemetryGenerator -- --api-key {provisioned.rawApiKey}
                  </code>
                </div>
              )}
            </div>

            <div className="flex items-center justify-between pt-4">
              <button
                type="button"
                onClick={() => setCurrentStep(5)}
                className="text-xs text-slate-400 hover:text-slate-200 underline"
              >
                Skip verification & test later
              </button>

              <div className="flex gap-3">
                <button
                  type="button"
                  onClick={handleCheckTelemetry}
                  disabled={verifying}
                  className="flex items-center gap-2 rounded-lg border border-indigo-500/30 bg-indigo-950/30 px-4 py-2.5 text-sm font-semibold text-indigo-300 hover:bg-indigo-900/40 transition-colors disabled:opacity-50"
                >
                  <RefreshCw className={`h-4 w-4 ${verifying ? "animate-spin" : ""}`} />
                  {verifying ? "Checking..." : "Check Telemetry Now"}
                </button>

                {telemetryResult?.hasReceivedTelemetry && (
                  <button
                    type="button"
                    onClick={() => setCurrentStep(5)}
                    className="flex items-center gap-2 rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-emerald-500 transition-colors shadow-lg shadow-emerald-600/20"
                  >
                    Finish Onboarding
                    <ArrowRight className="h-4 w-4" />
                  </button>
                )}
              </div>
            </div>
          </div>
        )}

        {/* STEP 5: Completion & Launch */}
        {currentStep === 5 && (
          <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-8 shadow-xl backdrop-blur text-center">
            <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-emerald-500/10 text-emerald-400 border border-emerald-500/30 mb-4">
              <Sparkles className="h-8 w-8" />
            </div>

            <h2 className="text-2xl font-bold text-white">Onboarding Complete!</h2>
            <p className="mx-auto max-w-lg text-sm text-slate-400 mt-2">
              Your organization and application are provisioned and ready. You have 14 days remaining in your free trial.
            </p>

            <div className="mx-auto max-w-sm rounded-lg border border-slate-800 bg-slate-950 p-4 my-6 text-left text-xs space-y-2">
              <div className="flex justify-between">
                <span className="text-slate-500">Organization:</span>
                <span className="font-semibold text-white">{orgName}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-500">Application:</span>
                <span className="font-semibold text-white">{appName}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-500">Plan Tier:</span>
                <span className="font-semibold text-indigo-400">{planTier}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-500">Trial Period:</span>
                <span className="font-semibold text-emerald-400">14 Days Active</span>
              </div>
            </div>

            <div className="flex justify-center gap-4">
              <button
                onClick={handleComplete}
                disabled={submitting}
                className="flex items-center gap-2 rounded-lg bg-gradient-to-r from-indigo-600 to-purple-600 px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-indigo-600/30 hover:opacity-90 transition-opacity"
              >
                Go to Operations Dashboard
                <ArrowRight className="h-4 w-4" />
              </button>
            </div>
          </div>
        )}
      </main>
    </div>
  );
};
export default OnboardingPage;
