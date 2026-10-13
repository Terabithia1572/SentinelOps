using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Application.Common.Security;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Domain.Enums;
using SentinelOps.Infrastructure.Services.Reporting;

namespace SentinelOps.Infrastructure.Services;

public class ReportingService : IReportingService
{
    private readonly IApplicationDbContext _context;

    public ReportingService(IApplicationDbContext context)
    {
        _context = context;
    }

    public List<ReportTypeOptionDto> GetAvailableReportTypes()
    {
        return
        [
            new()
            {
                Id = "ExecutiveSummary",
                Name = "Executive Summary & SLOs",
                Description = "High-level SLO compliance, open incidents, MTTR, overall error rate, and throughput.",
                Category = "Executive"
            },
            new()
            {
                Id = "IncidentPostmortem",
                Name = "Incident Postmortem & Timeline",
                Description = "Incident timeline, root cause patterns, MTTA/MTTR metrics, and resolution history.",
                Category = "Reliability"
            },
            new()
            {
                Id = "SloCompliance",
                Name = "SLO Compliance & Error Budget",
                Description = "Service Level Objectives status, error budget burn rates, and breach events.",
                Category = "Reliability"
            },
            new()
            {
                Id = "InfrastructureHealth",
                Name = "Infrastructure & Service Health",
                Description = "Service availability, latency health checks, host responsiveness, and uptime history.",
                Category = "Operations"
            },
            new()
            {
                Id = "SecurityAudit",
                Name = "Enterprise Security & Audit",
                Description = "Failed logins, administrative mutations, policy changes, and API key lifecycles.",
                Category = "Security"
            },
            new()
            {
                Id = "TelemetryVolume",
                Name = "Cost & Telemetry Volume",
                Description = "Ingested spans, logs, metric points by service/application, and projected storage.",
                Category = "FinOps"
            }
        ];
    }

