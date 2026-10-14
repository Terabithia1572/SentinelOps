using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Responses;
using SentinelOps.Infrastructure.Identity;
using SentinelOps.Infrastructure.Persistence;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class OnboardingControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OnboardingControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OnboardingWorkflow_EndToEnd_ProvisionsTenantSubscriptionAndApiKey()
    {
        // 1. Arrange a fresh test user
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"onboard-{unique}@sentinelops.test";
        var password = "Password123!";
        var userId = Guid.NewGuid().ToString();

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                Id = userId,
                UserName = email,
                Email = email,
                FullName = "Onboard Test User"
            };
            var createResult = await userManager.CreateAsync(user, password);
            createResult.Succeeded.Should().BeTrue();
        }

        // Login to get JWT
        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var authData = await loginRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        authData.Should().NotBeNull();
        var token = authData!.Data.Token;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Query initial status -> should be on "Organization" step
        var statusRes = await client.GetAsync("/api/onboarding/status");
        statusRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await statusRes.Content.ReadFromJsonAsync<ApiResponse<OnboardingStatusDto>>();
        status!.Data.IsCompleted.Should().BeFalse();
        status.Data.CurrentStep.Should().Be("Organization");

        // 3. Provision Organization, Application, Subscription, and Ingestion Key
        var provisionReq = new OnboardingProvisionRequest
        {
            OrganizationName = $"Acme Corp {unique}",
            OrganizationSlug = $"acme-{unique}",
            PlanTier = "Pro",
            ApplicationName = $"order-service-{unique}",
            ApplicationDisplayName = "Order Service",
            EnvironmentName = "Production"
        };

        var provisionRes = await client.PostAsJsonAsync("/api/onboarding/provision", provisionReq);
        provisionRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var provisioned = await provisionRes.Content.ReadFromJsonAsync<ApiResponse<OnboardingProvisionedResponse>>();
        provisioned.Should().NotBeNull();
        provisioned!.Data.OrganizationId.Should().NotBeEmpty();
        provisioned.Data.ApplicationId.Should().NotBeEmpty();
        provisioned.Data.RawApiKey.Should().StartWith("sent_live_");
        provisioned.Data.PlanTier.Should().Be("Pro");
        provisioned.Data.SubscriptionStatus.Should().Be("Trialing");
        provisioned.Data.SdkSnippetCSharp.Should().Contain("AddOpenTelemetry");

        // 4. Verify First Telemetry (initially false)
        var verifyRes = await client.GetAsync($"/api/onboarding/verify/{provisioned.Data.ApplicationId}");
        verifyRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var verification = await verifyRes.Content.ReadFromJsonAsync<ApiResponse<FirstTelemetryVerificationResponse>>();
        verification!.Data.HasReceivedTelemetry.Should().BeFalse();
        verification.Data.LogsCount.Should().Be(0);

        // 5. Complete Onboarding
        var completeRes = await client.PostAsync("/api/onboarding/complete", null);
        completeRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 6. Verify in Database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var org = await db.Organizations.FindAsync(provisioned.Data.OrganizationId);
            org.Should().NotBeNull();
            org!.Name.Should().Be(provisionReq.OrganizationName);

            var billing = db.BillingAccounts.FirstOrDefault(b => b.OrganizationId == org.Id);
            billing.Should().NotBeNull();
            billing!.BillingEmail.Should().Be(email);

            var sub = db.OrganizationSubscriptions.FirstOrDefault(s => s.OrganizationId == org.Id);
            sub.Should().NotBeNull();
            sub!.Status.Should().Be(SentinelOps.Domain.Enums.SubscriptionStatus.Trialing);
        }
    }
}
