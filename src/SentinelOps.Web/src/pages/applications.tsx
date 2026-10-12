import React, { useState, useEffect, useCallback } from "react";
import apiClient, { type ApiResponse, type ApplicationDto } from "@/lib/api-client";
import { useAuth } from "@/context/auth-context";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  AppWindow,
  Plus,
  Search,
  Server,
  Layers,
  Trash2,
  Loader2,
  AlertCircle,
  X,
  ExternalLink,
} from "lucide-react";
import { Link } from "react-router-dom";

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
      alert(err instanceof Error ? err.message : "Failed to create application");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleAddEnvironment = async (appId: string, e: React.FormEvent) => {
    e.preventDefault();
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
      alert(err instanceof Error ? err.message : "Failed to add environment");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleAddService = async (appId: string, e: React.FormEvent) => {
    e.preventDefault();
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
      alert(err instanceof Error ? err.message : "Failed to add service");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDeleteApplication = async (appId: string, appName: string) => {
    if (!window.confirm(`Are you sure you want to delete application "${appName}"? This cannot be undone.`)) {
      return;
    }
    try {
      const res = await apiClient.delete<ApiResponse<boolean>>(`/applications/${appId}`);
      if (res.data.success) {
        await fetchApplications();
      }
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Failed to delete application");
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
          <h1 className="text-3xl font-bold tracking-tight">Applications</h1>
          <p className="text-sm text-[hsl(var(--muted-foreground))]">
            Manage monitored software systems, environments, and connected microservices
          </p>
        </div>
        {isOperator && (
          <Button onClick={() => setShowCreateModal(true)} className="gap-2">
            <Plus className="h-4 w-4" />
            New Application
          </Button>
        )}
      </div>

      {/* Filter / Search Bar */}
      <div className="flex items-center gap-4">
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[hsl(var(--muted-foreground))]" />
          <Input
            placeholder="Search applications..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-9"
          />
        </div>
      </div>

      {/* Error display */}
      {errorMessage && (
        <div className="flex items-center gap-2 rounded-md border border-rose-500/20 bg-rose-500/10 p-4 text-sm text-rose-400">
          <AlertCircle className="h-5 w-5 shrink-0" />
          <span>{errorMessage}</span>
        </div>
      )}

      {/* Loading state */}
      {isLoading ? (
        <div className="flex h-64 items-center justify-center">
          <Loader2 className="h-8 w-8 animate-spin text-[hsl(var(--primary))]" />
        </div>
      ) : filteredApps.length === 0 ? (
        <Card className="flex flex-col items-center justify-center py-16 text-center border-dashed">
          <AppWindow className="h-12 w-12 text-[hsl(var(--muted-foreground))] mb-3" />
          <CardTitle className="text-lg">No applications found</CardTitle>
          <CardDescription className="max-w-sm mt-1">
            {searchQuery
              ? "No applications matched your search query."
              : "Register your first monitored application to begin collecting telemetry."}
          </CardDescription>
          {isOperator && !searchQuery && (
            <Button onClick={() => setShowCreateModal(true)} className="mt-4 gap-2">
              <Plus className="h-4 w-4" />
              Register Application
            </Button>
          )}
        </Card>
      ) : (
        <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
          {filteredApps.map((app) => (
            <Card key={app.id} className="flex flex-col justify-between hover:border-[hsl(var(--primary)/0.5)] transition-colors">
              <CardHeader className="pb-3">
                <div className="flex items-start justify-between gap-2">
                  <div>
                    <CardTitle className="text-xl font-bold">{app.displayName}</CardTitle>
                    <span className="font-mono text-xs text-[hsl(var(--muted-foreground))]">
                      {app.name}
                    </span>
                  </div>
                  <Badge variant={app.isActive ? "success" : "secondary"}>
                    {app.isActive ? "Active" : "Disabled"}
                  </Badge>
                </div>
                {app.description && (
                  <CardDescription className="line-clamp-2 mt-2">
                    {app.description}
                  </CardDescription>
                )}
              </CardHeader>

              <CardContent className="space-y-4 pb-4">
                {/* Environments */}
                <div>
                  <div className="flex items-center justify-between text-xs font-semibold text-[hsl(var(--muted-foreground))] mb-2">
                    <span className="flex items-center gap-1">
                      <Layers className="h-3.5 w-3.5" /> Environments ({app.environments?.length ?? 0})
                    </span>
                    {isOperator && (
                      <button
                        onClick={() => {
                          setEnvName("");
                          setShowAddEnvModal(app.id);
                        }}
                        className="text-[hsl(var(--primary))] hover:underline flex items-center gap-0.5"
                      >
                        <Plus className="h-3 w-3" /> Add
                      </button>
                    )}
                  </div>
                  <div className="flex flex-wrap gap-1.5">
                    {app.environments && app.environments.length > 0 ? (
                      app.environments.map((env) => (
                        <Badge key={env.id} variant="outline" className="text-xs">
                          {env.name}
                        </Badge>
                      ))
                    ) : (
                      <span className="text-xs italic text-[hsl(var(--muted-foreground))]">
                        No environments configured
                      </span>
                    )}
                  </div>
                </div>

                {/* Services */}
                <div>
                  <div className="flex items-center justify-between text-xs font-semibold text-[hsl(var(--muted-foreground))] mb-2">
                    <span className="flex items-center gap-1">
                      <Server className="h-3.5 w-3.5" /> Services ({app.services?.length ?? 0})
                    </span>
                    {isOperator && (
                      <button
                        onClick={() => {
                          setServiceName("");
                          setServiceDisplayName("");
                          setShowAddServiceModal(app.id);
                        }}
                        className="text-[hsl(var(--primary))] hover:underline flex items-center gap-0.5"
                      >
                        <Plus className="h-3 w-3" /> Add
                      </button>
                    )}
                  </div>
                  <div className="flex flex-wrap gap-1.5">
                    {app.services && app.services.length > 0 ? (
                      app.services.map((svc) => (
                        <Badge key={svc.id} variant="secondary" className="text-xs">
                          {svc.displayName || svc.name}
                        </Badge>
                      ))
                    ) : (
                      <span className="text-xs italic text-[hsl(var(--muted-foreground))]">
                        No services configured
                      </span>
                    )}
                  </div>
                </div>
              </CardContent>

              <div className="flex items-center justify-between border-t border-[hsl(var(--border))] px-6 py-3 bg-[hsl(var(--accent)/0.2)] rounded-b-lg">
                <Link
                  to={`/api-keys?app=${app.id}`}
                  className="flex items-center gap-1 text-xs font-medium text-[hsl(var(--primary))] hover:underline"
                >
                  <ExternalLink className="h-3.5 w-3.5" /> API Keys
                </Link>

                {isOperator && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-8 w-8 p-0 text-rose-400 hover:bg-rose-500/10 hover:text-rose-300"
                    onClick={() => handleDeleteApplication(app.id, app.displayName)}
                    title="Delete Application"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                )}
              </div>
            </Card>
          ))}
        </div>
      )}

      {/* Modal: Create Application */}
      {showCreateModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-xs">
          <Card className="w-full max-w-lg border-[hsl(var(--border))] shadow-2xl">
            <CardHeader className="flex flex-row items-center justify-between">
              <div>
                <CardTitle className="text-xl">Register Application</CardTitle>
                <CardDescription>Create a new monitored application profile</CardDescription>
              </div>
              <Button
                variant="ghost"
                size="sm"
                className="h-8 w-8 p-0"
                onClick={() => setShowCreateModal(false)}
              >
                <X className="h-4 w-4" />
              </Button>
            </CardHeader>
            <form onSubmit={handleCreateApplication}>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <label className="text-sm font-medium" htmlFor="appName">
                    Technical Identifier (Slug / Name)
                  </label>
                  <Input
                    id="appName"
                    placeholder="e.g. ecommerce-backend"
                    value={newAppName}
                    onChange={(e) => setNewAppName(e.target.value)}
                    required
                  />
                  <p className="text-xs text-[hsl(var(--muted-foreground))]">
                    Must be unique, lowercase, URL-friendly slug.
                  </p>
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-medium" htmlFor="appDisplayName">
                    Display Name
                  </label>
                  <Input
                    id="appDisplayName"
                    placeholder="e.g. E-Commerce Core API"
                    value={newAppDisplayName}
                    onChange={(e) => setNewAppDisplayName(e.target.value)}
                  />
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-medium" htmlFor="appDesc">
                    Description
                  </label>
                  <Input
                    id="appDesc"
                    placeholder="Brief description of this application's domain and scope"
                    value={newAppDesc}
                    onChange={(e) => setNewAppDesc(e.target.value)}
                  />
                </div>
              </CardContent>
              <div className="flex justify-end gap-2 border-t border-[hsl(var(--border))] px-6 py-4">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setShowCreateModal(false)}
                  disabled={isSubmitting}
                >
                  Cancel
                </Button>
                <Button type="submit" disabled={isSubmitting || !newAppName.trim()}>
                  {isSubmitting && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
                  Register Application
                </Button>
              </div>
            </form>
          </Card>
        </div>
      )}

      {/* Modal: Add Environment */}
      {showAddEnvModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-xs">
          <Card className="w-full max-w-md border-[hsl(var(--border))] shadow-2xl">
            <CardHeader className="flex flex-row items-center justify-between">
              <div>
                <CardTitle className="text-lg">Add Environment</CardTitle>
                <CardDescription>Configure a deployment stage</CardDescription>
              </div>
              <Button
                variant="ghost"
                size="sm"
                className="h-8 w-8 p-0"
                onClick={() => setShowAddEnvModal(null)}
              >
                <X className="h-4 w-4" />
              </Button>
            </CardHeader>
            <form onSubmit={(e) => handleAddEnvironment(showAddEnvModal, e)}>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <label className="text-sm font-medium" htmlFor="envName">
                    Environment Name
                  </label>
                  <Input
                    id="envName"
                    placeholder="e.g. staging-us-east"
                    value={envName}
                    onChange={(e) => setEnvName(e.target.value)}
                    required
                  />
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-medium" htmlFor="envType">
                    Type
                  </label>
                  <select
                    id="envType"
                    value={envType}
                    onChange={(e) => setEnvType(e.target.value)}
                    className="flex h-10 w-full rounded-md border border-[hsl(var(--border))] bg-[hsl(var(--background))] px-3 py-2 text-sm"
                  >
                    <option value="Development">Development</option>
                    <option value="Staging">Staging</option>
                    <option value="Production">Production</option>
                    <option value="Testing">Testing</option>
                  </select>
                </div>
              </CardContent>
              <div className="flex justify-end gap-2 border-t border-[hsl(var(--border))] px-6 py-4">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setShowAddEnvModal(null)}
                  disabled={isSubmitting}
                >
                  Cancel
                </Button>
                <Button type="submit" disabled={isSubmitting || !envName.trim()}>
                  {isSubmitting && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
                  Add Environment
                </Button>
              </div>
            </form>
          </Card>
        </div>
      )}

      {/* Modal: Add Service */}
      {showAddServiceModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-xs">
          <Card className="w-full max-w-md border-[hsl(var(--border))] shadow-2xl">
            <CardHeader className="flex flex-row items-center justify-between">
              <div>
                <CardTitle className="text-lg">Add Service</CardTitle>
                <CardDescription>Register a microservice or worker daemon</CardDescription>
              </div>
              <Button
                variant="ghost"
                size="sm"
                className="h-8 w-8 p-0"
                onClick={() => setShowAddServiceModal(null)}
              >
                <X className="h-4 w-4" />
              </Button>
            </CardHeader>
            <form onSubmit={(e) => handleAddService(showAddServiceModal, e)}>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <label className="text-sm font-medium" htmlFor="svcName">
                    Service Identifier
                  </label>
                  <Input
                    id="svcName"
                    placeholder="e.g. order-processing-worker"
                    value={serviceName}
                    onChange={(e) => setServiceName(e.target.value)}
                    required
                  />
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-medium" htmlFor="svcDisplay">
                    Display Name
                  </label>
                  <Input
                    id="svcDisplay"
                    placeholder="e.g. Order Processing Service"
                    value={serviceDisplayName}
                    onChange={(e) => setServiceDisplayName(e.target.value)}
                  />
                </div>
              </CardContent>
              <div className="flex justify-end gap-2 border-t border-[hsl(var(--border))] px-6 py-4">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setShowAddServiceModal(null)}
                  disabled={isSubmitting}
                >
                  Cancel
                </Button>
                <Button type="submit" disabled={isSubmitting || !serviceName.trim()}>
                  {isSubmitting && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
                  Add Service
                </Button>
              </div>
            </form>
          </Card>
        </div>
      )}
    </div>
  );
}
