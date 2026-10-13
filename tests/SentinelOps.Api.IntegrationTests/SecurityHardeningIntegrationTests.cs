using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class SecurityHardeningIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityHardeningIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ApplicationsEndpoint_WhenAnonymous_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/applications");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DashboardSummaryEndpoint_WhenAnonymous_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/dashboard/summary");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HttpResponses_ShouldContainOwaspSecurityHeaders()
    {
        var response = await _client.GetAsync("/health/live");

        response.Headers.Contains("X-Content-Type-Options").Should().BeTrue();
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");

        response.Headers.Contains("X-Frame-Options").Should().BeTrue();
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");

        response.Headers.Contains("Content-Security-Policy").Should().BeTrue();
        response.Headers.GetValues("Content-Security-Policy").First().Should().Contain("default-src 'self'");
    }

    [Fact]
    public async Task RefreshTokenReplayAttack_ShouldDetectBreachAndInvalidateTokenFamily()
    {
        var email = $"replay_test_{Guid.NewGuid():N}@sentinelops.io";
        var userName = $"replay_{Guid.NewGuid():N}"[..12];

        // 1. Register a user
        var registerRequest = new RegisterRequest
        {
            UserName = userName,
            Email = email,
            FullName = "Replay Attack Victim",
            Password = "P@ssword2026!Secure",
            ConfirmPassword = "P@ssword2026!Secure"
        };

        var regRes = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        regRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var regBody = await regRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var firstRefreshToken = regBody!.Data!.RefreshToken;
        var firstAccessToken = regBody.Data.Token;

        // 2. Legitimate user rotates the token (first rotation)
        var rotateRes = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = firstRefreshToken
        });
        rotateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotateBody = await rotateRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var secondRefreshToken = rotateBody!.Data!.RefreshToken;

        // 3. Attacker presents the FIRST (now revoked) refresh token again
        var replayAttackRes = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = firstRefreshToken
        });

        // Replay attempt must be rejected
        replayAttackRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 4. Verification: The SECOND (previously active) token must now ALSO be invalidated due to family revocation!
        var victimRefreshRes = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = secondRefreshToken
        });

        // The entire family was revoked for security
        victimRefreshRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Registration_WhenCallerAttemptsRoleEscalation_MustOnlyGrantViewerRole()
    {
        var email = $"attacker_{Guid.NewGuid():N}@sentinelops.io";
        var userName = $"att_{Guid.NewGuid():N}"[..12];

        // Attacker attempts to forge "role": "Admin" in registration payload
        var payload = new
        {
            userName,
            email,
            fullName = "Privilege Escalation Attacker",
            password = "P@ssword2026!Secure",
            confirmPassword = "P@ssword2026!Secure",
            role = "Admin"
        };

        var registerRes = await _client.PostAsJsonAsync("/api/auth/register", payload);
        registerRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var regBody = await registerRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        regBody.Should().NotBeNull();
        regBody!.Data!.Roles.Should().NotContain("Admin");
        regBody.Data.Roles.Should().NotContain("Operator");
        regBody.Data.Roles.Should().Contain("Viewer");

        // Verify the token cannot be used to perform Operator/Admin actions
        using var clientWithToken = new HttpClient();
        clientWithToken.BaseAddress = _client.BaseAddress;
        clientWithToken.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", regBody.Data.Token);

        var createReq = new CreateApplicationRequest
        {
            Name = "malicious-app",
            DisplayName = "Malicious Application",
            Description = "Should be rejected"
        };
        var appRes = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/applications")
        {
            Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", regBody.Data.Token) },
            Content = JsonContent.Create(createReq)
        });

        appRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ViewerRole_CannotDeleteApplicationOrRevokeApiKey_ReturnsForbidden()
    {
        var email = $"viewer_{Guid.NewGuid():N}@sentinelops.io";
        var userName = $"view_{Guid.NewGuid():N}"[..12];

        var registerRequest = new RegisterRequest
        {
            UserName = userName,
            Email = email,
            FullName = "Standard Viewer User",
            Password = "P@ssword2026!Secure",
            ConfirmPassword = "P@ssword2026!Secure"
        };

        var registerRes = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var regBody = await registerRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var viewerToken = regBody!.Data!.Token;

        var deleteAppRes = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/applications/{Guid.NewGuid()}")
        {
            Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", viewerToken) }
        });
        deleteAppRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var revokeKeyRes = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"/api/keys/{Guid.NewGuid()}/revoke")
        {
            Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", viewerToken) }
        });
        revokeKeyRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
