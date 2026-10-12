import axios from "axios";

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || "/api",
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 30000,
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("sentinelops_token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem("sentinelops_token");
      window.location.href = "/login";
    }
    return Promise.reject(error);
  }
);

export default apiClient;

// API types
export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string | null;
  errors: string[];
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface DashboardSummary {
  totalApplications: number;
  totalServices: number;
  activeAlerts: number;
  healthyServices: number;
  errorRate: number;
  avgResponseTimeMs: number;
  totalLogsToday: number;
  totalTracesToday: number;
}

export interface ApplicationDto {
  id: string;
  name: string;
  displayName: string;
  description: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface HealthCheckDto {
  status: string;
  totalDuration: string;
  entries: Record<string, { status: string; description: string }>;
}
