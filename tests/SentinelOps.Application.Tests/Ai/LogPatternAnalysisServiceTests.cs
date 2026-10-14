using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using SentinelOps.Infrastructure.Persistence;
using SentinelOps.Infrastructure.Services.Ai;
using Xunit;

namespace SentinelOps.Application.Tests.Ai;

public class LogPatternAnalysisServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly LogPatternAnalysisService _service;

    public LogPatternAnalysisServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _currentTenantService = Substitute.For<ICurrentTenantService>();
        _service = new LogPatternAnalysisService(_context, _currentTenantService, NullLogger<LogPatternAnalysisService>.Instance);
    }

    [Fact]
    public void ExtractPattern_MasksDynamicVariables_ProducesIdenticalFingerprintsForSameStructure()
    {
        var log1 = "Database timeout after 1200ms for user 12345 on host 10.0.1.42:5432";
        var log2 = "Database timeout after 2450ms for user 98765 on host 192.168.1.100:5432";

        var extraction1 = _service.ExtractPattern(log1);
        var extraction2 = _service.ExtractPattern(log2);

        extraction1.Template.Should().Be("Database timeout after {duration} for user {number} on host {ip}");
        extraction2.Template.Should().Be("Database timeout after {duration} for user {number} on host {ip}");

        extraction1.PatternFingerprint.Should().Be(extraction2.PatternFingerprint);
        extraction1.PatternFingerprint.Should().HaveLength(64); // SHA-256 hex string
    }

    [Fact]
    public void ExtractPattern_MasksEmailsAndUrlsSafely()
    {
        var raw = "Verification email sent to john.doe@example.org from https://auth.sentinelops.io/verify?code=abc";
        var extraction = _service.ExtractPattern(raw);

        extraction.Template.Should().Be("Verification email sent to {email} from {url}");
        extraction.ExtractedParameters.Should().Contain("john.doe@example.org");
        extraction.ExtractedParameters.Should().Contain("https://auth.sentinelops.io/verify?code=abc");
    }

    [Fact]
    public void ExtractPattern_StripsDangerousHtml_ToPreventStoredXss()
    {
        var maliciousLog = "Error handling input: <script>alert('xss');</script><img src=x onerror=alert(1)> from 127.0.0.1";
        var extraction = _service.ExtractPattern(maliciousLog);

        extraction.Template.Should().NotContain("<script>");
        extraction.Template.Should().NotContain("<img");
        extraction.Template.Should().NotContain("onerror");
        extraction.Template.Should().Contain("{ip}");
    }

    [Fact]
    public void ExtractPattern_WithExceptionDetail_IncludesExceptionTypeInTemplate()
    {
        var message = "Failed to connect to primary replica";
        var stackTrace = "Npgsql.NpgsqlException: Connection refused\n   at Npgsql.Connector.Connect()";

        var extraction = _service.ExtractPattern(message, stackTrace);

        extraction.Template.Should().Contain("[Npgsql.NpgsqlException]");
        extraction.Template.Should().Contain("Failed to connect to primary replica");
    }

    [Fact]
    public async Task AnalyzeApplicationLogsAsync_ClustersLogsAndTracksFrequencies()
    {
        var orgId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        _currentTenantService.OrganizationId.Returns(orgId);

        var now = DateTime.UtcNow;
        var logs = new List<LogEntry>
        {
            new() { ApplicationId = appId, ServiceName = "orders", EnvironmentName = "prod", Severity = Severity.Error, Message = "Timeout connecting to redis:6379 after 500ms", Timestamp = now.AddMinutes(-10), TraceId = "tr-1" },
            new() { ApplicationId = appId, ServiceName = "orders", EnvironmentName = "prod", Severity = Severity.Error, Message = "Timeout connecting to redis:6379 after 800ms", Timestamp = now.AddMinutes(-5), TraceId = "tr-2" },
            new() { ApplicationId = appId, ServiceName = "orders", EnvironmentName = "prod", Severity = Severity.Error, Message = "Timeout connecting to redis:6379 after 1200ms", Timestamp = now.AddMinutes(-1), TraceId = "tr-3" },
            new() { ApplicationId = appId, ServiceName = "orders", EnvironmentName = "prod", Severity = Severity.Information, Message = "Processed order 1001 successfully", Timestamp = now.AddMinutes(-2), TraceId = "tr-4" },
            new() { ApplicationId = appId, ServiceName = "orders", EnvironmentName = "prod", Severity = Severity.Information, Message = "Processed order 1002 successfully", Timestamp = now.AddMinutes(-1), TraceId = "tr-5" }
        };

        _context.LogEntries.AddRange(logs);
        await _context.SaveChangesAsync();

        var result = await _service.AnalyzeApplicationLogsAsync(appId, TimeSpan.FromHours(1));

        result.TotalLogsAnalyzed.Should().Be(5);
        result.PatternsFound.Should().Be(2);

        var timeoutPattern = result.Patterns.FirstOrDefault(p => p.Template.Contains("Timeout connecting to redis:{number} after {duration}"));
        timeoutPattern.Should().NotBeNull();
        timeoutPattern!.TotalOccurrences.Should().Be(3);
        timeoutPattern.Severity.Should().Be("Error");
        timeoutPattern.SampleTraceId.Should().NotBeNullOrEmpty();

        var successPattern = result.Patterns.FirstOrDefault(p => p.Template.Contains("Processed order {number} successfully"));
        successPattern.Should().NotBeNull();
        successPattern!.TotalOccurrences.Should().Be(2);
    }

    [Fact]
    public async Task GetPatternsAsync_StrictlyEnforcesTenantIsolation()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var appId = Guid.NewGuid();

        var patternA = new LogPattern(tenantA, appId, "api", "fp-a", "Template A {number}", Severity.Warning, "Sample A");
        var patternB = new LogPattern(tenantB, appId, "api", "fp-b", "Template B {number}", Severity.Error, "Sample B");

        _context.LogPatterns.AddRange(patternA, patternB);
        await _context.SaveChangesAsync();

        // Scope to tenant A
        _currentTenantService.OrganizationId.Returns(tenantA);
        var tenantAList = await _service.GetPatternsAsync(appId);
        tenantAList.Should().HaveCount(1);
        tenantAList[0].Template.Should().Be("Template A {number}");

        // Scope to tenant B
        _currentTenantService.OrganizationId.Returns(tenantB);
        var tenantBList = await _service.GetPatternsAsync(appId);
        tenantBList.Should().HaveCount(1);
        tenantBList[0].Template.Should().Be("Template B {number}");
    }
}
