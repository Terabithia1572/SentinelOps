using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class ApiKeyAndApplicationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiKeyAndApplicationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsRoleAsync(string role)
    {
        var email = $"user_{Guid.NewGuid():N}@sentinelops.io";
        var userName = $"user_{Guid.NewGuid():N}"[..12];

        var registerRequest = new RegisterRequest
        {
            UserName = userName,
            Email = email,
            FullName = "Role Test User",
            Password = "P@ssword2026!Secure",
            ConfirmPassword = "P@ssword2026!Secure",
            Role = role
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var auth = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Data!.Token);
    }

    [Fact]
    public async Task ApplicationAndApiKeyLifecycle_ShouldWorkCorrectly()
    {
        // 1. Authenticate as Operator
        await AuthenticateAsRoleAsync("Operator");

        // 2. Create Application
        var appName = $"app-{Guid.NewGuid():N}"[..12];
        var createAppReq = new CreateApplicationRequest
        {
            Name = appName,
            DisplayName = "Test Integration App",
            Description = "Integration Test Monitored Application"
        };

        var appRes = await _client.PostAsJsonAsync("/api/applications", createAppReq);
        appRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var appBody = await appRes.Content.ReadFromJsonAsync<ApiResponse<ApplicationDto>>();
        appBody.Should().NotBeNull();
        var appId = appBody!.Data!.Id;

        // 3. Add Environment
        var createEnvReq = new CreateEnvironmentRequest
        {
            Name = "production",
            EnvironmentType = "Production"
        };
        var envRes = await _client.PostAsJsonAsync($"/api/applications/{appId}/environments", createEnvReq);
        envRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Create API Key
        var createKeyReq = new CreateApiKeyRequest
        {
            Name = "Default Ingestion Key",
            ExpiresInDays = 60
        };

        var keyRes = await _client.PostAsJsonAsync($"/api/applications/{appId}/keys", createKeyReq);
        keyRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var keyBody = await keyRes.Content.ReadFromJsonAsync<ApiResponse<ApiKeyCreatedResponse>>();
        keyBody.Should().NotBeNull();
        keyBody!.Data!.RawApiKey.Should().StartWith("sent_live_");
        keyBody.Data.KeyPrefix.Should().StartWith("sent_live_");
        var keyId = keyBody.Data.Id;

        // 5. List API Keys (should not leak raw key)
        var listRes = await _client.GetAsync($"/api/applications/{appId}/keys");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var listBody = await listRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ApiKeyDto>>>();
        listBody.Should().NotBeNull();
        listBody!.Data.Should().ContainSingle(k => k.Id == keyId);
        listBody.Data.First().IsActive.Should().BeTrue();

        // 6. Rotate API Key
        var rotateReq = new RotateApiKeyRequest { ExpiresInDays = 90 };
        var rotateRes = await _client.PostAsJsonAsync($"/api/keys/{keyId}/rotate", rotateReq);
        rotateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var rotatedBody = await rotateRes.Content.ReadFromJsonAsync<ApiResponse<ApiKeyCreatedResponse>>();
        rotatedBody.Should().NotBeNull();
        rotatedBody!.Data!.Id.Should().NotBe(keyId);
        rotatedBody.Data.RawApiKey.Should().StartWith("sent_live_");

        // 7. Revoke the new key
        var revokeRes = await _client.PostAsJsonAsync($"/api/keys/{rotatedBody.Data.Id}/revoke", new { });
        revokeRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
