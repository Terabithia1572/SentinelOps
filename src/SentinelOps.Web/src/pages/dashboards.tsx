import React, { useState, useEffect, useCallback, useId } from "react";
import {
  getDashboards,
  getDashboardById,
  createDashboard,
  updateDashboard,
  deleteDashboard,
  duplicateDashboard,
  importDashboard,
  instantiateDashboardTemplate,
  setDefaultDashboard,
  type DashboardSummaryItemDto,
  type DashboardDetailDto,
  type DashboardWidgetDto
} from "../lib/api-client";
import { useTenant } from "../context/tenant-context";
import {
  LayoutGrid,
  Plus,
  RefreshCw,
  Copy,
  Trash2,
  Download,
  Upload,
  Sparkles,
  Save,
  Clock,
  ArrowUp,
  ArrowDown,
  Activity,
  BarChart2,
  Table as TableIcon,
  Flame,
  CheckCircle,
  FileCode,
  PieChart as PieIcon,
  Check,
  Star,
} from "lucide-react";

export const WIDGET_CATALOG = [
  { type: "timeseries", name: "Timeseries Chart", desc: "Metric trends over time with area fill", icon: Activity, defaultW: 6, defaultH: 4 },
  { type: "stat", name: "Stat / Single Value", desc: "Key performance KPI with sparkline & delta", icon: BarChart2, defaultW: 3, defaultH: 2 },
  { type: "bar", name: "Bar Chart", desc: "Comparative bar distribution across services", icon: BarChart2, defaultW: 6, defaultH: 3 },
  { type: "gauge", name: "Gauge / Progress", desc: "Target utilization meter with threshold colors", icon: Flame, defaultW: 4, defaultH: 3 },
  { type: "table", name: "Table View", desc: "Tabular telemetry logs or slow endpoints", icon: TableIcon, defaultW: 6, defaultH: 4 },
  { type: "status-grid", name: "Status Health Matrix", desc: "Service and node health state matrix", icon: CheckCircle, defaultW: 6, defaultH: 3 },
  { type: "log-stream", name: "Log Stream Tail", desc: "Real-time filtered event stream tail", icon: FileCode, defaultW: 12, defaultH: 4 },
  { type: "waterfall-summary", name: "Trace Latency Distribution", desc: "P50, P95, and P99 latency percentiles", icon: Activity, defaultW: 6, defaultH: 4 },
  { type: "incidents", name: "Active Incidents", desc: "Open incident queue with severity badges", icon: Flame, defaultW: 6, defaultH: 4 },
  { type: "slo-burn", name: "SLO Burn Indicator", desc: "Objective error budget consumption gauge", icon: Flame, defaultW: 6, defaultH: 3 },
  { type: "markdown", name: "Markdown Runbook", desc: "Operational guide, notes, and runbooks", icon: FileCode, defaultW: 6, defaultH: 3 },
  { type: "pie", name: "Pie / Donut Chart", desc: "Categorical status and severity breakdown", icon: PieIcon, defaultW: 4, defaultH: 3 },
];

