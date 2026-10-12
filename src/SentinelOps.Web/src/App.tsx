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
                <Route
                  path="services"
                  element={
                    <PlaceholderPage
                      title="Services"
                      description="View service instances and dependencies"
                    />
                  }
                />
                <Route
                  path="logs"
                  element={
                    <PlaceholderPage
                      title="Log Explorer"
                      description="Search and analyze structured application logs"
                    />
                  }
                />
                <Route
                  path="traces"
                  element={
                    <PlaceholderPage
                      title="Distributed Traces"
                      description="Explore distributed traces and span timelines"
                    />
                  }
                />
                <Route
                  path="metrics"
                  element={
                    <PlaceholderPage
                      title="Metrics"
                      description="Application performance metrics and dashboards"
                    />
                  }
                />
                <Route
                  path="exceptions"
                  element={
                    <PlaceholderPage
                      title="Exceptions"
                      description="Exception monitoring and error grouping"
                    />
                  }
                />
                <Route
                  path="alerts"
                  element={
                    <PlaceholderPage
                      title="Alert Rules"
                      description="Configure and manage alert rules"
                    />
                  }
                />
                <Route
                  path="incidents"
                  element={
                    <PlaceholderPage
                      title="Incidents"
                      description="Active and resolved alert incidents"
                    />
                  }
                />
                <Route
                  path="health"
                  element={
                    <PlaceholderPage
                      title="Health Monitoring"
                      description="Service health checks and availability"
                    />
                  }
                />
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
