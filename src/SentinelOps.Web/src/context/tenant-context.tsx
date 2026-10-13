import React, { createContext, useContext, useEffect, useState, useCallback } from "react";
import { getUserOrganizations, type OrganizationDto } from "../lib/api-client";
import { useAuth } from "./auth-context";

interface TenantContextType {
  organizations: OrganizationDto[];
  activeOrg: OrganizationDto | null;
  setActiveOrg: (org: OrganizationDto) => void;
  refreshOrganizations: () => Promise<void>;
  isLoading: boolean;
}

const TenantContext = createContext<TenantContextType | undefined>(undefined);

export const TenantProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated } = useAuth();
  const [organizations, setOrganizations] = useState<OrganizationDto[]>([]);
  const [activeOrg, setActiveOrgState] = useState<OrganizationDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const refreshOrganizations = useCallback(async () => {
    if (!isAuthenticated) {
      setOrganizations([]);
      setActiveOrgState(null);
      localStorage.removeItem("sentinelops_active_org_id");
      return;
    }

    try {
      setIsLoading(true);
      const orgs = await getUserOrganizations();
      setOrganizations(orgs);

      const savedOrgId = localStorage.getItem("sentinelops_active_org_id");
      const matched = orgs.find((o) => o.id === savedOrgId);

      if (matched) {
        setActiveOrgState(matched);
      } else if (orgs.length > 0) {
        setActiveOrgState(orgs[0]);
        localStorage.setItem("sentinelops_active_org_id", orgs[0].id);
      } else {
        setActiveOrgState(null);
        localStorage.removeItem("sentinelops_active_org_id");
      }
    } catch {
      // In unauthenticated or error state
    } finally {
      setIsLoading(false);
    }
  }, [isAuthenticated]);

  useEffect(() => {
    refreshOrganizations();
  }, [refreshOrganizations]);

  const setActiveOrg = (org: OrganizationDto) => {
    setActiveOrgState(org);
    localStorage.setItem("sentinelops_active_org_id", org.id);
  };

  return (
    <TenantContext.Provider
      value={{
        organizations,
        activeOrg,
        setActiveOrg,
        refreshOrganizations,
        isLoading,
      }}
    >
      {children}
    </TenantContext.Provider>
  );
};

export const useTenant = () => {
  const context = useContext(TenantContext);
  if (!context) {
    throw new Error("useTenant must be used within a TenantProvider");
  }
  return context;
};
