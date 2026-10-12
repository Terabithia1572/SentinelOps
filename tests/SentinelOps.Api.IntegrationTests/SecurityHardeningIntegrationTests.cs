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
            ConfirmPassword = "P@ssword2026!Secure",
            Role = "Viewer"
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
}
