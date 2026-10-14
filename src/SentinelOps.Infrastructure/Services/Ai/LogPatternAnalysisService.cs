using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Infrastructure.Services.Ai;

public class LogPatternAnalysisService : ILogPatternAnalysisService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<LogPatternAnalysisService> _logger;

    // Regex normalizers for variable masking
    private static readonly Regex GuidRegex = new(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b", RegexOptions.Compiled);
    private static readonly Regex IpRegex = new(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(:\d+)?\b", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"\b[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+\b", RegexOptions.Compiled);
    private static readonly Regex UrlRegex = new(@"https?://[^\s""'>]+", RegexOptions.Compiled);
    private static readonly Regex HexAddressRegex = new(@"\b0x[0-9a-fA-F]+\b", RegexOptions.Compiled);
    private static readonly Regex DurationRegex = new(@"\b\d+(\.\d+)?\s*(ms|s|m|us|ns|sec|seconds|milliseconds)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NumberRegex = new(@"\b\d+\b", RegexOptions.Compiled);
    private static readonly Regex FilePathRegex = new(@"([A-Za-z]:\\[^\s:]+|(/[a-zA-Z0-9_.-]+){2,})", RegexOptions.Compiled);
    private static readonly Regex HtmlTagRegex = new(@"<[^>]*>", RegexOptions.Compiled);

    public LogPatternAnalysisService(
        IApplicationDbContext context,
        ICurrentTenantService currentTenantService,
        ILogger<LogPatternAnalysisService> logger)
    {
        _context = context;
        _currentTenantService = currentTenantService;
        _logger = logger;
    }

    public LogPatternExtractionResult ExtractPattern(string rawMessage, string? exceptionDetail = null)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            return new LogPatternExtractionResult
            {
                PatternFingerprint = "empty_message",
                Template = "[EMPTY_LOG_MESSAGE]",
                ExtractedParameters = []
            };
        }

        // Prevent XSS: strip raw HTML tags
        var sanitized = HtmlTagRegex.Replace(rawMessage, "");
        var extractedParams = new List<string>();

        // Apply masks in strict sequence (longest/most specific patterns first)
        var template = sanitized;

        // 1. URLs
        template = UrlRegex.Replace(template, m => { extractedParams.Add(m.Value); return "{url}"; });

        // 2. Emails
        template = EmailRegex.Replace(template, m => { extractedParams.Add(m.Value); return "{email}"; });

        // 3. GUIDs
        template = GuidRegex.Replace(template, m => { extractedParams.Add(m.Value); return "{guid}"; });

        // 4. IP addresses
        template = IpRegex.Replace(template, m => { extractedParams.Add(m.Value); return "{ip}"; });

        // 5. Durations
        template = DurationRegex.Replace(template, m => { extractedParams.Add(m.Value); return "{duration}"; });

        // 6. Hex pointers
        template = HexAddressRegex.Replace(template, m => { extractedParams.Add(m.Value); return "{hex}"; });

        // 7. File paths
        template = FilePathRegex.Replace(template, m => { extractedParams.Add(m.Value); return "{path}"; });

        // 8. Standalone numbers / IDs
        template = NumberRegex.Replace(template, m => { extractedParams.Add(m.Value); return "{number}"; });

        // Collapse excess whitespace
        template = Regex.Replace(template, @"\s+", " ").Trim();

        // If exception detail exists, append the exception type to make the fingerprint distinct
        if (!string.IsNullOrWhiteSpace(exceptionDetail))
        {
            var firstLine = exceptionDetail.Split('\n')[0].Trim();
            var exTypeMatch = Regex.Match(firstLine, @"^([a-zA-Z0-9_\.]+(?:Exception|Error))");
            if (exTypeMatch.Success)
            {
                template = $"[{exTypeMatch.Value}] {template}";
            }
        }

        // Compute deterministic SHA-256 fingerprint
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(template));
        var fingerprint = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return new LogPatternExtractionResult
        {
            PatternFingerprint = fingerprint,
            Template = WebUtility.HtmlEncode(template), // Safe for display
            ExtractedParameters = extractedParams
        };
    }

    public async Task<LogPatternAnalysisResult> AnalyzeApplicationLogsAsync(Guid applicationId, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var orgId = _currentTenantService.OrganizationId;
        if (!orgId.HasValue && applicationId != Guid.Empty)
        {
            orgId = await _context.Applications
                .Where(a => a.Id == applicationId)
                .Select(a => a.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken);
        }
        var cutoff = DateTime.UtcNow - window;

        var query = _context.LogEntries.AsNoTracking()
            .Where(l => l.ApplicationId == applicationId && l.Timestamp >= cutoff);

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Take(1000)
            .ToListAsync(cancellationToken);

        if (logs.Count == 0)
        {
            return new LogPatternAnalysisResult
            {
                TotalLogsAnalyzed = 0,
                PatternsFound = 0,
                Patterns = []
            };
        }

        // Cluster logs in-memory by pattern fingerprint
        var patternGroups = new Dictionary<string, (LogPatternExtractionResult Extraction, List<LogEntry> Matches)>();

        foreach (var log in logs)
        {
            var extraction = ExtractPattern(log.Message, log.ExceptionDetail);
            if (!patternGroups.TryGetValue(extraction.PatternFingerprint, out var entry))
            {
                entry = (extraction, new List<LogEntry>());
                patternGroups[extraction.PatternFingerprint] = entry;
            }
            entry.Matches.Add(log);
        }

        var results = new List<LogPatternDto>();

        foreach (var (fingerprint, (extraction, matchedLogs)) in patternGroups)
        {
            var earliest = matchedLogs.Min(l => l.Timestamp);
            var latest = matchedLogs.Max(l => l.Timestamp);
            var topSeverity = matchedLogs.Max(l => l.Severity);
            var sampleLog = matchedLogs.First();

            var dto = new LogPatternDto
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId ?? Guid.Empty,
                ApplicationId = applicationId,
                ServiceName = sampleLog.ServiceName,
                PatternFingerprint = fingerprint,
                Template = extraction.Template,
                Severity = topSeverity.ToString(),
                TotalOccurrences = matchedLogs.Count,
                FirstSeenAtUtc = earliest,
                LastSeenAtUtc = latest,
                SampleMessage = WebUtility.HtmlEncode(sampleLog.Message),
                SampleTraceId = matchedLogs.FirstOrDefault(l => !string.IsNullOrEmpty(l.TraceId))?.TraceId
            };

            results.Add(dto);

            // Persist or update cluster in database if organization context is present
            if (orgId.HasValue && orgId.Value != Guid.Empty)
            {
                var existingPattern = await _context.LogPatterns
                    .FirstOrDefaultAsync(p => p.OrganizationId == orgId.Value &&
                                              p.ApplicationId == applicationId &&
                                              p.PatternFingerprint == fingerprint, cancellationToken);

                if (existingPattern != null)
                {
                    existingPattern.RecordOccurrence(latest, dto.SampleTraceId);
                }
                else
                {
                    var newEntity = new LogPattern(
                        orgId.Value,
                        applicationId,
                        sampleLog.ServiceName,
                        fingerprint,
                        extraction.Template,
                        topSeverity,
                        dto.SampleMessage,
                        dto.SampleTraceId);
                    newEntity.TotalOccurrences = matchedLogs.Count;
                    newEntity.FirstSeenAtUtc = earliest;
                    newEntity.LastSeenAtUtc = latest;
                    _context.LogPatterns.Add(newEntity);
                }
            }
        }

        if (orgId.HasValue && orgId.Value != Guid.Empty)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new LogPatternAnalysisResult
        {
            TotalLogsAnalyzed = logs.Count,
            PatternsFound = results.Count,
            Patterns = results.OrderByDescending(p => p.TotalOccurrences).ToList()
        };
    }

    public async Task<IReadOnlyList<LogPatternDto>> GetPatternsAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var orgId = _currentTenantService.OrganizationId;
        if (!orgId.HasValue && applicationId != Guid.Empty)
        {
            orgId = await _context.Applications
                .Where(a => a.Id == applicationId)
                .Select(a => a.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var query = _context.LogPatterns.AsNoTracking();

        if (orgId.HasValue && orgId.Value != Guid.Empty)
        {
            query = query.Where(p => p.OrganizationId == orgId.Value);
        }

        if (applicationId != Guid.Empty)
        {
            query = query.Where(p => p.ApplicationId == applicationId);
        }

        var patterns = await query
            .OrderByDescending(p => p.TotalOccurrences)
            .Take(100)
            .ToListAsync(cancellationToken);

        return patterns.Select(p => new LogPatternDto
        {
            Id = p.Id,
            OrganizationId = p.OrganizationId,
            ApplicationId = p.ApplicationId,
            ServiceName = p.ServiceName,
            PatternFingerprint = p.PatternFingerprint,
            Template = p.Template,
            Severity = p.Severity.ToString(),
            TotalOccurrences = p.TotalOccurrences,
            FirstSeenAtUtc = p.FirstSeenAtUtc,
            LastSeenAtUtc = p.LastSeenAtUtc,
            SampleMessage = p.SampleMessage,
            SampleTraceId = p.SampleTraceId
        }).ToList();
    }
}
