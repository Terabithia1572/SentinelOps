import { BrowserRouter, Routes, Route } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "@/components/theme-provider";
import { AuthProvider } from "@/context/auth-context";
import { TenantProvider } from "@/context/tenant-context";
import { ProtectedRoute } from "@/components/auth/protected-route";
import { MainLayout } from "@/components/layout/main-layout";
import { LoginPage } from "@/pages/login";
import { RegisterPage } from "@/pages/register";
import { OverviewPage } from "@/pages/overview";
import { DashboardsPage } from "@/pages/dashboards";
import { OrganizationsPage } from "@/pages/organizations";
import { ApplicationsPage } from "@/pages/applications";
import { ApiKeysPage } from "@/pages/api-keys";
import { LogsPage } from "@/pages/logs";
import { TracesPage } from "@/pages/traces";
import { MetricsPage } from "@/pages/metrics";
import { ExceptionsPage } from "@/pages/exceptions";
import { ServiceHealthPage } from "@/pages/health";
import { AlertsPage } from "@/pages/alerts";
import { AlertRulesPage } from "@/pages/alert-rules";
import { IncidentsPage } from "@/pages/incidents";
import { IncidentDetailPage } from "@/pages/incident-detail";
import { SlosPage } from "@/pages/slos";
import { UsersPage } from "@/pages/users";
import { AuditLogsPage } from "@/pages/audit-logs";
import { ReportsPage } from "@/pages/reports";
import { SettingsPage } from "@/pages/settings";

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
              <Routes>
                {/* Public Authentication Routes */}
                <Route path="/login" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />

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
                  <Route path="slos" element={<SlosPage />} />
                  <Route path="analytics" element={<ReportsPage />} />
                  <Route path="reports" element={<ReportsPage />} />
                  <Route path="team" element={<UsersPage />} />
                  <Route path="users" element={<UsersPage />} />
                  <Route path="audit-logs" element={<AuditLogsPage />} />
                  <Route path="settings" element={<SettingsPage />} />
                </Route>

              </Routes>
            </BrowserRouter>
          </TenantProvider>
        </AuthProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

export default App;
