import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import type { AlertEventDto, IncidentDto, InAppNotificationDto } from "@/lib/api-client";

export type SignalRConnectionStatus = "connected" | "connecting" | "reconnecting" | "disconnected";

interface UseAlertsHubOptions {
  applicationId?: string;
  onAlertFired?: (alert: AlertEventDto) => void;
  onAlertResolved?: (alert: AlertEventDto) => void;
  onIncidentUpdated?: (incident: IncidentDto) => void;
  onNotificationReceived?: (notification: InAppNotificationDto) => void;
}

export function useAlertsHub(options: UseAlertsHubOptions = {}) {
  const [isConnected, setIsConnected] = useState(false);
  const [status, setStatus] = useState<SignalRConnectionStatus>("disconnected");
  const [lastAlert, setLastAlert] = useState<AlertEventDto | null>(null);
  const [lastIncident, setLastIncident] = useState<IncidentDto | null>(null);
  const [lastNotification, setLastNotification] = useState<InAppNotificationDto | null>(null);

  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const queryClient = useQueryClient();
  const optionsRef = useRef(options);
  optionsRef.current = options;

  useEffect(() => {
    const token = localStorage.getItem("sentinelops_token");
    if (!token) {
      setStatus("disconnected");
      setIsConnected(false);
      return;
    }

    setStatus("connecting");

    const connection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/alerts", {
        accessTokenFactory: () => localStorage.getItem("sentinelops_token") || "",
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connectionRef.current = connection;

    connection.on("AlertFired", (alert: AlertEventDto) => {
      setLastAlert(alert);
      queryClient.invalidateQueries({ queryKey: ["alerts"] });
      queryClient.invalidateQueries({ queryKey: ["alert-summary"] });
      queryClient.invalidateQueries({ queryKey: ["alert-rules"] });
      queryClient.invalidateQueries({ queryKey: ["incidents"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
      queryClient.invalidateQueries({ queryKey: ["overview-incidents"] });
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
      optionsRef.current.onAlertFired?.(alert);
    });

    connection.on("AlertResolved", (alert: AlertEventDto) => {
      setLastAlert(alert);
      queryClient.invalidateQueries({ queryKey: ["alerts"] });
      queryClient.invalidateQueries({ queryKey: ["alert-summary"] });
      queryClient.invalidateQueries({ queryKey: ["alert-rules"] });
      queryClient.invalidateQueries({ queryKey: ["incidents"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
      queryClient.invalidateQueries({ queryKey: ["overview-incidents"] });
      optionsRef.current.onAlertResolved?.(alert);
    });

    connection.on("IncidentUpdated", (incident: IncidentDto) => {
      setLastIncident(incident);
      queryClient.invalidateQueries({ queryKey: ["incidents"] });
      queryClient.invalidateQueries({ queryKey: ["incident", incident.id] });
      queryClient.invalidateQueries({ queryKey: ["alert-summary"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard-summary"] });
      queryClient.invalidateQueries({ queryKey: ["overview-incidents"] });
      optionsRef.current.onIncidentUpdated?.(incident);
    });

    connection.on("NotificationReceived", (notification: InAppNotificationDto) => {
      setLastNotification(notification);
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
      optionsRef.current.onNotificationReceived?.(notification);
    });

    connection
      .start()
      .then(() => {
        setIsConnected(true);
        setStatus("connected");
        if (optionsRef.current.applicationId) {
          connection.invoke("JoinApplicationGroup", optionsRef.current.applicationId).catch(() => {});
        }
      })
      .catch((err: Error) => {
        // Ignore expected unmount / navigation cancellation errors
        if (
          err?.message?.includes("stopped during negotiation") ||
          err?.name === "AbortError"
        ) {
          return;
        }
        console.warn("SignalR connection error:", err);
        setIsConnected(false);
        setStatus("disconnected");
      });

    connection.onreconnecting(() => {
      setIsConnected(false);
      setStatus("reconnecting");
    });

    connection.onreconnected(() => {
      setIsConnected(true);
      setStatus("connected");
      if (optionsRef.current.applicationId) {
        connection.invoke("JoinApplicationGroup", optionsRef.current.applicationId).catch(() => {});
      }
    });

    connection.onclose(() => {
      setIsConnected(false);
      setStatus("disconnected");
    });

    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop().catch(() => {});
        connectionRef.current = null;
      }
    };
  }, [queryClient]);

  // Handle application group changes
  useEffect(() => {
    const connection = connectionRef.current;
    if (connection && isConnected && options.applicationId) {
      connection.invoke("JoinApplicationGroup", options.applicationId).catch(() => {});
    }
  }, [options.applicationId, isConnected]);

  return {
    isConnected,
    status,
    lastAlert,
    lastIncident,
    lastNotification,
  };
}
