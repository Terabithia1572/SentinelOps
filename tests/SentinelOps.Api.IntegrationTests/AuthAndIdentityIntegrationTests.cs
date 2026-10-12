using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class AuthAndIdentityIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthAndIdentityIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterAndLogin_WhenValid_ShouldIssueJwtAndRefreshToken()
    {
        var email = $"testuser_{Guid.NewGuid():N}@sentinelops.io";
        var userName = $"user_{Guid.NewGuid():N}"[..12];

        // 1. Register
        var registerRequest = new RegisterRequest
        {
            UserName = userName,
            Email = email,
            FullName = "Test Platform User",
            Password = "P@ssword2026!Secure",
            ConfirmPassword = "P@ssword2026!Secure",
            Role = "Operator"
        };

        var registerRes = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var regBody = await registerRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        regBody.Should().NotBeNull();
        regBody!.Success.Should().BeTrue();
        regBody.Data!.Token.Should().NotBeNullOrEmpty();
        regBody.Data.RefreshToken.Should().NotBeNullOrEmpty();
        regBody.Data.Roles.Should().Contain("Operator");

        // 2. Login
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = "P@ssword2026!Secure"
        };

        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        loginBody.Should().NotBeNull();
        loginBody!.Success.Should().BeTrue();
        loginBody.Data!.Token.Should().NotBeNullOrEmpty();
        loginBody.Data.RefreshToken.Should().NotBeNullOrEmpty();

        // 3. Access protected /api/auth/me with Bearer token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Data.Token);
        var meRes = await _client.GetAsync("/api/auth/me");
        meRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var meBody = await meRes.Content.ReadFromJsonAsync<ApiResponse<UserProfileDto>>();
        meBody.Should().NotBeNull();
        meBody!.Data!.Email.Should().Be(email);
        meBody.Data.Roles.Should().Contain("Operator");

        // 4. Refresh token rotation
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = loginBody.Data.RefreshToken
        };

        var refreshRes = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        refreshRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshBody = await refreshRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        refreshBody.Should().NotBeNull();
        refreshBody!.Success.Should().BeTrue();
        refreshBody.Data!.RefreshToken.Should().NotBe(loginBody.Data.RefreshToken);

        // 5. Old refresh token should now be rejected (rotation enforcement)
        var replayRes = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        replayRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WhenUnauthenticated_ShouldReturnUnauthorized()
    {
        var clientWithoutAuth = new CustomWebApplicationFactory().CreateClient();
        var response = await clientWithoutAuth.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
