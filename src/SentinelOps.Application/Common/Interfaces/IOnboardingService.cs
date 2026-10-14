using SentinelOps.Contracts.DTOs;

namespace SentinelOps.Application.Common.Interfaces;

public interface IOnboardingService
{
    Task<OnboardingStatusDto> GetStatusAsync(string userId, CancellationToken ct = default);

    Task<(bool Success, OnboardingProvisionedResponse? Response, string? Error)> ProvisionTenantAndAppAsync(
        OnboardingProvisionRequest request,
        string userId,
        CancellationToken ct = default);

    Task<FirstTelemetryVerificationResponse> VerifyFirstTelemetryAsync(
        Guid applicationId,
        string userId,
        CancellationToken ct = default);

    Task<(bool Success, string? Error)> CompleteOnboardingAsync(
        string userId,
        CancellationToken ct = default);
}
