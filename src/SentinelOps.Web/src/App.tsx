import { lazy, Suspense } from "react";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "@/components/theme-provider";
import { AuthProvider } from "@/context/auth-context";
import { TenantProvider } from "@/context/tenant-context";
import { ProtectedRoute } from "@/components/auth/protected-route";
import { MainLayout } from "@/components/layout/main-layout";

// Eager public auth & external status pages for fast initial landing
import { LoginPage } from "@/pages/login";
import { RegisterPage } from "@/pages/register";
import { PublicStatusPage } from "@/pages/public-status";

// Code-split application pages for optimized bundle size & fast lazy chunking
const OverviewPage = lazy(() => import("@/pages/overview").then(m => ({ default: m.OverviewPage })));
const DashboardsPage = lazy(() => import("@/pages/dashboards").then(m => ({ default: m.DashboardsPage })));
const ApmPage = lazy(() => import("@/pages/apm").then(m => ({ default: m.ApmPage })));
const OrganizationsPage = lazy(() => import("@/pages/organizations").then(m => ({ default: m.OrganizationsPage })));
const ApplicationsPage = lazy(() => import("@/pages/applications").then(m => ({ default: m.ApplicationsPage })));
const ApiKeysPage = lazy(() => import("@/pages/api-keys").then(m => ({ default: m.ApiKeysPage })));
const LogsPage = lazy(() => import("@/pages/logs").then(m => ({ default: m.LogsPage })));
const TracesPage = lazy(() => import("@/pages/traces").then(m => ({ default: m.TracesPage })));
const MetricsPage = lazy(() => import("@/pages/metrics").then(m => ({ default: m.MetricsPage })));
const ExceptionsPage = lazy(() => import("@/pages/exceptions").then(m => ({ default: m.ExceptionsPage })));
const ServiceHealthPage = lazy(() => import("@/pages/health").then(m => ({ default: m.ServiceHealthPage })));
const AlertsPage = lazy(() => import("@/pages/alerts").then(m => ({ default: m.AlertsPage })));
const AlertRulesPage = lazy(() => import("@/pages/alert-rules").then(m => ({ default: m.AlertRulesPage })));
const IncidentsPage = lazy(() => import("@/pages/incidents").then(m => ({ default: m.IncidentsPage })));
const IncidentDetailPage = lazy(() => import("@/pages/incident-detail").then(m => ({ default: m.IncidentDetailPage })));
const SlosPage = lazy(() => import("@/pages/slos").then(m => ({ default: m.SlosPage })));
const UsersPage = lazy(() => import("@/pages/users").then(m => ({ default: m.UsersPage })));
const AuditLogsPage = lazy(() => import("@/pages/audit-logs").then(m => ({ default: m.AuditLogsPage })));
const ReportsPage = lazy(() => import("@/pages/reports").then(m => ({ default: m.ReportsPage })));
const SettingsPage = lazy(() => import("@/pages/settings").then(m => ({ default: m.SettingsPage })));
const StatusPagesPage = lazy(() => import("@/pages/status-pages").then(m => ({ default: m.StatusPagesPage })));
const AiObservabilityPage = lazy(() => import("@/pages/ai-observability").then(m => ({ default: m.AiObservabilityPage })));
const OnboardingPage = lazy(() => import("@/pages/onboarding").then(m => ({ default: m.OnboardingPage })));
const BillingPage = lazy(() => import("@/pages/billing").then(m => ({ default: m.BillingPage })));
const NotFoundPage = lazy(() => import("@/pages/not-found").then(m => ({ default: m.NotFoundPage })));

function RouteFallback() {
  return (
    <div className="flex h-64 w-full items-center justify-center" role="status" aria-label="Loading page">
      <div className="h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent" />
    </div>
  );
}

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30 * 1000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider defaultTheme="dark" storageKey="sentinelops-ui-theme">
        <AuthProvider>
          <TenantProvider>
            <BrowserRouter>
              <Suspense fallback={<RouteFallback />}>
                <Routes>
                  {/* Public Authentication & External Status Routes */}
                  <Route path="/login" element={<LoginPage />} />
                  <Route path="/register" element={<RegisterPage />} />
                  <Route path="/status/:slug" element={<PublicStatusPage />} />
                  <Route
                    path="/onboarding"
                    element={
                      <ProtectedRoute>
                        <OnboardingPage />
                      </ProtectedRoute>
                    }
                  />

                  {/* Protected Platform Routes */}
                  <Route
                    path="/"
                    element={
                      <ProtectedRoute>
                        <MainLayout />
                      </ProtectedRoute>
                    }
                  >
                    <Route index element={<OverviewPage />} />
                    <Route path="overview" element={<OverviewPage />} />
                    <Route path="dashboards" element={<DashboardsPage />} />
                    <Route path="apm" element={<ApmPage />} />
                    <Route path="ai-observability" element={<AiObservabilityPage />} />
                    <Route path="organizations" element={<OrganizationsPage />} />
                    <Route path="applications" element={<ApplicationsPage />} />
                    <Route path="api-keys" element={<ApiKeysPage />} />
                    <Route path="services" element={<ServiceHealthPage />} />
                    <Route path="logs" element={<LogsPage />} />
                    <Route path="traces" element={<TracesPage />} />
                    <Route path="metrics" element={<MetricsPage />} />
                    <Route path="exceptions" element={<ExceptionsPage />} />
                    <Route path="health" element={<ServiceHealthPage />} />
                    <Route path="alerts" element={<AlertsPage />} />
                    <Route path="alert-rules" element={<AlertRulesPage />} />
                    <Route path="incidents" element={<IncidentsPage />} />
                    <Route path="incidents/:id" element={<IncidentDetailPage />} />
                    <Route path="status-pages" element={<StatusPagesPage />} />
                    <Route path="slos" element={<SlosPage />} />
                    <Route path="analytics" element={<ReportsPage />} />
                    <Route path="reports" element={<ReportsPage />} />
                    <Route path="team" element={<UsersPage />} />
                    <Route path="users" element={<UsersPage />} />
                    <Route path="audit-logs" element={<AuditLogsPage />} />
                    <Route path="billing" element={<BillingPage />} />
                    <Route path="subscription" element={<BillingPage />} />
                    <Route path="settings" element={<SettingsPage />} />
                  </Route>

                  {/* Catch-all 404 Route */}
                  <Route path="*" element={<NotFoundPage />} />
                </Routes>
              </Suspense>
            </BrowserRouter>
          </TenantProvider>
        </AuthProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

export default App;
