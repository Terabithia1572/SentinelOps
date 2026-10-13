import { BrowserRouter, Routes, Route } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "@/components/theme-provider";
import { AuthProvider } from "@/context/auth-context";
import { ProtectedRoute } from "@/components/auth/protected-route";
import { MainLayout } from "@/components/layout/main-layout";
import { LoginPage } from "@/pages/login";
import { RegisterPage } from "@/pages/register";
import { OverviewPage } from "@/pages/overview";
import { ApplicationsPage } from "@/pages/applications";
import { ApiKeysPage } from "@/pages/api-keys";
import { LogsPage } from "@/pages/logs";
import { TracesPage } from "@/pages/traces";
import { MetricsPage } from "@/pages/metrics";
import { ExceptionsPage } from "@/pages/exceptions";
import { ServiceHealthPage } from "@/pages/health";
import { PlaceholderPage } from "@/pages/placeholder";

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
                <Route path="applications" element={<ApplicationsPage />} />
                <Route path="api-keys" element={<ApiKeysPage />} />
                <Route path="services" element={<ServiceHealthPage />} />
                <Route path="logs" element={<LogsPage />} />
                <Route path="traces" element={<TracesPage />} />
                <Route path="metrics" element={<MetricsPage />} />
                <Route path="exceptions" element={<ExceptionsPage />} />
                <Route path="health" element={<ServiceHealthPage />} />
                <Route
                  path="analytics"
                  element={
                    <PlaceholderPage
                      title="Analytics"
                      description="Historical analytics and performance trends"
                    />
                  }
                />
                <Route
                  path="team"
                  element={
                    <PlaceholderPage
                      title="Team Management"
                      description="Manage users and role assignments"
                    />
                  }
                />
                <Route
                  path="audit-logs"
                  element={
                    <PlaceholderPage
                      title="Audit Logs"
                      description="System audit trail and activity history"
                    />
                  }
                />
                <Route
                  path="settings"
                  element={
                    <PlaceholderPage
                      title="Settings"
                      description="Platform configuration and preferences"
                    />
                  }
                />
              </Route>
            </Routes>
          </BrowserRouter>
        </AuthProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

export default App;
