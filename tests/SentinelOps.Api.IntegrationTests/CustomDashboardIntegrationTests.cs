using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class CustomDashboardIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CustomDashboardIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid OrgId)> RegisterAndCreateOrgAsync(string prefix = "dash")
    {
        var email = $"{prefix}_{Guid.NewGuid():N}@sentinelops.local";
        var password = "P@ssword2026!Secure";

        var registerRes = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = $"{prefix}_{Guid.NewGuid():N}"[..20],
            Email = email,
            FullName = "Dashboard Test User",
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
            Description = "Organization for dashboard tests"
        });
        orgRes.EnsureSuccessStatusCode();
        var orgDoc = await orgRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>();
        var orgId = orgDoc!.Data!.Id;

        return (token, orgId);
    }

    [Fact]
    public async Task CreateDashboard_ShouldSucceedAndPersistLayout()
    {
        var (token, orgId) = await RegisterAndCreateOrgAsync("create_dash");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var createReq = new CreateDashboardRequest
        {
            Title = "Core Services Overview",
            Description = "High-level overview of core microservices",
            IsDefault = true,
            IsShared = true,
            RefreshIntervalSeconds = 15,
            DefaultTimeRange = "30m",
            Tags = ["production", "core", "apm"],
            LayoutJson = "[{\"id\":\"w1\",\"type\":\"timeseries\",\"title\":\"CPU Usage\",\"x\":0,\"y\":0,\"w\":6,\"h\":3,\"configJson\":\"{}\"}]"
        };

        var postRes = await _client.PostAsJsonAsync("/api/dashboards", createReq);
        postRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await postRes.Content.ReadFromJsonAsync<ApiResponse<DashboardDetailDto>>();
        created!.Success.Should().BeTrue();
        created.Data!.Title.Should().Be("Core Services Overview");
        created.Data.OrganizationId.Should().Be(orgId);
        created.Data.IsDefault.Should().BeTrue();
        created.Data.Widgets.Should().HaveCount(1);
        created.Data.Widgets[0].Title.Should().Be("CPU Usage");
    }

    [Fact]
    public async Task GetDashboards_ShouldEnforceTenantIsolation()
    {
        var (token1, org1Id) = await RegisterAndCreateOrgAsync("tenant1");
        var (token2, org2Id) = await RegisterAndCreateOrgAsync("tenant2");

        // Org 1 creates a dashboard
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", org1Id.ToString());

        var postRes = await _client.PostAsJsonAsync("/api/dashboards", new CreateDashboardRequest
        {
            Title = "Org 1 Confidential Dashboard",
            LayoutJson = "[]"
        });
        postRes.EnsureSuccessStatusCode();

        // Org 2 lists dashboards
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", org2Id.ToString());

        var listRes = await _client.GetAsync("/api/dashboards");
        listRes.EnsureSuccessStatusCode();

        var listDoc = await listRes.Content.ReadFromJsonAsync<ApiResponse<List<DashboardSummaryItemDto>>>();
        listDoc!.Data!.Should().NotContain(d => d.Title == "Org 1 Confidential Dashboard");
    }

    [Fact]
    public async Task DuplicateDashboard_ShouldCloneWidgets()
    {
        var (token, orgId) = await RegisterAndCreateOrgAsync("dupe_dash");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var postRes = await _client.PostAsJsonAsync("/api/dashboards", new CreateDashboardRequest
        {
            Title = "Original Template Dashboard",
            LayoutJson = "[{\"id\":\"w-1\",\"type\":\"gauge\",\"title\":\"SLO Burn\",\"x\":0,\"y\":0,\"w\":4,\"h\":3,\"configJson\":\"{}\"}]"
        });
        postRes.EnsureSuccessStatusCode();
        var original = (await postRes.Content.ReadFromJsonAsync<ApiResponse<DashboardDetailDto>>())!.Data!;

        var dupeRes = await _client.PostAsync($"/api/dashboards/{original.Id}/duplicate", null);
        dupeRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var duplicated = (await dupeRes.Content.ReadFromJsonAsync<ApiResponse<DashboardDetailDto>>())!.Data!;
        duplicated.Id.Should().NotBe(original.Id);
        duplicated.Title.Should().Be("Original Template Dashboard (Copy)");
        duplicated.Widgets.Should().HaveCount(1);
        duplicated.Widgets[0].Title.Should().Be("SLO Burn");
    }

    [Fact]
    public async Task InstantiateTemplate_ShouldCreateAllPrebuiltWidgets()
    {
        var (token, orgId) = await RegisterAndCreateOrgAsync("tmpl_dash");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var postRes = await _client.PostAsync("/api/dashboards/templates/apm-services/instantiate", null);
        postRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var templateDash = (await postRes.Content.ReadFromJsonAsync<ApiResponse<DashboardDetailDto>>())!.Data!;
        templateDash.Title.Should().Be("API Gateway & Microservices APM");
        templateDash.Widgets.Should().HaveCountGreaterThanOrEqualTo(5);
        templateDash.Tags.Should().Contain("apm");
    }

    [Fact]
    public async Task DeleteDashboard_ShouldRemoveDashboard()
    {
        var (token, orgId) = await RegisterAndCreateOrgAsync("del_dash");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var postRes = await _client.PostAsJsonAsync("/api/dashboards", new CreateDashboardRequest
        {
            Title = "To Be Deleted",
            LayoutJson = "[]"
        });
        postRes.EnsureSuccessStatusCode();
        var dash = (await postRes.Content.ReadFromJsonAsync<ApiResponse<DashboardDetailDto>>())!.Data!;

        var delRes = await _client.DeleteAsync($"/api/dashboards/{dash.Id}");
        delRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var getRes = await _client.GetAsync($"/api/dashboards/{dash.Id}");
        getRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
