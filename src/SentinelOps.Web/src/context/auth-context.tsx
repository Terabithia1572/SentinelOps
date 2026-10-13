import React, { createContext, useContext, useState, useEffect, useCallback } from "react";
import apiClient, { type ApiResponse, type AuthResponse, type UserProfileDto } from "@/lib/api-client";

export interface User {
  id: string;
  userName: string;
  email: string;
  fullName: string;
  roles: string[];
}

interface AuthContextType {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  isAdmin: boolean;
  isOperator: boolean;
  isViewer: boolean;
  login: (userNameOrEmail: string, password: string) => Promise<void>;
  register: (fullName: string, userName: string, email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(() => localStorage.getItem("sentinelops_token"));
  const [isLoading, setIsLoading] = useState(true);

  const isAdmin = user?.roles.includes("Admin") ?? false;
  const isOperator = user?.roles.includes("Operator") ?? isAdmin;
  const isViewer = user?.roles.includes("Viewer") ?? (isOperator || isAdmin);

  const storeTokens = (accessToken: string, refreshToken: string) => {
    localStorage.setItem("sentinelops_token", accessToken);
    localStorage.setItem("sentinelops_refresh_token", refreshToken);
    setToken(accessToken);
  };

  const clearTokens = () => {
    localStorage.removeItem("sentinelops_token");
    localStorage.removeItem("sentinelops_refresh_token");
    setToken(null);
    setUser(null);
  };

  const refreshUser = useCallback(async () => {
    const currentToken = localStorage.getItem("sentinelops_token");
    if (!currentToken) {
      setIsLoading(false);
      return;
    }

    try {
      const response = await apiClient.get<ApiResponse<UserProfileDto>>("/auth/me");
      if (response.data.success && response.data.data) {
        const u = response.data.data;
        setUser({
          id: u.id,
          userName: u.userName,
          email: u.email,
          fullName: u.fullName,
          roles: u.roles || [],
        });
      }
    } catch {
      // Try refresh token if access token is expired
      const storedRefreshToken = localStorage.getItem("sentinelops_refresh_token");
      if (storedRefreshToken) {
        try {
          const refreshRes = await apiClient.post<ApiResponse<AuthResponse>>("/auth/refresh", {
            token: currentToken,
            refreshToken: storedRefreshToken,
          });
          if (refreshRes.data.success && refreshRes.data.data) {
            const data = refreshRes.data.data;
            storeTokens(data.token, data.refreshToken);
            setUser({
              id: data.userId,
              userName: data.userName,
              email: data.email,
              fullName: data.fullName,
              roles: data.roles || [],
            });
            setIsLoading(false);
            return;
          }
        } catch {
          clearTokens();
        }
      } else {
        clearTokens();
      }
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    refreshUser();
  }, [refreshUser]);

  const login = async (userNameOrEmail: string, password: string) => {
    const response = await apiClient.post<ApiResponse<AuthResponse>>("/auth/login", {
      email: userNameOrEmail,
      password,
    });

    if (response.data.success && response.data.data) {
      const data = response.data.data;
      storeTokens(data.token, data.refreshToken);
      setUser({
        id: data.userId,
        userName: data.userName,
        email: data.email,
        fullName: data.fullName,
        roles: data.roles || [],
      });
    } else {
      throw new Error(response.data.errors?.[0] || response.data.message || "Failed to log in");
    }
  };

  const register = async (fullName: string, userName: string, email: string, password: string) => {
    const response = await apiClient.post<ApiResponse<AuthResponse>>("/auth/register", {
      fullName,
      userName,
      email,
      password,
      confirmPassword: password,
    });

    if (response.data.success && response.data.data) {
      const data = response.data.data;
      storeTokens(data.token, data.refreshToken);
      setUser({
        id: data.userId,
        userName: data.userName,
        email: data.email,
        fullName: data.fullName,
        roles: data.roles || [],
      });
    } else {
      throw new Error(response.data.errors?.[0] || response.data.message || "Registration failed");
    }
  };

  const logout = async () => {
    const storedRefreshToken = localStorage.getItem("sentinelops_refresh_token");
    if (storedRefreshToken) {
      try {
        await apiClient.post("/auth/revoke", { refreshToken: storedRefreshToken });
      } catch {
        // Ignore network errors on logout
      }
    }
    clearTokens();
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isAuthenticated: !!user && !!token,
        isLoading,
        isAdmin,
        isOperator,
        isViewer,
        login,
        register,
        logout,
        refreshUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
