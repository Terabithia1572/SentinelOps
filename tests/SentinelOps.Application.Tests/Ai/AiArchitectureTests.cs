using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.DTOs;
using SentinelOps.Domain.Entities;
using SentinelOps.Infrastructure.Persistence;
using SentinelOps.Infrastructure.Services.Ai;
using Xunit;

namespace SentinelOps.Application.Tests.Ai;

public class AiArchitectureTests
{
    private readonly ApplicationDbContext _context;

    public AiArchitectureTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task DisabledAiProvider_AlwaysReturnsDisabledResult()
    {
        var provider = new DisabledAiProvider();

        provider.ProviderId.Should().Be("Disabled");
        provider.IsEnabled.Should().BeFalse();

        var completion = await provider.CompleteAsync(new AiCompletionRequest
        {
            Prompt = "Test prompt"
        });

        completion.IsSuccessful.Should().BeFalse();
        completion.ErrorMessage.Should().Contain("disabled");

        var chat = await provider.ChatAsync(new AiCopilotChatRequest
        {
            Message = "How do I fix this?"
        });

        chat.Reply.Should().Contain("disabled");
        chat.RequiresHumanApproval.Should().BeTrue();
    }

    [Fact]
    public async Task DeterministicAiProvider_ScrubsSensitiveSecrets_InPromptAndLogs()
    {
        var provider = new DeterministicAiProvider();
        provider.IsEnabled.Should().BeTrue();

        var rawMessage = "Incident with token Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.xyz and key sent_live_secret1234567890 and password = 'SuperSecret123!'";
        var scrubbed = DeterministicAiProvider.ScrubSensitiveData(rawMessage);

        scrubbed.Should().NotContain("sent_live_secret1234567890");
        scrubbed.Should().NotContain("SuperSecret123!");
        scrubbed.Should().Contain("[REDACTED_API_KEY]");
        scrubbed.Should().Contain("Bearer [REDACTED_TOKEN]");
        scrubbed.Should().Contain("[REDACTED_SECRET]");

        var completion = await provider.CompleteAsync(new AiCompletionRequest
        {
            Prompt = rawMessage
        });

        completion.IsSuccessful.Should().BeTrue();
        completion.IsDeterministic.Should().BeTrue();
        completion.TokensUsed.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExternalLlmProvider_WhenUnconfigured_FailsGracefullyWithoutNetworkCall()
    {
        var options = Options.Create(new AiOptions
        {
            Enabled = true,
            DefaultProvider = "ExternalLlm",
            ExternalProvider = new ExternalLlmOptions
            {
                Endpoint = "", // Unconfigured
                ApiKey = ""
            }
        });

        var usageMeter = Substitute.For<IAiUsageMeter>();
        var httpClient = new HttpClient();
        var provider = new ExternalLlmProvider(httpClient, options, usageMeter, NullLogger<ExternalLlmProvider>.Instance);

        provider.IsEnabled.Should().BeFalse();

        var result = await provider.CompleteAsync(new AiCompletionRequest
        {
            OrganizationId = Guid.NewGuid(),
            Prompt = "Analyze incident"
        });

        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("External LLM is not configured");
        result.IsDeterministic.Should().BeFalse();
    }

    [Fact]
    public async Task AiUsageMeter_BlocksExecution_WhenOrganizationQuotaExceeded()
    {
        var org = new Organization { Name = "Test Org", Slug = "test-org" };
        var orgId = org.Id;
        var settings = new OrganizationSettings
        {
            OrganizationId = orgId,
            IsAiEnabled = true,
            MaxMonthlyAiTokens = 5000
        };
        var usage = new OrganizationUsage
        {
            OrganizationId = orgId,
            AiTokensUsedMonthToDate = 4500,
            AiInvocationsMonthToDate = 10
        };

        _context.Organizations.Add(org);
        _context.OrganizationSettings.Add(settings);
        _context.OrganizationUsages.Add(usage);
        await _context.SaveChangesAsync();

        var meter = new AiUsageMeter(_context, NullLogger<AiUsageMeter>.Instance);

        // 4500 + 400 <= 5000 -> allowed
        var canExecuteSmall = await meter.CanExecuteAiOperationAsync(orgId, 400);
        canExecuteSmall.Should().BeTrue();

        // 4500 + 1000 > 5000 -> blocked
        var canExecuteLarge = await meter.CanExecuteAiOperationAsync(orgId, 1000);
        canExecuteLarge.Should().BeFalse();

        // Check usage report
        var report = await meter.GetUsageAsync(orgId);
        report.TokensUsedMonthToDate.Should().Be(4500);
        report.RemainingTokens.Should().Be(500);
        report.IsQuotaExceeded.Should().BeFalse();
    }

    [Fact]
    public async Task AiUsageMeter_BlocksExecution_WhenAiIsDisabledForOrganization()
    {
        var org = new Organization { Name = "Opt-Out Org", Slug = "opt-out-org" };
        var orgId = org.Id;
        var settings = new OrganizationSettings
        {
            OrganizationId = orgId,
            IsAiEnabled = false,
            MaxMonthlyAiTokens = 100000
        };

        _context.Organizations.Add(org);
        _context.OrganizationSettings.Add(settings);
        await _context.SaveChangesAsync();

        var meter = new AiUsageMeter(_context, NullLogger<AiUsageMeter>.Instance);

        var canExecute = await meter.CanExecuteAiOperationAsync(orgId, 100);
        canExecute.Should().BeFalse();
    }
}