export const DashboardsPage: React.FC = () => {
  const { activeOrg } = useTenant();
  const selectId = useId();
  const [dashboards, setDashboards] = useState<DashboardSummaryItemDto[]>([]);
  const [selectedDashboard, setSelectedDashboard] = useState<DashboardDetailDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [statusMessage, setStatusMessage] = useState<{ text: string; type: "success" | "error" } | null>(null);

  // Global Time & Refresh state
  const [timeRange, setTimeRange] = useState("1h");
  const [refreshInterval, setRefreshInterval] = useState(30);

  // Modals
  const [showAddWidgetModal, setShowAddWidgetModal] = useState(false);
  const [editingWidget, setEditingWidget] = useState<DashboardWidgetDto | null>(null);
  const [showTemplatesModal, setShowTemplatesModal] = useState(false);
  const [showExportModal, setShowExportModal] = useState(false);
  const [showImportModal, setShowImportModal] = useState(false);
  const [importJsonText, setImportJsonText] = useState("");
  const [copiedExport, setCopiedExport] = useState(false);

  // New Widget form state
  const [widgetType, setWidgetType] = useState("timeseries");
  const [widgetTitle, setWidgetTitle] = useState("");
  const [widgetWidth, setWidgetWidth] = useState(6);
  const [widgetConfigMetric, setWidgetConfigMetric] = useState("http.server.requests");

  // Load Dashboards List
  const loadDashboards = useCallback(async () => {
    try {
      setLoading(true);
      const list = await getDashboards();
      setDashboards(list);

      if (list.length > 0) {
        // Load default or first dashboard
        const defaultDash = list.find((d) => d.isDefault) || list[0];
        const detail = await getDashboardById(defaultDash.id);
        setSelectedDashboard(detail);
        setTimeRange(detail.defaultTimeRange || "1h");
        setRefreshInterval(detail.refreshIntervalSeconds || 30);
      } else {
        setSelectedDashboard(null);
      }
    } catch {
      setStatusMessage({ text: "Failed to load dashboards.", type: "error" });
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadDashboards();
  }, [loadDashboards, activeOrg?.id]);

  // Handle Switch Dashboard
  const handleSelectDashboard = async (id: string) => {
    try {
      setLoading(true);
      const detail = await getDashboardById(id);
      setSelectedDashboard(detail);
      setTimeRange(detail.defaultTimeRange || "1h");
      setRefreshInterval(detail.refreshIntervalSeconds || 30);
    } catch {
      setStatusMessage({ text: "Failed to load dashboard detail.", type: "error" });
    } finally {
      setLoading(false);
    }
  };

  // Auto-refresh timer
  useEffect(() => {
    if (refreshInterval <= 0 || !selectedDashboard) return;
    const interval = setInterval(() => {
      // Periodic refresh trigger
      if (selectedDashboard) {
        getDashboardById(selectedDashboard.id).then((fresh) => {
          setSelectedDashboard(fresh);
        }).catch(() => {});
      }
    }, refreshInterval * 1000);
    return () => clearInterval(interval);
  }, [refreshInterval, selectedDashboard]);

  // Create New Empty Dashboard
  const handleCreateEmpty = async () => {
    const title = prompt("Enter new dashboard title:", "Custom Observability View");
    if (!title) return;
    try {
      setSaving(true);
      const created = await createDashboard({
        title,
        description: "Created via Dashboard Builder",
        isDefault: dashboards.length === 0,
        isShared: true,
        refreshIntervalSeconds: 30,
        defaultTimeRange: "1h",
        layoutJson: "[]"
      });
      await loadDashboards();
      setSelectedDashboard(created);
      setStatusMessage({ text: `Dashboard "${created.title}" created.`, type: "success" });
    } catch {
      setStatusMessage({ text: "Failed to create dashboard.", type: "error" });
    } finally {
      setSaving(false);
    }
  };

  // Instantiate Template
  const handleInstantiateTemplate = async (templateKey: string) => {
    try {
      setSaving(true);
      const created = await instantiateDashboardTemplate(templateKey);
      setShowTemplatesModal(false);
      await loadDashboards();
      setSelectedDashboard(created);
      setStatusMessage({ text: `Instantiated "${created.title}" successfully!`, type: "success" });
    } catch {
      setStatusMessage({ text: "Failed to instantiate template.", type: "error" });
    } finally {
      setSaving(false);
    }
  };

  // Duplicate Dashboard
  const handleDuplicate = async () => {
    if (!selectedDashboard) return;
    try {
      setSaving(true);
      const duplicated = await duplicateDashboard(selectedDashboard.id);
      await loadDashboards();
      setSelectedDashboard(duplicated);
      setStatusMessage({ text: `Duplicated as "${duplicated.title}".`, type: "success" });
    } catch {
      setStatusMessage({ text: "Failed to duplicate dashboard.", type: "error" });
    } finally {
      setSaving(false);
    }
  };

  // Delete Dashboard
  const handleDelete = async () => {
    if (!selectedDashboard) return;
    if (!confirm(`Delete dashboard "${selectedDashboard.title}"?`)) return;
    try {
      setSaving(true);
      await deleteDashboard(selectedDashboard.id);
      await loadDashboards();
      setStatusMessage({ text: "Dashboard deleted.", type: "success" });
    } catch {
      setStatusMessage({ text: "Failed to delete dashboard.", type: "error" });
    } finally {
      setSaving(false);
    }
  };

  // Save Current Layout
  const handleSaveLayout = async () => {
    if (!selectedDashboard) return;
    try {
      setSaving(true);
      const updated = await updateDashboard(selectedDashboard.id, {
        title: selectedDashboard.title,
        description: selectedDashboard.description || undefined,
        isDefault: selectedDashboard.isDefault,
        isShared: selectedDashboard.isShared,
        refreshIntervalSeconds: refreshInterval,
        defaultTimeRange: timeRange,
        tags: selectedDashboard.tags,
        layoutJson: JSON.stringify(selectedDashboard.widgets)
      });
      setSelectedDashboard(updated);
      setStatusMessage({ text: "Dashboard layout and widgets saved.", type: "success" });
    } catch {
      setStatusMessage({ text: "Failed to save dashboard.", type: "error" });
    } finally {
      setSaving(false);
    }
  };

  const [settingDefault, setSettingDefault] = useState(false);

  const handleSetDefault = async () => {
    if (!selectedDashboard) return;
    try {
      setSettingDefault(true);
      await setDefaultDashboard(selectedDashboard.id);
      setStatusMessage({ text: "Dashboard set as default for organization.", type: "success" });
      setSelectedDashboard({ ...selectedDashboard, isDefault: true });
      setDashboards((prev) =>
        prev.map((d) => ({
          ...d,
          isDefault: d.id === selectedDashboard.id,
        }))
      );
    } catch {
      setStatusMessage({ text: "Failed to set default dashboard.", type: "error" });
    } finally {
      setSettingDefault(false);
    }
  };


  // Widget Actions
  const handleAddOrUpdateWidget = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedDashboard) return;

    const title = widgetTitle.trim() || `Widget (${widgetType})`;
    const config = JSON.stringify({ metric: widgetConfigMetric });

    let updatedWidgets = [...selectedDashboard.widgets];

    if (editingWidget) {
      updatedWidgets = updatedWidgets.map((w) =>
        w.id === editingWidget.id
          ? { ...w, title, type: widgetType, w: widgetWidth, configJson: config }
          : w
      );
    } else {
      const newWidget: DashboardWidgetDto = {
        id: "w-" + Math.random().toString(36).substring(2, 9),
        type: widgetType,
        title,
        x: 0,
        y: updatedWidgets.length * 3,
        w: widgetWidth,
        h: 3,
        configJson: config
      };
      updatedWidgets.push(newWidget);
    }

    setSelectedDashboard({
      ...selectedDashboard,
      widgets: updatedWidgets,
      layoutJson: JSON.stringify(updatedWidgets),
      widgetCount: updatedWidgets.length
    });

    setShowAddWidgetModal(false);
    setEditingWidget(null);
  };

  const handleRemoveWidget = (widgetId: string) => {
    if (!selectedDashboard) return;
    const filtered = selectedDashboard.widgets.filter((w) => w.id !== widgetId);
    setSelectedDashboard({
      ...selectedDashboard,
      widgets: filtered,
      layoutJson: JSON.stringify(filtered),
      widgetCount: filtered.length
    });
  };

  const handleMoveWidget = (index: number, direction: "up" | "down") => {
    if (!selectedDashboard) return;
    const newIdx = direction === "up" ? index - 1 : index + 1;
    if (newIdx < 0 || newIdx >= selectedDashboard.widgets.length) return;

    const reordered = [...selectedDashboard.widgets];
    const temp = reordered[index];
    reordered[index] = reordered[newIdx];
    reordered[newIdx] = temp;

    setSelectedDashboard({
      ...selectedDashboard,
      widgets: reordered,
      layoutJson: JSON.stringify(reordered)
    });
  };

  // Import JSON Handler
  const handleImportSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const parsed = JSON.parse(importJsonText);
      const title = parsed.title || "Imported Dashboard";
      const layoutJson = typeof parsed.layoutJson === "string" ? parsed.layoutJson : JSON.stringify(parsed.widgets || []);

      setSaving(true);
      const created = await importDashboard({
        title,
        description: parsed.description || "Imported via JSON",
        layoutJson,
        tags: parsed.tags || ["imported"]
      });
      setShowImportModal(false);
      setImportJsonText("");
      await loadDashboards();
      setSelectedDashboard(created);
      setStatusMessage({ text: `Imported "${created.title}" successfully!`, type: "success" });
    } catch {
      setStatusMessage({ text: "Invalid dashboard JSON format.", type: "error" });
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Top Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-white flex items-center gap-2">
            <LayoutGrid className="w-7 h-7 text-blue-400" />
            Custom Observability Dashboards
          </h1>
          <p className="text-sm text-slate-400 mt-1">
            Build, personalize, and share real-time telemetry dashboards with 12 enterprise widget types.
          </p>
        </div>

        {/* Global Controls & Actions */}
        <div className="flex flex-wrap items-center gap-2">
          {/* Dashboard Selector */}
          <select
            id={selectId}
            value={selectedDashboard?.id || ""}
            onChange={(e) => handleSelectDashboard(e.target.value)}
            className="bg-slate-800 border border-slate-700 text-slate-200 text-sm rounded-lg px-3 py-2 font-medium focus:outline-none focus:ring-2 focus:ring-blue-500"
            disabled={dashboards.length === 0}
          >
            {dashboards.map((d) => (
              <option key={d.id} value={d.id}>
                {d.title} {d.isDefault ? "★ (Default)" : ""} ({d.widgetCount} widgets)
              </option>
            ))}
          </select>

          {/* Time Range Selector */}
          <div className="flex items-center bg-slate-800 border border-slate-700 rounded-lg px-2.5 py-1.5 text-xs text-slate-300 gap-1.5">
            <Clock className="w-3.5 h-3.5 text-slate-400" />
            <select
              value={timeRange}
              onChange={(e) => setTimeRange(e.target.value)}
              className="bg-transparent border-0 text-slate-200 text-xs focus:outline-none cursor-pointer"
            >
              <option value="5m">Last 5 min</option>
              <option value="15m">Last 15 min</option>
              <option value="1h">Last 1 hour</option>
              <option value="6h">Last 6 hours</option>
              <option value="24h">Last 24 hours</option>
              <option value="7d">Last 7 days</option>
            </select>
          </div>

          {/* Auto Refresh */}
          <div className="flex items-center bg-slate-800 border border-slate-700 rounded-lg px-2.5 py-1.5 text-xs text-slate-300 gap-1.5">
            <RefreshCw className={`w-3.5 h-3.5 text-slate-400 ${refreshInterval > 0 ? "text-emerald-400" : ""}`} />
            <select
              value={refreshInterval}
              onChange={(e) => setRefreshInterval(Number(e.target.value))}
              className="bg-transparent border-0 text-slate-200 text-xs focus:outline-none cursor-pointer"
            >
              <option value={0}>Refresh: Off</option>
              <option value={5}>Every 5s</option>
              <option value={10}>Every 10s</option>
              <option value={30}>Every 30s</option>
              <option value={60}>Every 60s</option>
            </select>
          </div>

          {/* Template Gallery */}
          <button
            onClick={() => setShowTemplatesModal(true)}
            className="flex items-center gap-1.5 px-3 py-2 bg-purple-600/20 hover:bg-purple-600/30 text-purple-300 border border-purple-500/40 rounded-lg text-xs font-medium transition-colors"
          >
            <Sparkles className="w-3.5 h-3.5 text-purple-400" />
            Templates
          </button>

          {/* New Dashboard */}
          <button
            onClick={handleCreateEmpty}
            className="flex items-center gap-1.5 px-3 py-2 bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 rounded-lg text-xs font-medium transition-colors"
          >
            <Plus className="w-3.5 h-3.5" />
            New
          </button>

          {/* Save Layout */}
          {selectedDashboard && (
            <button
              onClick={handleSaveLayout}
              disabled={saving}
              className="flex items-center gap-1.5 px-3.5 py-2 bg-blue-600 hover:bg-blue-500 text-white rounded-lg text-xs font-medium transition-colors shadow-sm disabled:opacity-50"
            >
              <Save className="w-3.5 h-3.5" />
              {saving ? "Saving..." : "Save Layout"}
            </button>
          )}
        </div>
      </div>

      {/* Status banner */}
      {statusMessage && (
        <div
          className={`p-3 rounded-lg flex items-center justify-between text-sm ${
            statusMessage.type === "success"
              ? "bg-emerald-950/60 border border-emerald-800 text-emerald-200"
              : "bg-red-950/60 border border-red-800 text-red-200"
          }`}
        >
          <span>{statusMessage.text}</span>
          <button onClick={() => setStatusMessage(null)} className="text-slate-400 hover:text-white ml-2 text-xs">
            ✕
          </button>
        </div>
      )}

      {/* Subheader / Dashboard Toolbar */}
      {selectedDashboard && (
        <div className="bg-slate-900 border border-slate-800 rounded-xl px-4 py-3 flex flex-wrap items-center justify-between gap-3 shadow-sm">
          <div className="flex items-center gap-3">
            <h2 className="text-base font-bold text-white">{selectedDashboard.title}</h2>
            {selectedDashboard.description && (
              <span className="text-xs text-slate-400 hidden md:inline">
                • {selectedDashboard.description}
              </span>
            )}
            <div className="flex gap-1">
              {selectedDashboard.tags.map((tag) => (
                <span key={tag} className="px-2 py-0.5 text-[10px] font-medium bg-slate-800 text-slate-300 rounded border border-slate-700">
                  #{tag}
                </span>
              ))}
            </div>

            {selectedDashboard.isDefault ? (
              <span className="flex items-center gap-1 text-[11px] font-semibold text-amber-400 bg-amber-500/10 border border-amber-500/30 px-2 py-0.5 rounded-md">
                <Star className="w-3.5 h-3.5 fill-amber-400 text-amber-400" /> Default
              </span>
            ) : (
              <button
                onClick={handleSetDefault}
                disabled={settingDefault}
                className="flex items-center gap-1 text-[11px] font-medium text-slate-400 hover:text-amber-400 bg-slate-800/80 hover:bg-slate-800 border border-slate-700 px-2 py-0.5 rounded-md transition-colors cursor-pointer"
                title="Set as default dashboard for organization"
              >
                <Star className="w-3.5 h-3.5" />
                {settingDefault ? "Setting..." : "Set as Default"}
              </button>
            )}
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={() => {
                setEditingWidget(null);
                setWidgetTitle("");
                setWidgetType("timeseries");
                setWidgetWidth(6);
                setShowAddWidgetModal(true);
              }}
              className="flex items-center gap-1.5 px-3 py-1.5 bg-blue-600/20 hover:bg-blue-600/30 text-blue-300 border border-blue-500/30 rounded-lg text-xs font-medium transition-colors"
            >
              <Plus className="w-3.5 h-3.5" />
              Add Widget
            </button>

            <button
              onClick={handleDuplicate}
              className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded"
              title="Duplicate Dashboard"
            >
              <Copy className="w-4 h-4" />
            </button>

            <button
              onClick={() => setShowExportModal(true)}
              className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded"
              title="Export JSON"
            >
              <Download className="w-4 h-4" />
            </button>

            <button
              onClick={() => setShowImportModal(true)}
              className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-800 rounded"
              title="Import JSON"
            >
              <Upload className="w-4 h-4" />
            </button>

            <button
              onClick={handleDelete}
              className="p-1.5 text-rose-400 hover:text-rose-300 hover:bg-rose-950/40 rounded"
              title="Delete Dashboard"
            >
              <Trash2 className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}

      {/* Widgets Grid View */}
      {loading && !selectedDashboard ? (
        <div className="text-center py-16 bg-slate-900 border border-slate-800 rounded-xl text-slate-400">
          <RefreshCw className="w-6 h-6 animate-spin mx-auto mb-2 text-blue-400" />
          Loading dashboard telemetry...
        </div>
      ) : !selectedDashboard ? (
        <div className="text-center py-16 bg-slate-900 border border-slate-800 rounded-xl space-y-4">
          <LayoutGrid className="w-12 h-12 text-slate-600 mx-auto" />
          <h3 className="text-lg font-semibold text-white">No Dashboards Found</h3>
          <p className="text-sm text-slate-400 max-w-md mx-auto">
            Get started immediately by choosing a pre-built template from our gallery or build a custom layout from scratch.
          </p>
          <div className="flex justify-center gap-3">
            <button
              onClick={() => setShowTemplatesModal(true)}
              className="px-4 py-2 bg-purple-600 hover:bg-purple-500 text-white rounded-lg text-sm font-medium transition-colors"
            >
              Browse Template Gallery
            </button>
            <button
              onClick={handleCreateEmpty}
              className="px-4 py-2 bg-slate-800 hover:bg-slate-700 text-slate-200 rounded-lg text-sm font-medium transition-colors"
            >
              Create Empty Dashboard
            </button>
          </div>
        </div>
      ) : selectedDashboard.widgets.length === 0 ? (
        <div className="text-center py-16 bg-slate-900 border border-slate-800 rounded-xl space-y-3">
          <Activity className="w-10 h-10 text-slate-600 mx-auto" />
          <h3 className="text-base font-semibold text-white">This Dashboard Has No Widgets</h3>
          <p className="text-xs text-slate-400 max-w-sm mx-auto">
            Click &quot;Add Widget&quot; to add charts, stats, tables, log streams, SLO burn meters, or incidents.
          </p>
          <button
            onClick={() => {
              setEditingWidget(null);
              setWidgetTitle("");
              setWidgetType("timeseries");
              setWidgetWidth(6);
              setShowAddWidgetModal(true);
            }}
            className="px-3.5 py-1.5 bg-blue-600 hover:bg-blue-500 text-white rounded-lg text-xs font-medium"
          >
            Add First Widget
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-12 gap-4">
          {selectedDashboard.widgets.map((widget, idx) => {
            const colSpanClass =
              widget.w >= 12
                ? "md:col-span-12"
                : widget.w >= 8
                ? "md:col-span-8"
                : widget.w >= 6
                ? "md:col-span-6"
                : widget.w >= 4
                ? "md:col-span-4"
                : "md:col-span-3";

            return (
              <div
                key={widget.id}
                className={`${colSpanClass} bg-slate-900 border border-slate-800 rounded-xl flex flex-col overflow-hidden shadow-sm hover:border-slate-700 transition-colors`}
              >
                {/* Widget Header */}
                <div className="px-4 py-3 border-b border-slate-800/80 flex items-center justify-between bg-slate-900/60">
                  <div className="flex items-center gap-2">
                    <span className="text-xs font-semibold text-white truncate max-w-[200px]">
                      {widget.title}
                    </span>
                    <span className="px-1.5 py-0.5 text-[9px] font-mono rounded bg-slate-800 text-slate-400 border border-slate-700">
                      {widget.type}
                    </span>
                  </div>

                  {/* Widget Controls */}
                  <div className="flex items-center gap-1">
                    <button
                      onClick={() => handleMoveWidget(idx, "up")}
                      disabled={idx === 0}
                      className="p-1 text-slate-400 hover:text-slate-200 disabled:opacity-20"
                      title="Move Left/Up"
                    >
                      <ArrowUp className="w-3 h-3" />
                    </button>
                    <button
                      onClick={() => handleMoveWidget(idx, "down")}
                      disabled={idx === selectedDashboard.widgets.length - 1}
                      className="p-1 text-slate-400 hover:text-slate-200 disabled:opacity-20"
                      title="Move Right/Down"
                    >
                      <ArrowDown className="w-3 h-3" />
                    </button>
                    <button
                      onClick={() => handleRemoveWidget(widget.id)}
                      className="p-1 text-slate-400 hover:text-rose-400"
                      title="Remove Widget"
                    >
                      <Trash2 className="w-3 h-3" />
                    </button>
                  </div>
                </div>

                {/* Widget Body Content */}
                <div className="p-4 flex-1 min-h-[140px] flex flex-col justify-center">
                  {renderWidgetContent(widget)}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Add / Edit Widget Modal */}
      {showAddWidgetModal && (
        <div className="fixed inset-0 bg-black/70 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-slate-900 border border-slate-800 rounded-xl max-w-xl w-full p-6 space-y-5 shadow-2xl">
            <h3 className="text-lg font-bold text-white flex items-center gap-2">
              <LayoutGrid className="w-5 h-5 text-blue-400" />
              {editingWidget ? "Configure Widget" : "Add Widget to Dashboard"}
            </h3>

            <form onSubmit={handleAddOrUpdateWidget} className="space-y-4">
              <div>
                <label className="block text-xs font-medium text-slate-400 mb-1">Widget Type</label>
                <div className="grid grid-cols-2 sm:grid-cols-3 gap-2 max-h-48 overflow-y-auto pr-1">
                  {WIDGET_CATALOG.map((cat) => (
                    <button
                      key={cat.type}
                      type="button"
                      onClick={() => {
                        setWidgetType(cat.type);
                        setWidgetWidth(cat.defaultW);
                      }}
                      className={`p-2.5 rounded-lg border text-left flex flex-col gap-1 transition-colors ${
                        widgetType === cat.type
                          ? "bg-blue-600/20 border-blue-500 text-blue-300"
                          : "bg-slate-800/60 border-slate-700 text-slate-300 hover:border-slate-600"
                      }`}
                    >
                      <div className="flex items-center gap-1.5 font-semibold text-xs">
                        <cat.icon className="w-3.5 h-3.5" />
                        {cat.name}
                      </div>
                      <span className="text-[10px] text-slate-400 line-clamp-1">{cat.desc}</span>
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <label className="block text-xs font-medium text-slate-400 mb-1">Widget Title</label>
                <input
                  type="text"
                  value={widgetTitle}
                  onChange={(e) => setWidgetTitle(e.target.value)}
                  placeholder="e.g., API P95 Latency by Route"
                  className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-medium text-slate-400 mb-1">Width (Columns: 3, 4, 6, 8, 12)</label>
                  <select
                    value={widgetWidth}
                    onChange={(e) => setWidgetWidth(Number(e.target.value))}
                    className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    <option value={3}>3 Columns (1/4 Width)</option>
                    <option value={4}>4 Columns (1/3 Width)</option>
                    <option value={6}>6 Columns (Half Width)</option>
                    <option value={8}>8 Columns (2/3 Width)</option>
                    <option value={12}>12 Columns (Full Width)</option>
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-medium text-slate-400 mb-1">Target Metric</label>
                  <select
                    value={widgetConfigMetric}
                    onChange={(e) => setWidgetConfigMetric(e.target.value)}
                    className="w-full bg-slate-800 border border-slate-700 rounded-lg px-3 py-2 text-sm text-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    <option value="http.server.requests">http.server.requests</option>
                    <option value="http.server.duration">http.server.duration</option>
                    <option value="system.cpu.utilization">system.cpu.utilization</option>
                    <option value="system.memory.usage">system.memory.usage</option>
                    <option value="db.query.duration">db.query.duration</option>
                    <option value="redis.hit_ratio">redis.hit_ratio</option>
                  </select>
                </div>
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => setShowAddWidgetModal(false)}
                  className="px-4 py-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg text-sm"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-blue-600 hover:bg-blue-500 text-white rounded-lg text-sm font-medium"
                >
                  {editingWidget ? "Update Widget" : "Add to Dashboard"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Template Gallery Modal */}
      {showTemplatesModal && (
        <div className="fixed inset-0 bg-black/70 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-slate-900 border border-slate-800 rounded-xl max-w-2xl w-full p-6 space-y-5 shadow-2xl">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="text-lg font-bold text-white flex items-center gap-2">
                  <Sparkles className="w-5 h-5 text-purple-400" />
                  Pre-Built Observability Templates
                </h3>
                <p className="text-xs text-slate-400 mt-0.5">
                  Choose a turnkey curated template designed for enterprise telemetry patterns.
                </p>
              </div>
              <button onClick={() => setShowTemplatesModal(false)} className="text-slate-400 hover:text-white">✕</button>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-4 flex flex-col justify-between hover:border-purple-500/50 transition-colors">
                <div>
                  <h4 className="font-semibold text-sm text-white">Kubernetes & Infrastructure</h4>
                  <p className="text-xs text-slate-400 mt-1">
                    Cluster CPU/RAM telemetry, active node health matrix, and live infrastructure event tails.
                  </p>
                </div>
                <button
                  onClick={() => handleInstantiateTemplate("k8s-infra")}
                  className="mt-4 w-full py-1.5 bg-purple-600 hover:bg-purple-500 text-white text-xs font-medium rounded-lg"
                >
                  Instantiate Template
                </button>
              </div>

              <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-4 flex flex-col justify-between hover:border-purple-500/50 transition-colors">
                <div>
                  <h4 className="font-semibold text-sm text-white">API Gateway & Microservices APM</h4>
                  <p className="text-xs text-slate-400 mt-1">
                    Requests per second, P95/P99 latency waterfall summaries, and slow endpoint tables.
                  </p>
                </div>
                <button
                  onClick={() => handleInstantiateTemplate("apm-services")}
                  className="mt-4 w-full py-1.5 bg-blue-600 hover:bg-blue-500 text-white text-xs font-medium rounded-lg"
                >
                  Instantiate Template
                </button>
              </div>

              <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-4 flex flex-col justify-between hover:border-purple-500/50 transition-colors">
                <div>
                  <h4 className="font-semibold text-sm text-white">Executive SLO & Incident Summary</h4>
                  <p className="text-xs text-slate-400 mt-1">
                    High-level service reliability objectives, burn rate gauges, open incident queue, and runbooks.
                  </p>
                </div>
                <button
                  onClick={() => handleInstantiateTemplate("slo-incidents")}
                  className="mt-4 w-full py-1.5 bg-amber-600 hover:bg-amber-500 text-white text-xs font-medium rounded-lg"
                >
                  Instantiate Template
                </button>
              </div>

              <div className="bg-slate-800/60 border border-slate-700 rounded-xl p-4 flex flex-col justify-between hover:border-purple-500/50 transition-colors">
                <div>
                  <h4 className="font-semibold text-sm text-white">Database & Redis Cache Health</h4>
                  <p className="text-xs text-slate-400 mt-1">
                    PostgreSQL query latency, connection pool pressure, and Redis hit/miss ratios.
                  </p>
                </div>
                <button
                  onClick={() => handleInstantiateTemplate("db-redis")}
                  className="mt-4 w-full py-1.5 bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-medium rounded-lg"
                >
                  Instantiate Template
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Export Modal */}
      {showExportModal && selectedDashboard && (
        <div className="fixed inset-0 bg-black/70 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-slate-900 border border-slate-800 rounded-xl max-w-xl w-full p-6 space-y-4 shadow-2xl">
            <h3 className="text-base font-bold text-white flex items-center gap-2">
              <Download className="w-5 h-5 text-blue-400" />
              Export Dashboard JSON
            </h3>
            <textarea
              readOnly
              value={JSON.stringify(selectedDashboard, null, 2)}
              rows={12}
              className="w-full bg-slate-950 font-mono text-xs text-slate-300 p-3 rounded-lg border border-slate-800 focus:outline-none"
            />
            <div className="flex justify-between items-center">
              <button
                onClick={() => {
                  navigator.clipboard.writeText(JSON.stringify(selectedDashboard, null, 2));
                  setCopiedExport(true);
                  setTimeout(() => setCopiedExport(false), 2000);
                }}
                className="flex items-center gap-1.5 px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-medium rounded-lg"
              >
                {copiedExport ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
                {copiedExport ? "Copied!" : "Copy to Clipboard"}
              </button>
              <button
                onClick={() => setShowExportModal(false)}
                className="px-4 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs rounded-lg"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Import Modal */}
      {showImportModal && (
        <div className="fixed inset-0 bg-black/70 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-slate-900 border border-slate-800 rounded-xl max-w-xl w-full p-6 space-y-4 shadow-2xl">
            <h3 className="text-base font-bold text-white flex items-center gap-2">
              <Upload className="w-5 h-5 text-blue-400" />
              Import Dashboard JSON
            </h3>
            <form onSubmit={handleImportSubmit} className="space-y-4">
              <textarea
                value={importJsonText}
                onChange={(e) => setImportJsonText(e.target.value)}
                placeholder="Paste dashboard JSON definition here..."
                rows={10}
                required
                className="w-full bg-slate-950 font-mono text-xs text-slate-300 p-3 rounded-lg border border-slate-800 focus:outline-none focus:ring-1 focus:ring-blue-500"
              />
              <div className="flex justify-end gap-3">
                <button
                  type="button"
                  onClick={() => setShowImportModal(false)}
                  className="px-4 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs rounded-lg"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-1.5 bg-blue-600 hover:bg-blue-500 text-white text-xs font-medium rounded-lg"
                >
                  Import Dashboard
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

// ==========================================
// Widget Visualizer Dispatcher
// ==========================================

function renderWidgetContent(widget: DashboardWidgetDto) {
  switch (widget.type) {
    case "timeseries":
      return (
        <div className="space-y-2">
          <div className="flex items-center justify-between text-xs text-slate-400">
            <span>Aggregated Value:</span>
            <span className="font-semibold text-blue-400 font-mono">1.24k req/s</span>
          </div>
          {/* Timeseries mock chart / SVG sparkline */}
          <div className="h-20 w-full flex items-end gap-1 px-1">
            {[45, 60, 52, 78, 65, 82, 70, 89, 95, 88, 76, 92, 100, 85, 94].map((val, i) => (
              <div
                key={i}
                className="flex-1 bg-gradient-to-t from-blue-600/40 to-blue-500 rounded-t transition-all hover:bg-blue-400"
                style={{ height: `${val}%` }}
                title={`Interval ${i}: ${val}%`}
              />
            ))}
          </div>
          <div className="flex justify-between text-[10px] text-slate-500 font-mono">
            <span>-60m</span>
            <span>-30m</span>
            <span>Now</span>
          </div>
        </div>
      );

    case "stat":
      return (
        <div className="flex flex-col items-center justify-center py-2 space-y-1">
          <span className="text-3xl font-extrabold text-white tracking-tight font-mono">99.82%</span>
          <div className="flex items-center gap-1 text-xs text-emerald-400 font-medium">
            <span>▲ +0.14%</span>
            <span className="text-slate-500 text-[10px]">vs previous interval</span>
          </div>
        </div>
      );

    case "bar":
      return (
        <div className="space-y-2 text-xs">
          {[
            { name: "checkout-api", count: 4200, pct: 90 },
            { name: "orders-worker", count: 3100, pct: 68 },
            { name: "identity-svc", count: 1800, pct: 40 },
          ].map((item) => (
            <div key={item.name} className="space-y-1">
              <div className="flex justify-between text-[11px] text-slate-300">
                <span>{item.name}</span>
                <span className="font-mono text-slate-400">{item.count.toLocaleString()}</span>
              </div>
              <div className="w-full bg-slate-800 rounded-full h-1.5 overflow-hidden">
                <div className="bg-blue-500 h-1.5 rounded-full" style={{ width: `${item.pct}%` }} />
              </div>
            </div>
          ))}
        </div>
      );

    case "gauge":
      return (
        <div className="flex flex-col items-center justify-center space-y-2 py-1">
          <div className="relative w-24 h-24 flex items-center justify-center">
            <svg className="w-full h-full -rotate-90" viewBox="0 0 36 36">
              <path
                className="text-slate-800"
                strokeWidth="3.5"
                stroke="currentColor"
                fill="none"
                d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831"
              />
              <path
                className="text-emerald-400"
                strokeDasharray="85, 100"
                strokeWidth="3.5"
                strokeLinecap="round"
                stroke="currentColor"
                fill="none"
                d="M18 2.0845 a 15.9155 15.9155 0 0 1 0 31.831 a 15.9155 15.9155 0 0 1 0 -31.831"
              />
            </svg>
            <div className="absolute flex flex-col items-center">
              <span className="text-xl font-bold text-white font-mono">85%</span>
              <span className="text-[9px] text-slate-400">Target 95%</span>
            </div>
          </div>
        </div>
      );

    case "table":
      return (
        <div className="overflow-x-auto text-[11px]">
          <table className="w-full text-left text-slate-300">
            <thead className="bg-slate-800/60 text-slate-400 uppercase text-[9px]">
              <tr>
                <th className="p-1.5">Route</th>
                <th className="p-1.5">Method</th>
                <th className="p-1.5">P95</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800">
              <tr>
                <td className="p-1.5 font-mono text-slate-200">/api/orders</td>
                <td className="p-1.5 font-semibold text-emerald-400">POST</td>
                <td className="p-1.5 font-mono text-amber-400">142ms</td>
              </tr>
              <tr>
                <td className="p-1.5 font-mono text-slate-200">/api/catalog</td>
                <td className="p-1.5 font-semibold text-blue-400">GET</td>
                <td className="p-1.5 font-mono text-emerald-400">18ms</td>
              </tr>
              <tr>
                <td className="p-1.5 font-mono text-slate-200">/api/checkout</td>
                <td className="p-1.5 font-semibold text-emerald-400">POST</td>
                <td className="p-1.5 font-mono text-rose-400">310ms</td>
              </tr>
            </tbody>
          </table>
        </div>
      );

    case "status-grid":
      return (
        <div className="grid grid-cols-3 gap-2 text-center text-xs">
          <div className="p-2 rounded bg-emerald-950/40 border border-emerald-800/60 text-emerald-300">
            <span className="block text-[10px] text-slate-400">auth-api</span>
            <span className="font-bold">Healthy</span>
          </div>
          <div className="p-2 rounded bg-emerald-950/40 border border-emerald-800/60 text-emerald-300">
            <span className="block text-[10px] text-slate-400">orders-api</span>
            <span className="font-bold">Healthy</span>
          </div>
          <div className="p-2 rounded bg-amber-950/40 border border-amber-800/60 text-amber-300">
            <span className="block text-[10px] text-slate-400">payments-gw</span>
            <span className="font-bold">Degraded</span>
          </div>
        </div>
      );

    case "log-stream":
      return (
        <div className="bg-slate-950 p-2.5 rounded border border-slate-800 font-mono text-[11px] space-y-1 max-h-32 overflow-y-auto">
          <div className="text-slate-400">
            <span className="text-slate-600">[23:45:01]</span> <span className="text-blue-400 font-semibold">INFO</span> Incoming HTTP POST /api/orders from 10.0.4.12
          </div>
          <div className="text-slate-400">
            <span className="text-slate-600">[23:45:03]</span> <span className="text-amber-400 font-semibold">WARN</span> Database connection pool reached 82% threshold
          </div>
          <div className="text-slate-400">
            <span className="text-slate-600">[23:45:08]</span> <span className="text-blue-400 font-semibold">INFO</span> Span 81d9f0 executed in 14ms
          </div>
        </div>
      );

    case "waterfall-summary":
      return (
        <div className="space-y-2 text-xs">
          <div className="flex justify-between text-[11px]">
            <span className="text-slate-400">P50 Median</span>
            <span className="font-mono text-emerald-400 font-semibold">18ms</span>
          </div>
          <div className="w-full bg-slate-800 rounded-full h-1.5">
            <div className="bg-emerald-500 h-1.5 rounded-full" style={{ width: "20%" }} />
          </div>

          <div className="flex justify-between text-[11px]">
            <span className="text-slate-400">P95 Latency</span>
            <span className="font-mono text-amber-400 font-semibold">86ms</span>
          </div>
          <div className="w-full bg-slate-800 rounded-full h-1.5">
            <div className="bg-amber-500 h-1.5 rounded-full" style={{ width: "65%" }} />
          </div>

          <div className="flex justify-between text-[11px]">
            <span className="text-slate-400">P99 Tail</span>
            <span className="font-mono text-rose-400 font-semibold">240ms</span>
          </div>
          <div className="w-full bg-slate-800 rounded-full h-1.5">
            <div className="bg-rose-500 h-1.5 rounded-full" style={{ width: "95%" }} />
          </div>
        </div>
      );

    case "incidents":
      return (
        <div className="space-y-2 text-xs">
          <div className="p-2 rounded bg-rose-950/30 border border-rose-800/40 flex items-center justify-between">
            <div>
              <span className="font-semibold text-rose-300 block">High Error Rate: Payments</span>
              <span className="text-[10px] text-slate-400">Firing for 14 minutes</span>
            </div>
            <span className="px-1.5 py-0.5 text-[9px] font-bold bg-rose-900/60 text-rose-300 rounded border border-rose-700">
              P1 Critical
            </span>
          </div>
          <div className="p-2 rounded bg-amber-950/30 border border-amber-800/40 flex items-center justify-between">
            <div>
              <span className="font-semibold text-amber-300 block">Queue Lag Elevation</span>
              <span className="text-[10px] text-slate-400">Firing for 6 minutes</span>
            </div>
            <span className="px-1.5 py-0.5 text-[9px] font-bold bg-amber-900/60 text-amber-300 rounded border border-amber-700">
              P2 High
            </span>
          </div>
        </div>
      );

    case "slo-burn":
      return (
        <div className="space-y-2 text-xs">
          <div className="flex items-center justify-between">
            <span className="text-slate-400">Target Objective:</span>
            <span className="font-mono font-bold text-white">99.9%</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="text-slate-400">Error Budget Burn:</span>
            <span className="font-mono font-bold text-emerald-400">0.8x (Normal)</span>
          </div>
          <div className="w-full bg-slate-800 rounded-full h-2 overflow-hidden">
            <div className="bg-emerald-500 h-2 rounded-full" style={{ width: "18%" }} />
          </div>
          <span className="text-[10px] text-slate-500 block text-right">82% budget remaining this 30d window</span>
        </div>
      );

    case "markdown":
      return (
        <div className="text-xs text-slate-300 space-y-1.5 bg-slate-950/60 p-3 rounded border border-slate-800">
          <p className="font-semibold text-blue-400"># Runbook Guidelines</p>
          <p className="text-[11px] text-slate-400 leading-relaxed">
            - Verify Redis cluster quorum if error rate spikes above 2%.<br />
            - Trigger failover via primary replica election command.
          </p>
        </div>
      );

    case "pie":
      return (
        <div className="flex items-center justify-around py-1 text-xs">
          <div className="relative w-16 h-16 rounded-full border-4 border-blue-500 border-t-amber-400 border-r-rose-500 flex items-center justify-center">
            <span className="text-[10px] font-bold text-slate-300">200 OK</span>
          </div>
          <div className="space-y-1 text-[11px]">
            <div className="flex items-center gap-1.5 text-blue-400">
              <span className="w-2 h-2 rounded-full bg-blue-500" /> 2xx Success (94%)
            </div>
            <div className="flex items-center gap-1.5 text-amber-400">
              <span className="w-2 h-2 rounded-full bg-amber-500" /> 4xx Client (5%)
            </div>
            <div className="flex items-center gap-1.5 text-rose-400">
              <span className="w-2 h-2 rounded-full bg-rose-500" /> 5xx Server (1%)
            </div>
          </div>
        </div>
      );

    default:
      return <div className="text-xs text-slate-500">Widget content preview</div>;
  }
}
