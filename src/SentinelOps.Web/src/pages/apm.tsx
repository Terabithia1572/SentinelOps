import React, { useState, useEffect, useCallback } from "react";
import {
  getServiceMap,
  getServiceDependencies,
  getEndpoints,
  getEndpointBreakdown,
  getExternalDependencies,
  type ServiceMapDto,
  type ServiceMapNodeDto,
  type ServiceDependenciesDto,
  type EndpointPerformanceDto,
  type EndpointBreakdownDto,
  type ExternalDependencyDto
} from "../lib/api-client";
import { useTenant } from "../context/tenant-context";
import {
  Network,
  Activity,
  Layers,
  Database,
  Search,
  RefreshCw,
  Clock,
  ArrowRight,
  ExternalLink,
  CheckCircle,
  AlertTriangle,
  XCircle,
  Server,
  Zap,
  Globe
} from "lucide-react";

export const ApmPage: React.FC = () => {
  const { activeOrg } = useTenant();

  // Active Tab: "map" | "endpoints" | "external"
  const [activeTab, setActiveTab] = useState<"map" | "endpoints" | "external">("map");

  // Global filters
  const [timeRange, setTimeRange] = useState("1h");
  const [environment, setEnvironment] = useState("");
  const [loading, setLoading] = useState(false);

  // Service Map state
  const [serviceMap, setServiceMap] = useState<ServiceMapDto | null>(null);
  const [selectedNode, setSelectedNode] = useState<ServiceMapNodeDto | null>(null);
  const [nodeDependencies, setNodeDependencies] = useState<ServiceDependenciesDto | null>(null);
  const [loadingDependencies, setLoadingDependencies] = useState(false);

  // Endpoints state
  const [endpoints, setEndpoints] = useState<EndpointPerformanceDto[]>([]);
  const [searchEndpoint, setSearchEndpoint] = useState("");
  const [selectedEndpointKey, setSelectedEndpointKey] = useState<string | null>(null);
  const [endpointBreakdown, setEndpointBreakdown] = useState<EndpointBreakdownDto | null>(null);
  const [loadingBreakdown, setLoadingBreakdown] = useState(false);

  // External dependencies state
  const [externalDeps, setExternalDeps] = useState<ExternalDependencyDto[]>([]);

  // Load Service Map
  const loadServiceMap = useCallback(async () => {
    try {
      setLoading(true);
      const data = await getServiceMap(timeRange, environment || undefined);
      setServiceMap(data);
    } catch {
      // Error handling
    } finally {
      setLoading(false);
    }
  }, [timeRange, environment]);

  // Load Endpoints
  const loadEndpoints = useCallback(async () => {
    try {
      setLoading(true);
      const data = await getEndpoints(timeRange, undefined, searchEndpoint || undefined);
      setEndpoints(data);
    } catch {
      // Error handling
    } finally {
      setLoading(false);
    }
  }, [timeRange, searchEndpoint]);

  // Load External Dependencies
  const loadExternalDeps = useCallback(async () => {
    try {
      setLoading(true);
      const data = await getExternalDependencies(timeRange);
      setExternalDeps(data);
    } catch {
      // Error handling
    } finally {
      setLoading(false);
    }
  }, [timeRange]);

  // Tab switch loader
  useEffect(() => {
    if (activeTab === "map") {
      loadServiceMap();
    } else if (activeTab === "endpoints") {
      loadEndpoints();
    } else if (activeTab === "external") {
      loadExternalDeps();
    }
  }, [activeTab, loadServiceMap, loadEndpoints, loadExternalDeps, activeOrg?.id]);

  // Handle Node Click
  const handleNodeClick = async (node: ServiceMapNodeDto) => {
    setSelectedNode(node);
    try {
      setLoadingDependencies(true);
      const deps = await getServiceDependencies(node.serviceName, timeRange);
      setNodeDependencies(deps);
    } catch {
      // Error
    } finally {
      setLoadingDependencies(false);
    }
  };

  // Handle Endpoint Click
  const handleEndpointClick = async (endpointKey: string) => {
    setSelectedEndpointKey(endpointKey);
    try {
      setLoadingBreakdown(true);
      const breakdown = await getEndpointBreakdown(endpointKey, timeRange);
      setEndpointBreakdown(breakdown);
    } catch {
      // Error
    } finally {
      setLoadingBreakdown(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Top Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-white flex items-center gap-2">
            <Network className="w-7 h-7 text-indigo-400" />
            Application Performance Monitoring (APM)
          </h1>
          <p className="text-sm text-slate-400 mt-1">
            Trace-derived dynamic service topology, endpoint percentiles, latency histograms, and external dependency metrics.
          </p>
        </div>

        {/* Global Toolbar */}
        <div className="flex flex-wrap items-center gap-2.5">
          {/* Time Range */}
          <div className="flex items-center bg-slate-800 border border-slate-700 rounded-lg px-2.5 py-1.5 text-xs text-slate-300 gap-1.5">
            <Clock className="w-3.5 h-3.5 text-slate-400" />
            <select
              value={timeRange}
              onChange={(e) => setTimeRange(e.target.value)}
              className="bg-transparent border-0 text-slate-200 text-xs focus:outline-none cursor-pointer"
            >
              <option value="15m">Last 15 min</option>
              <option value="1h">Last 1 hour</option>
              <option value="6h">Last 6 hours</option>
              <option value="24h">Last 24 hours</option>
            </select>
          </div>

          {/* Environment */}
          <select
            value={environment}
            onChange={(e) => setEnvironment(e.target.value)}
            className="bg-slate-800 border border-slate-700 text-slate-200 text-xs rounded-lg px-3 py-1.5 focus:outline-none focus:ring-1 focus:ring-indigo-500"
          >
            <option value="">All Environments</option>
            <option value="Production">Production</option>
            <option value="Staging">Staging</option>
            <option value="Development">Development</option>
          </select>

          {/* Refresh Button */}
          <button
            onClick={() => {
              if (activeTab === "map") loadServiceMap();
              else if (activeTab === "endpoints") loadEndpoints();
              else loadExternalDeps();
            }}
            disabled={loading}
            className="flex items-center gap-1.5 px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 rounded-lg text-xs font-medium transition-colors"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? "animate-spin text-indigo-400" : ""}`} />
            Refresh
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex border-b border-slate-800 space-x-1">
        <button
          onClick={() => setActiveTab("map")}
          className={`flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
            activeTab === "map"
              ? "border-indigo-500 text-indigo-400"
              : "border-transparent text-slate-400 hover:text-slate-200"
          }`}
        >
          <Network className="w-4 h-4" />
          Service Dependency Map
        </button>
        <button
          onClick={() => setActiveTab("endpoints")}
          className={`flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
            activeTab === "endpoints"
              ? "border-indigo-500 text-indigo-400"
              : "border-transparent text-slate-400 hover:text-slate-200"
          }`}
        >
          <Layers className="w-4 h-4" />
          Endpoints Explorer
        </button>
        <button
          onClick={() => setActiveTab("external")}
          className={`flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
            activeTab === "external"
              ? "border-indigo-500 text-indigo-400"
              : "border-transparent text-slate-400 hover:text-slate-200"
          }`}
        >
          <Database className="w-4 h-4" />
          External & DB Dependencies
        </button>
      </div>

      {/* ======================================================== */}
      {/* TAB 1: SERVICE DEPENDENCY MAP */}
      {/* ======================================================== */}
      {activeTab === "map" && (
        <div className="space-y-6">
          {/* Service Map Visual Grid */}
          <div className="bg-slate-900 border border-slate-800 rounded-xl p-6 relative overflow-hidden shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h3 className="text-base font-semibold text-white flex items-center gap-2">
                  <Activity className="w-4 h-4 text-indigo-400" />
                  Live Topology Graph ({serviceMap?.nodes.length || 0} Monitored Services)
                </h3>
                <p className="text-xs text-slate-400 mt-0.5">
                  Click any service node to inspect inbound callers, outbound downstream dependencies, and latency percentiles.
                </p>
              </div>

              {/* Health Legend */}
              <div className="flex items-center gap-3 text-[11px] text-slate-400">
                <span className="flex items-center gap-1">
                  <span className="w-2.5 h-2.5 rounded-full bg-emerald-500" /> Healthy
                </span>
                <span className="flex items-center gap-1">
                  <span className="w-2.5 h-2.5 rounded-full bg-amber-500" /> Degraded
                </span>
                <span className="flex items-center gap-1">
                  <span className="w-2.5 h-2.5 rounded-full bg-rose-500" /> Critical
                </span>
              </div>
            </div>

            {/* Interactive Node Cards Grid */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-4">
              {serviceMap?.nodes.map((node) => {
                const isSelected = selectedNode?.id === node.id;
                const statusBorder =
                  node.healthStatus === "Critical"
                    ? "border-rose-500 ring-1 ring-rose-500/40"
                    : node.healthStatus === "Degraded"
                    ? "border-amber-500 ring-1 ring-amber-500/40"
                    : "border-slate-800 hover:border-indigo-500/60";

                return (
                  <div
                    key={node.id}
                    onClick={() => handleNodeClick(node)}
                    className={`bg-slate-950/80 rounded-xl p-4 cursor-pointer transition-all border ${statusBorder} ${
                      isSelected ? "bg-indigo-950/40 border-indigo-400" : ""
                    }`}
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-2">
                        <div className="w-7 h-7 rounded-lg bg-indigo-600/20 border border-indigo-500/30 flex items-center justify-center text-indigo-400">
                          <Server className="w-3.5 h-3.5" />
                        </div>
                        <div>
                          <h4 className="text-xs font-bold text-white truncate max-w-[120px]">
                            {node.displayName}
                          </h4>
                          <span className="text-[10px] text-slate-400 block font-mono">
                            {node.environmentName}
                          </span>
                        </div>
                      </div>

                      {/* Status Icon */}
                      {node.healthStatus === "Critical" ? (
                        <XCircle className="w-4 h-4 text-rose-400" />
                      ) : node.healthStatus === "Degraded" ? (
                        <AlertTriangle className="w-4 h-4 text-amber-400" />
                      ) : (
                        <CheckCircle className="w-4 h-4 text-emerald-400" />
                      )}
                    </div>

                    <div className="grid grid-cols-2 gap-2 mt-4 pt-3 border-t border-slate-900 text-[11px]">
                      <div>
                        <span className="text-[10px] text-slate-500 block">Throughput</span>
                        <span className="font-mono font-semibold text-slate-200">
                          {node.throughput} req/m
                        </span>
                      </div>
                      <div>
                        <span className="text-[10px] text-slate-500 block">P95 Latency</span>
                        <span className="font-mono font-semibold text-amber-400">
                          {node.p95LatencyMs}ms
                        </span>
                      </div>
                      <div>
                        <span className="text-[10px] text-slate-500 block">Error Rate</span>
                        <span
                          className={`font-mono font-semibold ${
                            node.errorRate > 1.0 ? "text-rose-400" : "text-slate-300"
                          }`}
                        >
                          {node.errorRate}%
                        </span>
                      </div>
                      <div>
                        <span className="text-[10px] text-slate-500 block">Total Calls</span>
                        <span className="font-mono text-slate-400">
                          {node.totalCalls.toLocaleString()}
                        </span>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

          {/* Node Detail Drawer / Dependency Inspector */}
          {selectedNode && (
            <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 shadow-sm space-y-4">
              <div className="flex items-center justify-between border-b border-slate-800 pb-3">
                <div className="flex items-center gap-3">
                  <div className="w-9 h-9 rounded-lg bg-indigo-600/20 border border-indigo-500/30 flex items-center justify-center text-indigo-400">
                    <Server className="w-4 h-4" />
                  </div>
                  <div>
                    <h3 className="text-base font-bold text-white flex items-center gap-2">
                      {selectedNode.displayName}
                      <span className="px-2 py-0.5 text-xs font-semibold rounded-full bg-slate-800 text-slate-300 border border-slate-700">
                        {selectedNode.healthStatus}
                      </span>
                    </h3>
                    <p className="text-xs text-slate-400 font-mono mt-0.5">
                      Service Key: {selectedNode.serviceName} • Environment: {selectedNode.environmentName}
                    </p>
                  </div>
                </div>

                <button
                  onClick={() => setSelectedNode(null)}
                  className="text-slate-400 hover:text-white text-xs px-2 py-1 rounded bg-slate-800"
                >
                  ✕ Close Inspector
                </button>
              </div>

              {loadingDependencies ? (
                <div className="text-center py-8 text-xs text-slate-400">
                  <RefreshCw className="w-5 h-5 animate-spin mx-auto mb-2 text-indigo-400" />
                  Inspecting dependencies...
                </div>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6 pt-2">
                  {/* Inbound Callers */}
                  <div className="space-y-3">
                    <h4 className="text-xs font-semibold uppercase tracking-wider text-slate-400 flex items-center gap-1.5">
                      <ArrowRight className="w-3.5 h-3.5 text-blue-400" />
                      Inbound Dependencies (Who calls {selectedNode.displayName})
                    </h4>
                    {nodeDependencies?.inbound.length === 0 ? (
                      <p className="text-xs text-slate-500 italic">No inbound callers recorded in this window.</p>
                    ) : (
                      <div className="space-y-2">
                        {nodeDependencies?.inbound.map((edge) => (
                          <div
                            key={edge.sourceService}
                            className="bg-slate-950 p-3 rounded-lg border border-slate-800 flex items-center justify-between text-xs"
                          >
                            <span className="font-semibold text-slate-200">{edge.sourceService}</span>
                            <div className="flex items-center gap-3 font-mono text-slate-400 text-[11px]">
                              <span>{edge.callCount.toLocaleString()} calls</span>
                              <span className="text-amber-400">{edge.avgLatencyMs}ms avg</span>
                              <span className={edge.errorRate > 0 ? "text-rose-400" : "text-emerald-400"}>
                                {edge.errorRate}% err
                              </span>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>

                  {/* Outbound Downstream */}
                  <div className="space-y-3">
                    <h4 className="text-xs font-semibold uppercase tracking-wider text-slate-400 flex items-center gap-1.5">
                      <ArrowRight className="w-3.5 h-3.5 text-indigo-400" />
                      Outbound Dependencies (Who {selectedNode.displayName} calls)
                    </h4>
                    {nodeDependencies?.outbound.length === 0 ? (
                      <p className="text-xs text-slate-500 italic">No outbound downstream services recorded.</p>
                    ) : (
                      <div className="space-y-2">
                        {nodeDependencies?.outbound.map((edge) => (
                          <div
                            key={edge.targetService}
                            className="bg-slate-950 p-3 rounded-lg border border-slate-800 flex items-center justify-between text-xs"
                          >
                            <span className="font-semibold text-slate-200">{edge.targetService}</span>
                            <div className="flex items-center gap-3 font-mono text-slate-400 text-[11px]">
                              <span>{edge.callCount.toLocaleString()} calls</span>
                              <span className="text-amber-400">{edge.avgLatencyMs}ms avg</span>
                              <span className={edge.errorRate > 0 ? "text-rose-400" : "text-emerald-400"}>
                                {edge.errorRate}% err
                              </span>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Directed Edges Table */}
          <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden shadow-sm">
            <div className="p-4 border-b border-slate-800 flex items-center justify-between">
              <h3 className="text-sm font-semibold text-white flex items-center gap-2">
                <Zap className="w-4 h-4 text-indigo-400" />
                Cross-Service Directed Invocations ({serviceMap?.edges.length || 0} Connections)
              </h3>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs text-slate-300">
                <thead className="bg-slate-800/60 text-[10px] font-semibold text-slate-400 uppercase tracking-wider">
                  <tr>
                    <th className="px-5 py-3">Source (Caller)</th>
                    <th className="px-5 py-3">Target (Callee)</th>
                    <th className="px-5 py-3">Call Volume</th>
                    <th className="px-5 py-3">Error Rate</th>
                    <th className="px-5 py-3">Avg Latency</th>
                    <th className="px-5 py-3">P95 Latency</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-800">
                  {serviceMap?.edges.map((edge, i) => (
                    <tr key={i} className="hover:bg-slate-800/40 transition-colors">
                      <td className="px-5 py-3 font-semibold text-slate-200">{edge.sourceService}</td>
                      <td className="px-5 py-3 font-semibold text-indigo-300 flex items-center gap-1.5">
                        <ArrowRight className="w-3 h-3 text-slate-500" />
                        {edge.targetService}
                      </td>
                      <td className="px-5 py-3 font-mono text-slate-300">{(edge.callCount ?? 0).toLocaleString()}</td>
                      <td className="px-5 py-3 font-mono">
                        <span className={(edge.errorRate ?? 0) > 1.0 ? "text-rose-400 font-bold" : "text-slate-400"}>
                          {edge.errorRate ?? 0}%
                        </span>
                      </td>
                      <td className="px-5 py-3 font-mono text-slate-300">{edge.avgLatencyMs ?? 0}ms</td>
                      <td className="px-5 py-3 font-mono text-amber-400">{edge.p95LatencyMs ?? 0}ms</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* ======================================================== */}
      {/* TAB 2: ENDPOINTS EXPLORER */}
      {/* ======================================================== */}
      {activeTab === "endpoints" && (
        <div className="space-y-6">
          <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden shadow-sm">
            <div className="p-4 border-b border-slate-800 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div className="relative flex-1 max-w-md">
                <Search className="w-4 h-4 text-slate-400 absolute left-3 top-2.5" />
                <input
                  type="text"
                  value={searchEndpoint}
                  onChange={(e) => setSearchEndpoint(e.target.value)}
                  placeholder="Search routes or services..."
                  className="w-full bg-slate-800 border border-slate-700 rounded-lg pl-9 pr-3 py-1.5 text-xs text-slate-200 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                />
              </div>
              <span className="text-xs text-slate-400">
                Showing {endpoints.length} aggregated endpoints
              </span>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs text-slate-300">
                <thead className="bg-slate-800/60 text-[10px] font-semibold text-slate-400 uppercase tracking-wider">
                  <tr>
                    <th className="px-5 py-3">Method</th>
                    <th className="px-5 py-3">Route / Operation</th>
                    <th className="px-5 py-3">Service</th>
                    <th className="px-5 py-3">Throughput</th>
                    <th className="px-5 py-3">Error Rate</th>
                    <th className="px-5 py-3">Avg Duration</th>
                    <th className="px-5 py-3">P50</th>
                    <th className="px-5 py-3">P95</th>
                    <th className="px-5 py-3">P99</th>
                    <th className="px-5 py-3">Action</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-800">
                  {endpoints.map((ep) => (
                    <tr key={ep.endpointKey} className="hover:bg-slate-800/40 transition-colors">
                      <td className="px-5 py-3">
                        <span
                          className={`px-1.5 py-0.5 rounded text-[10px] font-bold font-mono ${
                            ep.method === "POST"
                              ? "bg-emerald-950 text-emerald-400 border border-emerald-800"
                              : ep.method === "GET"
                              ? "bg-blue-950 text-blue-400 border border-blue-800"
                              : "bg-slate-800 text-slate-300 border border-slate-700"
                          }`}
                        >
                          {ep.method}
                        </span>
                      </td>
                      <td className="px-5 py-3 font-mono font-medium text-slate-200">{ep.route}</td>
                      <td className="px-5 py-3 text-slate-400">{ep.serviceName}</td>
                      <td className="px-5 py-3 font-mono text-slate-300">{ep.throughputPerMin} req/m</td>
                      <td className="px-5 py-3 font-mono">
                        <span className={ep.errorRate > 1.0 ? "text-rose-400 font-bold" : "text-slate-400"}>
                          {ep.errorRate}%
                        </span>
                      </td>
                      <td className="px-5 py-3 font-mono text-slate-300">{ep.avgDurationMs}ms</td>
                      <td className="px-5 py-3 font-mono text-emerald-400">{ep.p50DurationMs}ms</td>
                      <td className="px-5 py-3 font-mono text-amber-400">{ep.p95DurationMs}ms</td>
                      <td className="px-5 py-3 font-mono text-rose-400">{ep.p99DurationMs}ms</td>
                      <td className="px-5 py-3">
                        <button
                          onClick={() => handleEndpointClick(ep.endpointKey)}
                          className="px-2.5 py-1 bg-indigo-600/20 hover:bg-indigo-600/30 text-indigo-300 rounded text-[11px] font-medium"
                        >
                          Drill Down
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Endpoint Breakdown Modal / Drawer */}
          {selectedEndpointKey && (
            <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 shadow-sm space-y-4">
              <div className="flex items-center justify-between border-b border-slate-800 pb-3">
                <div className="flex items-center gap-2">
                  <span className="px-2 py-0.5 rounded text-xs font-bold font-mono bg-indigo-950 text-indigo-400 border border-indigo-800">
                    {endpointBreakdown?.method || "GET"}
                  </span>
                  <h3 className="text-base font-bold text-white font-mono">
                    {endpointBreakdown?.route || selectedEndpointKey}
                  </h3>
                  <span className="text-xs text-slate-400">• {endpointBreakdown?.serviceName}</span>
                </div>
                <button
                  onClick={() => setSelectedEndpointKey(null)}
                  className="text-slate-400 hover:text-white text-xs px-2 py-1 rounded bg-slate-800"
                >
                  ✕ Close Breakdown
                </button>
              </div>

              {loadingBreakdown ? (
                <div className="text-center py-8 text-xs text-slate-400">
                  <RefreshCw className="w-5 h-5 animate-spin mx-auto mb-2 text-indigo-400" />
                  Computing latency distribution and trace samples...
                </div>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-3 gap-6 pt-2">
                  {/* Latency Histogram */}
                  <div className="space-y-3">
                    <h4 className="text-xs font-semibold uppercase tracking-wider text-slate-400">
                      Latency Histogram Distribution
                    </h4>
                    <div className="space-y-2">
                      {endpointBreakdown?.latencyBuckets.map((bucket) => (
                        <div key={bucket.rangeLabel} className="space-y-1">
                          <div className="flex justify-between text-[11px] text-slate-300 font-mono">
                            <span>{bucket.rangeLabel}</span>
                            <span className="text-slate-400">{bucket.count.toLocaleString()} calls</span>
                          </div>
                          <div className="w-full bg-slate-800 rounded-full h-1.5 overflow-hidden">
                            <div
                              className="bg-indigo-500 h-1.5 rounded-full"
                              style={{ width: `${Math.min(100, Math.max(5, (bucket.count / 1000) * 100))}%` }}
                            />
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>

                  {/* Errors Breakdown */}
                  <div className="space-y-3">
                    <h4 className="text-xs font-semibold uppercase tracking-wider text-slate-400">
                      Error Categories
                    </h4>
                    {endpointBreakdown?.errorBreakdown.length === 0 ? (
                      <p className="text-xs text-slate-500 italic">No errors detected on this endpoint.</p>
                    ) : (
                      <div className="space-y-2">
                        {endpointBreakdown?.errorBreakdown.map((err) => (
                          <div
                            key={err.statusCodeOrType}
                            className="bg-slate-950 p-2.5 rounded-lg border border-slate-800 flex justify-between text-xs"
                          >
                            <span className="text-rose-400 font-semibold">{err.statusCodeOrType}</span>
                            <span className="font-mono text-slate-400">{err.count} occurrences</span>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>

                  {/* Sample Traces */}
                  <div className="space-y-3">
                    <h4 className="text-xs font-semibold uppercase tracking-wider text-slate-400">
                      Sample Distributed Traces
                    </h4>
                    <div className="space-y-2">
                      {endpointBreakdown?.sampleTraceIds.map((traceId) => (
                        <a
                          key={traceId}
                          href={`/traces?search=${traceId}`}
                          className="bg-slate-950 p-2.5 rounded-lg border border-slate-800 flex items-center justify-between text-xs hover:border-indigo-500/50 transition-colors"
                        >
                          <span className="font-mono text-indigo-300">{traceId}</span>
                          <ExternalLink className="w-3.5 h-3.5 text-slate-500" />
                        </a>
                      ))}
                    </div>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {/* ======================================================== */}
      {/* TAB 3: EXTERNAL & DATABASE DEPENDENCIES */}
      {/* ======================================================== */}
      {activeTab === "external" && (
        <div className="space-y-6">
          <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden shadow-sm">
            <div className="p-4 border-b border-slate-800">
              <h3 className="text-base font-semibold text-white flex items-center gap-2">
                <Database className="w-4 h-4 text-emerald-400" />
                Database Engines & Remote HTTP Dependencies
              </h3>
              <p className="text-xs text-slate-400 mt-0.5">
                Latency, volume, and error telemetry for external datastores and third-party APIs.
              </p>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs text-slate-300">
                <thead className="bg-slate-800/60 text-[10px] font-semibold text-slate-400 uppercase tracking-wider">
                  <tr>
                    <th className="px-5 py-3">Type</th>
                    <th className="px-5 py-3">Target Host / Datastore</th>
                    <th className="px-5 py-3">Target Resource / Operation</th>
                    <th className="px-5 py-3">Total Calls</th>
                    <th className="px-5 py-3">Error Rate</th>
                    <th className="px-5 py-3">Avg Latency</th>
                    <th className="px-5 py-3">P95 Latency</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-800">
                  {externalDeps.map((dep, i) => (
                    <tr key={i} className="hover:bg-slate-800/40 transition-colors">
                      <td className="px-5 py-3">
                        <span
                          className={`px-2 py-0.5 rounded text-[10px] font-bold font-mono flex items-center gap-1 w-fit ${
                            dep.type === "Database"
                              ? "bg-emerald-950 text-emerald-400 border border-emerald-800"
                              : "bg-blue-950 text-blue-400 border border-blue-800"
                          }`}
                        >
                          {dep.type === "Database" ? <Database className="w-3 h-3" /> : <Globe className="w-3 h-3" />}
                          {dep.type}
                        </span>
                      </td>
                      <td className="px-5 py-3 font-semibold text-slate-200">{dep.targetHostOrSystem}</td>
                      <td className="px-5 py-3 font-mono text-slate-400">{dep.operationOrDb}</td>
                      <td className="px-5 py-3 font-mono text-slate-200">{dep.callCount.toLocaleString()}</td>
                      <td className="px-5 py-3 font-mono">
                        <span className={dep.errorRate > 0.1 ? "text-rose-400 font-bold" : "text-slate-400"}>
                          {dep.errorRate}%
                        </span>
                      </td>
                      <td className="px-5 py-3 font-mono text-slate-300">{dep.avgDurationMs}ms</td>
                      <td className="px-5 py-3 font-mono text-amber-400">{dep.p95DurationMs}ms</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
