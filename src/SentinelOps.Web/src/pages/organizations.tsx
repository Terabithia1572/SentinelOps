import React, { useState, useEffect } from "react";
import {
  useTenant
} from "../context/tenant-context";
import {
  getOrganizationById,
  createOrganization,
  inviteOrganizationMember,
  revokeOrganizationInvitation,
  removeOrganizationMember,
  changeOrganizationMemberRole,
  updateOrganizationSettings,
  getOrganizationQuota,
  updateOrganizationQuota,
  type OrganizationDetailDto,
  type InvitationCreatedResponseDto,
  type OrganizationQuotaStatusDto,
} from "../lib/api-client";
import {
  Building2,
  Users,
  UserPlus,
  Settings,
  BarChart3,
  CheckCircle2,
  Copy,
  Check,
  RefreshCw,
  Trash2,
  Zap,
  AlertTriangle,
  Sliders,
} from "lucide-react";
import { Dialog } from "@/components/ui/dialog";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";

export const OrganizationsPage: React.FC = () => {
  const { organizations, activeOrg, setActiveOrg, refreshOrganizations } = useTenant();
  const [detail, setDetail] = useState<OrganizationDetailDto | null>(null);
  const [quota, setQuota] = useState<OrganizationQuotaStatusDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [activeTab, setActiveTab] = useState<"members" | "invites" | "settings" | "usage">("members");

  // Create Org Modal
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [newOrgName, setNewOrgName] = useState("");
  const [newOrgSlug, setNewOrgSlug] = useState("");
  const [newOrgDesc, setNewOrgDesc] = useState("");
  const [createSubmitting, setCreateSubmitting] = useState(false);

  // Invite Modal
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteRole, setInviteRole] = useState("Member");
  const [inviteSubmitting, setInviteSubmitting] = useState(false);
  const [inviteResult, setInviteResult] = useState<InvitationCreatedResponseDto | null>(null);
  const [copiedToken, setCopiedToken] = useState(false);

  // Settings state
  const [allowInvites, setAllowInvites] = useState(true);
  const [require2fa, setRequire2fa] = useState(false);
  const [retentionDays, setRetentionDays] = useState(30);
  const [settingsSaving, setSettingsSaving] = useState(false);

  // Quota Modal state
  const [showQuotaModal, setShowQuotaModal] = useState(false);
  const [quotaTier, setQuotaTier] = useState("Pro");
  const [quotaMaxApps, setQuotaMaxApps] = useState(20);
  const [quotaMaxMembers, setQuotaMaxMembers] = useState(25);
  const [quotaMaxDashboards, setQuotaMaxDashboards] = useState(15);
  const [quotaMaxLogs, setQuotaMaxLogs] = useState(1000000);
  const [quotaMaxTraces, setQuotaMaxTraces] = useState(500000);
  const [quotaSaving, setQuotaSaving] = useState(false);
  const [memberToRemove, setMemberToRemove] = useState<{ id: string; name: string } | null>(null);

  // Status message
  const [statusMessage, setStatusMessage] = useState<{ text: string; type: "success" | "error" } | null>(null);

  const fetchDetail = async (orgId: string) => {
    try {
      setLoading(true);
      const data = await getOrganizationById(orgId);
      setDetail(data);
      if (data.settings) {
        setAllowInvites(data.settings.allowMemberInvites);
        setRequire2fa(data.settings.requireTwoFactor);
        setRetentionDays(data.settings.defaultRetentionDays);
        if (data.settings.planTier) setQuotaTier(data.settings.planTier);
      }
      try {
        const q = await getOrganizationQuota(orgId);
        setQuota(q);
      } catch {
        // Quota fetch fallback
      }
    } catch {
      setStatusMessage({ text: "Failed to load organization details.", type: "error" });
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateQuota = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!activeOrg) return;
    try {
      setQuotaSaving(true);
      await updateOrganizationQuota(activeOrg.id, {
        planTier: quotaTier,
        maxApplications: quotaMaxApps,
        maxMembers: quotaMaxMembers,
        maxCustomDashboards: quotaMaxDashboards,
        maxMonthlyLogs: quotaMaxLogs,
        maxMonthlyTraces: quotaMaxTraces,
      });
      const updatedQuota = await getOrganizationQuota(activeOrg.id);
      setQuota(updatedQuota);
      setShowQuotaModal(false);
      setStatusMessage({ text: "Organization quota limits successfully updated!", type: "success" });
    } catch {
      setStatusMessage({ text: "Failed to update organization quota limits.", type: "error" });
    } finally {
      setQuotaSaving(false);
    }
  };

  useEffect(() => {
    if (activeOrg) {
      fetchDetail(activeOrg.id);
    }
  }, [activeOrg?.id]);

  const handleCreateOrg = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newOrgName.trim()) return;
    try {
      setCreateSubmitting(true);
      const newOrg = await createOrganization({
        name: newOrgName.trim(),
        slug: newOrgSlug.trim() || newOrgName.trim().toLowerCase().replace(/\s+/g, "-"),
        description: newOrgDesc.trim() || undefined
      });
      await refreshOrganizations();
      setActiveOrg(newOrg);
      setShowCreateModal(false);
      setNewOrgName("");
      setNewOrgSlug("");
      setNewOrgDesc("");
      setStatusMessage({ text: `Organization "${newOrg.name}" created successfully!`, type: "success" });
    } catch {
      setStatusMessage({ text: "Failed to create organization. Slug may already be in use.", type: "error" });
    } finally {
      setCreateSubmitting(false);
    }
  };

  const handleInvite = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!activeOrg || !inviteEmail.trim()) return;
    try {
      setInviteSubmitting(true);
      const res = await inviteOrganizationMember(activeOrg.id, {
        email: inviteEmail.trim(),
        role: inviteRole
      });
      setInviteResult(res);
      setInviteEmail("");
      if (activeOrg) fetchDetail(activeOrg.id);
    } catch {
      setStatusMessage({ text: "Failed to create invitation. Email might already be member.", type: "error" });
    } finally {
      setInviteSubmitting(false);
    }
  };

  const handleRevokeInvite = async (invitationId: string) => {
    if (!activeOrg) return;
    try {
      await revokeOrganizationInvitation(activeOrg.id, invitationId);
      fetchDetail(activeOrg.id);
      setStatusMessage({ text: "Invitation revoked.", type: "success" });
    } catch {
      setStatusMessage({ text: "Failed to revoke invitation.", type: "error" });
    }
  };

  const handleRemoveMember = async (userId: string) => {
    if (!activeOrg) return;
    try {
      await removeOrganizationMember(activeOrg.id, userId);
      fetchDetail(activeOrg.id);
      setStatusMessage({ text: "Member removed from organization.", type: "success" });
    } catch {
      setStatusMessage({ text: "Failed to remove member.", type: "error" });
    }
  };

  const handleChangeRole = async (userId: string, newRole: string) => {
    if (!activeOrg) return;
    try {
      await changeOrganizationMemberRole(activeOrg.id, userId, newRole);
      fetchDetail(activeOrg.id);
      setStatusMessage({ text: "Member role updated.", type: "success" });
    } catch {
      setStatusMessage({ text: "Failed to update member role.", type: "error" });
    }
  };

  const handleSaveSettings = async () => {
    if (!activeOrg) return;
    try {
      setSettingsSaving(true);
      await updateOrganizationSettings(activeOrg.id, {
        allowMemberInvites: allowInvites,
        requireTwoFactor: require2fa,
        defaultRetentionDays: retentionDays
      });
      fetchDetail(activeOrg.id);
      setStatusMessage({ text: "Organization settings saved successfully.", type: "success" });
    } catch {
      setStatusMessage({ text: "Failed to update organization settings.", type: "error" });
    } finally {
      setSettingsSaving(false);
    }
  };

  const isOwnerOrAdmin =
    detail?.userRole === "Owner" ||
    detail?.userRole === "Admin" ||
    activeOrg?.userRole === "Owner" ||
    activeOrg?.userRole === "Admin";
  const isOwner = detail?.userRole === "Owner" || activeOrg?.userRole === "Owner";

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-white flex items-center gap-2">
            <Building2 className="w-7 h-7 text-blue-400" />
            Organizations & Multi-Tenant SaaS
          </h1>
          <p className="text-sm text-slate-400 mt-1">
            Manage isolated organizational tenants, user memberships, role assignments, and quotas.
          </p>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={() => setShowCreateModal(true)}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-500 text-white text-sm font-medium rounded-lg transition-colors shadow-sm"
          >
            <Building2 className="w-4 h-4" />
            New Organization
          </button>
        </div>
      </div>

      {/* Status banner */}
      {statusMessage && (
        <div
          className={`p-3 rounded-lg flex items-center justify-between text-sm ${
            statusMessage.type === "success"
              ? "bg-emerald-950/60 border border-emerald-800 text-emerald-200"
              : "bg-red-950/60 border border-red-800 text-red-200"
          }`}
        >
          <span>{statusMessage.text}</span>
          <button onClick={() => setStatusMessage(null)} className="text-slate-400 hover:text-white ml-2 text-xs">
            ✕
          </button>
        </div>
      )}

      {/* Active Organization Switcher Card */}
      <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 shadow-sm">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex items-center gap-4">
            <div className="w-12 h-12 rounded-xl bg-blue-600/20 border border-blue-500/30 flex items-center justify-center text-blue-400 font-bold text-lg">
              {activeOrg?.name ? activeOrg.name.slice(0, 2).toUpperCase() : "SO"}
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h2 className="text-lg font-bold text-white">{activeOrg?.name || "No Organization Selected"}</h2>
                {activeOrg && (
                  <span className="px-2 py-0.5 text-xs font-semibold rounded-full bg-blue-900/50 text-blue-300 border border-blue-700/50">
                    Role: {detail?.userRole || activeOrg.userRole || (activeOrg as any)?.role || "Member"}
                  </span>
                )}
              </div>
              <p className="text-xs text-slate-400 mt-0.5 font-mono">
                Slug: <span className="text-slate-300">{activeOrg?.slug || "—"}</span> • ID: {activeOrg?.id || "—"}
              </p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            {loading && <RefreshCw className="w-3.5 h-3.5 animate-spin text-blue-400" />}
            <label className="text-xs font-medium text-slate-400">Switch Organization:</label>
            <select
              value={activeOrg?.id || ""}
              onChange={(e) => {
                const selected = organizations.find((o) => o.id === e.target.value);
                if (selected) setActiveOrg(selected);
              }}
              className="bg-slate-800 border border-slate-700 text-slate-200 text-sm rounded-lg px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              {organizations.map((org) => (
                <option key={org.id} value={org.id}>
                  {org.name} ({org.userRole || (org as any)?.role || "Member"})
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Navigation Tabs */}
      <div className="flex border-b border-slate-800 space-x-1">
        <button
          onClick={() => setActiveTab("members")}
          className={`flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
            activeTab === "members"
              ? "border-blue-500 text-blue-400"
              : "border-transparent text-slate-400 hover:text-slate-200"
          }`}
        >
          <Users className="w-4 h-4" />
          Members ({detail?.members?.length || 0})
        </button>
        <button
          onClick={() => setActiveTab("invites")}
          className={`flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
            activeTab === "invites"
              ? "border-blue-500 text-blue-400"
              : "border-transparent text-slate-400 hover:text-slate-200"
          }`}
        >
          <UserPlus className="w-4 h-4" />
          Pending Invites ({detail?.pendingInvitations?.length || 0})
        </button>
        <button
          onClick={() => setActiveTab("settings")}
          className={`flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
            activeTab === "settings"
              ? "border-blue-500 text-blue-400"
              : "border-transparent text-slate-400 hover:text-slate-200"
          }`}
        >
          <Settings className="w-4 h-4" />
          Tenant Settings
        </button>
        <button
          onClick={() => setActiveTab("usage")}
          className={`flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
            activeTab === "usage"
              ? "border-blue-500 text-blue-400"
              : "border-transparent text-slate-400 hover:text-slate-200"
          }`}
        >
          <BarChart3 className="w-4 h-4" />
          Resource Usage
        </button>
      </div>

      {/* Tab: Members */}
      {activeTab === "members" && (
        <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden shadow-sm">
          <div className="p-4 border-b border-slate-800 flex items-center justify-between">
            <h3 className="text-base font-semibold text-white">Organization Members</h3>
            {isOwnerOrAdmin && (
              <button
                onClick={() => {
                  setInviteResult(null);
                  setShowInviteModal(true);
                }}
                className="flex items-center gap-2 px-3 py-1.5 bg-blue-600 hover:bg-blue-500 text-white text-xs font-medium rounded-lg transition-colors"
              >
                <UserPlus className="w-3.5 h-3.5" />
                Invite Member
              </button>
            )}
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm text-slate-300">
              <thead className="bg-slate-800/60 text-xs font-semibold text-slate-400 uppercase tracking-wider">
                <tr>
                  <th className="px-6 py-3">Member</th>
                  <th className="px-6 py-3">Role</th>
                  <th className="px-6 py-3">Joined Date</th>
                  {isOwner && <th className="px-6 py-3 text-right">Actions</th>}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800">
                {loading ? (
                  <tr>
                    <td colSpan={isOwner ? 4 : 3} className="p-4 space-y-2">
                      <Skeleton className="h-8 w-full rounded" />
                      <Skeleton className="h-8 w-full rounded" />
                    </td>
                  </tr>
                ) : !detail?.members || detail.members.length === 0 ? (
                  <tr>
                    <td colSpan={isOwner ? 4 : 3} className="py-6">
                      <EmptyState
                        icon={Users}
                        title="No Members"
                        description="This organization does not have any active members yet."
                      />
                    </td>
                  </tr>
                ) : (
                  detail.members.map((m) => (
                    <tr key={m.id} className="hover:bg-slate-800/30 transition-colors">
                      <td className="px-6 py-4">
                        <div>
                          <div className="font-medium text-white">{m.fullName || m.userName}</div>
                          <div className="text-xs text-slate-400">{m.email}</div>
                        </div>
                      </td>
                    <td className="px-6 py-4">
                      <span
                        className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold ${
                          m.role === "Owner"
                            ? "bg-purple-900/60 text-purple-200 border border-purple-700/50"
                            : m.role === "Admin"
                            ? "bg-blue-900/60 text-blue-200 border border-blue-700/50"
                            : "bg-slate-800 text-slate-300 border border-slate-700"
                        }`}
                      >
                        {m.role}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-xs text-slate-400">
                      {new Date(m.joinedAtUtc).toLocaleDateString()}
                    </td>
                    {isOwner && (
                      <td className="px-6 py-4 text-right">
                        <div className="flex items-center justify-end gap-2">
                          <select
                            value={m.role}
                            onChange={(e) => handleChangeRole(m.userId, e.target.value)}
                            className="bg-slate-800 border border-slate-700 text-xs rounded px-2 py-1 text-slate-200 focus:outline-none"
                          >
                            <option value="Viewer">Viewer</option>
                            <option value="Member">Member</option>
                            <option value="Admin">Admin</option>
                            <option value="Owner">Owner</option>
                          </select>
                          <button
                            onClick={() => setMemberToRemove({ id: m.userId, name: m.userName || m.email })}
                            className="p-1 hover:text-red-400 text-slate-500 transition-colors"
                            title="Remove Member"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        </div>
                      </td>
                    )}
                  </tr>
                ))
              )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Tab: Invites */}
      {activeTab === "invites" && (
        <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden shadow-sm">
          <div className="p-4 border-b border-slate-800 flex items-center justify-between">
            <h3 className="text-base font-semibold text-white">Pending Invitations</h3>
            {isOwnerOrAdmin && (
              <button
                onClick={() => {
                  setInviteResult(null);
                  setShowInviteModal(true);
                }}
                className="flex items-center gap-2 px-3 py-1.5 bg-blue-600 hover:bg-blue-500 text-white text-xs font-medium rounded-lg transition-colors"
              >
                <UserPlus className="w-3.5 h-3.5" />
                Invite New Member
              </button>
            )}
          </div>
          {(!detail?.pendingInvitations || detail.pendingInvitations.length === 0) ? (
            <div className="p-8 text-center text-slate-400 text-sm">No pending invitations.</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm text-slate-300">
                <thead className="bg-slate-800/60 text-xs font-semibold text-slate-400 uppercase tracking-wider">
                  <tr>
                    <th className="px-6 py-3">Invited Email</th>
                    <th className="px-6 py-3">Role</th>
                    <th className="px-6 py-3">Expires At</th>
                    {isOwnerOrAdmin && <th className="px-6 py-3 text-right">Actions</th>}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-800">
                  {(detail?.pendingInvitations || []).map((inv) => (
                    <tr key={inv.id} className="hover:bg-slate-800/30 transition-colors">
                      <td className="px-6 py-4 font-medium text-white">{inv.email}</td>
                      <td className="px-6 py-4">
                        <span className="px-2 py-0.5 rounded text-xs font-semibold bg-blue-900/60 text-blue-200 border border-blue-700/50">
                          {inv.role}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-xs text-slate-400">
                        {new Date(inv.expiresAtUtc).toLocaleDateString()}
                      </td>
                      {isOwnerOrAdmin && (
                        <td className="px-6 py-4 text-right">
                          <button
                            onClick={() => handleRevokeInvite(inv.id)}
                            className="text-xs text-red-400 hover:text-red-300 font-medium transition-colors"
                          >
                            Revoke
                          </button>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* Tab: Settings */}
      {activeTab === "settings" && (
        <div className="bg-slate-900 border border-slate-800 rounded-xl p-6 shadow-sm max-w-2xl space-y-6">
          <h3 className="text-base font-semibold text-white">Tenant Policy Settings</h3>
          
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-sm font-medium text-white">Allow Member Invitations</div>
                <div className="text-xs text-slate-400">Allow Organization Admins to invite new members.</div>
              </div>
              <input
                type="checkbox"
                checked={allowInvites}
                onChange={(e) => setAllowInvites(e.target.checked)}
                disabled={!isOwnerOrAdmin}
                className="w-4 h-4 rounded bg-slate-800 border-slate-700 text-blue-600 focus:ring-blue-500"
              />
            </div>

            <div className="flex items-center justify-between border-t border-slate-800 pt-4">
              <div>
                <div className="text-sm font-medium text-white">Require Two-Factor Authentication</div>
                <div className="text-xs text-slate-400">Enforce mandatory MFA for all organization members.</div>
              </div>
              <input
                type="checkbox"
                checked={require2fa}
                onChange={(e) => setRequire2fa(e.target.checked)}
                disabled={!isOwnerOrAdmin}
                className="w-4 h-4 rounded bg-slate-800 border-slate-700 text-blue-600 focus:ring-blue-500"
              />
            </div>

            <div className="border-t border-slate-800 pt-4">
              <label className="text-sm font-medium text-white block">Default Data Retention (Days): {retentionDays}</label>
              <input
                type="range"
                min="7"
                max="90"
                value={retentionDays}
                onChange={(e) => setRetentionDays(Number(e.target.value))}
                disabled={!isOwnerOrAdmin}
                className="w-full mt-2 h-2 bg-slate-800 rounded-lg appearance-none cursor-pointer"
              />
            </div>
          </div>

          {isOwnerOrAdmin && (
            <div className="pt-4 border-t border-slate-800">
              <button
                onClick={handleSaveSettings}
                disabled={settingsSaving}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-500 text-white text-sm font-medium rounded-lg transition-colors"
              >
                {settingsSaving ? "Saving..." : "Save Settings"}
              </button>
            </div>
          )}
        </div>
      )}

      {/* Tab: Usage & Quotas */}
      {activeTab === "usage" && (
        <div className="space-y-6">
          {/* Header Bar: Current Plan & Actions */}
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 rounded-xl bg-slate-900 border border-slate-800">
            <div className="flex items-center gap-3">
              <div className="p-2.5 rounded-lg bg-sky-950/60 border border-sky-800/40 text-sky-400">
                <Zap className="w-5 h-5" />
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <span className="font-bold text-base text-white">SaaS Quotas & Resource Usage</span>
                  <span className="px-2 py-0.5 rounded-full text-xs font-semibold bg-sky-500/10 text-sky-400 border border-sky-500/20">
                    {quota?.planTier || detail?.settings?.planTier || "Pro"} Plan
                  </span>
                </div>
                <p className="text-xs text-slate-400 mt-0.5">
                  Monthly allocation limits and live telemetry consumption for {detail?.name || activeOrg?.name}.
                </p>
              </div>
            </div>

            {isOwnerOrAdmin && (
              <button
                onClick={() => {
                  if (quota) {
                    setQuotaTier(quota.planTier);
                    setQuotaMaxApps(quota.applicationsQuota);
                    setQuotaMaxMembers(quota.membersQuota);
                    setQuotaMaxDashboards(quota.dashboardsQuota);
                    setQuotaMaxLogs(quota.logsQuota);
                    setQuotaMaxTraces(quota.tracesQuota);
                  }
                  setShowQuotaModal(true);
                }}
                className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-xs font-medium text-slate-200 rounded-lg border border-slate-700 transition-colors"
              >
                <Sliders className="w-3.5 h-3.5" />
                Configure Plan Quotas
              </button>
            )}
          </div>

          {/* Warning Banner if quota reached */}
          {quota && (quota.hasAnyQuotaExceeded || quota.hasAnyQuotaWarning) && (
            <div
              className={`p-4 rounded-xl border flex items-start gap-3 ${
                quota.hasAnyQuotaExceeded
                  ? "bg-rose-950/30 border-rose-500/40 text-rose-300"
                  : "bg-amber-950/30 border-amber-500/40 text-amber-300"
              }`}
            >
              <AlertTriangle className="w-5 h-5 flex-shrink-0 mt-0.5" />
              <div className="text-xs space-y-1">
                <span className="font-bold block">
                  {quota.hasAnyQuotaExceeded ? "Quota Limit Exceeded" : "Quota Warning: Approaching Capacity"}
                </span>
                <p>
                  {quota.hasAnyQuotaExceeded
                    ? "One or more organization quotas have reached 100% capacity. Additional resource creation or telemetry ingestion may be throttled."
                    : "Your organization has consumed over 80% of its monthly resource allocation. Consider upgrading your plan or adjusting limits."}
                </p>
              </div>
            </div>
          )}

          {/* Quota Progress Cards Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
            {/* 1. Monthly Logs */}
            <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 space-y-3">
              <div className="flex items-center justify-between text-xs">
                <span className="font-semibold text-slate-400 uppercase tracking-wider">Monthly Logs Ingestion</span>
                <span
                  className={`px-2 py-0.5 rounded text-[10px] font-semibold ${
                    quota?.isLogsExceeded
                      ? "bg-rose-950 text-rose-400 border border-rose-800"
                      : quota?.isLogsWarning
                      ? "bg-amber-950 text-amber-400 border border-amber-800"
                      : "bg-emerald-950 text-emerald-400 border border-emerald-800"
                  }`}
                >
                  {quota?.isLogsExceeded ? "Exceeded" : quota?.isLogsWarning ? "Warning" : "Normal"}
                </span>
              </div>
              <div className="flex items-baseline justify-between">
                <div className="text-2xl font-bold text-white">
                  {(quota?.logsUsed ?? detail?.usage?.logsIngestedMonthToDate ?? 0).toLocaleString()}
                </div>
                <div className="text-xs text-slate-400 font-mono">
                  / {(quota?.logsQuota ?? 1000000).toLocaleString()} logs
                </div>
              </div>
              <div className="w-full bg-slate-800 rounded-full h-2 overflow-hidden">
                <div
                  className={`h-2 rounded-full transition-all ${
                    quota?.isLogsExceeded
                      ? "bg-rose-500"
                      : quota?.isLogsWarning
                      ? "bg-amber-500"
                      : "bg-sky-500"
                  }`}
                  style={{ width: `${Math.min(quota?.logsPercentUsed ?? 0, 100)}%` }}
                />
              </div>
              <div className="flex justify-between text-[11px] text-slate-400">
                <span>{quota?.logsPercentUsed ?? 0}% capacity used</span>
                <span>Resets on 1st of month</span>
              </div>
            </div>

            {/* 2. Monthly Traces */}
            <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 space-y-3">
              <div className="flex items-center justify-between text-xs">
                <span className="font-semibold text-slate-400 uppercase tracking-wider">Monthly Distributed Traces</span>
                <span
                  className={`px-2 py-0.5 rounded text-[10px] font-semibold ${
                    quota?.isTracesExceeded
                      ? "bg-rose-950 text-rose-400 border border-rose-800"
                      : quota?.isTracesWarning
                      ? "bg-amber-950 text-amber-400 border border-amber-800"
                      : "bg-emerald-950 text-emerald-400 border border-emerald-800"
                  }`}
                >
                  {quota?.isTracesExceeded ? "Exceeded" : quota?.isTracesWarning ? "Warning" : "Normal"}
                </span>
              </div>
              <div className="flex items-baseline justify-between">
                <div className="text-2xl font-bold text-white">
                  {(quota?.tracesUsed ?? detail?.usage?.tracesIngestedMonthToDate ?? 0).toLocaleString()}
                </div>
                <div className="text-xs text-slate-400 font-mono">
                  / {(quota?.tracesQuota ?? 500000).toLocaleString()} spans
                </div>
              </div>
              <div className="w-full bg-slate-800 rounded-full h-2 overflow-hidden">
                <div
                  className={`h-2 rounded-full transition-all ${
                    quota?.isTracesExceeded
                      ? "bg-rose-500"
                      : quota?.isTracesWarning
                      ? "bg-amber-500"
                      : "bg-blue-500"
                  }`}
                  style={{ width: `${Math.min(quota?.tracesPercentUsed ?? 0, 100)}%` }}
                />
              </div>
              <div className="flex justify-between text-[11px] text-slate-400">
                <span>{quota?.tracesPercentUsed ?? 0}% capacity used</span>
                <span>Resets on 1st of month</span>
              </div>
            </div>

            {/* 3. Monitored Applications */}
            <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 space-y-3">
              <div className="flex items-center justify-between text-xs">
                <span className="font-semibold text-slate-400 uppercase tracking-wider">Monitored Applications</span>
                <span className="text-slate-400 text-xs">Limit</span>
              </div>
              <div className="flex items-baseline justify-between">
                <div className="text-2xl font-bold text-white">
                  {quota?.applicationsUsed ?? 0}
                </div>
                <div className="text-xs text-slate-400 font-mono">
                  / {quota?.applicationsQuota ?? 20} apps
                </div>
              </div>
              <div className="w-full bg-slate-800 rounded-full h-2 overflow-hidden">
                <div
                  className="bg-emerald-500 h-2 rounded-full transition-all"
                  style={{ width: `${Math.min(quota?.applicationsPercentUsed ?? 0, 100)}%` }}
                />
              </div>
              <div className="flex justify-between text-[11px] text-slate-400">
                <span>{quota?.applicationsPercentUsed ?? 0}% allocated</span>
                <span>Active services</span>
              </div>
            </div>

            {/* 4. Custom Dashboards */}
            <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 space-y-3">
              <div className="flex items-center justify-between text-xs">
                <span className="font-semibold text-slate-400 uppercase tracking-wider">Custom Dashboards</span>
                <span className="text-slate-400 text-xs">Limit</span>
              </div>
              <div className="flex items-baseline justify-between">
                <div className="text-2xl font-bold text-white">
                  {quota?.dashboardsUsed ?? 0}
                </div>
                <div className="text-xs text-slate-400 font-mono">
                  / {quota?.dashboardsQuota ?? 15} dashboards
                </div>
              </div>
              <div className="w-full bg-slate-800 rounded-full h-2 overflow-hidden">
                <div
                  className="bg-purple-500 h-2 rounded-full transition-all"
                  style={{ width: `${Math.min(quota?.dashboardsPercentUsed ?? 0, 100)}%` }}
                />
              </div>
              <div className="flex justify-between text-[11px] text-slate-400">
                <span>{quota?.dashboardsPercentUsed ?? 0}% allocated</span>
                <span>Team & Personal</span>
              </div>
            </div>

            {/* 5. Team Members */}
            <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 space-y-3">
              <div className="flex items-center justify-between text-xs">
                <span className="font-semibold text-slate-400 uppercase tracking-wider">Team Members</span>
                <span className="text-slate-400 text-xs">Seats</span>
              </div>
              <div className="flex items-baseline justify-between">
                <div className="text-2xl font-bold text-white">
                  {quota?.membersUsed ?? detail?.members?.length ?? 0}
                </div>
                <div className="text-xs text-slate-400 font-mono">
                  / {quota?.membersQuota ?? 25} seats
                </div>
              </div>
              <div className="w-full bg-slate-800 rounded-full h-2 overflow-hidden">
                <div
                  className="bg-indigo-500 h-2 rounded-full transition-all"
                  style={{ width: `${Math.min(quota?.membersPercentUsed ?? 0, 100)}%` }}
                />
              </div>
              <div className="flex justify-between text-[11px] text-slate-400">
                <span>{quota?.membersPercentUsed ?? 0}% seats filled</span>
                <span>Active logins</span>
              </div>
            </div>

            {/* 6. Estimated Storage */}
            <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 space-y-3">
              <div className="flex items-center justify-between text-xs">
                <span className="font-semibold text-slate-400 uppercase tracking-wider">Estimated Storage</span>
                <span className="text-slate-400 text-xs">Active</span>
              </div>
              <div className="flex items-baseline justify-between">
                <div className="text-2xl font-bold text-white">
                  {quota?.estimatedStorageBytes
                    ? `${(quota.estimatedStorageBytes / (1024 * 1024)).toFixed(1)} MB`
                    : "0.0 MB"}
                </div>
                <div className="text-xs text-slate-400 font-mono">Compressed</div>
              </div>
              <div className="w-full bg-slate-800 rounded-full h-2 overflow-hidden">
                <div className="bg-teal-500 h-2 rounded-full" style={{ width: "25%" }} />
              </div>
              <div className="flex justify-between text-[11px] text-slate-400">
                <span>PostgreSQL & Redis buffers</span>
                <span>Retention: {detail?.settings?.defaultRetentionDays ?? 30} days</span>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Create Org Modal */}
      <Dialog
        open={showCreateModal}
        onOpenChange={setShowCreateModal}
        title="Create New Organization"
      >
        <form onSubmit={handleCreateOrg} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-slate-300 mb-1">Organization Name *</label>
            <input
              type="text"
              required
              placeholder="e.g. Acme Corporation"
              value={newOrgName}
              onChange={(e) => setNewOrgName(e.target.value)}
              className="w-full bg-slate-950/80 border border-slate-800 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:ring-1 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-300 mb-1">Slug (URL identifier)</label>
            <input
              type="text"
              placeholder="e.g. acme-corp"
              value={newOrgSlug}
              onChange={(e) => setNewOrgSlug(e.target.value)}
              className="w-full bg-slate-950/80 border border-slate-800 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:ring-1 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-300 mb-1">Description</label>
            <textarea
              rows={2}
              placeholder="Optional brief description"
              value={newOrgDesc}
              onChange={(e) => setNewOrgDesc(e.target.value)}
              className="w-full bg-slate-950/80 border border-slate-800 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:ring-1 focus:ring-blue-500"
            />
          </div>
          <div className="flex justify-end gap-3 pt-3 border-t border-slate-800">
            <button
              type="button"
              onClick={() => setShowCreateModal(false)}
              className="px-4 py-2 text-sm text-slate-400 hover:text-white"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={createSubmitting}
              className="px-4 py-2 bg-blue-600 hover:bg-blue-500 text-white text-sm font-medium rounded-lg"
            >
              {createSubmitting ? "Creating..." : "Create Organization"}
            </button>
          </div>
        </form>
      </Dialog>

      {/* Invite Member Modal */}
      <Dialog
        open={showInviteModal}
        onOpenChange={setShowInviteModal}
        title="Invite Organization Member"
        description="Generate a cryptographic invitation link to onboard a new team member to this tenant."
      >
        {inviteResult ? (
          <div className="space-y-4">
            <div className="p-4 bg-emerald-950/40 border border-emerald-800/60 rounded-lg">
              <div className="flex items-center gap-2 text-emerald-400 text-sm font-semibold mb-1">
                <CheckCircle2 className="w-4 h-4" /> Invitation Token Generated!
              </div>
              <p className="text-xs text-slate-300">
                Send this invitation token to <strong>{inviteResult.email}</strong>. It expires in 7 days and can be used once.
              </p>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-1">
                Raw Invitation Token (One-Time Display)
              </label>
              <div className="flex items-center gap-2">
                <input
                  type="text"
                  readOnly
                  value={inviteResult.rawToken}
                  className="w-full bg-slate-950 border border-slate-800 font-mono text-xs text-amber-300 p-2.5 rounded-lg select-all"
                />
                <button
                  type="button"
                  onClick={() => {
                    navigator.clipboard.writeText(inviteResult.rawToken);
                    setCopiedToken(true);
                    setTimeout(() => setCopiedToken(false), 2000);
                  }}
                  className="p-2.5 bg-slate-800 hover:bg-slate-700 text-slate-200 rounded-lg transition-colors"
                  title="Copy Token"
                >
                  {copiedToken ? <Check className="w-4 h-4 text-emerald-400" /> : <Copy className="w-4 h-4" />}
                </button>
              </div>
            </div>

            <div className="flex justify-end pt-2 border-t border-slate-800">
              <button
                type="button"
                onClick={() => {
                  setShowInviteModal(false);
                  setInviteResult(null);
                }}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-500 text-white text-sm font-medium rounded-lg"
              >
                Done
              </button>
            </div>
          </div>
        ) : (
          <form onSubmit={handleInvite} className="space-y-4">
            <div>
              <label className="block text-xs font-medium text-slate-300 mb-1">Member Email *</label>
              <input
                type="email"
                required
                placeholder="teammate@company.com"
                value={inviteEmail}
                onChange={(e) => setInviteEmail(e.target.value)}
                className="w-full bg-slate-950/80 border border-slate-800 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:ring-1 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-300 mb-1">Assigned Role</label>
              <select
                value={inviteRole}
                onChange={(e) => setInviteRole(e.target.value)}
                className="w-full bg-slate-950/80 border border-slate-800 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:ring-1 focus:ring-blue-500"
              >
                <option value="Viewer">Viewer (Read-only)</option>
                <option value="Member">Member (Edit alerts & dashboards)</option>
                <option value="Admin">Admin (Manage apps & members)</option>
                {isOwner && <option value="Owner">Owner (Full control)</option>}
              </select>
            </div>
            <div className="flex justify-end gap-3 pt-3 border-t border-slate-800">
              <button
                type="button"
                onClick={() => setShowInviteModal(false)}
                className="px-4 py-2 text-sm text-slate-400 hover:text-white"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={inviteSubmitting}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-500 text-white text-sm font-medium rounded-lg"
              >
                {inviteSubmitting ? "Generating..." : "Generate Invitation"}
              </button>
            </div>
          </form>
        )}
      </Dialog>

      {/* Plan Quotas Configuration Modal */}
      <Dialog
        open={showQuotaModal}
        onOpenChange={setShowQuotaModal}
        title="Configure Organization Plan Quotas"
        description={`Adjust resource capacity ceilings and billing tier for ${detail?.name || activeOrg?.name}.`}
      >
        <form onSubmit={handleUpdateQuota} className="space-y-4 text-xs">
          <div className="space-y-1">
            <label className="font-medium text-slate-300">Plan Tier</label>
            <select
              value={quotaTier}
              onChange={(e) => {
                const tier = e.target.value;
                setQuotaTier(tier);
                if (tier === "Starter") {
                  setQuotaMaxApps(5);
                  setQuotaMaxMembers(5);
                  setQuotaMaxDashboards(3);
                  setQuotaMaxLogs(100000);
                  setQuotaMaxTraces(50000);
                } else if (tier === "Pro") {
                  setQuotaMaxApps(20);
                  setQuotaMaxMembers(25);
                  setQuotaMaxDashboards(15);
                  setQuotaMaxLogs(1000000);
                  setQuotaMaxTraces(500000);
                } else if (tier === "Enterprise") {
                  setQuotaMaxApps(100);
                  setQuotaMaxMembers(100);
                  setQuotaMaxDashboards(50);
                  setQuotaMaxLogs(10000000);
                  setQuotaMaxTraces(5000000);
                }
              }}
              className="w-full bg-slate-950/80 border border-slate-800 rounded-lg px-3 py-2 text-xs text-white"
            >
              <option value="Starter">Starter (5 apps, 100K logs/mo)</option>
              <option value="Pro">Pro (20 apps, 1M logs/mo)</option>
              <option value="Enterprise">Enterprise (100 apps, 10M logs/mo)</option>
            </select>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="font-medium text-slate-300">Max Applications</label>
              <input
                type="number"
                min="1"
                value={quotaMaxApps}
                onChange={(e) => setQuotaMaxApps(Number(e.target.value))}
                className="w-full bg-slate-950/80 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white"
              />
            </div>
            <div className="space-y-1">
              <label className="font-medium text-slate-300">Max Team Seats</label>
              <input
                type="number"
                min="1"
                value={quotaMaxMembers}
                onChange={(e) => setQuotaMaxMembers(Number(e.target.value))}
                className="w-full bg-slate-950/80 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="font-medium text-slate-300">Max Custom Dashboards</label>
              <input
                type="number"
                min="1"
                value={quotaMaxDashboards}
                onChange={(e) => setQuotaMaxDashboards(Number(e.target.value))}
                className="w-full bg-slate-950/80 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white"
              />
            </div>
            <div className="space-y-1">
              <label className="font-medium text-slate-300">Monthly Logs Ceiling</label>
              <input
                type="number"
                min="1000"
                step="100000"
                value={quotaMaxLogs}
                onChange={(e) => setQuotaMaxLogs(Number(e.target.value))}
                className="w-full bg-slate-950/80 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white"
              />
            </div>
          </div>

          <div className="space-y-1">
            <label className="font-medium text-slate-300">Monthly Distributed Traces Ceiling</label>
            <input
              type="number"
              min="1000"
              step="50000"
              value={quotaMaxTraces}
              onChange={(e) => setQuotaMaxTraces(Number(e.target.value))}
              className="w-full bg-slate-950/80 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white"
            />
          </div>

          <div className="flex justify-end gap-3 pt-3 border-t border-slate-800">
            <button
              type="button"
              onClick={() => setShowQuotaModal(false)}
              className="px-3 py-1.5 text-xs text-slate-400 hover:text-white"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={quotaSaving}
              className="px-4 py-1.5 bg-sky-600 hover:bg-sky-500 text-white text-xs font-semibold rounded-lg"
            >
              {quotaSaving ? "Saving..." : "Save Quota Configuration"}
            </button>
          </div>
        </form>
      </Dialog>

      {/* Confirm Member Removal Dialog */}
      <ConfirmDialog
        open={!!memberToRemove}
        onOpenChange={(open) => !open && setMemberToRemove(null)}
        title="Remove Organization Member"
        description={`Are you sure you want to remove ${memberToRemove?.name} from ${activeOrg?.name}? They will immediately lose access to all applications and resources in this tenant.`}
        confirmLabel="Remove Member"
        variant="destructive"
        onConfirm={() => {
          if (memberToRemove) {
            handleRemoveMember(memberToRemove.id);
            setMemberToRemove(null);
          }
        }}
      />
    </div>
  );
};

export default OrganizationsPage;
