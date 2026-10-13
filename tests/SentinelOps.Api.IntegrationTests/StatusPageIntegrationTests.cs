using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class StatusPageIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public StatusPageIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid OrgId)> SeedAuthAndOrgAsync(string prefix = "sp")
    {
        var email = $"{prefix}_{Guid.NewGuid():N}@sentinelops.local";
        var password = "P@ssword2026!Secure";

        var registerRes = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = $"{prefix}_{Guid.NewGuid():N}"[..20],
            Email = email,
            FullName = "Status Page Test User",
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
            Description = "Organization for status page tests"
        });
        orgRes.EnsureSuccessStatusCode();
        var orgDoc = await orgRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>();
        var orgId = orgDoc!.Data!.Id;

        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        return (token, orgId);
    }

    [Fact]
    public async Task CreateStatusPage_And_GetStatusPage_ReturnsSuccessWithTenantIsolation()
    {
        var (_, orgId) = await SeedAuthAndOrgAsync("crud");
        var slug = $"acme-status-{Guid.NewGuid():N}"[..20];

        var createReq = new CreateStatusPageRequest
        {
            Name = "Acme Corp Public Status",
            Slug = slug,
            Description = "Live service status for Acme Cloud services",
            SupportUrl = "https://support.acme.local",
            IsPublic = true,
            AutoRollupEnabled = true
        };

        var postRes = await _client.PostAsJsonAsync("/api/status-pages", createReq);
        postRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var postDoc = await postRes.Content.ReadFromJsonAsync<ApiResponse<StatusPageDto>>();
        postDoc.Should().NotBeNull();
        postDoc!.Success.Should().BeTrue();
        var created = postDoc.Data!;
        created.Name.Should().Be("Acme Corp Public Status");
        created.Slug.Should().Be(slug);
        created.Components.Should().NotBeEmpty();

        // Get by ID
        var getRes = await _client.GetAsync($"/api/status-pages/{created.Id}");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var getDoc = await getRes.Content.ReadFromJsonAsync<ApiResponse<StatusPageDto>>();
        getDoc!.Data!.Id.Should().Be(created.Id);

        // List
        var listRes = await _client.GetAsync("/api/status-pages");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var listDoc = await listRes.Content.ReadFromJsonAsync<ApiResponse<List<StatusPageDto>>>();
        listDoc!.Data.Should().Contain(p => p.Id == created.Id);
    }

    [Fact]
    public async Task GetPublicStatusPage_Unauthenticated_ReturnsLiveStatusRollup()
    {
        await SeedAuthAndOrgAsync("pub");
        var slug = $"public-{Guid.NewGuid():N}"[..20];

        var createReq = new CreateStatusPageRequest
        {
            Name = "Global Public Status",
            Slug = slug,
            Description = "Unauthenticated status page",
            IsPublic = true,
            AutoRollupEnabled = true
        };

        var postRes = await _client.PostAsJsonAsync("/api/status-pages", createReq);
        postRes.EnsureSuccessStatusCode();

        // Clear auth header to verify unauthenticated public access
        _client.DefaultRequestHeaders.Authorization = null;
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");

        var pubRes = await _client.GetAsync($"/api/status/{slug}");
        pubRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var pubDoc = await pubRes.Content.ReadFromJsonAsync<ApiResponse<PublicStatusPageDto>>();
        pubDoc.Should().NotBeNull();
        pubDoc!.Success.Should().BeTrue();
        pubDoc.Data!.Name.Should().Be("Global Public Status");
        pubDoc.Data.OverallStatus.Should().Be("All Systems Operational");
        pubDoc.Data.Components.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostIncident_And_AddUpdate_UpdatesTimelineAndOverallStatus()
    {
        await SeedAuthAndOrgAsync("inc");
        var slug = $"incident-feed-{Guid.NewGuid():N}"[..20];

        var createReq = new CreateStatusPageRequest
        {
            Name = "Incident Feed Status",
            Slug = slug,
            IsPublic = true,
            AutoRollupEnabled = true
        };

        var postPageRes = await _client.PostAsJsonAsync("/api/status-pages", createReq);
        postPageRes.EnsureSuccessStatusCode();
        var pageDoc = await postPageRes.Content.ReadFromJsonAsync<ApiResponse<StatusPageDto>>();
        var statusPageId = pageDoc!.Data!.Id;
        var firstComponentId = pageDoc.Data.Components.First().Id;

        // Post status incident
        var postIncReq = new CreateStatusPageIncidentRequest
        {
            Title = "API Gateway Elevated Latency",
            Status = "Investigating",
            Severity = "Major",
            AffectedComponentIds = new List<string> { firstComponentId },
            InitialMessage = "We are currently investigating elevated latency affecting API gateway endpoints."
        };

        var postIncRes = await _client.PostAsJsonAsync($"/api/status-pages/{statusPageId}/incidents", postIncReq);
        postIncRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var incDoc = await postIncRes.Content.ReadFromJsonAsync<ApiResponse<StatusPageIncidentDto>>();
        var incidentId = incDoc!.Data!.Id;
        incDoc.Data.Status.Should().Be("Investigating");
        incDoc.Data.Updates.Should().HaveCount(1);

        // Check public status
        var pubRes1 = await _client.GetAsync($"/api/status/{slug}");
        var pubDoc1 = await pubRes1.Content.ReadFromJsonAsync<ApiResponse<PublicStatusPageDto>>();
        pubDoc1!.Data!.ActiveIncidents.Should().HaveCount(1);
        pubDoc1.Data.OverallStatus.Should().Be("Partial System Outage");

        // Add update resolving incident
        var updateReq = new AddStatusPageIncidentUpdateRequest
        {
            Status = "Resolved",
            Message = "All endpoints have returned to nominal latency and error thresholds."
        };

        var updateRes = await _client.PostAsJsonAsync($"/api/status-pages/{statusPageId}/incidents/{incidentId}/updates", updateReq);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check public status again
        var pubRes2 = await _client.GetAsync($"/api/status/{slug}");
        var pubDoc2 = await pubRes2.Content.ReadFromJsonAsync<ApiResponse<PublicStatusPageDto>>();
        pubDoc2!.Data!.ActiveIncidents.Should().BeEmpty();
        pubDoc2.Data.PastIncidents.Should().HaveCount(1);
        pubDoc2.Data.OverallStatus.Should().Be("All Systems Operational");
    }

    [Fact]
    public async Task StatusPage_TenantIsolation_CrossTenantAccessBlocked()
    {
        await SeedAuthAndOrgAsync("tenantA");
        var slugA = $"org-a-status-{Guid.NewGuid():N}"[..20];

        var createReq = new CreateStatusPageRequest
        {
            Name = "Org A Private Status",
            Slug = slugA,
            IsPublic = false
        };

        var postRes = await _client.PostAsJsonAsync("/api/status-pages", createReq);
        var pageA = (await postRes.Content.ReadFromJsonAsync<ApiResponse<StatusPageDto>>())!.Data!;

        // Switch to Tenant B
        await SeedAuthAndOrgAsync("tenantB");

        // Tenant B attempting to read Org A's status page via admin endpoint
        var getRes = await _client.GetAsync($"/api/status-pages/{pageA.Id}");
        getRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Tenant B attempting to delete Org A's status page
        var deleteRes = await _client.DeleteAsync($"/api/status-pages/{pageA.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
