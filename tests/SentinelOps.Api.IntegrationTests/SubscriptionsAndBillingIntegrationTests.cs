using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Contracts.Requests;
using SentinelOps.Contracts.Responses;
using SentinelOps.Domain.Constants;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using SentinelOps.Infrastructure.Identity;
using SentinelOps.Infrastructure.Persistence;
using Xunit;

namespace SentinelOps.Api.IntegrationTests;

public class SubscriptionsAndBillingIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SubscriptionsAndBillingIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPlans_PublicEndpoint_ReturnsActivePlansWithEntitlements()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync("/api/subscriptions/plans");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await res.Content.ReadFromJsonAsync<ApiResponse<List<SubscriptionPlanDto>>>();
        data.Should().NotBeNull();
        data!.Data.Should().NotBeEmpty();

        var tiers = data.Data.Select(p => p.Tier).ToList();
        tiers.Should().Contain(["Starter", "Pro", "Enterprise"]);

        var pro = data.Data.First(p => p.Tier == "Pro");
        pro.MaxApplications.Should().Be(20);
        pro.MaxMonthlyLogs.Should().Be(1_000_000);
        pro.MonthlyPriceUsd.Should().Be(49m);
        pro.AllowCustomReports.Should().BeTrue();
    }

    [Fact]
    public async Task SubscriptionLifecycle_ChangePlan_Cancel_Reactivate_WorksIdempotently()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"sub-{unique}@sentinelops.test";
        var password = "Password123!";
        var userId = Guid.NewGuid().ToString();

        // 1. Create User & Provision Tenant
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                Id = userId,
                UserName = email,
                Email = email,
                FullName = "Subscription User"
            };
            await userManager.CreateAsync(user, password);
        }

        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        var authData = await loginRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var token = authData!.Data!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var provisionRes = await client.PostAsJsonAsync("/api/onboarding/provision", new OnboardingProvisionRequest
        {
            OrganizationName = $"Sub Org {unique}",
            PlanTier = "Starter",
            ApplicationName = $"app-{unique}"
        });
        var provisioned = await provisionRes.Content.ReadFromJsonAsync<ApiResponse<OnboardingProvisionedResponse>>();
        var orgId = provisioned!.Data!.OrganizationId;

        client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        // 2. Query Current Subscription
        var getSubRes = await client.GetAsync("/api/subscriptions/current");
        getSubRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var sub = await getSubRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationSubscriptionDto>>();
        sub!.Data!.PlanTier.Should().Be("Starter");
        sub.Data.Status.Should().Be("Trialing");

        // 3. Upgrade Plan to Pro
        var upgradeRes = await client.PostAsJsonAsync("/api/subscriptions/change-plan", new ChangePlanRequest
        {
            TargetTier = "Pro",
            BillingInterval = "Monthly",
            Immediate = true
        });
        upgradeRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var upgraded = await upgradeRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationSubscriptionDto>>();
        upgraded!.Data!.PlanTier.Should().Be("Pro");
        upgraded.Data.Status.Should().Be("Active");
        upgraded.Data.History.Should().NotBeEmpty();

        // 4. Cancel Subscription (Period End)
        var cancelRes = await client.PostAsJsonAsync("/api/subscriptions/cancel", new CancelSubscriptionRequest
        {
            Reason = "Testing cancellation flow",
            Immediate = false
        });
        cancelRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var checkCancelRes = await client.GetAsync("/api/subscriptions/current");
        var checkCancel = await checkCancelRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationSubscriptionDto>>();
        checkCancel!.Data!.CancelAtPeriodEnd.Should().BeTrue();

        // 5. Reactivate Subscription
        var reactivateRes = await client.PostAsync("/api/subscriptions/reactivate", null);
        reactivateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var checkReactivateRes = await client.GetAsync("/api/subscriptions/current");
        var checkReactivate = await checkReactivateRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationSubscriptionDto>>();
        checkReactivate!.Data!.CancelAtPeriodEnd.Should().BeFalse();
        checkReactivate.Data.Status.Should().Be("Active");
    }

    [Fact]
    public async Task BillingAndInvoicing_AdministrativeSettlement_AuditedAndEnforcesAdminRole()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var adminEmail = $"admin-{unique}@sentinelops.test";
        var userEmail = $"user-{unique}@sentinelops.test";
        var password = "Password123!";
        var adminUserId = Guid.NewGuid().ToString();
        var normalUserId = Guid.NewGuid().ToString();

        Guid orgId;
        Guid invoiceId;

        // 1. Seed admin and normal user
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (!await roleManager.RoleExistsAsync(Roles.Admin))
            {
                await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
            }

            var adminUser = new ApplicationUser { Id = adminUserId, UserName = adminEmail, Email = adminEmail, FullName = "Admin User" };
            await userManager.CreateAsync(adminUser, password);
            await userManager.AddToRoleAsync(adminUser, Roles.Admin);

            var normalUser = new ApplicationUser { Id = normalUserId, UserName = userEmail, Email = userEmail, FullName = "Normal User" };
            await userManager.CreateAsync(normalUser, password);

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var org = new Organization
            {
                Name = $"Invoice Org {unique}",
                Slug = $"inv-org-{unique}",
                IsActive = true
            };
            db.Organizations.Add(org);

            db.OrganizationMembers.Add(new OrganizationMember
            {
                OrganizationId = org.Id,
                UserId = adminUserId,
                Role = OrganizationRole.Owner
            });
            db.OrganizationMembers.Add(new OrganizationMember
            {
                OrganizationId = org.Id,
                UserId = normalUserId,
                Role = OrganizationRole.Member
            });

            var invoice = new SaaSInvoice
            {
                OrganizationId = org.Id,
                InvoiceNumber = $"INV-TEST-{unique}",
                Status = InvoiceStatus.Open,
                AmountDue = 149.00m,
                AmountPaid = 0m,
                Currency = "USD",
                PeriodStartUtc = DateTime.UtcNow,
                PeriodEndUtc = DateTime.UtcNow.AddMonths(1),
                DueDateUtc = DateTime.UtcNow.AddDays(14),
                BillingReason = "SubscriptionCycle"
            };
            invoice.LineItems.Add(new InvoiceLineItem
            {
                InvoiceId = invoice.Id,
                Description = "Professional Plan (Monthly)",
                Quantity = 1,
                UnitPrice = 149.00m,
                Amount = 149.00m
            });
            db.Invoices.Add(invoice);

            await db.SaveChangesAsync();

            orgId = org.Id;
            invoiceId = invoice.Id;
        }

        // 2. Normal user tries to settle invoice -> Forbidden (403)
        var loginResNormal = await client.PostAsJsonAsync("/api/auth/login", new { Email = userEmail, Password = password });
        var normalAuth = await loginResNormal.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", normalAuth!.Data!.Token);
        client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var settleNormalRes = await client.PostAsJsonAsync($"/api/billing/invoices/{invoiceId}/settle-admin", new SettleInvoiceRequest
        {
            Reason = "Unauthorized settlement attempt",
            AmountSettled = 149.00m
        });
        settleNormalRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. Admin user settles invoice -> Success (200)
        var loginResAdmin = await client.PostAsJsonAsync("/api/auth/login", new { Email = adminEmail, Password = password });
        var adminAuth = await loginResAdmin.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.Data!.Token);

        var settleAdminRes = await client.PostAsJsonAsync($"/api/billing/invoices/{invoiceId}/settle-admin", new SettleInvoiceRequest
        {
            Reason = "Bank Wire transfer verified by finance",
            PaymentReference = "WIRE-987654",
            AmountSettled = 149.00m
        });
        settleAdminRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Verify invoice is Paid with an audited transaction record
        var getInvoiceRes = await client.GetAsync($"/api/billing/invoices/{invoiceId}");
        getInvoiceRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var invData = await getInvoiceRes.Content.ReadFromJsonAsync<ApiResponse<SaaSInvoiceDto>>();
        invData!.Data!.Status.Should().Be("Paid");
        invData.Data.AmountPaid.Should().Be(149.00m);
        invData.Data.Transactions.Should().HaveCount(1);

        var txn = invData.Data.Transactions.First();
        txn.Status.Should().Be("Succeeded");
        txn.PaymentProvider.Should().Be("Manual");
        txn.PaymentProviderTransactionId.Should().Be("WIRE-987654");
        txn.SettledReason.Should().Be("Bank Wire transfer verified by finance");
    }
}
