using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class ApmIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApmIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid OrgId)> RegisterAndCreateOrgAsync(string prefix = "apm")
    {
        var email = $"{prefix}_{Guid.NewGuid():N}@sentinelops.local";
        var password = "P@ssword2026!Secure";

        var registerRes = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = $"{prefix}_{Guid.NewGuid():N}"[..20],
            Email = email,
            FullName = "APM Test User",
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
            Description = "Organization for APM tests"
        });
        orgRes.EnsureSuccessStatusCode();
        var orgDoc = await orgRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>();
        var orgId = orgDoc!.Data!.Id;

        return (token, orgId);
    }

    [Fact]
    public async Task GetServiceMap_ShouldReturnNodesAndDirectedEdges()
    {
        var (token, orgId) = await RegisterAndCreateOrgAsync("map_test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var res = await _client.GetAsync("/api/apm/service-map?timeRange=1h");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await res.Content.ReadFromJsonAsync<ApiResponse<ServiceMapDto>>();
        doc!.Success.Should().BeTrue();
        doc.Data!.Nodes.Should().NotBeEmpty();
        doc.Data.Nodes.Should().Contain(n => n.ServiceName == "api-gateway" || n.ServiceName == "orders-service");
        doc.Data.Edges.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetServiceDependencies_ShouldReturnInboundAndOutbound()
    {
        var (token, orgId) = await RegisterAndCreateOrgAsync("dep_test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var res = await _client.GetAsync("/api/apm/services/orders-service/dependencies");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await res.Content.ReadFromJsonAsync<ApiResponse<ServiceDependenciesDto>>();
        doc!.Success.Should().BeTrue();
        doc.Data!.ServiceName.Should().Be("orders-service");
        doc.Data.Inbound.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetEndpoints_ShouldReturnEndpointsWithThroughputAndPercentiles()
    {
        var (token, orgId) = await RegisterAndCreateOrgAsync("ep_test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var res = await _client.GetAsync("/api/apm/endpoints?timeRange=1h");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await res.Content.ReadFromJsonAsync<ApiResponse<List<EndpointPerformanceDto>>>();
        doc!.Success.Should().BeTrue();
        doc.Data!.Should().NotBeEmpty();
        doc.Data!.Should().Contain(e => e.Route.Contains("/api/orders"));
    }

    [Fact]
    public async Task GetEndpointBreakdown_ShouldReturnLatencyBucketsAndErrors()
    {
        var (token, orgId) = await RegisterAndCreateOrgAsync("bk_test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var res = await _client.GetAsync("/api/apm/endpoints/POST:%2Fapi%2Forders:orders-service/breakdown");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await res.Content.ReadFromJsonAsync<ApiResponse<EndpointBreakdownDto>>();
        doc!.Success.Should().BeTrue();
        doc.Data!.LatencyBuckets.Should().NotBeEmpty();
        doc.Data.SampleTraceIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetExternalDependencies_ShouldReturnDatabaseAndHttpTargets()
    {
        var (token, orgId) = await RegisterAndCreateOrgAsync("ext_test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Organization-Id");
        _client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var res = await _client.GetAsync("/api/apm/external-dependencies");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await res.Content.ReadFromJsonAsync<ApiResponse<List<ExternalDependencyDto>>>();
        doc!.Success.Should().BeTrue();
        doc.Data!.Should().Contain(e => e.Type == "Database");
        doc.Data!.Should().Contain(e => e.Type == "ExternalHttp");
    }
}
