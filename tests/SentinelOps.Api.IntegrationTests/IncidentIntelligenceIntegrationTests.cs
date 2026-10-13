using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using SentinelOps.Infrastructure.Persistence;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class IncidentIntelligenceIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public IncidentIntelligenceIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid OrgId, Guid AppId, Guid IncidentId)> SeedIncidentAsync(string prefix = "ai")
    {
        var email = $"{prefix}_{Guid.NewGuid():N}@sentinelops.local";
        var password = "P@ssword2026!Secure";

        var registerRes = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = $"{prefix}_{Guid.NewGuid():N}"[..20],
            Email = email,
            FullName = "AI Test User",
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
            Description = "Organization for AI intelligence tests"
        });
        orgRes.EnsureSuccessStatusCode();
        var orgDoc = await orgRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>();
        var orgId = orgDoc!.Data!.Id;

        // Seed App & Incident in DbContext
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var app = new SentinelOps.Domain.Entities.Application
        {
            Name = $"{prefix}-app",
            DisplayName = "AI Monitored Application",
            OrganizationId = orgId,
            ApiKeyHash = Guid.NewGuid().ToString("N")
        };
        db.Applications.Add(app);

        var incident = new Incident
        {
            IncidentNumber = "INC-" + Guid.NewGuid().ToString("N")[..6],
            Title = "High Transaction Error Rate & Connection Latency",
            Description = "Excessive connection timeouts observed on primary worker pool with secret=sk_live_1234567890abcdef",
            Severity = Severity.Critical,
            Status = IncidentStatus.Open,
            ApplicationId = app.Id
        };
        db.Incidents.Add(incident);
        await db.SaveChangesAsync();

        return (token, orgId, app.Id, incident.Id);
    }

    [Fact]
    public async Task AnalyzeIncident_ShouldReturnHypothesesAndTelemetryEvidence()
    {
        var (token, orgId, _, incidentId) = await SeedIncidentAsync("analyze");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var res = await _client.PostAsync($"/api/incidents/{incidentId}/intelligence/analyze", null);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await res.Content.ReadFromJsonAsync<ApiResponse<IncidentAnalysisResult>>();
        doc!.Success.Should().BeTrue();
        doc.Data!.IncidentId.Should().Be(incidentId);
        doc.Data.Hypotheses.Should().NotBeEmpty();
        doc.Data.Hypotheses[0].ConfidenceScore.Should().BeGreaterThan(50);
        doc.Data.Hypotheses[0].Evidence.Should().NotBeEmpty();
        doc.Data.CorrelatedTelemetry.Should().NotBeNull();
    }

    [Fact]
    public async Task GeneratePostmortem_ShouldReturnStructuredMarkdown()
    {
        var (token, orgId, _, incidentId) = await SeedIncidentAsync("postmortem");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var res = await _client.PostAsync($"/api/incidents/{incidentId}/intelligence/postmortem", null);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await res.Content.ReadFromJsonAsync<ApiResponse<IncidentPostmortemResult>>();
        doc!.Success.Should().BeTrue();
        doc.Data!.MarkdownContent.Should().Contain("# Incident Postmortem:");
        doc.Data.MarkdownContent.Should().Contain("## 1. Executive Summary");
        doc.Data.MarkdownContent.Should().Contain("## 2. Root Cause Analysis");
    }

    [Fact]
    public async Task SuggestRemediation_ShouldReturnActionableChecklist()
    {
        var (token, orgId, _, incidentId) = await SeedIncidentAsync("remediate");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var res = await _client.PostAsync($"/api/incidents/{incidentId}/intelligence/remediate", null);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await res.Content.ReadFromJsonAsync<ApiResponse<RemediationSuggestionsResult>>();
        doc!.Success.Should().BeTrue();
        doc.Data!.Actions.Should().NotBeEmpty();
        doc.Data.Actions[0].StepNumber.Should().Be(1);
        doc.Data.Actions[0].CommandOrAction.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RecordFeedback_ShouldAcceptUserFeedback()
    {
        var (token, orgId, _, incidentId) = await SeedIncidentAsync("feedback");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var feedbackReq = new IntelligenceFeedbackRequest
        {
            HypothesisId = "hyp-1",
            IsHelpful = true,
            Comment = "Identified connection pool issue precisely."
        };

        var res = await _client.PostAsJsonAsync($"/api/incidents/{incidentId}/intelligence/feedback", feedbackReq);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await res.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        doc!.Success.Should().BeTrue();
        doc.Data.Should().BeTrue();
    }
}
