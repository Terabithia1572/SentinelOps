import { useState, useEffect } from "react";
import {
  CheckCircle2,
  AlertTriangle,
  Download,
  ArrowUpRight,
  RefreshCw,
  FileText,
  UserCheck,
  Trash2,
  Info,
  ExternalLink
} from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Dialog } from "@/components/ui/dialog";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { useTenant } from "@/context/tenant-context";
import { useAuth } from "@/context/auth-context";
import {
  getSubscription,
  getPlans,
  changePlan,
  cancelSubscription,
  reactivateSubscription,
  getInvoices,
  getTenantUsage,
  getEntitlements,
  createCheckoutSession,
  createCustomerPortalSession,
  transferOwnership,
  exportTenantData,
  deleteOrganizationAccount,
  getOrganizationMembers,
  type OrganizationSubscriptionDto,
  type SubscriptionPlanDto,
  type SaaSInvoiceDto,
  type TenantUsageSummaryDto,
  type TenantEntitlementsDto,
  type OrganizationMemberDto
} from "@/lib/api-client";

type TabType = "overview" | "plans" | "usage" | "invoices" | "governance";

export function BillingPage() {
  const { activeOrg } = useTenant();
  const { user } = useAuth();

  const [activeTab, setActiveTab] = useState<TabType>("overview");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // Data states
  const [subscription, setSubscription] = useState<OrganizationSubscriptionDto | null>(null);
  const [plans, setPlans] = useState<SubscriptionPlanDto[]>([]);
  const [invoices, setInvoices] = useState<SaaSInvoiceDto[]>([]);
  const [usage, setUsage] = useState<TenantUsageSummaryDto | null>(null);
  const [entitlements, setEntitlements] = useState<TenantEntitlementsDto | null>(null);
  const [members, setMembers] = useState<OrganizationMemberDto[]>([]);

  // Dialog/Modal states
  const [selectedInvoice, setSelectedInvoice] = useState<SaaSInvoiceDto | null>(null);
  const [showPlanModal, setShowPlanModal] = useState<SubscriptionPlanDto | null>(null);
  const [showCancelModal, setShowCancelModal] = useState(false);
  const [cancelImmediately, setCancelImmediately] = useState(false);
  const [actionLoading, setActionLoading] = useState(false);

  // Governance action states
  const [selectedNewOwner, setSelectedNewOwner] = useState<string>("");
  const [showTransferModal, setShowTransferModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [deleteConfirmationPhrase, setDeleteConfirmationPhrase] = useState("");

  const isOwner = activeOrg?.userRole === "Owner";
  const isAdminOrOwner = isOwner || activeOrg?.userRole === "Admin";

  const loadAllData = async () => {
    if (!activeOrg) return;
    try {
      setLoading(true);
      setError(null);
      const [subData, plansData, invoicesData, usageData, entData, membersData] = await Promise.all([
        getSubscription().catch(() => null),
        getPlans().catch(() => []),
        getInvoices().catch(() => []),
        getTenantUsage().catch(() => null),
        getEntitlements().catch(() => null),
        getOrganizationMembers(activeOrg.id).catch(() => [])
      ]);

      setSubscription(subData);
      setPlans(plansData);
      setInvoices(invoicesData);
      setUsage(usageData);
      setEntitlements(entData);
      setMembers(membersData);
    } catch (err: any) {
      setError(err?.message || "Failed to load billing and subscription information.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadAllData();
  }, [activeOrg?.id]);

  const handlePlanChange = async (plan: SubscriptionPlanDto) => {
    try {
      setActionLoading(true);
      setError(null);
      await changePlan({ newPlanName: plan.name, billingInterval: "Monthly" });
      setSuccessMessage(`Plan successfully updated to ${plan.name}.`);
      setShowPlanModal(null);
      await loadAllData();
    } catch (err: any) {
      setError(err?.response?.data?.message || err?.message || "Failed to change subscription plan.");
    } finally {
      setActionLoading(false);
    }
  };

  const handleCancelSubscription = async () => {
    try {
      setActionLoading(true);
      setError(null);
      await cancelSubscription({ cancelImmediately });
      setSuccessMessage(
        cancelImmediately
          ? "Subscription canceled immediately."
          : "Subscription scheduled for cancellation at the end of the billing period."
      );
      setShowCancelModal(false);
      await loadAllData();
    } catch (err: any) {
      setError(err?.response?.data?.message || err?.message || "Failed to cancel subscription.");
    } finally {
      setActionLoading(false);
    }
  };

  const handleReactivateSubscription = async () => {
    try {
      setActionLoading(true);
      setError(null);
      await reactivateSubscription();
      setSuccessMessage("Subscription reactivated successfully.");
      await loadAllData();
    } catch (err: any) {
      setError(err?.response?.data?.message || err?.message || "Failed to reactivate subscription.");
    } finally {
      setActionLoading(false);
    }
  };

  const handleLaunchCheckout = async (planName: string) => {
    try {
      setActionLoading(true);
      const res = await createCheckoutSession({ planName, interval: "Monthly" });
      setSuccessMessage(`Checkout session generated (${res.sessionId}). Redirecting to deterministic sandbox gateway...`);
      // Deterministic simulation
      setTimeout(() => {
        window.open(res.checkoutUrl, "_blank");
      }, 500);
    } catch (err: any) {
      setError(err?.response?.data?.message || err?.message || "Failed to initialize checkout.");
    } finally {
      setActionLoading(false);
    }
  };

  const handleLaunchCustomerPortal = async () => {
    try {
      setActionLoading(true);
      const res = await createCustomerPortalSession();
      setSuccessMessage(`Opening customer billing portal (${res.provider})...`);
      setTimeout(() => {
        window.open(res.portalUrl, "_blank");
      }, 500);
    } catch (err: any) {
      setError(err?.response?.data?.message || err?.message || "Failed to open billing portal.");
    } finally {
      setActionLoading(false);
    }
  };

  const handleTransferOwnership = async () => {
    if (!activeOrg || !selectedNewOwner) return;
    try {
      setActionLoading(true);
      setError(null);
      await transferOwnership(activeOrg.id, selectedNewOwner);
      setSuccessMessage("Ownership transferred successfully. Your role has been updated to Admin.");
      setShowTransferModal(false);
      await loadAllData();
    } catch (err: any) {
      setError(err?.response?.data?.message || err?.message || "Failed to transfer organization ownership.");
    } finally {
      setActionLoading(false);
    }
  };

  const handleExportData = async () => {
    if (!activeOrg) return;
    try {
      setActionLoading(true);
      setError(null);
      const data = await exportTenantData(activeOrg.id);
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: "application/json" });
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `sentinelops-export-${activeOrg.slug || activeOrg.id}-${new Date().toISOString().slice(0, 10)}.json`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
      setSuccessMessage("Tenant data archive exported successfully.");
    } catch (err: any) {
      setError(err?.response?.data?.message || err?.message || "Failed to export tenant data.");
    } finally {
      setActionLoading(false);
    }
  };

  const handleDeleteAccount = async () => {
    if (!activeOrg) return;
    try {
      setActionLoading(true);
      setError(null);
      await deleteOrganizationAccount(activeOrg.id, deleteConfirmationPhrase);
      setSuccessMessage("Organization account has been soft-deleted. Redirecting to workspace overview...");
      setShowDeleteModal(false);
      setTimeout(() => {
        window.location.href = "/organizations";
      }, 1500);
    } catch (err: any) {
      setError(err?.response?.data?.message || err?.message || "Failed to delete organization account.");
    } finally {
      setActionLoading(false);
    }
  };

  const getStatusBadge = (status?: string) => {
    switch (status) {
      case "Active":
        return <Badge className="bg-emerald-500/10 text-emerald-500 border-emerald-500/20">Active</Badge>;
      case "Trialing":
        return <Badge className="bg-blue-500/10 text-blue-500 border-blue-500/20">Trial Period</Badge>;
      case "PastDue":
        return <Badge className="bg-amber-500/10 text-amber-500 border-amber-500/20">Payment Past Due</Badge>;
      case "GracePeriod":
        return <Badge className="bg-orange-500/10 text-orange-500 border-orange-500/20">Grace Period (7 Days)</Badge>;
      case "Suspended":
        return <Badge className="bg-rose-500/10 text-rose-500 border-rose-500/20">Suspended</Badge>;
      case "Canceled":
        return <Badge className="bg-gray-500/10 text-gray-400 border-gray-500/20">Canceled</Badge>;
      default:
        return <Badge variant="outline">{status || "Unknown"}</Badge>;
    }
  };

  const getUsageBar = (label: string, used: number, quota: number, pct: number, isWarning: boolean, isExceeded: boolean) => {
    const isInfinity = quota <= 0;
    const barWidth = isInfinity ? 0 : Math.min(pct, 100);
    const colorClass = isExceeded
      ? "bg-rose-500"
      : isWarning
      ? "bg-amber-500"
      : "bg-[hsl(var(--primary))]";

    return (
      <div className="space-y-1.5">
        <div className="flex items-center justify-between text-xs">
          <span className="font-medium text-[hsl(var(--foreground))]">{label}</span>
          <div className="flex items-center gap-2">
            <span className="text-[hsl(var(--muted-foreground))]">
              {used.toLocaleString()} / {isInfinity ? "Unlimited" : quota.toLocaleString()}
            </span>
            {isExceeded && (
              <Badge className="bg-rose-500/10 text-rose-500 text-[10px] px-1 py-0 h-4 border-rose-500/20">
                100% Exceeded
              </Badge>
            )}
            {!isExceeded && isWarning && (
              <Badge className="bg-amber-500/10 text-amber-500 text-[10px] px-1 py-0 h-4 border-amber-500/20">
                80% Quota Warning
              </Badge>
            )}
          </div>
        </div>
        <div className="h-2 w-full rounded-full bg-[hsl(var(--muted)/0.3)] overflow-hidden">
          <div
            className={`h-full transition-all duration-300 rounded-full ${colorClass}`}
            style={{ width: `${barWidth}%` }}
          />
        </div>
      </div>
    );
  };

  return (
    <div className="container mx-auto space-y-6 p-6 max-w-7xl">
      {/* Sandbox Demo Alert Banner */}
      <div className="rounded-lg border border-blue-500/30 bg-blue-500/10 p-4 text-xs text-blue-400 flex items-start gap-3">
        <Info className="h-4 w-4 shrink-0 mt-0.5 text-blue-400" />
        <div>
          <span className="font-semibold text-blue-300">Deterministic SaaS Sandbox Environment: </span>
          All payment flows, Stripe checkout sessions, customer billing portals, and invoice settlements are simulated
          via authorized test providers. No real payment credentials or live charges are ever stored or processed.
        </div>
      </div>

      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-[hsl(var(--foreground))]">
            Subscription & SaaS Management
          </h1>
          <p className="text-sm text-[hsl(var(--muted-foreground))] mt-1">
            Manage plans, billing details, quotas, invoices, and tenant account governance for{" "}
            <span className="font-semibold text-[hsl(var(--foreground))]">{activeOrg?.name}</span>.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={loadAllData}
            disabled={loading}
            className="h-9 gap-1.5"
          >
            <RefreshCw className={`h-3.5 w-3.5 ${loading ? "animate-spin" : ""}`} />
            Refresh
          </Button>
          {isAdminOrOwner && (
            <Button
              size="sm"
              onClick={handleLaunchCustomerPortal}
              disabled={actionLoading}
              className="h-9 gap-1.5"
            >
              <ExternalLink className="h-3.5 w-3.5" />
              Customer Billing Portal
            </Button>
          )}
        </div>
      </div>

      {/* Notifications */}
      {error && (
        <div className="rounded-lg border border-rose-500/30 bg-rose-500/10 p-3 text-xs text-rose-400 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <AlertTriangle className="h-4 w-4 shrink-0" />
            <span>{error}</span>
          </div>
          <button onClick={() => setError(null)} className="text-rose-400 hover:text-rose-300">
            &times;
          </button>
        </div>
      )}

      {successMessage && (
        <div className="rounded-lg border border-emerald-500/30 bg-emerald-500/10 p-3 text-xs text-emerald-400 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <CheckCircle2 className="h-4 w-4 shrink-0" />
            <span>{successMessage}</span>
          </div>
          <button onClick={() => setSuccessMessage(null)} className="text-emerald-400 hover:text-emerald-300">
            &times;
          </button>
        </div>
      )}

      {/* Navigation Tabs */}
      <div className="flex border-b border-[hsl(var(--border))] space-x-6 overflow-x-auto text-sm">
        <button
          onClick={() => setActiveTab("overview")}
          className={`pb-3 font-medium transition-colors border-b-2 whitespace-nowrap ${
            activeTab === "overview"
              ? "border-[hsl(var(--primary))] text-[hsl(var(--primary))]"
              : "border-transparent text-[hsl(var(--muted-foreground))] hover:text-[hsl(var(--foreground))]"
          }`}
        >
          Subscription Overview
        </button>
        <button
          onClick={() => setActiveTab("plans")}
          className={`pb-3 font-medium transition-colors border-b-2 whitespace-nowrap ${
            activeTab === "plans"
              ? "border-[hsl(var(--primary))] text-[hsl(var(--primary))]"
              : "border-transparent text-[hsl(var(--muted-foreground))] hover:text-[hsl(var(--foreground))]"
          }`}
        >
          Plans & Pricing
        </button>
        <button
          onClick={() => setActiveTab("usage")}
          className={`pb-3 font-medium transition-colors border-b-2 whitespace-nowrap ${
            activeTab === "usage"
              ? "border-[hsl(var(--primary))] text-[hsl(var(--primary))]"
              : "border-transparent text-[hsl(var(--muted-foreground))] hover:text-[hsl(var(--foreground))]"
          }`}
        >
          Usage & Quotas
        </button>
        <button
          onClick={() => setActiveTab("invoices")}
          className={`pb-3 font-medium transition-colors border-b-2 whitespace-nowrap ${
            activeTab === "invoices"
              ? "border-[hsl(var(--primary))] text-[hsl(var(--primary))]"
              : "border-transparent text-[hsl(var(--muted-foreground))] hover:text-[hsl(var(--foreground))]"
          }`}
        >
          Invoices & History ({invoices.length})
        </button>
        <button
          onClick={() => setActiveTab("governance")}
          className={`pb-3 font-medium transition-colors border-b-2 whitespace-nowrap ${
            activeTab === "governance"
              ? "border-[hsl(var(--primary))] text-[hsl(var(--primary))]"
              : "border-transparent text-[hsl(var(--muted-foreground))] hover:text-[hsl(var(--foreground))]"
          }`}
        >
          Governance & Security
        </button>
      </div>

      {/* TAB 1: OVERVIEW */}
      {activeTab === "overview" && (
        <div className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {/* Current Plan Card */}
            <Card className="md:col-span-2">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <div>
                  <CardTitle className="text-base font-semibold">Active Subscription</CardTitle>
                  <CardDescription className="text-xs">
                    Current entitlement tier and billing cadence
                  </CardDescription>
                </div>
                {getStatusBadge(subscription?.status)}
              </CardHeader>
              <CardContent className="space-y-4 pt-2">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between p-4 rounded-lg bg-[hsl(var(--accent)/0.3)] border border-[hsl(var(--border))] gap-4">
                  <div>
                    <div className="text-xs text-[hsl(var(--muted-foreground))] uppercase font-semibold">
                      Current Plan
                    </div>
                    <div className="text-2xl font-bold text-[hsl(var(--foreground))] mt-0.5">
                      {subscription?.planName || "Starter"}
                    </div>
                    <div className="text-xs text-[hsl(var(--muted-foreground))] mt-1">
                      Billing interval: {subscription?.billingInterval || "Monthly"}
                    </div>
                  </div>

                  <div className="flex flex-wrap items-center gap-2">
                    {isAdminOrOwner && (
                      <Button
                        size="sm"
                        onClick={() => setActiveTab("plans")}
                        className="gap-1 text-xs"
                      >
                        <ArrowUpRight className="h-3.5 w-3.5" />
                        Upgrade / Change Plan
                      </Button>
                    )}
                    {subscription?.cancelAtPeriodEnd ? (
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={handleReactivateSubscription}
                        disabled={actionLoading}
                        className="text-xs text-emerald-500 border-emerald-500/30 hover:bg-emerald-500/10"
                      >
                        Reactivate Subscription
                      </Button>
                    ) : (
                      isAdminOrOwner &&
                      subscription?.status !== "Canceled" && (
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => setShowCancelModal(true)}
                          className="text-xs text-rose-400 border-rose-500/30 hover:bg-rose-500/10"
                        >
                          Cancel Subscription
                        </Button>
                      )
                    )}
                  </div>
                </div>

                <div className="grid grid-cols-2 sm:grid-cols-3 gap-4 text-xs pt-2">
                  <div>
                    <span className="text-[hsl(var(--muted-foreground))]">Current Period Start</span>
                    <p className="font-medium mt-0.5">
                      {subscription?.currentPeriodStartUtc
                        ? new Date(subscription.currentPeriodStartUtc).toLocaleDateString()
                        : "—"}
                    </p>
                  </div>
                  <div>
                    <span className="text-[hsl(var(--muted-foreground))]">Renewal / End Date</span>
                    <p className="font-medium mt-0.5">
                      {subscription?.currentPeriodEndUtc
                        ? new Date(subscription.currentPeriodEndUtc).toLocaleDateString()
                        : "—"}
                    </p>
                  </div>
                  <div>
                    <span className="text-[hsl(var(--muted-foreground))]">Auto-Renew</span>
                    <p className="font-medium mt-0.5">
                      {subscription?.cancelAtPeriodEnd ? (
                        <span className="text-amber-500">Cancels at period end</span>
                      ) : (
                        <span className="text-emerald-500">Enabled</span>
                      )}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Quick Usage Summary */}
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-base font-semibold">Monthly Quotas</CardTitle>
                <CardDescription className="text-xs">
                  Resource consumption for current period
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4 pt-2">
                {usage ? (
                  <>
                    {getUsageBar(
                      "Logs Ingested",
                      usage.logsUsed,
                      usage.logsQuota,
                      usage.logsPercentUsed,
                      usage.isLogsWarning,
                      usage.isLogsExceeded
                    )}
                    {getUsageBar(
                      "Traces Ingested",
                      usage.tracesUsed,
                      usage.tracesQuota,
                      usage.tracesPercentUsed,
                      usage.isTracesWarning,
                      usage.isTracesExceeded
                    )}
                    {getUsageBar(
                      "AI Tokens",
                      usage.aiTokensUsed,
                      usage.aiTokensQuota,
                      usage.aiTokensPercentUsed,
                      usage.isAiTokensWarning,
                      usage.isAiTokensExceeded
                    )}
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setActiveTab("usage")}
                      className="w-full text-xs text-[hsl(var(--primary))] hover:bg-[hsl(var(--primary)/0.1)] mt-2"
                    >
                      View All Quotas & Limits &rarr;
                    </Button>
                  </>
                ) : (
                  <div className="py-6 text-center text-xs text-[hsl(var(--muted-foreground))]">
                    Loading usage data...
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </div>
      )}

      {/* TAB 2: PLANS & PRICING */}
      {activeTab === "plans" && (
        <div className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {plans.map((p) => {
              const isCurrent = (subscription?.planName || "Starter").toLowerCase() === p.name.toLowerCase();
              return (
                <Card
                  key={p.id}
                  className={`relative flex flex-col justify-between transition-all ${
                    isCurrent
                      ? "border-[hsl(var(--primary))] shadow-sm shadow-[hsl(var(--primary)/0.2)] bg-[hsl(var(--card))]"
                      : "border-[hsl(var(--border))]"
                  }`}
                >
                  {isCurrent && (
                    <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                      <Badge className="bg-[hsl(var(--primary))] text-white font-medium text-xs px-3">
                        Active Plan
                      </Badge>
                    </div>
                  )}

                  <CardHeader className="pt-6">
                    <CardTitle className="text-xl font-bold">{p.name}</CardTitle>
                    <CardDescription className="text-xs min-h-[32px]">{p.description}</CardDescription>
                    <div className="pt-4 flex items-baseline gap-1">
                      <span className="text-3xl font-extrabold text-[hsl(var(--foreground))]">
                        ${p.monthlyPrice}
                      </span>
                      <span className="text-xs text-[hsl(var(--muted-foreground))]">/ month</span>
                    </div>
                  </CardHeader>

                  <CardContent className="space-y-4 flex-1">
                    <div className="space-y-2 border-t border-[hsl(var(--border))] pt-4 text-xs">
                      <div className="flex items-center gap-2">
                        <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500 shrink-0" />
                        <span>Up to {p.maxApplications} applications</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500 shrink-0" />
                        <span>{p.maxMembers} team seats included</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500 shrink-0" />
                        <span>{(p.maxMonthlyLogs / 1000).toLocaleString()}k logs / month</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500 shrink-0" />
                        <span>{(p.maxMonthlyTraces / 1000).toLocaleString()}k traces / month</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500 shrink-0" />
                        <span>{(p.maxMonthlyAiTokens / 1000).toLocaleString()}k AI Copilot tokens</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500 shrink-0" />
                        <span>{p.dataRetentionDays} days data retention</span>
                      </div>
                      {p.allowCustomReports && (
                        <div className="flex items-center gap-2">
                          <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500 shrink-0" />
                          <span>Custom analytics & SLA reporting</span>
                        </div>
                      )}
                      {p.allowPrioritySupport && (
                        <div className="flex items-center gap-2">
                          <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500 shrink-0" />
                          <span>24/7 Priority SLA support</span>
                        </div>
                      )}
                    </div>
                  </CardContent>

                  <div className="p-6 pt-0">
                    {isCurrent ? (
                      <Button disabled className="w-full text-xs" variant="outline">
                        Current Plan
                      </Button>
                    ) : (
                      <div className="flex gap-2">
                        <Button
                          onClick={() => setShowPlanModal(p)}
                          className="flex-1 text-xs gap-1.5"
                          disabled={!isAdminOrOwner || actionLoading}
                        >
                          Switch to {p.name}
                        </Button>
                        <Button
                          variant="outline"
                          onClick={() => handleLaunchCheckout(p.name)}
                          className="text-xs px-2.5"
                          disabled={actionLoading}
                          title="Simulate Sandbox Checkout Session"
                        >
                          <ExternalLink className="h-3.5 w-3.5" />
                        </Button>
                      </div>
                    )}
                  </div>
                </Card>
              );
            })}
          </div>
        </div>
      )}

      {/* TAB 3: USAGE & ENTITLEMENTS */}
      {activeTab === "usage" && (
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="text-base font-semibold">Tenant Capability Matrix & Limits</CardTitle>
              <CardDescription className="text-xs">
                Real-time usage measurements and server-side hard quotas for active billing cycle
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              {usage ? (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div className="space-y-4">
                    <h3 className="text-xs font-semibold text-[hsl(var(--muted-foreground))] uppercase">
                      Core Resources
                    </h3>
                    {getUsageBar(
                      "Monitored Applications",
                      usage.applicationsUsed,
                      usage.applicationsQuota,
                      usage.applicationsPercentUsed,
                      usage.applicationsPercentUsed >= 80,
                      usage.applicationsPercentUsed >= 100
                    )}
                    {getUsageBar(
                      "Team Member Seats",
                      usage.membersUsed,
                      usage.membersQuota,
                      usage.membersPercentUsed,
                      usage.membersPercentUsed >= 80,
                      usage.membersPercentUsed >= 100
                    )}
                    {getUsageBar(
                      "Custom Dashboards",
                      usage.dashboardsUsed,
                      usage.dashboardsQuota,
                      usage.dashboardsPercentUsed,
                      usage.dashboardsPercentUsed >= 80,
                      usage.dashboardsPercentUsed >= 100
                    )}
                  </div>

                  <div className="space-y-4">
                    <h3 className="text-xs font-semibold text-[hsl(var(--muted-foreground))] uppercase">
                      Monthly Ingestion Quotas
                    </h3>
                    {getUsageBar(
                      "Logs Ingestion",
                      usage.logsUsed,
                      usage.logsQuota,
                      usage.logsPercentUsed,
                      usage.isLogsWarning,
                      usage.isLogsExceeded
                    )}
                    {getUsageBar(
                      "Traces Ingestion",
                      usage.tracesUsed,
                      usage.tracesQuota,
                      usage.tracesPercentUsed,
                      usage.isTracesWarning,
                      usage.isTracesExceeded
                    )}
                    {getUsageBar(
                      "AI Copilot Tokens",
                      usage.aiTokensUsed,
                      usage.aiTokensQuota,
                      usage.aiTokensPercentUsed,
                      usage.isAiTokensWarning,
                      usage.isAiTokensExceeded
                    )}
                  </div>

                  {entitlements && (
                    <div className="border-t border-[hsl(var(--border))] pt-4 col-span-1 md:col-span-2">
                      <h3 className="text-xs font-semibold text-[hsl(var(--muted-foreground))] uppercase mb-3">
                        Entitlements & Feature Availability
                      </h3>
                      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs">
                        <div className="p-2.5 rounded bg-[hsl(var(--muted)/0.2)] border border-[hsl(var(--border))]">
                          <span className="text-[hsl(var(--muted-foreground))] block">AI Copilot Analysis</span>
                          <span className={entitlements.canExecuteAiAnalysis ? "text-emerald-500 font-medium" : "text-gray-400"}>
                            {entitlements.canExecuteAiAnalysis ? "Enabled" : "Disabled"}
                          </span>
                        </div>
                        <div className="p-2.5 rounded bg-[hsl(var(--muted)/0.2)] border border-[hsl(var(--border))]">
                          <span className="text-[hsl(var(--muted-foreground))] block">Custom Reports</span>
                          <span className={entitlements.allowCustomReports ? "text-emerald-500 font-medium" : "text-gray-400"}>
                            {entitlements.allowCustomReports ? "Enabled" : "Disabled"}
                          </span>
                        </div>
                        <div className="p-2.5 rounded bg-[hsl(var(--muted)/0.2)] border border-[hsl(var(--border))]">
                          <span className="text-[hsl(var(--muted-foreground))] block">API Key Access</span>
                          <span className={entitlements.allowApiAccess ? "text-emerald-500 font-medium" : "text-gray-400"}>
                            {entitlements.allowApiAccess ? "Enabled" : "Disabled"}
                          </span>
                        </div>
                        <div className="p-2.5 rounded bg-[hsl(var(--muted)/0.2)] border border-[hsl(var(--border))]">
                          <span className="text-[hsl(var(--muted-foreground))] block">Data Retention</span>
                          <span className="font-medium text-[hsl(var(--foreground))]">
                            {entitlements.dataRetentionDays} Days
                          </span>
                        </div>
                      </div>
                    </div>
                  )}
                </div>
              ) : (
                <div className="py-6 text-center text-xs text-[hsl(var(--muted-foreground))]">
                  No usage data recorded for this organization.
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      )}

      {/* TAB 4: INVOICES & HISTORY */}
      {activeTab === "invoices" && (
        <div className="space-y-6">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <div>
                <CardTitle className="text-base font-semibold">Invoices & Billing History</CardTitle>
                <CardDescription className="text-xs">
                  Tax-compliant itemized invoices and settlement records
                </CardDescription>
              </div>
            </CardHeader>
            <CardContent>
              {loading ? (
                <div className="space-y-2 p-4">
                  <Skeleton className="h-8 w-full rounded" />
                  <Skeleton className="h-8 w-full rounded" />
                  <Skeleton className="h-8 w-full rounded" />
                </div>
              ) : invoices.length === 0 ? (
                <EmptyState
                  icon={FileText}
                  title="No Invoices Generated"
                  description="No invoices or billing transactions have been generated yet for this account."
                />
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-left text-xs">
                    <thead>
                      <tr className="border-b border-[hsl(var(--border))] text-[hsl(var(--muted-foreground))]">
                        <th className="py-3 px-4 font-semibold">Invoice #</th>
                        <th className="py-3 px-4 font-semibold">Date</th>
                        <th className="py-3 px-4 font-semibold">Billing Period</th>
                        <th className="py-3 px-4 font-semibold">Amount</th>
                        <th className="py-3 px-4 font-semibold">Tax</th>
                        <th className="py-3 px-4 font-semibold">Status</th>
                        <th className="py-3 px-4 font-semibold text-right">Action</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-[hsl(var(--border))]">
                      {invoices.map((inv) => (
                        <tr key={inv.id} className="hover:bg-[hsl(var(--muted)/0.2)]">
                          <td className="py-3 px-4 font-mono font-medium">{inv.invoiceNumber}</td>
                          <td className="py-3 px-4">
                            {new Date(inv.createdAtUtc).toLocaleDateString()}
                          </td>
                          <td className="py-3 px-4 text-[hsl(var(--muted-foreground))]">
                            {new Date(inv.periodStartUtc).toLocaleDateString()} -{" "}
                            {new Date(inv.periodEndUtc).toLocaleDateString()}
                          </td>
                          <td className="py-3 px-4 font-medium">
                            ${inv.total.toFixed(2)} {inv.currency}
                          </td>
                          <td className="py-3 px-4 text-[hsl(var(--muted-foreground))]">
                            ${inv.taxAmount.toFixed(2)} ({inv.taxRatePercent}%)
                          </td>
                          <td className="py-3 px-4">
                            {inv.status === "Paid" ? (
                              <Badge className="bg-emerald-500/10 text-emerald-500 border-emerald-500/20 text-[10px]">
                                Paid
                              </Badge>
                            ) : inv.status === "Open" ? (
                              <Badge className="bg-amber-500/10 text-amber-500 border-amber-500/20 text-[10px]">
                                Open
                              </Badge>
                            ) : (
                              <Badge variant="outline" className="text-[10px]">
                                {inv.status}
                              </Badge>
                            )}
                          </td>
                          <td className="py-3 px-4 text-right">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => setSelectedInvoice(inv)}
                              className="h-7 text-xs px-2"
                            >
                              Details
                            </Button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      )}

      {/* TAB 5: GOVERNANCE & SECURITY */}
      {activeTab === "governance" && (
        <div className="space-y-6">
          {/* Ownership Transfer */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base font-semibold">Ownership Transfer</CardTitle>
              <CardDescription className="text-xs">
                Transfer primary account ownership to another active team member in this organization.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {isOwner ? (
                <div className="flex flex-col sm:flex-row items-start sm:items-center gap-4">
                  <div className="flex-1 w-full max-w-sm">
                    <select
                      aria-label="New Organization Owner"
                      value={selectedNewOwner}
                      onChange={(e) => setSelectedNewOwner(e.target.value)}
                      className="w-full bg-[hsl(var(--background))] border border-[hsl(var(--border))] text-[hsl(var(--foreground))] text-xs rounded-md px-3 py-2 focus:outline-none focus:ring-1 focus:ring-[hsl(var(--primary))]"
                    >
                      <option value="">Select new owner from members...</option>
                      {members
                        .filter((m) => m.userId !== user?.id)
                        .map((m) => (
                          <option key={m.userId} value={m.userId}>
                            {m.fullName || m.userName} ({m.email}) - {m.role}
                          </option>
                        ))}
                    </select>
                  </div>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={!selectedNewOwner || actionLoading}
                    onClick={() => setShowTransferModal(true)}
                    className="text-xs gap-1.5"
                  >
                    <UserCheck className="h-3.5 w-3.5" />
                    Transfer Ownership
                  </Button>
                </div>
              ) : (
                <p className="text-xs text-[hsl(var(--muted-foreground))]">
                  Only the current organization Owner can transfer account ownership.
                </p>
              )}
            </CardContent>
          </Card>

          {/* GDPR Tenant Data Export */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base font-semibold">Tenant Data Portability & GDPR Export</CardTitle>
              <CardDescription className="text-xs">
                Download a complete, machine-readable JSON archive of all tenant metadata, settings, team members, applications, alert rules, custom dashboards, and audit logs.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Button
                variant="outline"
                size="sm"
                onClick={handleExportData}
                disabled={actionLoading || !isAdminOrOwner}
                className="text-xs gap-1.5"
              >
                <Download className="h-3.5 w-3.5" />
                Export Tenant Data Archive (.json)
              </Button>
            </CardContent>
          </Card>

          {/* Danger Zone: Account Deletion */}
          <Card className="border-rose-500/30">
            <CardHeader>
              <CardTitle className="text-base font-semibold text-rose-500">Danger Zone: Account Deletion</CardTitle>
              <CardDescription className="text-xs">
                Soft-delete this organization account, immediately terminate all active subscriptions, deactivate monitored applications, and revoke pending invitations.
              </CardDescription>
            </CardHeader>
            <CardContent>
              {isOwner ? (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowDeleteModal(true)}
                  className="text-xs text-rose-400 border-rose-500/40 hover:bg-rose-500/10 gap-1.5"
                >
                  <Trash2 className="h-3.5 w-3.5" />
                  Delete Organization Account
                </Button>
              ) : (
                <p className="text-xs text-[hsl(var(--muted-foreground))]">
                  Only the organization Owner can delete this organization account.
                </p>
              )}
            </CardContent>
          </Card>
        </div>
      )}

      {/* MODAL: CHANGE PLAN CONFIRMATION */}
      <ConfirmDialog
        open={!!showPlanModal}
        onOpenChange={(open) => !open && setShowPlanModal(null)}
        title="Confirm Plan Change"
        description={`Are you sure you want to switch to the ${showPlanModal?.name} plan? New pricing: $${showPlanModal?.monthlyPrice}/mo. Your quota limits will be immediately updated to ${showPlanModal?.maxApplications} applications, ${showPlanModal?.maxMembers} seats, and ${(showPlanModal ? showPlanModal.maxMonthlyLogs / 1000 : 0).toLocaleString()}k logs/mo.`}
        confirmLabel="Confirm & Switch"
        variant="default"
        isLoading={actionLoading}
        onConfirm={() => {
          if (showPlanModal) handlePlanChange(showPlanModal);
        }}
      />

      {/* MODAL: CANCEL SUBSCRIPTION CONFIRMATION */}
      <Dialog
        open={showCancelModal}
        onOpenChange={setShowCancelModal}
        title="Cancel Subscription"
        description="Please select how you would like your subscription canceled."
      >
        <div className="space-y-4 text-xs">
          <div className="space-y-2">
            <label className="flex items-center gap-2 cursor-pointer text-slate-200">
              <input
                type="radio"
                name="cancelTiming"
                checked={!cancelImmediately}
                onChange={() => setCancelImmediately(false)}
                className="accent-[hsl(var(--primary))]"
              />
              <span>Cancel at end of current period (Keep access until renewal)</span>
            </label>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="radio"
                name="cancelTiming"
                checked={cancelImmediately}
                onChange={() => setCancelImmediately(true)}
                className="accent-rose-500"
              />
              <span className="text-rose-400">Cancel immediately (Cease quota access now)</span>
            </label>
          </div>
          <div className="flex justify-end gap-2 pt-3 border-t border-[hsl(var(--border))]">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowCancelModal(false)}
              disabled={actionLoading}
            >
              Keep Subscription
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={handleCancelSubscription}
              disabled={actionLoading}
              className="text-rose-400 border-rose-500/40 hover:bg-rose-500/10"
            >
              {actionLoading ? "Processing..." : "Confirm Cancellation"}
            </Button>
          </div>
        </div>
      </Dialog>

      {/* MODAL: OWNERSHIP TRANSFER CONFIRMATION */}
      <Dialog
        open={showTransferModal}
        onOpenChange={setShowTransferModal}
        title="Confirm Ownership Transfer"
        description="This action will promote the selected member to Owner and demote your account to Admin."
      >
        <div className="space-y-4 text-xs">
          <p className="text-[hsl(var(--muted-foreground))]">
            You will retain Admin access but will no longer be able to delete the organization or re-assign ownership.
          </p>
          <div className="flex justify-end gap-2 pt-3 border-t border-[hsl(var(--border))]">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowTransferModal(false)}
              disabled={actionLoading}
            >
              Cancel
            </Button>
            <Button
              size="sm"
              onClick={handleTransferOwnership}
              disabled={actionLoading}
            >
              {actionLoading ? "Transferring..." : "Confirm Transfer"}
            </Button>
          </div>
        </div>
      </Dialog>

      {/* MODAL: DELETE ACCOUNT CONFIRMATION */}
      <Dialog
        open={showDeleteModal}
        onOpenChange={setShowDeleteModal}
        title="Danger: Soft-Delete Organization Account"
        description="This is a guarded, audited deletion workflow. Type the phrase below to confirm:"
      >
        <div className="space-y-4 text-xs">
          <div className="p-2.5 rounded bg-[hsl(var(--muted)/0.3)] border border-[hsl(var(--border))] font-mono text-[11px] select-all text-slate-200">
            CONFIRM DELETE {activeOrg?.name}
          </div>
          <Input
            placeholder={`CONFIRM DELETE ${activeOrg?.name}`}
            value={deleteConfirmationPhrase}
            onChange={(e) => setDeleteConfirmationPhrase(e.target.value)}
            className="text-xs"
          />
          <div className="flex justify-end gap-2 pt-3 border-t border-[hsl(var(--border))]">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowDeleteModal(false)}
              disabled={actionLoading}
            >
              Cancel
            </Button>
            <Button
              size="sm"
              onClick={handleDeleteAccount}
              disabled={
                deleteConfirmationPhrase.trim() !== `CONFIRM DELETE ${activeOrg?.name}` || actionLoading
              }
              className="bg-rose-600 hover:bg-rose-700 text-white"
            >
              {actionLoading ? "Deleting..." : "Permanently Delete"}
            </Button>
          </div>
        </div>
      </Dialog>

      {/* MODAL: INVOICE DETAILS */}
      {selectedInvoice && (
        <Dialog
          open={!!selectedInvoice}
          onOpenChange={(open) => !open && setSelectedInvoice(null)}
          title={`Invoice ${selectedInvoice.invoiceNumber}`}
          description="Itemized tax-compliant invoice breakdown"
          className="max-w-lg"
        >
          <div className="space-y-4 text-xs">
            <div className="flex items-center justify-between pb-1 border-b border-[hsl(var(--border))]">
              <span className="text-[hsl(var(--muted-foreground))]">Status:</span>
              <Badge className="bg-emerald-500/10 text-emerald-500 border-emerald-500/20 text-xs">
                {selectedInvoice.status}
              </Badge>
            </div>

            <div className="grid grid-cols-2 gap-2 text-[hsl(var(--muted-foreground))]">
              <div>Date: {new Date(selectedInvoice.createdAtUtc).toLocaleDateString()}</div>
              <div>Currency: {selectedInvoice.currency}</div>
              <div>
                Period: {new Date(selectedInvoice.periodStartUtc).toLocaleDateString()} -{" "}
                {new Date(selectedInvoice.periodEndUtc).toLocaleDateString()}
              </div>
              <div>Due Date: {new Date(selectedInvoice.dueDateUtc).toLocaleDateString()}</div>
            </div>

            <div className="border rounded-md border-[hsl(var(--border))] divide-y divide-[hsl(var(--border))]">
              <div className="grid grid-cols-12 p-2 font-semibold text-[hsl(var(--muted-foreground))] bg-[hsl(var(--muted)/0.2)]">
                <div className="col-span-8">Description</div>
                <div className="col-span-2 text-right">Qty</div>
                <div className="col-span-2 text-right">Amount</div>
              </div>
              {selectedInvoice.lineItems && selectedInvoice.lineItems.length > 0 ? (
                selectedInvoice.lineItems.map((item) => (
                  <div key={item.id} className="grid grid-cols-12 p-2">
                    <div className="col-span-8">{item.description}</div>
                    <div className="col-span-2 text-right">{item.quantity}</div>
                    <div className="col-span-2 text-right font-medium">
                      ${item.amount.toFixed(2)}
                    </div>
                  </div>
                ))
              ) : (
                <div className="p-2 text-center text-[hsl(var(--muted-foreground))]">
                  Standard Subscription Tier Billing
                </div>
              )}
            </div>

            <div className="space-y-1 text-right pt-2">
              <div className="flex justify-between text-[hsl(var(--muted-foreground))]">
                <span>Subtotal:</span>
                <span>${selectedInvoice.subtotal.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-[hsl(var(--muted-foreground))]">
                <span>Tax ({selectedInvoice.taxRatePercent}%):</span>
                <span>${selectedInvoice.taxAmount.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-sm font-bold text-[hsl(var(--foreground))] pt-1 border-t border-[hsl(var(--border))]">
                <span>Total Amount Paid:</span>
                <span>${selectedInvoice.total.toFixed(2)} {selectedInvoice.currency}</span>
              </div>
            </div>

            <div className="flex justify-end pt-3 border-t border-[hsl(var(--border))]">
              <Button size="sm" variant="outline" onClick={() => setSelectedInvoice(null)}>
                Close
              </Button>
            </div>
          </div>
        </Dialog>
      )}
    </div>
  );
}
