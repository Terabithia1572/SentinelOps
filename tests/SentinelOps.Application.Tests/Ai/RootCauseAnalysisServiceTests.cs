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

public class RootCauseAnalysisServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly RootCauseAnalysisService _service;

    public RootCauseAnalysisServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _currentTenantService = Substitute.For<ICurrentTenantService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _service = new RootCauseAnalysisService(_context, _currentTenantService, _currentUserService, NullLogger<RootCauseAnalysisService>.Instance);
    }

    [Fact]
    public async Task PerformRcaAsync_WhenDatabaseLogsPresent_GeneratesLikelyCauseHypothesisWithCitations()
    {
        var orgId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        _currentTenantService.OrganizationId.Returns(orgId);

        var now = DateTime.UtcNow;
        var incident = new Incident
        {
            ApplicationId = appId,
            Title = "High Error Rate in Order Processing",
            Description = "Order creation failures",
            Severity = Severity.Critical,
            CreatedAtUtc = now
        };
        _context.Incidents.Add(incident);

        // Add database timeout error logs
        _context.LogEntries.AddRange(
            new LogEntry { ApplicationId = appId, ServiceName = "orders", EnvironmentName = "prod", Severity = Severity.Error, Message = "Npgsql.NpgsqlException: Connection pool timeout after 15000ms", Timestamp = now.AddMinutes(-2), TraceId = "trace-db-001" },
            new LogEntry { ApplicationId = appId, ServiceName = "orders", EnvironmentName = "prod", Severity = Severity.Error, Message = "Npgsql.NpgsqlException: Connection pool timeout after 15000ms", Timestamp = now.AddMinutes(-1), TraceId = "trace-db-002" }
        );

        await _context.SaveChangesAsync();

        var result = await _service.PerformRcaAsync(incident.Id);

        result.Should().NotBeNull();
        result.IncidentId.Should().Be(incident.Id);
        result.IsDeterministic.Should().BeTrue();
        result.Hypotheses.Should().NotBeEmpty();

        var dbHypothesis = result.Hypotheses.FirstOrDefault(h => h.Hypothesis.Contains("Database Connection"));
        dbHypothesis.Should().NotBeNull();
        dbHypothesis!.ConfidenceLevel.Should().BeOneOf("Likely cause", "Possible cause");
        dbHypothesis.ConfidenceScore.Should().BeGreaterThanOrEqualTo(0.60);
        dbHypothesis.SupportingEvidence.Should().Contain(e => e.Contains("trace-db-001") || e.Contains("Npgsql"));
        dbHypothesis.ContradictoryEvidence.Should().NotBeEmpty();
        dbHypothesis.RecommendedSteps.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PerformRcaAsync_WhenSparseTelemetry_ReturnsInsufficientEvidenceHypothesis()
    {
        var orgId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        _currentTenantService.OrganizationId.Returns(orgId);

        var incident = new Incident
        {
            ApplicationId = appId,
            Title = "Alert Fired Without Logs",
            Description = "Transient probe glitch",
            Severity = Severity.Warning,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Incidents.Add(incident);
        await _context.SaveChangesAsync();

        var result = await _service.PerformRcaAsync(incident.Id);

        result.Hypotheses.Should().HaveCount(1);
        var fallback = result.Hypotheses[0];
        fallback.ConfidenceLevel.Should().Be("Insufficient evidence");
        fallback.ConfidenceScore.Should().Be(0.20);
        fallback.SupportingEvidence.Should().Contain(e => e.Contains("no correlated error logs or traces"));
    }

    [Fact]
    public async Task VerifyHypothesisAsync_RecordsOperatorAttributionAndUpdatesVerification()
    {
        var orgId = Guid.NewGuid();
        var incidentId = Guid.NewGuid();
        _currentUserService.UserName.Returns("lead-sre-alice");

        var rca = new IncidentRca(
            orgId,
            incidentId,
            "Initial RCA summary",
            "[\"orders\"]",
            "[{\"Hypothesis\":\"Database Pool Exhaustion\",\"ConfidenceLevel\":\"Likely cause\",\"ConfidenceScore\":0.85,\"SupportingEvidence\":[],\"ContradictoryEvidence\":[],\"RecommendedSteps\":[],\"IsVerifiedByOperator\":false}]",
            "[]",
            "[]",
            "Deterministic Heuristic Engine",
            true);

        _context.IncidentRcas.Add(rca);
        await _context.SaveChangesAsync();

        var success = await _service.VerifyHypothesisAsync(rca.Id, "Database Pool Exhaustion", isConfirmed: true, operatorNotes: "Verified via pg_stat_activity connection saturation.");

        success.Should().BeTrue();

        var updated = await _context.IncidentRcas.FindAsync(rca.Id);
        updated!.OperatorVerificationStatus.Should().Be("Confirmed");
        updated.VerifiedBy.Should().Be("lead-sre-alice");
        updated.VerifiedAtUtc.Should().NotBeNull();
        updated.OperatorNotes.Should().Contain("pg_stat_activity");
        updated.HypothesesJson.Should().Contain("CONFIRMED");
    }
}
