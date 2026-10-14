using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SentinelOps.Application.Common.Interfaces;
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

public class PaymentProviderAndWebhookIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private const string WebhookSecret = "whsec_sandbox_test_secret_key_12345";

    public PaymentProviderAndWebhookIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateCheckoutSession_ReturnsValidSessionWithSimulatedUrl()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"pay-{unique}@sentinelops.test";
        var password = "Password123!";

        var (token, orgId) = await SetupUserAndOrgAsync(client, email, password, unique);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var res = await client.PostAsJsonAsync("/api/billing/checkout-session", new CreateCheckoutSessionRequest
        {
            TargetTier = "Pro",
            BillingInterval = "Monthly",
            SuccessUrl = "https://app.sentinelops.local/billing/success",
            CancelUrl = "https://app.sentinelops.local/billing/cancel"
        });

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await res.Content.ReadFromJsonAsync<ApiResponse<CheckoutSessionDto>>();
        data.Should().NotBeNull();
        data!.Data!.SessionId.Should().StartWith("cs_test_");
        data.Data.Provider.Should().Be("Sandbox");
        data.Data.CheckoutUrl.Should().Contain("session_id=cs_test_");
    }

    [Fact]
    public async Task HandleWebhook_CheckoutSessionCompleted_ActivatesSubscription()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"wh-cs-{unique}@sentinelops.test";
        var password = "Password123!";

        var (token, orgId) = await SetupUserAndOrgAsync(client, email, password, unique);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var eventId = $"evt_cs_{unique}";
        var payload = JsonSerializer.Serialize(new
        {
            id = eventId,
            type = "checkout.session.completed",
            data = new
            {
                @object = new
                {
                    id = $"cs_{unique}",
                    metadata = new
                    {
                        organizationId = orgId.ToString(),
                        tier = "Pro"
                    },
                    subscription = $"sub_stripe_{unique}"
                }
            }
        });

        var verifier = _factory.Services.GetRequiredService<IPaymentWebhookVerifier>();
        var signature = verifier.ComputeSignatureHeader(payload, WebhookSecret);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/billing/webhooks/Sandbox")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", signature);

        var webhookRes = await client.SendAsync(request);
        webhookRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var receipt = await webhookRes.Content.ReadFromJsonAsync<WebhookEventReceiptDto>();
        receipt!.Received.Should().BeTrue();
        receipt.IsDuplicate.Should().BeFalse();

        // Verify subscription updated
        var subRes = await client.GetAsync("/api/subscriptions/current");
        subRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var subData = await subRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationSubscriptionDto>>();
        subData!.Data!.Status.Should().Be("Active");
        subData.Data.PlanTier.Should().Be("Pro");
    }

    [Fact]
    public async Task HandleWebhook_InvoicePaymentSucceeded_MarksInvoicePaidAndRecordsTransaction()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"wh-inv-{unique}@sentinelops.test";
        var password = "Password123!";

        var (token, orgId) = await SetupUserAndOrgAsync(client, email, password, unique);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var invoiceId = Guid.NewGuid();
        var invoiceNum = $"INV-WH-{unique}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var invoice = new SaaSInvoice
            {
                Id = invoiceId,
                OrganizationId = orgId,
                InvoiceNumber = invoiceNum,
                Status = InvoiceStatus.Open,
                AmountDue = 49.00m,
                AmountPaid = 0m,
                Currency = "USD",
                BillingReason = "SubscriptionCycle",
                DueDateUtc = DateTime.UtcNow.AddDays(7),
                PeriodStartUtc = DateTime.UtcNow,
                PeriodEndUtc = DateTime.UtcNow.AddMonths(1)
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();
        }

        var eventId = $"evt_inv_paid_{unique}";
        var payload = JsonSerializer.Serialize(new
        {
            id = eventId,
            type = "invoice.payment_succeeded",
            data = new
            {
                @object = new
                {
                    number = invoiceNum,
                    amount_paid = 49.00m,
                    currency = "usd",
                    metadata = new { organizationId = orgId.ToString() }
                }
            }
        });

        var verifier = _factory.Services.GetRequiredService<IPaymentWebhookVerifier>();
        var signature = verifier.ComputeSignatureHeader(payload, WebhookSecret);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/billing/webhooks/Sandbox")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", signature);

        var webhookRes = await client.SendAsync(request);
        webhookRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify invoice is now Paid with a transaction
        var getInvoiceRes = await client.GetAsync($"/api/billing/invoices/{invoiceId}");
        getInvoiceRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var invData = await getInvoiceRes.Content.ReadFromJsonAsync<ApiResponse<SaaSInvoiceDto>>();
        invData!.Data!.Status.Should().Be("Paid");
        invData.Data.AmountPaid.Should().Be(49.00m);
        invData.Data.Transactions.Should().ContainSingle(t => t.PaymentProviderTransactionId == eventId);
    }

    [Fact]
    public async Task HandleWebhook_InvoicePaymentFailed_TransitionsActiveToPastDueWithGracePeriod()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"wh-fail-{unique}@sentinelops.test";
        var password = "Password123!";

        var (token, orgId) = await SetupUserAndOrgAsync(client, email, password, unique);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        // Upgrade to Active first
        await client.PostAsJsonAsync("/api/subscriptions/change-plan", new ChangePlanRequest
        {
            TargetTier = "Pro",
            BillingInterval = "Monthly",
            Immediate = true
        });

        var eventId = $"evt_fail_{unique}";
        var payload = JsonSerializer.Serialize(new
        {
            id = eventId,
            type = "invoice.payment_failed",
            data = new
            {
                @object = new
                {
                    metadata = new { organizationId = orgId.ToString() }
                }
            }
        });

        var verifier = _factory.Services.GetRequiredService<IPaymentWebhookVerifier>();
        var signature = verifier.ComputeSignatureHeader(payload, WebhookSecret);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/billing/webhooks/Sandbox")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", signature);

        var webhookRes = await client.SendAsync(request);
        webhookRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify subscription is now PastDue with GracePeriod
        var subRes = await client.GetAsync("/api/subscriptions/current");
        var subData = await subRes.Content.ReadFromJsonAsync<ApiResponse<OrganizationSubscriptionDto>>();
        subData!.Data!.Status.Should().Be("PastDue");
        subData.Data.GracePeriodEndsAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleWebhook_DuplicateEvent_IsIdempotentAndReturns200WithoutDuplicating()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"wh-dup-{unique}@sentinelops.test";
        var password = "Password123!";

        var (token, orgId) = await SetupUserAndOrgAsync(client, email, password, unique);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var eventId = $"evt_dup_{unique}";
        var payload = JsonSerializer.Serialize(new
        {
            id = eventId,
            type = "checkout.session.completed",
            data = new
            {
                @object = new
                {
                    metadata = new { organizationId = orgId.ToString(), tier = "Pro" }
                }
            }
        });

        var verifier = _factory.Services.GetRequiredService<IPaymentWebhookVerifier>();
        var signature = verifier.ComputeSignatureHeader(payload, WebhookSecret);

        // First delivery
        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/billing/webhooks/Sandbox")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        req1.Headers.Add("Stripe-Signature", signature);
        var res1 = await client.SendAsync(req1);
        res1.StatusCode.Should().Be(HttpStatusCode.OK);
        var receipt1 = await res1.Content.ReadFromJsonAsync<WebhookEventReceiptDto>();
        receipt1!.IsDuplicate.Should().BeFalse();

        // Second delivery (duplicate replay)
        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/billing/webhooks/Sandbox")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        req2.Headers.Add("Stripe-Signature", signature);
        var res2 = await client.SendAsync(req2);
        res2.StatusCode.Should().Be(HttpStatusCode.OK);
        var receipt2 = await res2.Content.ReadFromJsonAsync<WebhookEventReceiptDto>();
        receipt2!.IsDuplicate.Should().BeTrue();
    }

    [Fact]
    public async Task HandleWebhook_ForgedSignature_Returns400BadRequest()
    {
        var client = _factory.CreateClient();
        var payload = "{\"id\":\"evt_forged\",\"type\":\"checkout.session.completed\"}";

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/billing/webhooks/Sandbox")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        request.Headers.Add("Stripe-Signature", $"t={ts},v1=badbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadbadbad");

        var res = await client.SendAsync(request);
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HandleWebhook_ExpiredTimestamp_Returns400BadRequest()
    {
        var client = _factory.CreateClient();
        var payload = "{\"id\":\"evt_expired\",\"type\":\"checkout.session.completed\"}";

        var verifier = _factory.Services.GetRequiredService<IPaymentWebhookVerifier>();
        var expiredTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 400; // > 300s tolerance
        var signature = verifier.ComputeSignatureHeader(payload, WebhookSecret, expiredTs);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/billing/webhooks/Sandbox")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", signature);

        var res = await client.SendAsync(request);
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefundTransaction_AdminOnly_ProcessesRefundAndAdjustsInvoice()
    {
        var client = _factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var adminEmail = $"admin-ref-{unique}@sentinelops.test";
        var userEmail = $"user-ref-{unique}@sentinelops.test";
        var password = "Password123!";
        var adminUserId = Guid.NewGuid().ToString();
        var normalUserId = Guid.NewGuid().ToString();

        Guid orgId;
        Guid invoiceId;
        string txnProviderId = $"txn_paid_{unique}";

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
            var org = new Organization { Name = $"Refund Org {unique}", Slug = $"ref-org-{unique}", IsActive = true };
            db.Organizations.Add(org);

            db.OrganizationMembers.Add(new OrganizationMember { OrganizationId = org.Id, UserId = adminUserId, Role = OrganizationRole.Owner });
            db.OrganizationMembers.Add(new OrganizationMember { OrganizationId = org.Id, UserId = normalUserId, Role = OrganizationRole.Member });

            var invoice = new SaaSInvoice
            {
                OrganizationId = org.Id,
                InvoiceNumber = $"INV-REF-{unique}",
                Status = InvoiceStatus.Paid,
                AmountDue = 100.00m,
                AmountPaid = 100.00m,
                Currency = "USD",
                BillingReason = "SubscriptionCycle",
                PaidAtUtc = DateTime.UtcNow
            };
            var txn = new PaymentTransaction
            {
                OrganizationId = org.Id,
                InvoiceId = invoice.Id,
                Status = PaymentTransactionStatus.Succeeded,
                Amount = 100.00m,
                Currency = "USD",
                PaymentProvider = "Sandbox",
                PaymentProviderTransactionId = txnProviderId
            };
            invoice.Transactions.Add(txn);
            db.Invoices.Add(invoice);
            db.PaymentTransactions.Add(txn);

            await db.SaveChangesAsync();

            orgId = org.Id;
            invoiceId = invoice.Id;
        }

        // 1. Normal user tries refund -> 403 Forbidden
        var loginResNormal = await client.PostAsJsonAsync("/api/auth/login", new { Email = userEmail, Password = password });
        var normalAuth = await loginResNormal.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", normalAuth!.Data!.Token);
        client.DefaultRequestHeaders.Add("X-Organization-Id", orgId.ToString());

        var refundNormalRes = await client.PostAsJsonAsync($"/api/billing/transactions/{txnProviderId}/refund", new RefundTransactionRequest
        {
            Amount = 50.00m,
            Reason = "Unauthorized refund attempt"
        });
        refundNormalRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 2. Admin user processes refund -> 200 OK
        var loginResAdmin = await client.PostAsJsonAsync("/api/auth/login", new { Email = adminEmail, Password = password });
        var adminAuth = await loginResAdmin.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.Data!.Token);

        var refundAdminRes = await client.PostAsJsonAsync($"/api/billing/transactions/{txnProviderId}/refund", new RefundTransactionRequest
        {
            Amount = 50.00m,
            Reason = "Customer requested partial refund"
        });
        refundAdminRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var refundData = await refundAdminRes.Content.ReadFromJsonAsync<ApiResponse<RefundResultDto>>();
        refundData!.Data!.Success.Should().BeTrue();
        refundData.Data.RefundId.Should().StartWith("re_test_");
        refundData.Data.Amount.Should().Be(50.00m);

        // 3. Verify invoice adjusted: AmountPaid decreased to 50, Status back to Open
        var getInvoiceRes = await client.GetAsync($"/api/billing/invoices/{invoiceId}");
        var invData = await getInvoiceRes.Content.ReadFromJsonAsync<ApiResponse<SaaSInvoiceDto>>();
        invData!.Data!.AmountPaid.Should().Be(50.00m);
        invData.Data.Status.Should().Be("Open");
        invData.Data.Transactions.Should().HaveCount(2);
    }

    private async Task<(string Token, Guid OrgId)> SetupUserAndOrgAsync(HttpClient client, string email, string password, string unique)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = email,
                Email = email,
                FullName = "Payment Test User"
            };
            await userManager.CreateAsync(user, password);
        }

        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        var authData = await loginRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var token = authData!.Data!.Token;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var provisionRes = await client.PostAsJsonAsync("/api/onboarding/provision", new OnboardingProvisionRequest
        {
            OrganizationName = $"Pay Org {unique}",
            PlanTier = "Starter",
            ApplicationName = $"app-pay-{unique}"
        });
        var provisioned = await provisionRes.Content.ReadFromJsonAsync<ApiResponse<OnboardingProvisionedResponse>>();
        return (token, provisioned!.Data!.OrganizationId);
    }
}
