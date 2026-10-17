import { renderHook, act } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import React from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useAlertsHub } from "../use-alerts-hub";

const { mockConnection, mockCallbacks } = vi.hoisted(() => {
  const handlers: Record<string, Function> = {};
  const callbacks: {
    reconnecting: Function | null;
    reconnected: Function | null;
    close: Function | null;
    handlers: Record<string, Function>;
  } = {
    reconnecting: null,
    reconnected: null,
    close: null,
    handlers,
  };

  const connection = {
    on: vi.fn((event: string, handler: Function) => {
      handlers[event] = handler;
    }),
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    invoke: vi.fn().mockResolvedValue(undefined),
    onreconnecting: vi.fn((cb: Function) => {
      callbacks.reconnecting = cb;
    }),
    onreconnected: vi.fn((cb: Function) => {
      callbacks.reconnected = cb;
    }),
    onclose: vi.fn((cb: Function) => {
      callbacks.close = cb;
    }),
  };

  class MockBuilder {
    withUrl() {
      return this;
    }
    withAutomaticReconnect() {
      return this;
    }
    configureLogging() {
      return this;
    }
    build() {
      return connection;
    }
  }

  return { mockConnection: connection, mockCallbacks: callbacks, MockHubConnectionBuilder: MockBuilder };
});

vi.mock("@microsoft/signalr", () => {
  class MockHubConnectionBuilder {
    withUrl() {
      return this;
    }
    withAutomaticReconnect() {
      return this;
    }
    configureLogging() {
      return this;
    }
    build() {
      return mockConnection;
    }
  }

  return {
    HubConnectionBuilder: MockHubConnectionBuilder,
    HttpTransportType: {
      WebSockets: 1,
      LongPolling: 4,
    },
    LogLevel: {
      Warning: 2,
    },
  };
});

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });
  return {
    queryClient,
    wrapper: ({ children }: { children: React.ReactNode }) =>
      React.createElement(QueryClientProvider, { client: queryClient }, children),
  };
}

describe("useAlertsHub hook", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    Object.keys(mockCallbacks.handlers).forEach((key) => delete mockCallbacks.handlers[key]);
    mockCallbacks.reconnecting = null;
    mockCallbacks.reconnected = null;
    mockCallbacks.close = null;
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it("remains disconnected if no authentication token is present in localStorage", () => {
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useAlertsHub(), { wrapper });

    expect(result.current.isConnected).toBe(false);
    expect(result.current.status).toBe("disconnected");
    expect(mockConnection.start).not.toHaveBeenCalled();
  });

  it("connects to SignalR hub when token is present and updates state", async () => {
    localStorage.setItem("sentinelops_token", "fake-jwt-token");
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useAlertsHub(), { wrapper });

    expect(mockConnection.start).toHaveBeenCalled();
    await act(async () => {
      await Promise.resolve();
    });

    expect(result.current.isConnected).toBe(true);
    expect(result.current.status).toBe("connected");
  });

  it("joins application group if applicationId is provided", async () => {
    localStorage.setItem("sentinelops_token", "fake-jwt-token");
    const { wrapper } = createWrapper();
    renderHook(() => useAlertsHub({ applicationId: "app-123" }), { wrapper });

    await act(async () => {
      await Promise.resolve();
    });

    expect(mockConnection.invoke).toHaveBeenCalledWith("JoinApplicationGroup", "app-123");
  });

  it("handles AlertFired event, invalidates queries and calls onAlertFired", async () => {
    localStorage.setItem("sentinelops_token", "fake-jwt-token");
    const onAlertFired = vi.fn();
    const { wrapper, queryClient } = createWrapper();
    const invalidateSpy = vi.spyOn(queryClient, "invalidateQueries");

    const { result } = renderHook(() => useAlertsHub({ onAlertFired }), { wrapper });

    await act(async () => {
      await Promise.resolve();
    });

    const fakeAlert = {
      id: "alt-1",
      ruleName: "High CPU",
      severity: "Critical",
      message: "CPU > 90%",
      firedAtUtc: new Date().toISOString(),
      status: "Firing",
    };

    act(() => {
      mockCallbacks.handlers["AlertFired"]?.(fakeAlert);
    });

    expect(result.current.lastAlert).toEqual(fakeAlert);
    expect(onAlertFired).toHaveBeenCalledWith(fakeAlert);
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ["alerts"] });
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ["dashboard-summary"] });
  });

  it("transitions status during reconnection lifecycle", async () => {
    localStorage.setItem("sentinelops_token", "fake-jwt-token");
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useAlertsHub(), { wrapper });

    await act(async () => {
      await Promise.resolve();
    });

    expect(result.current.status).toBe("connected");

    // Network drops -> reconnecting
    act(() => {
      mockCallbacks.reconnecting?.(new Error("Connection lost"));
    });

    expect(result.current.isConnected).toBe(false);
    expect(result.current.status).toBe("reconnecting");

    // Network restores -> reconnected
    act(() => {
      mockCallbacks.reconnected?.("new-connection-id");
    });

    expect(result.current.isConnected).toBe(true);
    expect(result.current.status).toBe("connected");

    // Hub closed
    act(() => {
      mockCallbacks.close?.();
    });

    expect(result.current.isConnected).toBe(false);
    expect(result.current.status).toBe("disconnected");
  });

  it("stops SignalR connection on component unmount", async () => {
    localStorage.setItem("sentinelops_token", "fake-jwt-token");
    const { wrapper } = createWrapper();
    const { unmount } = renderHook(() => useAlertsHub(), { wrapper });

    await act(async () => {
      await Promise.resolve();
    });

    unmount();
    expect(mockConnection.stop).toHaveBeenCalled();
  });
});
