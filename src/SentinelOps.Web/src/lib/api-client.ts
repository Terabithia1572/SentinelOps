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
    if (error.response?.status === 401 && !window.location.pathname.startsWith("/login") && !window.location.pathname.startsWith("/register")) {
      localStorage.removeItem("sentinelops_token");
      localStorage.removeItem("sentinelops_refresh_token");
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
  environments?: EnvironmentDto[];
  services?: ServiceDto[];
}

export interface EnvironmentDto {
  id: string;
  applicationId: string;
  name: string;
  environmentType: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface ServiceDto {
  id: string;
  applicationId: string;
  name: string;
  displayName: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface AuthResponse {
  userId: string;
  token: string;
  refreshToken: string;
  expiration: string;
  userName: string;
  email: string;
  fullName: string;
  roles: string[];
}

export interface UserProfileDto {
  id: string;
  userName: string;
  email: string;
  fullName: string;
  isActive: boolean;
  createdAtUtc: string;
  roles: string[];
}

export interface ApiKeyDto {
  id: string;
  applicationId: string;
  name: string;
  keyPrefix: string;
  createdAtUtc: string;
  expiresAtUtc: string | null;
  revokedAtUtc: string | null;
  lastUsedAtUtc: string | null;
  createdBy: string;
  isActive: boolean;
}

export interface ApiKeyCreatedResponse {
  id: string;
  applicationId: string;
  name: string;
  keyPrefix: string;
  rawApiKey: string;
  createdAtUtc: string;
  expiresAtUtc: string | null;
}

export interface HealthCheckDto {
  status: string;
  totalDuration: string;
  entries: Record<string, { status: string; description: string }>;
}
