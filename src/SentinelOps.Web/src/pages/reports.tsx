import { useState, useEffect, useCallback } from "react";
import {
  getReportTypes,
  previewReport,
  exportReportCsv,
  exportReportPrint,
  exportReportPdf,
  type ReportTypeOptionDto,
  type ReportPreviewDto,
} from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  BarChart3,
  Printer,
  FileSpreadsheet,
  FileText,
  RefreshCw,
  Clock,
  ShieldCheck,
  Activity,
  AlertTriangle,
  Zap,
  Target,
  HeartPulse,
  Calendar,
} from "lucide-react";

export function ReportsPage() {
  const [reportTypes, setReportTypes] = useState<ReportTypeOptionDto[]>([]);
  const [selectedType, setSelectedType] = useState<string>("ExecutiveSummary");
  const [timeRange, setTimeRange] = useState<string>("24h");
  const [serviceName, setServiceName] = useState<string>("");

  const [preview, setPreview] = useState<ReportPreviewDto | null>(null);
  const [isLoadingPreview, setIsLoadingPreview] = useState(false);
  const [isExportingCsv, setIsExportingCsv] = useState(false);
  const [isExportingPrint, setIsExportingPrint] = useState(false);
  const [isExportingPdf, setIsExportingPdf] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Load available report types
  useEffect(() => {
    const loadTypes = async () => {
      try {
        const types = await getReportTypes();
        setReportTypes(types);
      } catch (err: any) {
        setErrorMessage(err.message || "Failed to load report definitions");
      }
    };
    loadTypes();
  }, []);


  const handleGeneratePreview = useCallback(async () => {
    try {
      setIsLoadingPreview(true);
      setErrorMessage(null);
      const res = await previewReport({
        reportType: selectedType,
        timeRange,
        serviceName: serviceName.trim() || undefined,
      });
      setPreview(res);
    } catch (err: any) {
      setErrorMessage(err.response?.data?.message || err.message || "Failed to generate report preview");
    } finally {
      setIsLoadingPreview(false);
    }
  }, [selectedType, timeRange, serviceName]);

  useEffect(() => {
    handleGeneratePreview();
  }, [handleGeneratePreview]);

  const handleExportCsv = async () => {
    try {
      setIsExportingCsv(true);
      const blob = await exportReportCsv({
        reportType: selectedType,
        timeRange,
        serviceName: serviceName.trim() || undefined,
      });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `SentinelOps_${selectedType}_${timeRange}.csv`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      window.URL.revokeObjectURL(url);
    } catch (err: any) {
      setErrorMessage(err.message || "Failed to export CSV");
    } finally {
      setIsExportingCsv(false);
    }
  };

  const handleExportPrint = async () => {
    try {
      setIsExportingPrint(true);
      const blob = await exportReportPrint({
        reportType: selectedType,
        timeRange,
        serviceName: serviceName.trim() || undefined,
      });
      const url = window.URL.createObjectURL(blob);
      const newTab = window.open(url, "_blank");
      if (newTab) {
        newTab.focus();
      }
    } catch (err: any) {
      setErrorMessage(err.message || "Failed to generate printable report");
    } finally {
      setIsExportingPrint(false);
    }
  };

  const handleExportPdf = async () => {
    try {
      setIsExportingPdf(true);
      setErrorMessage(null);
      const blob = await exportReportPdf({
        reportType: selectedType,
        timeRange,
        serviceName: serviceName.trim() || undefined,
      });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `SentinelOps_${selectedType}_${timeRange}.pdf`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      window.URL.revokeObjectURL(url);
    } catch (err: any) {
      setErrorMessage(err.message || "Failed to export PDF report");
    } finally {
      setIsExportingPdf(false);
    }
  };

  const getReportIcon = (id: string) => {
    switch (id) {
      case "ExecutiveSummary": return <Zap className="w-5 h-5 text-amber-400" />;
      case "IncidentPostmortem": return <Clock className="w-5 h-5 text-purple-400" />;
      case "SloCompliance": return <Target className="w-5 h-5 text-emerald-400" />;
      case "InfrastructureHealth": return <HeartPulse className="w-5 h-5 text-teal-400" />;
      case "SecurityAudit": return <ShieldCheck className="w-5 h-5 text-rose-400" />;
      case "TelemetryVolume": return <BarChart3 className="w-5 h-5 text-indigo-400" />;
      case "Performance": return <Zap className="w-5 h-5 text-amber-400" />;
      case "ErrorsAndExceptions": return <AlertTriangle className="w-5 h-5 text-rose-400" />;
      case "IncidentHistory": return <Clock className="w-5 h-5 text-purple-400" />;
      case "AlertActivity": return <Activity className="w-5 h-5 text-sky-400" />;
      case "ApplicationHealth": return <HeartPulse className="w-5 h-5 text-teal-400" />;
      default: return <BarChart3 className="w-5 h-5 text-sky-400" />;
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-white flex items-center gap-2">
            <BarChart3 className="w-7 h-7 text-sky-400" />
            Enterprise Reporting & Analytics
          </h1>
          <p className="text-sm text-slate-400">
            Exportable operational metrics, reliability trends, and formula-injection sanitized reports
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={handleExportPdf}
            disabled={isExportingPdf || !preview}
            className="flex items-center gap-1.5 border-sky-800/60 bg-sky-950/30 text-sky-300 hover:bg-sky-900/50 hover:text-white"
          >
            <FileText className="w-4 h-4 text-sky-400" />
            {isExportingPdf ? "Generating PDF..." : "Export PDF"}
          </Button>

          <Button
            variant="outline"
            size="sm"
            onClick={handleExportCsv}
            disabled={isExportingCsv || !preview}
            className="flex items-center gap-1.5"
          >
            <FileSpreadsheet className="w-4 h-4 text-emerald-400" />
            {isExportingCsv ? "Exporting CSV..." : "Export CSV"}
          </Button>

          <Button
            variant="outline"
            size="sm"
            onClick={handleExportPrint}
            disabled={isExportingPrint || !preview}
            className="flex items-center gap-1.5"
          >
            <Printer className="w-4 h-4 text-purple-400" />
            {isExportingPrint ? "Preparing Print..." : "Executive Print"}
          </Button>

          <Button
            size="sm"
            onClick={handleGeneratePreview}
            disabled={isLoadingPreview}
            className="flex items-center gap-1.5 bg-sky-600 hover:bg-sky-500 text-white"
          >
            <RefreshCw className={`w-4 h-4 ${isLoadingPreview ? "animate-spin" : ""}`} />
            Run Query
          </Button>
        </div>
      </div>

      {errorMessage && (
        <div className="bg-rose-950/60 border border-rose-800 text-rose-300 p-3.5 rounded-lg flex items-center justify-between text-sm">
          <div className="flex items-center gap-2">
            <AlertTriangle className="w-4 h-4 text-rose-400 shrink-0" />
            <span>{errorMessage}</span>
          </div>
        </div>
      )}

      {/* Report Type Selector Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
        {reportTypes.map((type) => {
          const isSelected = selectedType === type.id;
          return (
            <div
              key={type.id}
              onClick={() => setSelectedType(type.id)}
              className={`cursor-pointer p-4 rounded-xl border transition-all text-left flex items-start gap-3.5 ${
                isSelected
                  ? "bg-slate-900 border-sky-500 shadow-sm shadow-sky-500/20"
                  : "bg-slate-950/60 border-slate-800/80 hover:bg-slate-900/50 hover:border-slate-700"
              }`}
            >
              <div className="p-2.5 rounded-lg bg-slate-900 border border-slate-800 shrink-0">
                {getReportIcon(type.id)}
              </div>
              <div className="min-w-0 flex-1">
                <div className="flex items-center justify-between gap-1 mb-1">
                  <h4 className="text-sm font-semibold text-white truncate">{type.name}</h4>
                  <Badge variant="outline" className="text-[10px] bg-slate-900">{type.category}</Badge>
                </div>
                <p className="text-xs text-slate-400 line-clamp-2 leading-relaxed">{type.description}</p>
              </div>
            </div>
          );
        })}
      </div>

      {/* Controls & Filter Bar */}
      <Card className="bg-slate-900/60 border-slate-800">
        <CardContent className="p-4">
          <div className="flex flex-col sm:flex-row items-center justify-between gap-3 text-sm">
            <div className="flex flex-wrap items-center gap-3 w-full sm:w-auto">
              <div className="flex items-center gap-2">
                <Calendar className="w-4 h-4 text-slate-400" />
                <span className="text-xs text-slate-400">Evaluation Window:</span>
                <select
                  value={timeRange}
                  onChange={(e) => setTimeRange(e.target.value)}
                  className="bg-slate-950 border border-slate-800 text-xs text-slate-200 rounded-md px-3 py-1.5 outline-none"
                >
                  <option value="24h">Last 24 Hours</option>
                  <option value="7d">Last 7 Days</option>
                  <option value="30d">Last 30 Days</option>
                </select>
              </div>

              <div className="flex items-center gap-2">
                <Input
                  placeholder="Optional service name filter..."
                  value={serviceName}
                  onChange={(e) => setServiceName(e.target.value)}
                  className="bg-slate-950 border-slate-800 text-xs h-8 w-56"
                />
              </div>
            </div>

            <div className="text-xs text-slate-500 flex items-center gap-1.5 self-start sm:self-center">
              <ShieldCheck className="w-4 h-4 text-emerald-400" />
              <span>CSV Formula Injection Defense Active</span>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Report Preview */}
      {preview && (
        <div className="space-y-4">
          {/* Executive Summary Metrics */}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
            {Object.entries(preview.summaryMetrics).map(([key, val]) => (
              <Card key={key} className="bg-slate-900/80 border-slate-800">
                <CardContent className="p-4 text-center">
                  <p className="text-xs font-medium text-slate-400 uppercase tracking-wider">{key}</p>
                  <h3 className="text-xl font-bold text-sky-400 mt-1">{String(val)}</h3>
                </CardContent>
              </Card>
            ))}
          </div>

          {/* Table Preview */}
          <Card className="bg-slate-900/60 border-slate-800">
            <CardHeader className="pb-3 border-b border-slate-800 flex flex-row items-center justify-between">
              <div>
                <CardTitle className="text-base font-semibold text-white">{preview.title}</CardTitle>
                <CardDescription className="text-xs text-slate-400">{preview.description}</CardDescription>
              </div>
              <Badge variant="outline" className="text-xs font-mono text-slate-400">
                {preview.rows.length} rows returned
              </Badge>
            </CardHeader>
            <CardContent className="p-0">
              <div className="overflow-x-auto max-h-[500px]">
                <table className="w-full text-left text-sm">
                  <thead className="bg-slate-950/80 sticky top-0 text-xs text-slate-400 uppercase border-b border-slate-800 z-10">
                    <tr>
                      {preview.columnHeaders.map((h, i) => (
                        <th key={i} className="px-4 py-3">{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-800/60">
                    {preview.rows.length === 0 ? (
                      <tr>
                        <td colSpan={preview.columnHeaders.length} className="py-12 text-center text-slate-500">
                          No records matched the selected query window.
                        </td>
                      </tr>
                    ) : (
                      preview.rows.map((row, rowIdx) => (
                        <tr key={rowIdx} className="hover:bg-slate-800/30 transition-colors">
                          {row.map((cell, cellIdx) => (
                            <td key={cellIdx} className="px-4 py-3 text-xs text-slate-300">
                              {cell}
                            </td>
                          ))}
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}
