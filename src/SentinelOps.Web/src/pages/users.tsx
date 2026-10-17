import { useState, useEffect, useCallback } from "react";
import {
  getUsers,
  getUserDetail,
  updateUserRole,
  updateUserStatus,
  revokeUserSessions,
  adminResetPassword,
  type UserDto,
  type UserDetailDto,
} from "@/lib/api-client";
import { useAuth } from "@/context/auth-context";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Dialog } from "@/components/ui/dialog";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import {
  Users,
  Shield,
  Search,
  Filter,
  RefreshCw,
  Lock,
  Unlock,
  KeyRound,
  LogOut,
  UserCheck,
  UserX,
  Activity,
  Layers,
  AlertTriangle,
  Loader2,
  CheckCircle,
  X,
} from "lucide-react";

export function UsersPage() {
  const { user: currentUser, isAdmin } = useAuth();

  const [users, setUsers] = useState<UserDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // Filters
  const [search, setSearch] = useState("");
  const [selectedRole, setSelectedRole] = useState("all");
  const [selectedStatus, setSelectedStatus] = useState("all");

  // User Detail Drawer
  const [selectedUserDetail, setSelectedUserDetail] = useState<UserDetailDto | null>(null);


  // Role Edit Modal
  const [roleModalUser, setRoleModalUser] = useState<UserDto | null>(null);
  const [targetRole, setTargetRole] = useState<string>("Viewer");
  const [isSubmittingRole, setIsSubmittingRole] = useState(false);

  // Status Modal
  const [statusModalUser, setStatusModalUser] = useState<UserDto | null>(null);
  const [targetStatusAction, setTargetStatusAction] = useState<"activate" | "deactivate" | "lock" | "unlock">("activate");
  const [isSubmittingStatus, setIsSubmittingStatus] = useState(false);

  // Password Reset Modal
  const [pwdModalUser, setPwdModalUser] = useState<UserDto | null>(null);
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [isSubmittingPwd, setIsSubmittingPwd] = useState(false);

  // Revoke Sessions Confirmation Modal
  const [revokeModalUser, setRevokeModalUser] = useState<UserDto | null>(null);
  const [isSubmittingRevoke, setIsSubmittingRevoke] = useState(false);

  const fetchUsers = useCallback(async () => {
    try {
      setIsLoading(true);
      setErrorMessage(null);
      const res = await getUsers({
        page,
        pageSize,
        search: search.trim() || undefined,
        role: selectedRole !== "all" ? selectedRole : undefined,
        status: selectedStatus !== "all" ? selectedStatus : undefined,
      });
      setUsers(res.items);
      setTotalCount(res.totalCount);
      setTotalPages(res.totalPages);
    } catch (err: any) {
      setErrorMessage(err.response?.data?.message || err.message || "Failed to load users");
    } finally {
      setIsLoading(false);
    }
  }, [page, pageSize, search, selectedRole, selectedStatus]);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

  const handleInspectUser = async (user: UserDto) => {
    try {
      const detail = await getUserDetail(user.id);
      setSelectedUserDetail(detail);
    } catch (err: any) {
      setErrorMessage(err.response?.data?.message || "Failed to load user details");
    }
  };


  const handleUpdateRole = async () => {
    if (!roleModalUser) return;
    try {
      setIsSubmittingRole(true);
      setErrorMessage(null);
      await updateUserRole(roleModalUser.id, [targetRole]);
      setSuccessMessage(`Updated role for ${roleModalUser.userName} to ${targetRole}`);
      setRoleModalUser(null);
      fetchUsers();
    } catch (err: any) {
      setErrorMessage(err.response?.data?.message || err.message || "Failed to update role");
    } finally {
      setIsSubmittingRole(false);
    }
  };

  const handleUpdateStatus = async () => {
    if (!statusModalUser) return;
    try {
      setIsSubmittingStatus(true);
      setErrorMessage(null);

      let isActive = statusModalUser.isActive;
      let isLocked = statusModalUser.isLockedOut;

      if (targetStatusAction === "activate") {
        isActive = true;
        isLocked = false;
      } else if (targetStatusAction === "deactivate") {
        isActive = false;
      } else if (targetStatusAction === "lock") {
        isLocked = true;
      } else if (targetStatusAction === "unlock") {
        isLocked = false;
      }

      await updateUserStatus(statusModalUser.id, isActive, isLocked);
      setSuccessMessage(`Account status updated for ${statusModalUser.userName}`);
      setStatusModalUser(null);
      fetchUsers();
    } catch (err: any) {
      setErrorMessage(err.response?.data?.message || err.message || "Failed to update account status");
    } finally {
      setIsSubmittingStatus(false);
    }
  };

  const handleResetPassword = async () => {
    if (!pwdModalUser) return;
    if (newPassword.length < 8) {
      setErrorMessage("Password must be at least 8 characters");
      return;
    }
    if (newPassword !== confirmPassword) {
      setErrorMessage("Passwords do not match");
      return;
    }

    try {
      setIsSubmittingPwd(true);
      setErrorMessage(null);
      await adminResetPassword(pwdModalUser.id, newPassword, confirmPassword);
      setSuccessMessage(`Password has been reset for ${pwdModalUser.userName}`);
      setPwdModalUser(null);
      setNewPassword("");
      setConfirmPassword("");
    } catch (err: any) {
      setErrorMessage(err.response?.data?.message || err.message || "Failed to reset password");
    } finally {
      setIsSubmittingPwd(false);
    }
  };

  const handleRevokeSessions = async () => {
    if (!revokeModalUser) return;
    try {
      setIsSubmittingRevoke(true);
      setErrorMessage(null);
      await revokeUserSessions(revokeModalUser.id);
      setSuccessMessage(`All active sessions and refresh tokens invalidated for ${revokeModalUser.userName}`);
      setRevokeModalUser(null);
      fetchUsers();
    } catch (err: any) {
      setErrorMessage(err.response?.data?.message || err.message || "Failed to revoke sessions");
    } finally {
      setIsSubmittingRevoke(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-white flex items-center gap-2">
            <Users className="w-7 h-7 text-sky-400" />
            User & Access Management
          </h1>
          <p className="text-sm text-slate-400">
            Enterprise RBAC directory, role permissions, account lifecycle, and session security
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={fetchUsers}
            disabled={isLoading}
            className="flex items-center gap-1.5"
          >
            <RefreshCw className={`w-4 h-4 ${isLoading ? "animate-spin" : ""}`} />
            Refresh
          </Button>
        </div>
      </div>

      {/* Notifications */}
      {successMessage && (
        <div className="bg-emerald-950/60 border border-emerald-800 text-emerald-300 p-3.5 rounded-lg flex items-center justify-between">
          <div className="flex items-center gap-2">
            <CheckCircle className="w-5 h-5 text-emerald-400" />
            <span>{successMessage}</span>
          </div>
          <button onClick={() => setSuccessMessage(null)} className="text-emerald-400 hover:text-emerald-200">
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {errorMessage && (
        <div className="bg-rose-950/60 border border-rose-800 text-rose-300 p-3.5 rounded-lg flex items-center justify-between">
          <div className="flex items-center gap-2">
            <AlertTriangle className="w-5 h-5 text-rose-400" />
            <span>{errorMessage}</span>
          </div>
          <button onClick={() => setErrorMessage(null)} className="text-rose-400 hover:text-rose-200">
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="bg-slate-900/60 border-slate-800">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-xs font-medium text-slate-400">Total Directory Users</p>
              <h3 className="text-2xl font-bold text-white mt-1">{totalCount}</h3>
            </div>
            <div className="w-10 h-10 rounded-lg bg-sky-500/10 flex items-center justify-center text-sky-400">
              <Users className="w-5 h-5" />
            </div>
          </CardContent>
        </Card>

        <Card className="bg-slate-900/60 border-slate-800">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-xs font-medium text-slate-400">Active Accounts</p>
              <h3 className="text-2xl font-bold text-emerald-400 mt-1">
                {users.filter((u) => u.isActive && !u.isLockedOut).length}
              </h3>
            </div>
            <div className="w-10 h-10 rounded-lg bg-emerald-500/10 flex items-center justify-center text-emerald-400">
              <UserCheck className="w-5 h-5" />
            </div>
          </CardContent>
        </Card>

        <Card className="bg-slate-900/60 border-slate-800">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-xs font-medium text-slate-400">Locked / Inactive</p>
              <h3 className="text-2xl font-bold text-amber-400 mt-1">
                {users.filter((u) => !u.isActive || u.isLockedOut).length}
              </h3>
            </div>
            <div className="w-10 h-10 rounded-lg bg-amber-500/10 flex items-center justify-center text-amber-400">
              <Lock className="w-5 h-5" />
            </div>
          </CardContent>
        </Card>

        <Card className="bg-slate-900/60 border-slate-800">
          <CardContent className="p-4 flex items-center justify-between">
            <div>
              <p className="text-xs font-medium text-slate-400">Platform Administrators</p>
              <h3 className="text-2xl font-bold text-purple-400 mt-1">
                {users.filter((u) => u.roles.includes("Admin")).length}
              </h3>
            </div>
            <div className="w-10 h-10 rounded-lg bg-purple-500/10 flex items-center justify-center text-purple-400">
              <Shield className="w-5 h-5" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filter Bar */}
      <Card className="bg-slate-900/60 border-slate-800">
        <CardContent className="p-4">
          <div className="flex flex-col md:flex-row gap-3">
            <div className="flex-1 relative">
              <Search className="w-4 h-4 absolute left-3 top-3 text-slate-500" />
              <Input
                placeholder="Search by username, email, or full name..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="pl-9 bg-slate-950 border-slate-800 text-slate-100"
              />
            </div>

            <div className="flex items-center gap-2">
              <Filter className="w-4 h-4 text-slate-400" />
              <select
                value={selectedRole}
                onChange={(e) => {
                  setSelectedRole(e.target.value);
                  setPage(1);
                }}
                className="bg-slate-950 border border-slate-800 text-sm text-slate-200 rounded-md px-3 py-2 outline-none"
              >
                <option value="all">All Roles</option>
                <option value="Admin">Admin</option>
                <option value="Operator">Operator</option>
                <option value="Viewer">Viewer</option>
              </select>

              <select
                value={selectedStatus}
                onChange={(e) => {
                  setSelectedStatus(e.target.value);
                  setPage(1);
                }}
                className="bg-slate-950 border border-slate-800 text-sm text-slate-200 rounded-md px-3 py-2 outline-none"
              >
                <option value="all">All Statuses</option>
                <option value="active">Active Only</option>
                <option value="inactive">Inactive Only</option>
                <option value="locked">Locked Only</option>
              </select>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* User Directory Table */}
      <Card className="bg-slate-900/60 border-slate-800">
        <CardHeader className="pb-3 border-b border-slate-800">
          <CardTitle className="text-base font-semibold text-white">Users Directory</CardTitle>
          <CardDescription className="text-xs text-slate-400">
            Showing {users.length} of {totalCount} platform accounts
          </CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-950/60 text-xs text-slate-400 uppercase border-b border-slate-800">
                <tr>
                  <th className="px-4 py-3">User Profile</th>
                  <th className="px-4 py-3">Roles</th>
                  <th className="px-4 py-3">Account Status</th>
                  <th className="px-4 py-3">Created Date</th>
                  <th className="px-4 py-3 text-right">Administrative Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/60">
                {isLoading ? (
                  <tr>
                    <td colSpan={5} className="p-4 space-y-2">
                      <Skeleton className="h-10 w-full rounded" />
                      <Skeleton className="h-10 w-full rounded" />
                      <Skeleton className="h-10 w-full rounded" />
                    </td>
                  </tr>
                ) : users.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="py-8">
                      <EmptyState
                        icon={Users}
                        title="No Users Found"
                        description="No platform accounts match the active search term or role/status filters."
                      />
                    </td>
                  </tr>
                ) : (
                  users.map((u) => {
                    const isSelf = currentUser?.id === u.id;
                    return (
                      <tr key={u.id} className="hover:bg-slate-800/30 transition-colors">
                        <td className="px-4 py-3.5">
                          <div className="flex items-center gap-3">
                            <div className="w-9 h-9 rounded-full bg-slate-800 border border-slate-700 flex items-center justify-center font-bold text-sky-400 text-sm">
                              {u.fullName?.charAt(0) || u.userName?.charAt(0) || "U"}
                            </div>
                            <div>
                              <div className="font-medium text-white flex items-center gap-1.5">
                                {u.fullName || u.userName}
                                {isSelf && (
                                  <span className="text-[10px] bg-sky-950 text-sky-400 border border-sky-800 px-1.5 py-0.2 rounded">
                                    You
                                  </span>
                                )}
                              </div>
                              <div className="text-xs text-slate-400">@{u.userName} • {u.email}</div>
                            </div>
                          </div>
                        </td>
                        <td className="px-4 py-3.5">
                          <div className="flex flex-wrap gap-1">
                            {u.roles.map((r) => (
                              <Badge
                                key={r}
                                variant={r === "Admin" ? "default" : r === "Operator" ? "secondary" : "outline"}
                                className={`text-[11px] font-medium ${
                                  r === "Admin"
                                    ? "bg-purple-950/80 text-purple-300 border-purple-800"
                                    : r === "Operator"
                                    ? "bg-sky-950/80 text-sky-300 border-sky-800"
                                    : "bg-slate-800 text-slate-300 border-slate-700"
                                }`}
                              >
                                {r}
                              </Badge>
                            ))}
                          </div>
                        </td>
                        <td className="px-4 py-3.5">
                          {u.isLockedOut ? (
                            <span className="inline-flex items-center gap-1.5 px-2 py-0.5 rounded text-xs font-medium bg-rose-950/70 text-rose-300 border border-rose-800">
                              <Lock className="w-3 h-3" /> Locked Out
                            </span>
                          ) : u.isActive ? (
                            <span className="inline-flex items-center gap-1.5 px-2 py-0.5 rounded text-xs font-medium bg-emerald-950/70 text-emerald-300 border border-emerald-800">
                              <UserCheck className="w-3 h-3" /> Active
                            </span>
                          ) : (
                            <span className="inline-flex items-center gap-1.5 px-2 py-0.5 rounded text-xs font-medium bg-slate-800 text-slate-400 border border-slate-700">
                              <UserX className="w-3 h-3" /> Deactivated
                            </span>
                          )}
                        </td>
                        <td className="px-4 py-3.5 text-xs text-slate-400">
                          {new Date(u.createdAtUtc).toLocaleDateString()}
                        </td>
                        <td className="px-4 py-3.5 text-right">
                          <div className="flex items-center justify-end gap-1.5">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleInspectUser(u)}
                              title="Inspect Details"
                              className="h-8 px-2 text-slate-300 hover:text-white"
                            >
                              <Activity className="w-3.5 h-3.5" />
                            </Button>

                            {isAdmin && (
                              <>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => {
                                    setRoleModalUser(u);
                                    setTargetRole(u.roles[0] || "Viewer");
                                  }}
                                  title="Change Role"
                                  className="h-8 px-2 text-slate-300 hover:text-sky-400"
                                >
                                  <Shield className="w-3.5 h-3.5" />
                                </Button>

                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => {
                                    setStatusModalUser(u);
                                    setTargetStatusAction(u.isLockedOut ? "unlock" : u.isActive ? "deactivate" : "activate");
                                  }}
                                  disabled={isSelf}
                                  title={u.isActive ? "Deactivate / Lock" : "Activate"}
                                  className="h-8 px-2 text-slate-300 hover:text-amber-400"
                                >
                                  {u.isLockedOut ? <Unlock className="w-3.5 h-3.5" /> : <Lock className="w-3.5 h-3.5" />}
                                </Button>

                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => setRevokeModalUser(u)}
                                  title="Revoke All Sessions"
                                  className="h-8 px-2 text-slate-300 hover:text-rose-400"
                                >
                                  <LogOut className="w-3.5 h-3.5" />
                                </Button>

                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => setPwdModalUser(u)}
                                  title="Reset Password"
                                  className="h-8 px-2 text-slate-300 hover:text-emerald-400"
                                >
                                  <KeyRound className="w-3.5 h-3.5" />
                                </Button>
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between p-4 border-t border-slate-800 text-xs text-slate-400">
              <div>Page {page} of {totalPages}</div>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page >= totalPages}
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                >
                  Next
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* User Details Drawer Modal */}
      {selectedUserDetail && (
        <div className="fixed inset-0 z-50 bg-black/70 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto p-6 space-y-6">
            <div className="flex justify-between items-start border-b border-slate-800 pb-4">
              <div>
                <h3 className="text-xl font-bold text-white flex items-center gap-2">
                  <Shield className="w-5 h-5 text-sky-400" />
                  {selectedUserDetail.fullName || selectedUserDetail.userName}
                </h3>
                <p className="text-xs text-slate-400">@{selectedUserDetail.userName} • {selectedUserDetail.email}</p>
              </div>
              <button
                onClick={() => setSelectedUserDetail(null)}
                className="text-slate-400 hover:text-white"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Memberships */}
            <div className="space-y-3">
              <h4 className="text-sm font-semibold text-white flex items-center gap-2">
                <Layers className="w-4 h-4 text-purple-400" />
                Application Memberships
              </h4>
              {selectedUserDetail.applicationMemberships.length === 0 ? (
                <p className="text-xs text-slate-500 italic">No application-scoped memberships assigned.</p>
              ) : (
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                  {selectedUserDetail.applicationMemberships.map((m) => (
                    <div key={m.id} className="p-3 bg-slate-950 border border-slate-800 rounded-lg flex justify-between items-center">
                      <div>
                        <div className="text-xs font-medium text-white">{m.applicationName}</div>
                        <div className="text-[10px] text-slate-500">Assigned: {new Date(m.createdAtUtc).toLocaleDateString()}</div>
                      </div>
                      <Badge variant="outline" className="text-xs">{m.role}</Badge>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Recent Security Activity */}
            <div className="space-y-3">
              <h4 className="text-sm font-semibold text-white flex items-center gap-2">
                <Activity className="w-4 h-4 text-emerald-400" />
                Recent Security Audit Trail
              </h4>
              {selectedUserDetail.recentSecurityActivity.length === 0 ? (
                <p className="text-xs text-slate-500 italic">No recent security events logged for this user.</p>
              ) : (
                <div className="space-y-2">
                  {selectedUserDetail.recentSecurityActivity.map((act) => (
                    <div key={act.id} className="p-2.5 bg-slate-950 border border-slate-800 rounded-lg flex items-center justify-between text-xs">
                      <div>
                        <div className="font-medium text-slate-200">{act.action}</div>
                        <div className="text-slate-500 text-[11px]">{act.details}</div>
                      </div>
                      <div className="text-right">
                        <span className={`text-[10px] font-semibold ${act.outcome === "Success" ? "text-emerald-400" : "text-rose-400"}`}>
                          {act.outcome}
                        </span>
                        <div className="text-[10px] text-slate-500">{new Date(act.timestampUtc).toLocaleTimeString()}</div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="pt-4 border-t border-slate-800 flex justify-end">
              <Button variant="outline" onClick={() => setSelectedUserDetail(null)}>Close</Button>
            </div>
          </div>
        </div>
      )}

      {/* Role Assignment Modal */}
      {roleModalUser && (
        <Dialog
          open={!!roleModalUser}
          onOpenChange={(open) => !open && setRoleModalUser(null)}
          title={`Update Roles for ${roleModalUser.userName}`}
          description="Select the administrative or operational role for this user account."
        >
          <div className="space-y-4">
            <div className="space-y-2">
              <label className="text-xs font-medium text-slate-300">Assign Platform Role</label>
              <select
                value={targetRole}
                onChange={(e) => setTargetRole(e.target.value)}
                className="w-full bg-slate-950 border border-slate-800 text-sm text-slate-200 rounded-lg p-2.5 outline-none"
              >
                <option value="Admin">Administrator (Full Platform Control)</option>
                <option value="Operator">Operator (Alerts, Traces, Telemetry Ops)</option>
                <option value="Viewer">Viewer (Read-Only Dashboards)</option>
              </select>
            </div>

            <div className="p-3 bg-amber-950/40 border border-amber-800/60 rounded-lg text-xs text-amber-300 flex items-start gap-2">
              <AlertTriangle className="w-4 h-4 text-amber-400 shrink-0 mt-0.5" />
              <span>Guardrail: Removing the Administrator role from the last active admin is strictly prohibited.</span>
            </div>

            <div className="flex justify-end gap-2 pt-2 border-t border-slate-800">
              <Button variant="ghost" size="sm" onClick={() => setRoleModalUser(null)}>Cancel</Button>
              <Button size="sm" onClick={handleUpdateRole} disabled={isSubmittingRole}>
                {isSubmittingRole ? <Loader2 className="w-4 h-4 animate-spin mr-1" /> : null}
                Save Role
              </Button>
            </div>
          </div>
        </Dialog>
      )}

      {/* Status Modal */}
      {statusModalUser && (
        <Dialog
          open={!!statusModalUser}
          onOpenChange={(open) => !open && setStatusModalUser(null)}
          title={`Manage Account Status: ${statusModalUser.userName}`}
        >
          <div className="space-y-4">
            <div className="space-y-2">
              <label className="text-xs font-medium text-slate-300">Status Action</label>
              <select
                value={targetStatusAction}
                onChange={(e: any) => setTargetStatusAction(e.target.value)}
                className="w-full bg-slate-950 border border-slate-800 text-sm text-slate-200 rounded-lg p-2.5 outline-none"
              >
                <option value="activate">Activate Account</option>
                <option value="deactivate">Deactivate Account (Revokes All Sessions)</option>
                <option value="lock">Lock Account (24 Hours)</option>
                <option value="unlock">Unlock Account</option>
              </select>
            </div>

            <div className="flex justify-end gap-2 pt-2 border-t border-slate-800">
              <Button variant="ghost" size="sm" onClick={() => setStatusModalUser(null)}>Cancel</Button>
              <Button size="sm" onClick={handleUpdateStatus} disabled={isSubmittingStatus}>
                {isSubmittingStatus ? <Loader2 className="w-4 h-4 animate-spin mr-1" /> : null}
                Confirm Status
              </Button>
            </div>
          </div>
        </Dialog>
      )}

      {/* Revoke Sessions Modal */}
      <ConfirmDialog
        open={!!revokeModalUser}
        onOpenChange={(open) => !open && setRevokeModalUser(null)}
        title="Revoke User Sessions"
        description={`Are you sure you want to forcibly revoke all active sessions for ${revokeModalUser?.userName}? This invalidates all refresh tokens and triggers security stamp renewal. The user will be required to log in again immediately.`}
        confirmLabel="Revoke All Sessions"
        variant="destructive"
        isLoading={isSubmittingRevoke}
        onConfirm={handleRevokeSessions}
      />

      {/* Admin Reset Password Modal */}
      {pwdModalUser && (
        <Dialog
          open={!!pwdModalUser}
          onOpenChange={(open) => !open && setPwdModalUser(null)}
          title={`Reset Password: ${pwdModalUser.userName}`}
        >
          <div className="space-y-4">
            <div className="space-y-3">
              <div>
                <label className="text-xs font-medium text-slate-300 mb-1 block">New Password</label>
                <Input
                  type="password"
                  placeholder="Minimum 8 characters"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  className="bg-slate-950 border-slate-800"
                />
              </div>

              <div>
                <label className="text-xs font-medium text-slate-300 mb-1 block">Confirm Password</label>
                <Input
                  type="password"
                  placeholder="Repeat new password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  className="bg-slate-950 border-slate-800"
                />
              </div>
            </div>

            <div className="flex justify-end gap-2 pt-2 border-t border-slate-800">
              <Button variant="ghost" size="sm" onClick={() => setPwdModalUser(null)}>Cancel</Button>
              <Button size="sm" onClick={handleResetPassword} disabled={isSubmittingPwd}>
                {isSubmittingPwd ? <Loader2 className="w-4 h-4 animate-spin mr-1" /> : null}
                Reset Password
              </Button>
            </div>
          </div>
        </Dialog>
      )}
    </div>
  );
}