    public async Task<ReportPreviewDto> GenerateReportPreviewAsync(
        GenerateReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var (startUtc, endUtc) = ResolveTimeRange(request.TimeRange, request.StartUtc, request.EndUtc);

        return request.ReportType.ToLowerInvariant() switch
        {
            "executivesummary" => await GenerateExecutiveSummaryReportAsync(request, startUtc, endUtc, cancellationToken),
            "incidentpostmortem" or "incidenthistory" => await GenerateIncidentsReportAsync(request, startUtc, endUtc, cancellationToken),
            "slocompliance" => await GenerateSloReportAsync(request, startUtc, endUtc, cancellationToken),
            "infrastructurehealth" or "applicationhealth" => await GenerateHealthReportAsync(request, startUtc, endUtc, cancellationToken),
            "securityaudit" => await GenerateSecurityAuditReportAsync(request, startUtc, endUtc, cancellationToken),
            "telemetryvolume" or "costandvolume" => await GenerateTelemetryVolumeReportAsync(request, startUtc, endUtc, cancellationToken),
            "performance" => await GeneratePerformanceReportAsync(request, startUtc, endUtc, cancellationToken),
            "errorsandexceptions" => await GenerateErrorsReportAsync(request, startUtc, endUtc, cancellationToken),
            "alertactivity" => await GenerateAlertActivityReportAsync(request, startUtc, endUtc, cancellationToken),
            _ => await GenerateExecutiveSummaryReportAsync(request, startUtc, endUtc, cancellationToken)
        };
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> ExportCsvAsync(
        ExportReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var preview = await GenerateReportPreviewAsync(request, cancellationToken);

        var sb = new StringBuilder();

        // Metadata Header
        sb.AppendLine($"# SentinelOps Report: {preview.Title}");
        sb.AppendLine($"# Period: {preview.StartUtc:yyyy-MM-dd HH:mm:ss} UTC to {preview.EndUtc:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"# Generated: {preview.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("#");

        // KPI Summary Line
        var kpis = string.Join(", ", preview.SummaryMetrics.Select(m => $"{m.Key}: {m.Value}"));
        sb.AppendLine($"# Summary Metrics: {kpis}");
        sb.AppendLine();

        // CSV Header with injection prevention
        var headerLine = string.Join(",", preview.ColumnHeaders.Select(CsvSanitizer.Sanitize));
        sb.AppendLine(headerLine);

        // CSV Rows with formula injection defense
        foreach (var row in preview.Rows)
        {
            var rowLine = string.Join(",", row.Select(CsvSanitizer.Sanitize));
            sb.AppendLine(rowLine);
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var fileName = $"SentinelOps_{request.ReportType}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        return (bytes, "text/csv; charset=utf-8", fileName);
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> ExportPrintableReportAsync(
        ExportReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var preview = await GenerateReportPreviewAsync(request, cancellationToken);

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"utf-8\" />");
        html.AppendLine($"<title>{preview.Title} - SentinelOps</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background: #0b0f17; color: #f1f5f9; margin: 0; padding: 40px; }");
        html.AppendLine(".container { max-width: 1200px; margin: 0 auto; background: #111827; border: 1px solid #1f2937; border-radius: 8px; padding: 32px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.5); }");
        html.AppendLine(".header { display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #374151; padding-bottom: 20px; margin-bottom: 24px; }");
        html.AppendLine(".brand { font-size: 24px; font-weight: 700; color: #38bdf8; display: flex; align-items: center; gap: 8px; }");
        html.AppendLine(".title { font-size: 20px; font-weight: 600; margin-top: 4px; color: #f8fafc; }");
        html.AppendLine(".meta { font-size: 13px; color: #94a3b8; text-align: right; }");
        html.AppendLine(".kpi-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; margin-bottom: 32px; }");
        html.AppendLine(".kpi-card { background: #1e293b; border: 1px solid #334155; border-radius: 6px; padding: 16px; text-align: center; }");
        html.AppendLine(".kpi-label { font-size: 12px; font-weight: 600; color: #94a3b8; text-transform: uppercase; margin-bottom: 6px; }");
        html.AppendLine(".kpi-val { font-size: 22px; font-weight: 700; color: #38bdf8; }");
        html.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 16px; font-size: 13px; }");
        html.AppendLine("th { background: #1e293b; color: #cbd5e1; text-align: left; padding: 12px 14px; border-bottom: 2px solid #334155; font-weight: 600; }");
        html.AppendLine("td { padding: 10px 14px; border-bottom: 1px solid #1f2937; color: #e2e8f0; }");
        html.AppendLine("tr:hover { background: #1e293b40; }");
        html.AppendLine(".footer { margin-top: 32px; padding-top: 16px; border-top: 1px solid #1f2937; font-size: 12px; color: #64748b; display: flex; justify-content: space-between; }");
        html.AppendLine("@media print { body { background: #fff; color: #000; padding: 0; } .container { background: #fff; border: none; padding: 0; } .kpi-card { background: #f8fafc; border: 1px solid #e2e8f0; } .brand { color: #0284c7; } .kpi-val { color: #0284c7; } th { background: #f1f5f9; color: #334155; } td { color: #1e293b; } }");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<div class=\"container\">");
        html.AppendLine("<div class=\"header\">");
        html.AppendLine("  <div>");
        html.AppendLine("    <div class=\"brand\">⚡ SentinelOps Enterprise</div>");
        html.AppendLine($"    <div class=\"title\">{preview.Title}</div>");
        html.AppendLine($"    <div style=\"color: #94a3b8; font-size: 14px; margin-top: 2px;\">{preview.Description}</div>");
        html.AppendLine("  </div>");
        html.AppendLine("  <div class=\"meta\">");
        html.AppendLine($"    <div><strong>Period:</strong> {preview.StartUtc:yyyy-MM-dd HH:mm} - {preview.EndUtc:yyyy-MM-dd HH:mm} UTC</div>");
        html.AppendLine($"    <div><strong>Generated:</strong> {preview.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC</div>");
        html.AppendLine("  </div>");
        html.AppendLine("</div>");

        // KPI Cards
        html.AppendLine("<div class=\"kpi-grid\">");
        foreach (var metric in preview.SummaryMetrics)
        {
            html.AppendLine("  <div class=\"kpi-card\">");
            html.AppendLine($"    <div class=\"kpi-label\">{metric.Key}</div>");
            html.AppendLine($"    <div class=\"kpi-val\">{metric.Value}</div>");
            html.AppendLine("  </div>");
        }
        html.AppendLine("</div>");

        // Table
        html.AppendLine("<table>");
        html.AppendLine("<thead><tr>");
        foreach (var header in preview.ColumnHeaders)
        {
            html.AppendLine($"  <th>{header}</th>");
        }
        html.AppendLine("</tr></thead>");
        html.AppendLine("<tbody>");
        foreach (var row in preview.Rows)
        {
            html.AppendLine("<tr>");
            foreach (var cell in row)
            {
                html.AppendLine($"  <td>{System.Net.WebUtility.HtmlEncode(cell)}</td>");
            }
            html.AppendLine("</tr>");
        }
        html.AppendLine("</tbody></table>");

        html.AppendLine("<div class=\"footer\">");
        html.AppendLine("  <div>Confidential & Proprietary - SentinelOps Observability Platform</div>");
        html.AppendLine($"  <div>Total Rows: {preview.Rows.Count}</div>");
        html.AppendLine("</div>");

        html.AppendLine("</div>");
        html.AppendLine("</body></html>");

        var bytes = Encoding.UTF8.GetBytes(html.ToString());
        var fileName = $"SentinelOps_{request.ReportType}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html";

        return (bytes, "text/html; charset=utf-8", fileName);
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> ExportPdfAsync(
        ExportReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var preview = await GenerateReportPreviewAsync(request, cancellationToken);
        var bytes = PdfReportGenerator.Generate(preview);
        var fileName = $"SentinelOps_{request.ReportType}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

        return (bytes, "application/pdf", fileName);
    }


    private static (DateTime StartUtc, DateTime EndUtc) ResolveTimeRange(string? timeRange, DateTime? start, DateTime? end)
    {
        var now = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(timeRange))
        {
            return timeRange.ToLowerInvariant() switch
            {
                "24h" => (now.AddHours(-24), now),
                "7d" => (now.AddDays(-7), now),
                "30d" => (now.AddDays(-30), now),
                _ => (start ?? now.AddHours(-24), end ?? now)
            };
        }

        return (start ?? now.AddHours(-24), end ?? now);
    }

    private async Task<ReportPreviewDto> GeneratePerformanceReportAsync(
        GenerateReportRequest req, DateTime start, DateTime end, CancellationToken ct)
    {
        var query = _context.SpanRecords.AsNoTracking().Where(s => s.StartedAtUtc >= start && s.StartedAtUtc <= end);

        if (req.ApplicationId.HasValue)
        {
            var appName = await _context.Applications.Where(a => a.Id == req.ApplicationId.Value).Select(a => a.Name).FirstOrDefaultAsync(ct);
            if (!string.IsNullOrEmpty(appName)) query = query.Where(s => s.ServiceName == appName);
        }

        if (!string.IsNullOrWhiteSpace(req.ServiceName))
        {
            query = query.Where(s => s.ServiceName == req.ServiceName);
        }

        var totalSpans = await query.CountAsync(ct);
        var errorSpans = await query.CountAsync(s => s.Status.ToLower() == "error", ct);
        var avgDuration = totalSpans > 0 ? await query.AverageAsync(s => (double)s.Duration.TotalMilliseconds, ct) : 0;

        var grouped = await query
            .GroupBy(s => s.ServiceName)
            .Select(g => new
            {
                ServiceName = g.Key,
                Count = g.Count(),
                AvgDurationMs = g.Average(x => (double)x.Duration.TotalMilliseconds),
                MaxDurationMs = g.Max(x => (double)x.Duration.TotalMilliseconds),
                Errors = g.Count(x => x.Status.ToLower() == "error")
            })
            .OrderByDescending(g => g.Count)
            .Take(100)
            .ToListAsync(ct);

        var rows = grouped.Select(g => new List<string>
        {
            g.ServiceName,
            g.Count.ToString(CultureInfo.InvariantCulture),
            g.AvgDurationMs.ToString("F2", CultureInfo.InvariantCulture),
            g.MaxDurationMs.ToString("F2", CultureInfo.InvariantCulture),
            g.Errors.ToString(CultureInfo.InvariantCulture),
            g.Count > 0 ? ((double)g.Errors / g.Count * 100).ToString("F2", CultureInfo.InvariantCulture) + "%" : "0%"
        }).ToList();

        var errorRate = totalSpans > 0 ? ((double)errorSpans / totalSpans * 100) : 0;

        return new ReportPreviewDto
        {
            ReportType = "Performance",
            Title = "Service Performance & Latency Report",
            Description = "Throughput, response times, and error rate breakdowns by monitored service.",
            StartUtc = start,
            EndUtc = end,
            SummaryMetrics = new Dictionary<string, object>
            {
                { "Total Requests", totalSpans },
                { "Error Rate", $"{errorRate:F2}%" },
                { "Avg Latency (ms)", $"{avgDuration:F2}" },
                { "Active Services", grouped.Count }
            },
            ColumnHeaders = ["Service Name", "Total Spans", "Avg Latency (ms)", "Max Latency (ms)", "Errors", "Error Rate"],
            Rows = rows
        };
    }

    private async Task<ReportPreviewDto> GenerateErrorsReportAsync(
        GenerateReportRequest req, DateTime start, DateTime end, CancellationToken ct)
    {
        var query = _context.LogEntries.AsNoTracking()
            .Where(l => l.Timestamp >= start && l.Timestamp <= end && (l.Severity == Domain.Enums.Severity.Error || l.Severity == Domain.Enums.Severity.Critical));

        if (req.ApplicationId.HasValue)
        {
            query = query.Where(l => l.ApplicationId == req.ApplicationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(req.ServiceName))
        {
            query = query.Where(l => l.ServiceName == req.ServiceName);
        }

        var totalErrors = await query.CountAsync(ct);

        var grouped = await query
            .GroupBy(l => new { l.ServiceName, l.EnvironmentName, l.Severity, l.Message })
            .Select(g => new
            {
                g.Key.ServiceName,
                g.Key.EnvironmentName,
                Severity = g.Key.Severity.ToString(),
                Message = g.Key.Message.Length > 120 ? g.Key.Message.Substring(0, 120) + "..." : g.Key.Message,
                Count = g.Count(),
                LastSeenUtc = g.Max(l => l.Timestamp)
            })
            .OrderByDescending(g => g.Count)
            .Take(100)
            .ToListAsync(ct);

        var rows = grouped.Select(g => new List<string>
        {
            g.ServiceName,
            g.EnvironmentName,
            g.Severity,
            g.Message,
            g.Count.ToString(CultureInfo.InvariantCulture),
            g.LastSeenUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
        }).ToList();

        return new ReportPreviewDto
        {
            ReportType = "ErrorsAndExceptions",
            Title = "Errors & Exceptions Analysis Report",
            Description = "Top exception patterns, frequency, severity, and last occurrence by service.",
            StartUtc = start,
            EndUtc = end,
            SummaryMetrics = new Dictionary<string, object>
            {
                { "Total Errors", totalErrors },
                { "Distinct Patterns", grouped.Count },
                { "Top Affected Service", grouped.FirstOrDefault()?.ServiceName ?? "None" }
            },
            ColumnHeaders = ["Service", "Environment", "Severity", "Message Pattern", "Count", "Last Seen (UTC)"],
            Rows = rows
        };
    }

    private async Task<ReportPreviewDto> GenerateIncidentsReportAsync(
        GenerateReportRequest req, DateTime start, DateTime end, CancellationToken ct)
    {
        var query = _context.Incidents.AsNoTracking().Where(i => i.CreatedAtUtc >= start && i.CreatedAtUtc <= end);

        if (req.ApplicationId.HasValue)
        {
            query = query.Where(i => i.ApplicationId == req.ApplicationId.Value);
        }

        var totalIncidents = await query.CountAsync(ct);
        var resolvedIncidents = await query.CountAsync(i => i.ResolvedAtUtc != null, ct);

        var incidents = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Take(100)
            .ToListAsync(ct);

        var durations = incidents.Where(i => i.ResolvedAtUtc != null).Select(i => (i.ResolvedAtUtc!.Value - i.CreatedAtUtc).TotalMinutes).ToList();
        var mttrMinutes = durations.Count > 0 ? durations.Average() : 0;

        var ackDurations = incidents.Where(i => i.AcknowledgedAtUtc != null).Select(i => (i.AcknowledgedAtUtc!.Value - i.CreatedAtUtc).TotalMinutes).ToList();
        var mttaMinutes = ackDurations.Count > 0 ? ackDurations.Average() : 0;

        var rows = incidents.Select(i => new List<string>
        {
            i.IncidentNumber.ToString(CultureInfo.InvariantCulture),
            i.Title,
            i.Severity.ToString(),
            i.Status.ToString(),
            i.ServiceName ?? "General",
            i.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            i.AcknowledgedAtUtc?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "N/A",
            i.ResolvedAtUtc?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "Active",
            i.ResolvedAtUtc != null ? (i.ResolvedAtUtc.Value - i.CreatedAtUtc).TotalMinutes.ToString("F1", CultureInfo.InvariantCulture) + "m" : "Pending"
        }).ToList();

        return new ReportPreviewDto
        {
            ReportType = "IncidentHistory",
            Title = "Incident History & Reliability Report",
            Description = "Chronological record of operational incidents, MTT Acknowledge and Resolve metrics.",
            StartUtc = start,
            EndUtc = end,
            SummaryMetrics = new Dictionary<string, object>
            {
                { "Total Incidents", totalIncidents },
                { "Resolved", resolvedIncidents },
                { "MTTA (mins)", $"{mttaMinutes:F1}" },
                { "MTTR (mins)", $"{mttrMinutes:F1}" }
            },
            ColumnHeaders = ["#", "Title", "Severity", "Status", "Service", "Created (UTC)", "Acked (UTC)", "Resolved (UTC)", "Resolution Time"],
            Rows = rows
        };
    }

    private async Task<ReportPreviewDto> GenerateAlertActivityReportAsync(
        GenerateReportRequest req, DateTime start, DateTime end, CancellationToken ct)
    {
        var rulesQuery = _context.AlertRules.AsNoTracking();
        if (req.ApplicationId.HasValue) rulesQuery = rulesQuery.Where(r => r.ApplicationId == req.ApplicationId.Value);

        var totalRules = await rulesQuery.CountAsync(ct);
        var activeRules = await rulesQuery.CountAsync(r => r.IsEnabled, ct);

        var evaluationsQuery = _context.AlertEvaluations.AsNoTracking().Where(e => e.EvaluatedAtUtc >= start && e.EvaluatedAtUtc <= end);
        var totalEvals = await evaluationsQuery.CountAsync(ct);
        var totalBreaches = await evaluationsQuery.CountAsync(e => e.IsTriggered, ct);

        var rules = await rulesQuery.Take(100).ToListAsync(ct);

        var rows = rules.Select(r => new List<string>
        {
            r.Name,
            r.MetricName,
            r.ConditionType.ToString(),
            r.Threshold.ToString(CultureInfo.InvariantCulture),
            r.Status.ToString(),
            r.IsEnabled ? "Yes" : "No",
            r.LastFiredAtUtc?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "Never"
        }).ToList();

        return new ReportPreviewDto
        {
            ReportType = "AlertActivity",
            Title = "Alert Rule Activity & Breaches Report",
            Description = "Summary of configured alert rules, evaluation health, and trigger status.",
            StartUtc = start,
            EndUtc = end,
            SummaryMetrics = new Dictionary<string, object>
            {
                { "Configured Rules", totalRules },
                { "Active Rules", activeRules },
                { "Total Evaluations", totalEvals },
                { "Threshold Breaches", totalBreaches }
            },
            ColumnHeaders = ["Rule Name", "Metric", "Condition", "Threshold", "Status", "Enabled", "Last Fired (UTC)"],
            Rows = rows
        };
    }

    private async Task<ReportPreviewDto> GenerateSloReportAsync(
        GenerateReportRequest req, DateTime start, DateTime end, CancellationToken ct)
    {
        var sloQuery = _context.ServiceLevelObjectives.AsNoTracking();
        if (req.ApplicationId.HasValue) sloQuery = sloQuery.Where(s => s.ApplicationId == req.ApplicationId.Value);

        var slos = await sloQuery.Take(100).ToListAsync(ct);

        var totalSlos = slos.Count;
        var compliantSlos = slos.Count(s => !s.IsBreached);

        var rows = slos.Select(s => new List<string>
        {
            s.Name,
            s.ServiceName,
            s.TargetPercentage.ToString("F2", CultureInfo.InvariantCulture) + "%",
            (s.CurrentCompliancePercentage ?? 100.0).ToString("F2", CultureInfo.InvariantCulture) + "%",
            (s.CurrentErrorBudgetRemaining ?? 100.0).ToString("F2", CultureInfo.InvariantCulture) + "%",
            (s.BurnRate ?? 0.0).ToString("F2", CultureInfo.InvariantCulture),
            !s.IsBreached ? "COMPLIANT" : "BREACHED"
        }).ToList();

        return new ReportPreviewDto
        {
            ReportType = "SloCompliance",
            Title = "SLO Compliance & Error Budget Report",
            Description = "Compliance health against Service Level Objectives and consumption of error budgets.",
            StartUtc = start,
            EndUtc = end,
            SummaryMetrics = new Dictionary<string, object>
            {
                { "Total SLOs", totalSlos },
                { "Compliant SLOs", compliantSlos },
                { "Breached SLOs", totalSlos - compliantSlos },
                { "Compliance Rate", totalSlos > 0 ? $"{(double)compliantSlos / totalSlos * 100:F1}%" : "100%" }
            },
            ColumnHeaders = ["SLO Name", "Service", "Target %", "Current SLI %", "Remaining Budget %", "Burn Rate", "Compliance"],
            Rows = rows
        };
    }

    private async Task<ReportPreviewDto> GenerateHealthReportAsync(
        GenerateReportRequest req, DateTime start, DateTime end, CancellationToken ct)
    {
        var servicesQuery = _context.Services.AsNoTracking().Include(s => s.Application);
        var services = await servicesQuery.Take(100).ToListAsync(ct);

        var healthChecks = await _context.HealthCheckResults.AsNoTracking()
            .Where(h => h.CheckedAtUtc >= start && h.CheckedAtUtc <= end)
            .GroupBy(h => h.ServiceName)
            .Select(g => new
            {
                ServiceName = g.Key,
                LatestStatus = g.OrderByDescending(x => x.CheckedAtUtc).First().Status.ToString(),
                AvgLatencyMs = g.Average(x => x.ResponseTimeMs),
                LastCheckUtc = g.Max(x => x.CheckedAtUtc)
            })
            .ToListAsync(ct);

        var healthMap = healthChecks.ToDictionary(h => h.ServiceName);

        var rows = services.Select(s =>
        {
            healthMap.TryGetValue(s.Name, out var hc);
            return new List<string>
            {
                s.Name,
                s.Application != null && !string.IsNullOrEmpty(s.Application.DisplayName) ? s.Application.DisplayName : (s.Application?.Name ?? "App"),
                s.IsActive ? "Active" : "Inactive",
                hc?.LatestStatus ?? "Healthy",
                hc != null ? $"{hc.AvgLatencyMs:F1}ms" : "N/A",
                hc?.LastCheckUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "N/A"
            };
        }).ToList();

        return new ReportPreviewDto
        {
            ReportType = "ApplicationHealth",
            Title = "Application & Service Health Report",
            Description = "Overview of service registrations, latest health check statuses, and responsiveness.",
            StartUtc = start,
            EndUtc = end,
            SummaryMetrics = new Dictionary<string, object>
            {
                { "Registered Services", services.Count },
                { "Healthy Services", healthChecks.Count(h => h.LatestStatus == "Healthy") },
                { "Degraded/Unhealthy", healthChecks.Count(h => h.LatestStatus != "Healthy") }
            },
            ColumnHeaders = ["Service Name", "Application", "Registration Status", "Health Status", "Avg Latency", "Last Check (UTC)"],
            Rows = rows
        };
    }

    private async Task<ReportPreviewDto> GenerateExecutiveSummaryReportAsync(
        GenerateReportRequest req, DateTime start, DateTime end, CancellationToken ct)
    {
        var logsQuery = _context.LogEntries.AsNoTracking().Where(l => l.Timestamp >= start && l.Timestamp <= end);
        var tracesQuery = _context.TraceRecords.AsNoTracking().Where(t => t.StartedAtUtc >= start && t.StartedAtUtc <= end);
        var slosQuery = _context.ServiceLevelObjectives.AsNoTracking();
        var incidentsQuery = _context.AlertIncidents.AsNoTracking().Where(i => i.FiredAtUtc >= start && i.FiredAtUtc <= end);

        if (req.ApplicationId.HasValue)
        {
            logsQuery = logsQuery.Where(l => l.ApplicationId == req.ApplicationId.Value);
            tracesQuery = tracesQuery.Where(t => t.ApplicationId == req.ApplicationId.Value);
            slosQuery = slosQuery.Where(s => s.ApplicationId == req.ApplicationId.Value);
        }

        var totalLogs = await logsQuery.CountAsync(ct);
        var errorLogs = await logsQuery.CountAsync(l => l.Severity == Severity.Error || l.Severity == Severity.Critical, ct);
        var totalTraces = await tracesQuery.CountAsync(ct);
        var avgDurationMs = totalTraces > 0 ? (await tracesQuery.AverageAsync(t => t.Duration.TotalMilliseconds, ct)) : 0.0;

        var totalSlos = await slosQuery.CountAsync(ct);
        var compliantSlos = await slosQuery.CountAsync(s => !s.IsBreached, ct);
        var openIncidents = await incidentsQuery.CountAsync(i => i.Status == AlertStatus.Firing || i.Status == AlertStatus.Acknowledged, ct);

        double errorRate = totalLogs > 0 ? ((double)errorLogs / totalLogs * 100.0) : 0.0;
        double sloRate = totalSlos > 0 ? ((double)compliantSlos / totalSlos * 100.0) : 100.0;

        var services = await _context.Services.AsNoTracking().Include(s => s.Application).Take(50).ToListAsync(ct);
        var rows = new List<List<string>>();

        foreach (var svc in services)
        {
            var svcLogs = await logsQuery.CountAsync(l => l.ServiceName == svc.Name, ct);
            var svcErrors = await logsQuery.CountAsync(l => l.ServiceName == svc.Name && (l.Severity == Severity.Error || l.Severity == Severity.Critical), ct);
            var svcRate = svcLogs > 0 ? $"{(double)svcErrors / svcLogs * 100.0:F2}%" : "0.00%";
            var appName = svc.Application != null && !string.IsNullOrEmpty(svc.Application.DisplayName) ? svc.Application.DisplayName : (svc.Application?.Name ?? "App");

            rows.Add([
                svc.Name,
                appName,
                svc.IsActive ? "Online" : "Offline",
                $"{svcLogs:N0}",
                svcRate,
                $"{avgDurationMs:F1}ms",
                svcErrors == 0 ? "Compliant" : "Degraded"
            ]);
        }

        return new ReportPreviewDto
        {
            ReportType = "ExecutiveSummary",
            Title = "Executive Summary & SLO Compliance Report",
            Description = "High-level operational overview including SLO health, active incidents, error rates, and throughput.",
            StartUtc = start,
            EndUtc = end,
            SummaryMetrics = new Dictionary<string, object>
            {
                { "SLO Compliance", $"{sloRate:F1}%" },
                { "Open Incidents", openIncidents },
                { "Error Rate", $"{errorRate:F2}%" },
                { "Total Telemetry", $"{totalLogs + totalTraces:N0}" }
            },
            ColumnHeaders = ["Service Name", "Application", "Status", "Request Volume", "Error Rate", "Avg Latency", "SLO Health"],
            Rows = rows
        };
    }

    private async Task<ReportPreviewDto> GenerateSecurityAuditReportAsync(
        GenerateReportRequest req, DateTime start, DateTime end, CancellationToken ct)
    {
        var auditQuery = _context.AuditEvents.AsNoTracking()
            .Where(a => a.Timestamp >= start && a.Timestamp <= end);

        var totalEvents = await auditQuery.CountAsync(ct);
        var failedLogins = await auditQuery.CountAsync(a => a.Action.Contains("Failed"), ct);
        var adminActions = await auditQuery.CountAsync(a => a.Action.StartsWith("User") || a.Action.StartsWith("Role") || a.Action.StartsWith("Application"), ct);
        var activeKeys = await _context.ApiKeys.AsNoTracking().CountAsync(k => k.RevokedAtUtc == null, ct);

        var recentEvents = await auditQuery
            .OrderByDescending(a => a.Timestamp)
            .Take(100)
            .ToListAsync(ct);

        var rows = recentEvents.Select(a => new List<string>
        {
            a.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            a.UserName ?? "system",
            a.Action,
            a.EntityType ?? "General",
            a.EntityId ?? "-",
            a.IpAddress ?? "internal",
            a.Action.Contains("Failed") ? "FAILED" : "SUCCESS"
        }).ToList();

        return new ReportPreviewDto
        {
            ReportType = "SecurityAudit",
            Title = "Enterprise Security & Audit Compliance Report",
            Description = "Detailed trail of authentication attempts, administrative mutations, and API key lifecycles.",
            StartUtc = start,
            EndUtc = end,
            SummaryMetrics = new Dictionary<string, object>
            {
                { "Total Audit Events", totalEvents },
                { "Failed Logins", failedLogins },
                { "Admin Mutations", adminActions },
                { "Active API Keys", activeKeys }
            },
            ColumnHeaders = ["Timestamp (UTC)", "Actor", "Action", "Target Type", "Entity ID", "IP Address", "Outcome"],
            Rows = rows
        };
    }

    private async Task<ReportPreviewDto> GenerateTelemetryVolumeReportAsync(
        GenerateReportRequest req, DateTime start, DateTime end, CancellationToken ct)
    {
        var logsQuery = _context.LogEntries.AsNoTracking().Where(l => l.Timestamp >= start && l.Timestamp <= end);
        var tracesQuery = _context.TraceRecords.AsNoTracking().Where(t => t.StartedAtUtc >= start && t.StartedAtUtc <= end);
        var metricsQuery = _context.MetricRecords.AsNoTracking().Where(m => m.Timestamp >= start && m.Timestamp <= end);

        var totalLogs = await logsQuery.CountAsync(ct);
        var totalTraces = await tracesQuery.CountAsync(ct);
        var totalMetrics = await metricsQuery.CountAsync(ct);

        var services = await _context.Services.AsNoTracking().Include(s => s.Application).Take(50).ToListAsync(ct);
        var rows = new List<List<string>>();

        foreach (var svc in services)
        {
            var svcLogs = await logsQuery.CountAsync(l => l.ServiceName == svc.Name, ct);
            var svcTraces = await tracesQuery.CountAsync(l => l.ServiceName == svc.Name, ct);
            var svcMetrics = await metricsQuery.CountAsync(l => l.ServiceName == svc.Name, ct);

            double estKb = (svcLogs * 1.0) + (svcTraces * 2.0) + (svcMetrics * 0.5);
            string estStr = estKb > 1024 ? $"{estKb / 1024.0:F2} MB" : $"{estKb:F0} KB";

            rows.Add([
                svc.Name,
                svc.Application?.DisplayName ?? svc.Application?.Name ?? "App",
                $"{svcLogs:N0}",
                $"{svcTraces:N0}",
                $"{svcMetrics:N0}",
                estStr,
                "Standard (30d)"
            ]);
        }

        double totalMb = ((totalLogs * 1.0) + (totalTraces * 2.0) + (totalMetrics * 0.5)) / 1024.0;

        return new ReportPreviewDto
        {
            ReportType = "TelemetryVolume",
            Title = "Cost & Telemetry Volume Ingestion Report",
            Description = "Ingestion statistics, span counts, log volumes, metric frequencies, and projected storage footprint.",
            StartUtc = start,
            EndUtc = end,
            SummaryMetrics = new Dictionary<string, object>
            {
                { "Total Logs", totalLogs },
                { "Total Traces", totalTraces },
                { "Total Metrics", totalMetrics },
                { "Est. Ingestion", $"{totalMb:F1} MB" }
            },
            ColumnHeaders = ["Service Name", "Application", "Log Records", "Span Count", "Metric Points", "Est. Volume", "Retention Tier"],
            Rows = rows
        };
    }
}
