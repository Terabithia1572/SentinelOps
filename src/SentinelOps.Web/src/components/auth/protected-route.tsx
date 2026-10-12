import React from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "@/context/auth-context";
import { ShieldAlert, Loader2 } from "lucide-react";

interface ProtectedRouteProps {
  children?: React.ReactNode;
  requiredRole?: "Admin" | "Operator" | "Viewer";
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children, requiredRole }) => {
  const { isAuthenticated, isLoading, user } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return (
      <div className="flex h-screen w-full items-center justify-center bg-[hsl(var(--background))]">
        <div className="flex flex-col items-center gap-3">
          <Loader2 className="h-8 w-8 animate-spin text-[hsl(var(--primary))]" />
          <p className="text-sm text-[hsl(var(--muted-foreground))]">Loading SentinelOps...</p>
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (requiredRole) {
    const roles = user?.roles || [];
    const hasAdmin = roles.includes("Admin");
    const hasOperator = roles.includes("Operator") || hasAdmin;
    const hasViewer = roles.includes("Viewer") || hasOperator || hasAdmin;

    let authorized = false;
    if (requiredRole === "Admin" && hasAdmin) authorized = true;
    if (requiredRole === "Operator" && hasOperator) authorized = true;
    if (requiredRole === "Viewer" && hasViewer) authorized = true;

    if (!authorized) {
      return (
        <div className="flex h-[80vh] flex-col items-center justify-center text-center">
          <ShieldAlert className="h-16 w-16 text-rose-500 mb-4" />
          <h2 className="text-2xl font-bold tracking-tight">Access Restricted</h2>
          <p className="mt-2 text-sm text-[hsl(var(--muted-foreground))] max-w-md">
            Your role (<span className="font-semibold">{roles.join(", ") || "None"}</span>) does not have sufficient permissions to view this resource. Requires <span className="font-semibold">{requiredRole}</span> privileges.
          </p>
        </div>
      );
    }
  }

  return children ? <>{children}</> : null;
};
