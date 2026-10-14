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

public class IncidentCopilotIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public IncidentCopilotIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ChatWithCopilot_ReturnsStructuredAssistance_WithMandatoryHumanApproval()
    {
        var email = $"copilot_{Guid.NewGuid():N}@sentinelops.local";
        var password = "P@ssword2026!Secure";

        var registerRes = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = $"copilot_{Guid.NewGuid():N}"[..20],
            Email = email,
            FullName = "Copilot Test User",
            Password = password,
            ConfirmPassword = password
        });
        registerRes.EnsureSuccessStatusCode();

        var regDoc = await registerRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var token = regDoc!.Data!.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var orgRes = await _client.PostAsJsonAsync("/api/organizations", new CreateOrganizationRequest
        {
            Name = $"Copilot Org {Guid.NewGuid():N}"[..25],
            Slug = $"org-{Guid.NewGuid():N}"[..20],
            Description = "Organization for copilot tests"
        });
        orgRes.EnsureSuccessStatusCode();
        var orgDoc = await orgRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>();
        var orgId = orgDoc!.Data!.Id;

        _client.DefaultRequestHeaders.TryAddWithoutValidation("X-Organization-Id", orgId.ToString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var app = new Domain.Entities.Application
        {
            Name = "copilot-app",
            DisplayName = "Copilot App",
            OrganizationId = orgId,
            ApiKeyHash = "hash"
        };
        db.Applications.Add(app);

        var incident = new Incident
        {
            ApplicationId = app.Id,
            Title = "Payments API Elevated Latency",
            Description = "P99 spike on /charge",
            Severity = Severity.Error,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Incidents.Add(incident);
        await db.SaveChangesAsync();

        var chatReq = new AiCopilotChatRequest
        {
            OrganizationId = orgId,
            IncidentId = incident.Id,
            ApplicationId = app.Id,
            Message = "How should I mitigate the latency on Payments API?"
        };

        var response = await _client.PostAsJsonAsync($"/api/incidents/{incident.Id}/intelligence/copilot/chat", chatReq);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AiCopilotChatResponse>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.RequiresHumanApproval.Should().BeTrue();
        body.Data.Citations.Should().NotBeEmpty();
        body.Data.RecommendedActions.Should().NotBeEmpty();
    }
}
