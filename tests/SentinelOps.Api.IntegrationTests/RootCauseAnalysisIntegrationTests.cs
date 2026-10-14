using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using SentinelOps.Infrastructure.Persistence;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class RootCauseAnalysisIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public RootCauseAnalysisIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid OrgId, Guid AppId, Guid IncidentId)> SetupIncidentWithEvidenceAsync(string prefix = "rca")
    {
        var email = $"{prefix}_{Guid.NewGuid():N}@sentinelops.local";
        var password = "P@ssword2026!Secure";

        var registerRes = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = $"{prefix}_{Guid.NewGuid():N}"[..20],
            Email = email,
            FullName = "RCA Test User",
            Password = password,
            ConfirmPassword = password
        });
        registerRes.EnsureSuccessStatusCode();

        var regDoc = await registerRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var token = regDoc!.Data!.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var orgRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = $"{prefix} Org {Guid.NewGuid():N}"[..25],
            Slug = $"org-{Guid.NewGuid():N}"[..20],
            Description = "Organization for RCA tests"
        });
        orgRes.EnsureSuccessStatusCode();
        var orgDoc = await orgRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>();
        var orgId = orgDoc!.Data!.Id;

        _client.DefaultRequestHeaders.TryAddWithoutValidation("X-Organization-Id", orgId.ToString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var app = new Domain.Entities.Application
        {
            Name = $"{prefix}-app",
            DisplayName = $"{prefix} App",
            OrganizationId = orgId,
            ApiKeyHash = "hash"
        };
        db.Applications.Add(app);

        var now = DateTime.UtcNow;
        var incident = new Incident
        {
            ApplicationId = app.Id,
            Title = "High Latency & Connection Timeout in Gateway",
            Description = "Gateway downstream timeout cascade",
            Severity = Severity.Critical,
            CreatedAtUtc = now
        };
        db.Incidents.Add(incident);

        // Correlated error logs
        db.LogEntries.Add(new LogEntry
        {
            ApplicationId = app.Id,
            ServiceName = "gateway",
            EnvironmentName = "prod",
            Severity = Severity.Error,
            Message = "Npgsql.NpgsqlException: Connection pool exhaustion timeout",
            Timestamp = now.AddMinutes(-3),
            TraceId = "tr-rca-101"
        });

        // Correlated slow trace
        db.TraceRecords.Add(new TraceRecord
        {
            TraceId = "tr-rca-101",
            ApplicationId = app.Id,
            ServiceName = "gateway",
            EnvironmentName = "prod",
            RootSpanName = "POST /api/checkout",
            Duration = TimeSpan.FromMilliseconds(4200),
            SpanCount = 8,
            HasErrors = true,
            StartedAtUtc = now.AddMinutes(-3)
        });

        await db.SaveChangesAsync();

        return (token, orgId, app.Id, incident.Id);
    }

    [Fact]
    public async Task PerformRca_And_Verify_WorkEndToEndWithProvenance()
    {
        var (token, orgId, appId, incidentId) = await SetupIncidentWithEvidenceAsync("e2e");

        // 1. Perform RCA
        var rcaRes = await _client.PostAsync($"/api/incidents/{incidentId}/intelligence/rca", null);
        rcaRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var rcaBody = await rcaRes.Content.ReadFromJsonAsync<ApiResponse<RcaAnalysisResult>>();
        rcaBody.Should().NotBeNull();
        rcaBody!.Success.Should().BeTrue();
        rcaBody.Data!.IncidentId.Should().Be(incidentId);
        rcaBody.Data.IsDeterministic.Should().BeTrue();
        rcaBody.Data.Hypotheses.Should().NotBeEmpty();

        var topHypothesis = rcaBody.Data.Hypotheses.First();
        topHypothesis.ConfidenceLevel.Should().BeOneOf("Likely cause", "Possible cause");
        topHypothesis.SupportingEvidence.Should().Contain(e => e.Contains("tr-rca-101") || e.Contains("Npgsql"));

        // 2. Fetch the persisted RCA entity ID
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var rcaRecord = db.IncidentRcas.FirstOrDefault(r => r.IncidentId == incidentId);
        rcaRecord.Should().NotBeNull();

        // 3. Operator verification
        var verifyUrl = $"/api/incidents/{incidentId}/intelligence/rca/{rcaRecord!.Id}/verify?hypothesis={Uri.EscapeDataString(topHypothesis.Hypothesis)}&isConfirmed=true&notes={Uri.EscapeDataString("Confirmed pool bottleneck")}";
        var verifyRes = await _client.PostAsync(verifyUrl, null);
        verifyRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Verify DB updated
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = verifyDb.IncidentRcas.AsNoTracking().FirstOrDefault(r => r.Id == rcaRecord.Id);
        updated!.OperatorVerificationStatus.Should().Be("Confirmed");
        updated.OperatorNotes.Should().Contain("Confirmed pool bottleneck");
    }
}
