import React, { useState, useEffect, useCallback } from "react";
import { Link } from "react-router-dom";
import apiClient, { type ApiResponse, type ApplicationDto } from "@/lib/api-client";
import { useAuth } from "@/context/auth-context";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Dialog } from "@/components/ui/dialog";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorState } from "@/components/ui/error-state";
import {
  AppWindow,
  Plus,
  Search,
  Server,
  Layers,
  Trash2,
  Loader2,
  Key,
  FileText,
  Activity,
} from "lucide-react";

export function ApplicationsPage() {
  const { isOperator } = useAuth();
  const [applications, setApplications] = useState<ApplicationDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Modals state
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showAddEnvModal, setShowAddEnvModal] = useState<string | null>(null); // app id
  const [showAddServiceModal, setShowAddServiceModal] = useState<string | null>(null); // app id
  const [appToDelete, setAppToDelete] = useState<{ id: string; name: string } | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  // Form states
  const [newAppName, setNewAppName] = useState("");
  const [newAppDisplayName, setNewAppDisplayName] = useState("");
  const [newAppDesc, setNewAppDesc] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Env form
  const [envName, setEnvName] = useState("");
  const [envType, setEnvType] = useState("Development");

  // Service form
  const [serviceName, setServiceName] = useState("");
  const [serviceDisplayName, setServiceDisplayName] = useState("");

  const fetchApplications = useCallback(async () => {
    try {
      setIsLoading(true);
      setErrorMessage(null);
      const res = await apiClient.get<ApiResponse<ApplicationDto[]>>("/applications");
      if (res.data.success) {
        setApplications(res.data.data || []);
      }
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : "Failed to load applications.");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchApplications();
  }, [fetchApplications]);

  const handleCreateApplication = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newAppName.trim()) return;
    try {
      setIsSubmitting(true);
      const res = await apiClient.post<ApiResponse<ApplicationDto>>("/applications", {
        name: newAppName.trim(),
        displayName: newAppDisplayName.trim() || newAppName.trim(),
        description: newAppDesc.trim(),
      });
      if (res.data.success) {
        setShowCreateModal(false);
        setNewAppName("");
        setNewAppDisplayName("");
        setNewAppDesc("");
        await fetchApplications();
      }
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : "Failed to create application");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleAddEnvironment = async (appId: string, e: React.FormEvent) => {
    e.preventDefault();
    if (!envName.trim()) return;
    try {
      setIsSubmitting(true);
      const res = await apiClient.post<ApiResponse<unknown>>(`/applications/${appId}/environments`, {
        name: envName.trim(),
        environmentType: envType,
      });
      if (res.data.success) {
        setShowAddEnvModal(null);
        setEnvName("");
        await fetchApplications();
      }
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : "Failed to add environment");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleAddService = async (appId: string, e: React.FormEvent) => {
    e.preventDefault();
    if (!serviceName.trim()) return;
    try {
      setIsSubmitting(true);
      const res = await apiClient.post<ApiResponse<unknown>>(`/applications/${appId}/services`, {
        name: serviceName.trim(),
        displayName: serviceDisplayName.trim() || serviceName.trim(),
      });
      if (res.data.success) {
        setShowAddServiceModal(null);
        setServiceName("");
        setServiceDisplayName("");
        await fetchApplications();
      }
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : "Failed to add service");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleConfirmDelete = async () => {
    if (!appToDelete) return;
    try {
      setIsDeleting(true);
      const res = await apiClient.delete<ApiResponse<boolean>>(`/applications/${appToDelete.id}`);
      if (res.data.success) {
        setAppToDelete(null);
        await fetchApplications();
      }
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : "Failed to delete application");
    } finally {
      setIsDeleting(false);
    }
  };

  const filteredApps = applications.filter(
    (app) =>
      app.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      app.displayName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      (app.description && app.description.toLowerCase().includes(searchQuery.toLowerCase()))
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-white">Applications</h1>
          <p className="text-xs text-slate-400">
            Manage monitored software systems, environments, and connected microservices
          </p>
        </div>
        {isOperator && (
          <Button
            onClick={() => setShowCreateModal(true)}
            className="gap-2 bg-blue-600 hover:bg-blue-500 text-white text-xs h-9 shadow-md shadow-blue-600/20"
          >
            <Plus className="h-4 w-4" />
            New Application
          </Button>
        )}
      </div>

      {/* Filter / Search Bar */}
      <div className="flex items-center gap-4">
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-500" />
          <Input
            placeholder="Search applications..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-9 bg-slate-900/60 border-slate-800 text-xs text-slate-200"
          />
        </div>
      </div>

      {/* Error display */}
      {errorMessage && (
        <ErrorState
          title="Application Management Warning"
          message={errorMessage}
          onRetry={fetchApplications}
        />
      )}

      {/* Loading state */}
      {isLoading ? (
        <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <Card key={i} className="bg-slate-900/40 border-slate-800">
              <CardHeader className="pb-3">
                <Skeleton className="h-6 w-3/4 mb-2" />
                <Skeleton className="h-4 w-1/2" />
              </CardHeader>
              <CardContent className="space-y-4">
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
              </CardContent>
            </Card>
          ))}
        </div>
      ) : filteredApps.length === 0 ? (
        <EmptyState
          icon={AppWindow}
          title={searchQuery ? "No applications matched" : "Zero Registered Applications"}
          description={
            searchQuery
              ? `No systems found matching "${searchQuery}". Clear your search query to see all applications.`
              : "Register your first monitored software application to begin capturing distributed traces, logs, and metrics."
          }
          action={
            isOperator && !searchQuery ? (
              <Button
                onClick={() => setShowCreateModal(true)}
                className="gap-2 bg-blue-600 hover:bg-blue-500 text-white text-xs"
              >
                <Plus className="h-4 w-4" />
                Register Application
              </Button>
            ) : undefined
          }
        />
      ) : (
        <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
          {filteredApps.map((app) => (
            <Card
              key={app.id}
              className="flex flex-col justify-between hover:border-slate-700 transition-colors bg-slate-900/60 border-slate-800"
            >
              <CardHeader className="pb-3">
                <div className="flex items-start justify-between gap-2">
                  <div>
                    <CardTitle className="text-lg font-bold text-white">{app.displayName}</CardTitle>
                    <span className="font-mono text-xs text-slate-400 block mt-0.5">
                      {app.name}
                    </span>
                  </div>
                  <Badge variant={app.isActive ? "success" : "secondary"}>
                    {app.isActive ? "Active" : "Disabled"}
                  </Badge>
                </div>
                {app.description && (
                  <CardDescription className="line-clamp-2 mt-2 text-xs text-slate-400">
                    {app.description}
                  </CardDescription>
                )}
              </CardHeader>

              <CardContent className="space-y-4 pb-4">
                {/* Environments */}
                <div>
                  <div className="flex items-center justify-between text-xs font-semibold text-slate-400 mb-2">
                    <span className="flex items-center gap-1.5">
                      <Layers className="h-3.5 w-3.5 text-blue-400" /> Environments ({app.environments?.length ?? 0})
                    </span>
                    {isOperator && (
                      <button
                        onClick={() => {
                          setEnvName("");
                          setShowAddEnvModal(app.id);
                        }}
                        className="text-xs text-blue-400 hover:text-blue-300 hover:underline flex items-center gap-0.5 font-medium"
                      >
                        <Plus className="h-3 w-3" /> Add
                      </button>
                    )}
                  </div>
                  <div className="flex flex-wrap gap-1.5">
                    {app.environments && app.environments.length > 0 ? (
                      app.environments.map((env) => (
                        <Badge key={env.id} variant="outline" className="text-xs border-slate-700 text-slate-300">
                          {env.name}
                        </Badge>
                      ))
                    ) : (
                      <span className="text-xs italic text-slate-500">
                        No environments configured
                      </span>
                    )}
                  </div>
                </div>

                {/* Services */}
                <div>
                  <div className="flex items-center justify-between text-xs font-semibold text-slate-400 mb-2">
                    <span className="flex items-center gap-1.5">
                      <Server className="h-3.5 w-3.5 text-purple-400" /> Services ({app.services?.length ?? 0})
                    </span>
                    {isOperator && (
                      <button
                        onClick={() => {
                          setServiceName("");
                          setServiceDisplayName("");
                          setShowAddServiceModal(app.id);
                        }}
                        className="text-xs text-blue-400 hover:text-blue-300 hover:underline flex items-center gap-0.5 font-medium"
                      >
                        <Plus className="h-3 w-3" /> Add
                      </button>
                    )}
                  </div>
                  <div className="flex flex-wrap gap-1.5">
                    {app.services && app.services.length > 0 ? (
                      app.services.map((svc) => (
                        <Badge key={svc.id} variant="secondary" className="text-xs bg-slate-800 text-slate-300 border border-slate-700">
                          {svc.displayName || svc.name}
                        </Badge>
                      ))
                    ) : (
                      <span className="text-xs italic text-slate-500">
                        No services configured
                      </span>
                    )}
                  </div>
                </div>
              </CardContent>

              {/* Action Bar Footer */}
              <div className="flex items-center justify-between border-t border-slate-800 px-5 py-2.5 bg-slate-950/40 rounded-b-lg text-xs">
                <div className="flex items-center gap-3">
                  <Link
                    to={`/api-keys?app=${app.id}`}
                    className="flex items-center gap-1 text-slate-400 hover:text-blue-400 transition-colors"
                    title="View API Keys"
                  >
                    <Key className="h-3.5 w-3.5" />
                    <span>Keys</span>
                  </Link>
                  <Link
                    to={`/services?app=${app.id}`}
                    className="flex items-center gap-1 text-slate-400 hover:text-purple-400 transition-colors"
                    title="Service Health"
                  >
                    <Activity className="h-3.5 w-3.5" />
                    <span>Health</span>
                  </Link>
                  <Link
                    to={`/logs?app=${app.name}`}
                    className="flex items-center gap-1 text-slate-400 hover:text-emerald-400 transition-colors"
                    title="Application Logs"
                  >
                    <FileText className="h-3.5 w-3.5" />
                    <span>Logs</span>
                  </Link>
                </div>

                {isOperator && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-7 w-7 p-0 text-slate-400 hover:bg-rose-500/10 hover:text-rose-400"
                    onClick={() => setAppToDelete({ id: app.id, name: app.displayName })}
                    title="Delete Application"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </Button>
                )}
              </div>
            </Card>
          ))}
        </div>
      )}

      {/* Modal: Register Application */}
      <Dialog
        open={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        title="Register Application"
        description="Create a new monitored application profile to begin telemetry ingestion."
      >
        <form onSubmit={handleCreateApplication} className="space-y-4">
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-300" htmlFor="appName">
              Technical Identifier (Slug / Name)
            </label>
            <Input
              id="appName"
              placeholder="e.g. ecommerce-backend"
              value={newAppName}
              onChange={(e) => setNewAppName(e.target.value)}
              required
              className="bg-slate-950/80 border-slate-800 text-xs text-slate-200"
            />
            <p className="text-[11px] text-slate-500">
              Must be unique, lowercase, URL-friendly slug.
            </p>
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-300" htmlFor="appDisplayName">
              Display Name
            </label>
            <Input
              id="appDisplayName"
              placeholder="e.g. E-Commerce Core API"
              value={newAppDisplayName}
              onChange={(e) => setNewAppDisplayName(e.target.value)}
              className="bg-slate-950/80 border-slate-800 text-xs text-slate-200"
            />
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-300" htmlFor="appDesc">
              Description
            </label>
            <Input
              id="appDesc"
              placeholder="Brief description of this application's domain and scope"
              value={newAppDesc}
              onChange={(e) => setNewAppDesc(e.target.value)}
              className="bg-slate-950/80 border-slate-800 text-xs text-slate-200"
            />
          </div>

          <div className="flex justify-end gap-2 pt-3 border-t border-slate-800">
            <Button
              type="button"
              variant="outline"
              onClick={() => setShowCreateModal(false)}
              disabled={isSubmitting}
              className="border-slate-800 text-xs text-slate-300"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={isSubmitting || !newAppName.trim()}
              className="bg-blue-600 hover:bg-blue-500 text-white text-xs"
            >
              {isSubmitting && <Loader2 className="h-3.5 w-3.5 animate-spin mr-1.5" />}
              Register Application
            </Button>
          </div>
        </form>
      </Dialog>

      {/* Modal: Add Environment */}
      <Dialog
        open={Boolean(showAddEnvModal)}
        onClose={() => setShowAddEnvModal(null)}
        title="Add Environment"
        description="Configure a deployment stage (e.g. Production, Staging, Development)."
      >
        <form onSubmit={(e) => showAddEnvModal && handleAddEnvironment(showAddEnvModal, e)} className="space-y-4">
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-300" htmlFor="envName">
              Environment Name
            </label>
            <Input
              id="envName"
              placeholder="e.g. staging-us-east"
              value={envName}
              onChange={(e) => setEnvName(e.target.value)}
              required
              className="bg-slate-950/80 border-slate-800 text-xs text-slate-200"
            />
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-300" htmlFor="envType">
              Type
            </label>
            <select
              id="envType"
              value={envType}
              onChange={(e) => setEnvType(e.target.value)}
              className="flex h-9 w-full rounded-md border border-slate-800 bg-slate-950/80 px-3 py-1.5 text-xs text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500"
            >
              <option value="Development">Development</option>
              <option value="Staging">Staging</option>
              <option value="Production">Production</option>
              <option value="Testing">Testing</option>
            </select>
          </div>

          <div className="flex justify-end gap-2 pt-3 border-t border-slate-800">
            <Button
              type="button"
              variant="outline"
              onClick={() => setShowAddEnvModal(null)}
              disabled={isSubmitting}
              className="border-slate-800 text-xs text-slate-300"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={isSubmitting || !envName.trim()}
              className="bg-blue-600 hover:bg-blue-500 text-white text-xs"
            >
              {isSubmitting && <Loader2 className="h-3.5 w-3.5 animate-spin mr-1.5" />}
              Add Environment
            </Button>
          </div>
        </form>
      </Dialog>

      {/* Modal: Add Service */}
      <Dialog
        open={Boolean(showAddServiceModal)}
        onClose={() => setShowAddServiceModal(null)}
        title="Add Service"
        description="Register a microservice or worker component for this application."
      >
        <form onSubmit={(e) => showAddServiceModal && handleAddService(showAddServiceModal, e)} className="space-y-4">
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-300" htmlFor="svcName">
              Service Identifier
            </label>
            <Input
              id="svcName"
              placeholder="e.g. order-processing-worker"
              value={serviceName}
              onChange={(e) => setServiceName(e.target.value)}
              required
              className="bg-slate-950/80 border-slate-800 text-xs text-slate-200"
            />
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-300" htmlFor="svcDisplay">
              Display Name
            </label>
            <Input
              id="svcDisplay"
              placeholder="e.g. Order Processing Service"
              value={serviceDisplayName}
              onChange={(e) => setServiceDisplayName(e.target.value)}
              className="bg-slate-950/80 border-slate-800 text-xs text-slate-200"
            />
          </div>

          <div className="flex justify-end gap-2 pt-3 border-t border-slate-800">
            <Button
              type="button"
              variant="outline"
              onClick={() => setShowAddServiceModal(null)}
              disabled={isSubmitting}
              className="border-slate-800 text-xs text-slate-300"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={isSubmitting || !serviceName.trim()}
              className="bg-blue-600 hover:bg-blue-500 text-white text-xs"
            >
              {isSubmitting && <Loader2 className="h-3.5 w-3.5 animate-spin mr-1.5" />}
              Add Service
            </Button>
          </div>
        </form>
      </Dialog>

      {/* Deletion Confirmation Dialog */}
      <ConfirmDialog
        open={Boolean(appToDelete)}
        onClose={() => setAppToDelete(null)}
        onConfirm={handleConfirmDelete}
        title="Delete Application"
        description={`Are you sure you want to delete application "${appToDelete?.name}"? All associated environments, service registrations, and API key linkages will be permanently revoked.`}
        confirmText="Delete Application"
        variant="destructive"
        isLoading={isDeleting}
      />
    </div>
  );
}
