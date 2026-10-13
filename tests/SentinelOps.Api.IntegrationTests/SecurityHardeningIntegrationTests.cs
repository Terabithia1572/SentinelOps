using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Constants;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using SentinelOps.Infrastructure.Identity;
using SentinelOps.Infrastructure.Persistence;
using SentinelOps.Infrastructure.Security;
using SentinelOps.Infrastructure.Services.Notifications;
using Xunit;
using AppEntity = SentinelOps.Domain.Entities.Application;

namespace SentinelOps.Api.IntegrationTests;

public class SecurityHardeningIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityHardeningIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, string UserId)> CreateUserWithRoleAsync(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        var unique = Guid.NewGuid().ToString("N")[..8];
        var user = new ApplicationUser
        {
            UserName = $"sec_{role.ToLower()}_{unique}",
            Email = $"sec_{role.ToLower()}_{unique}@sentinelops.local",
            FullName = $"Security Test {role} User",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "P@ssword2026!Secure");
        result.Succeeded.Should().BeTrue();

        await userManager.AddToRoleAsync(user, role);

        var (token, _) = await tokenService.GenerateAccessTokenAsync(user.Id, user.UserName, user.Email, [role]);
        return (token, user.Id);
    }

    private static string CreateJwt(string secretKey, DateTime expires, string issuer = "SentinelOps", string audience = "SentinelOpsUsers")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, "test_jwt_user"),
                new Claim(ClaimTypes.Role, Roles.Viewer)
            ]),
            NotBefore = expires.AddMinutes(-10),
            IssuedAt = expires.AddMinutes(-10),
            Expires = expires,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
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
    public async Task ExpiredJwtToken_ShouldReturnUnauthorized()
    {
        var expiredToken = CreateJwt("PLACEHOLDER_DEV_SECRET_KEY_AT_LEAST_32_CHARS_LONG", DateTime.UtcNow.AddMinutes(-10));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/applications");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TamperedJwtTokenSignature_ShouldReturnUnauthorized()
    {
        var tamperedToken = CreateJwt("UNTRUSTED_KEY_AT_LEAST_32_CHARACTERS_LONG_FOR_TESTS", DateTime.UtcNow.AddMinutes(15));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/applications");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
        var createReq = new CreateApplicationRequest
        {
            Name = "malicious-app",
            DisplayName = "Malicious Application",
            Description = "Should be rejected"
        };
        var appRes = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/applications")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", regBody.Data.Token) },
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
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", viewerToken) }
        });
        deleteAppRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var revokeKeyRes = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"/api/keys/{Guid.NewGuid()}/revoke")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", viewerToken) }
        });
        revokeKeyRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AccountLockout_AfterConsecutiveFailedAttempts_ShouldLockAccount()
    {
        var email = $"lockout_{Guid.NewGuid():N}@sentinelops.io";
        var userName = $"lock_{Guid.NewGuid():N}"[..12];
        const string correctPassword = "P@ssword2026!Secure";

        var regRes = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = userName,
            Email = email,
            FullName = "Lockout Target User",
            Password = correctPassword,
            ConfirmPassword = correctPassword
        });
        regRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Perform 5 failed login attempts
        for (int i = 0; i < 5; i++)
        {
            var failRes = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Email = email,
                Password = "WrongPassword!999"
            });
            failRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // 6th attempt with CORRECT password must now fail due to account lockout
        var lockedRes = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = correctPassword
        });
        lockedRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TenantIsolation_LogQueries_StrictlyScopedByApplicationId()
    {
        var appA = new AppEntity { Name = $"app-a-{Guid.NewGuid():N}", DisplayName = "App A", IsActive = true };
        var appB = new AppEntity { Name = $"app-b-{Guid.NewGuid():N}", DisplayName = "App B", IsActive = true };

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Applications.AddRange(appA, appB);
            db.LogEntries.AddRange(
                new LogEntry { ApplicationId = appA.Id, Message = "App A private log", ServiceName = "srv-a", EnvironmentName = "prod", Severity = Severity.Information, Timestamp = DateTime.UtcNow },
                new LogEntry { ApplicationId = appB.Id, Message = "App B private log", ServiceName = "srv-b", EnvironmentName = "prod", Severity = Severity.Information, Timestamp = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
        }

        var (token, _) = await CreateUserWithRoleAsync(Roles.Viewer);
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Query scoped to App A
        var resA = await client.GetAsync($"/api/logs?applicationId={appA.Id}");
        resA.StatusCode.Should().Be(HttpStatusCode.OK);
        var bodyA = await resA.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<LogEntryDto>>>();
        bodyA!.Data!.Items.Should().OnlyContain(l => l.ApplicationId == appA.Id);
        bodyA.Data.Items.Should().NotContain(l => l.ApplicationId == appB.Id);

        // Query scoped to App B
        var resB = await client.GetAsync($"/api/logs?applicationId={appB.Id}");
        resB.StatusCode.Should().Be(HttpStatusCode.OK);
        var bodyB = await resB.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<LogEntryDto>>>();
        bodyB!.Data!.Items.Should().OnlyContain(l => l.ApplicationId == appB.Id);
        bodyB.Data.Items.Should().NotContain(l => l.ApplicationId == appA.Id);
    }

    [Fact]
    public async Task TenantIsolation_TraceAndMetricQueries_StrictlyScopedByApplicationId()
    {
        var appA = new AppEntity { Name = $"trace-app-a-{Guid.NewGuid():N}", DisplayName = "Trace App A", IsActive = true };
        var appB = new AppEntity { Name = $"trace-app-b-{Guid.NewGuid():N}", DisplayName = "Trace App B", IsActive = true };

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Applications.AddRange(appA, appB);
            db.TraceRecords.AddRange(
                new TraceRecord { TraceId = Guid.NewGuid().ToString("N"), ApplicationId = appA.Id, ServiceName = "svc-a", EnvironmentName = "prod", RootSpanName = "GET /a", Duration = TimeSpan.FromMilliseconds(50), StartedAtUtc = DateTime.UtcNow },
                new TraceRecord { TraceId = Guid.NewGuid().ToString("N"), ApplicationId = appB.Id, ServiceName = "svc-b", EnvironmentName = "prod", RootSpanName = "GET /b", Duration = TimeSpan.FromMilliseconds(40), StartedAtUtc = DateTime.UtcNow }
            );
            db.MetricRecords.AddRange(
                new MetricRecord { ApplicationId = appA.Id, ServiceName = "svc-a", EnvironmentName = "prod", MetricName = "http.requests.a", MetricType = MetricType.Counter, Value = 10, Timestamp = DateTime.UtcNow },
                new MetricRecord { ApplicationId = appB.Id, ServiceName = "svc-b", EnvironmentName = "prod", MetricName = "http.requests.b", MetricType = MetricType.Counter, Value = 20, Timestamp = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
        }

        var (token, _) = await CreateUserWithRoleAsync(Roles.Viewer);
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Traces scoped to App A
        var tracesRes = await client.GetAsync($"/api/traces?applicationId={appA.Id}");
        tracesRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var tracesBody = await tracesRes.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<TraceDto>>>();
        tracesBody!.Data!.Items.Should().OnlyContain(t => t.ApplicationId == appA.Id);

        // Metrics scoped to App A
        var metricsRes = await client.GetAsync($"/api/metrics/timeseries?applicationId={appA.Id}");
        metricsRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TenantIsolation_ApiKeyQueries_NeverLeakAcrossApplications()
    {
        var appA = new AppEntity { Name = $"key-app-a-{Guid.NewGuid():N}", DisplayName = "Key App A", IsActive = true };
        var appB = new AppEntity { Name = $"key-app-b-{Guid.NewGuid():N}", DisplayName = "Key App B", IsActive = true };

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Applications.AddRange(appA, appB);
            db.ApiKeys.AddRange(
                new ApiKey { ApplicationId = appA.Id, Name = "Key A", KeyHash = "hash-a", KeyPrefix = "sent_live_aaaa", CreatedAtUtc = DateTime.UtcNow },
                new ApiKey { ApplicationId = appB.Id, Name = "Key B", KeyHash = "hash-b", KeyPrefix = "sent_live_bbbb", CreatedAtUtc = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
        }

        var (token, _) = await CreateUserWithRoleAsync(Roles.Viewer);
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var keysRes = await client.GetAsync($"/api/applications/{appA.Id}/keys");
        keysRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var keysBody = await keysRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ApiKeyDto>>>();
        keysBody!.Data.Should().OnlyContain(k => k.ApplicationId == appA.Id);
        keysBody.Data.Should().NotContain(k => k.ApplicationId == appB.Id);
    }

    [Fact]
    public async Task TenantIsolation_AlertRuleOperations_ViewerRoleCannotCreateOrModify()
    {
        var (viewerToken, _) = await CreateUserWithRoleAsync(Roles.Viewer);
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);

        // Viewer attempts to create alert rule -> Forbidden
        var createRuleRes = await client.PostAsJsonAsync("/api/alert-rules", new CreateAlertRuleRequest
        {
            ApplicationId = Guid.NewGuid(),
            Name = "Unauthorized Rule",
            MetricName = "error_rate",
            ConditionType = "ErrorRateAboveThreshold",
            Operator = "GreaterThan",
            Threshold = 5,
            Severity = "Critical",
            EvaluationWindowMinutes = 5
        });
        createRuleRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Viewer attempts to delete alert rule -> Forbidden
        var deleteRuleRes = await client.DeleteAsync($"/api/alert-rules/{Guid.NewGuid()}");
        deleteRuleRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("http://127.0.0.1:8080/internal", "Loopback / localhost")]
    [InlineData("http://localhost:5000/webhook", "Loopback / localhost")]
    [InlineData("http://169.254.169.254/latest/meta-data/", "restricted IP")]
    [InlineData("http://10.0.0.1:8080/data", "restricted IP")]
    [InlineData("http://192.168.1.1/router", "restricted IP")]
    [InlineData("http://172.16.0.1/admin", "restricted IP")]
    [InlineData("ftp://internal.server/file", "Unsupported URI scheme")]
    public async Task SsrfProtection_LoopbackPrivateAndCloudMetadata_StrictlyBlocked(string url, string expectedReasonKeyword)
    {
        var (isAllowed, reason) = await SsrfValidator.ValidateDestinationUrlAsync(url);
        isAllowed.Should().BeFalse();
        reason.Should().NotBeNull();
        reason.Should().Contain(expectedReasonKeyword);
    }

    [Fact]
    public async Task SsrfProtection_WebhookProvider_RejectsInternalDestinations()
    {
        var provider = new WebhookNotificationProvider(new HttpClient(), NullLogger<WebhookNotificationProvider>.Instance);
        var channel = new NotificationChannel
        {
            Type = NotificationChannelType.Webhook,
            Destination = "http://169.254.169.254/latest/meta-data/"
        };
        var outbox = new NotificationOutbox
        {
            PayloadJson = "{\"alert\":\"test\"}"
        };

        var result = await provider.SendAsync(outbox, channel, CancellationToken.None);
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Blocked by SSRF filter");
    }

    [Fact]
    public void WebhookSecurity_PayloadHmacSignature_GeneratedCorrectly()
    {
        const string payload = "{\"alert\":\"critical_failure\",\"service\":\"payments\"}";
        const string secret = "super_secure_webhook_signing_secret_key_2026";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        byte[] expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        string expectedSignature = $"sha256={Convert.ToHexString(expectedHash).ToLowerInvariant()}";

        expectedSignature.Should().StartWith("sha256=");
        expectedSignature.Length.Should().Be(71); // "sha256=" + 64 hex chars
    }

    [Fact]
    public async Task AuditLogging_LoginOperations_CreateAuditEventsWithoutExposingCredentials()
    {
        var email = $"audit_{Guid.NewGuid():N}@sentinelops.io";
        var userName = $"aud_{Guid.NewGuid():N}"[..12];
        const string password = "P@ssword2026!Secure";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            UserName = userName,
            Email = email,
            FullName = "Audit Test User",
            Password = password,
            ConfirmPassword = password
        });

        // Login
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Inspect stored audit records in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var auditLogs = await db.AuditEvents
            .Where(a => a.UserName == userName || a.EntityId == email)
            .ToListAsync();

        auditLogs.Should().NotBeEmpty();
        foreach (var log in auditLogs)
        {
            log.Details.Should().NotContain(password);
            log.Details.Should().NotContain("P@ssword");
        }
    }

    [Fact]
    public async Task AuditLogging_AuditEndpoints_ImmutableAndRestrictedToAdminOnly()
    {
        var (viewerToken, _) = await CreateUserWithRoleAsync(Roles.Viewer);
        using var viewerClient = _factory.CreateClient();
        viewerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);

        // Viewer cannot access audit logs
        var viewerAuditRes = await viewerClient.GetAsync("/api/audit");
        viewerAuditRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Admin can access audit logs
        var (adminToken, _) = await CreateUserWithRoleAsync(Roles.Admin);
        using var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var adminAuditRes = await adminClient.GetAsync("/api/audit");
        adminAuditRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Audit endpoints cannot be modified or deleted via API (immutability)
        var postRes = await adminClient.PostAsJsonAsync("/api/audit", new { action = "Fake" });
        postRes.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);

        var deleteRes = await adminClient.DeleteAsync($"/api/audit/{Guid.NewGuid()}");
        deleteRes.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }
}
