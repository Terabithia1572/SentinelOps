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
  type OrganizationDetailDto,
  type InvitationCreatedResponseDto
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
  Trash2
} from "lucide-react";

export const OrganizationsPage: React.FC = () => {
  const { organizations, activeOrg, setActiveOrg, refreshOrganizations } = useTenant();
  const [detail, setDetail] = useState<OrganizationDetailDto | null>(null);
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
      }
    } catch {
      setStatusMessage({ text: "Failed to load organization details.", type: "error" });
    } finally {
      setLoading(false);
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
    if (!activeOrg || !confirm("Are you sure you want to remove this member?")) return;
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

  const isOwnerOrAdmin = detail?.userRole === "Owner" || detail?.userRole === "Admin";
  const isOwner = detail?.userRole === "Owner";

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
              {activeOrg?.name.slice(0, 2).toUpperCase() || "SO"}
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h2 className="text-lg font-bold text-white">{activeOrg?.name || "No Organization Selected"}</h2>
                {activeOrg && (
                  <span className="px-2 py-0.5 text-xs font-semibold rounded-full bg-blue-900/50 text-blue-300 border border-blue-700/50">
                    Role: {detail?.userRole || activeOrg.userRole}
                  </span>
                )}
              </div>
              <p className="text-xs text-slate-400 mt-0.5 font-mono">
                Slug: <span className="text-slate-300">{activeOrg?.slug}</span> • ID: {activeOrg?.id}
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
                  {org.name} ({org.userRole})
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
          Members ({detail?.members.length || 0})
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
          Pending Invites ({detail?.pendingInvitations.length || 0})
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
                {detail?.members.map((m) => (
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
                            onClick={() => handleRemoveMember(m.userId)}
                            className="p-1 hover:text-red-400 text-slate-500 transition-colors"
                            title="Remove Member"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        </div>
                      </td>
                    )}
                  </tr>
                ))}
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
          {detail?.pendingInvitations.length === 0 ? (
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
                  {detail?.pendingInvitations.map((inv) => (
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

      {/* Tab: Usage */}
      {activeTab === "usage" && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
          <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 shadow-sm">
            <div className="text-xs font-semibold text-slate-400 uppercase">Logs Ingested MTD</div>
            <div className="text-2xl font-bold text-white mt-2">
              {detail?.usage.logsIngestedMonthToDate.toLocaleString() || "0"}
            </div>
            <div className="text-xs text-emerald-400 mt-1">Telemetry stream buffer active</div>
          </div>

          <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 shadow-sm">
            <div className="text-xs font-semibold text-slate-400 uppercase">Traces Ingested MTD</div>
            <div className="text-2xl font-bold text-white mt-2">
              {detail?.usage.tracesIngestedMonthToDate.toLocaleString() || "0"}
            </div>
            <div className="text-xs text-blue-400 mt-1">Distributed trace trees</div>
          </div>

          <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 shadow-sm">
            <div className="text-xs font-semibold text-slate-400 uppercase">API Requests MTD</div>
            <div className="text-2xl font-bold text-white mt-2">
              {detail?.usage.apiRequestsMonthToDate.toLocaleString() || "0"}
            </div>
            <div className="text-xs text-purple-400 mt-1">SaaS endpoints queried</div>
          </div>
        </div>
      )}

      {/* Create Org Modal */}
      {showCreateModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-xl p-6 max-w-md w-full shadow-xl">
            <h3 className="text-lg font-bold text-white mb-4">Create New Organization</h3>
            <form onSubmit={handleCreateOrg} className="space-y-4">
              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">Organization Name *</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. Acme Corporation"
                  value={newOrgName}
                  onChange={(e) => setNewOrgName(e.target.value)}
                  className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">Slug (URL identifier)</label>
                <input
                  type="text"
                  placeholder="e.g. acme-corp"
                  value={newOrgSlug}
                  onChange={(e) => setNewOrgSlug(e.target.value)}
                  className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">Description</label>
                <textarea
                  rows={2}
                  placeholder="Optional brief description"
                  value={newOrgDesc}
                  onChange={(e) => setNewOrgDesc(e.target.value)}
                  className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div className="flex justify-end gap-3 pt-3">
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
          </div>
        </div>
      )}

      {/* Invite Member Modal */}
      {showInviteModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-xl p-6 max-w-md w-full shadow-xl">
            <h3 className="text-lg font-bold text-white mb-2">Invite Organization Member</h3>
            <p className="text-xs text-slate-400 mb-4">
              Generate a cryptographic invitation link to onboard a new team member to this tenant.
            </p>

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

                <div className="flex justify-end pt-2">
                  <button
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
                    className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-slate-300 mb-1">Assigned Role</label>
                  <select
                    value={inviteRole}
                    onChange={(e) => setInviteRole(e.target.value)}
                    className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    <option value="Viewer">Viewer (Read-only)</option>
                    <option value="Member">Member (Edit alerts & dashboards)</option>
                    <option value="Admin">Admin (Manage apps & members)</option>
                    {isOwner && <option value="Owner">Owner (Full control)</option>}
                  </select>
                </div>
                <div className="flex justify-end gap-3 pt-3">
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
          </div>
        </div>
      )}
    </div>
  );
};

export default OrganizationsPage;
