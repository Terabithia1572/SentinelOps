using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Contracts.Responses;

namespace SentinelOps.Api.Authentication;

public class ApiKeyAuthFilter : IAsyncActionFilter
{
    public const string ApplicationIdItemKey = "ApplicationId";
    public const string ApplicationNameItemKey = "ApplicationName";

    private readonly IApplicationDbContext _dbContext;
    private readonly IApiKeyService _apiKeyService;

    public ApiKeyAuthFilter(
        IApplicationDbContext dbContext,
        IApiKeyService apiKeyService)
    {
        _dbContext = dbContext;
        _apiKeyService = apiKeyService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var rawApiKey = ExtractApiKey(context.HttpContext);
        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            context.Result = new UnauthorizedObjectResult(
                ApiResponse<object>.Fail("Missing API key. Provide 'X-API-Key' header or 'Authorization: Bearer <key>'."));
            return;
        }

        var keyHash = _apiKeyService.HashApiKey(rawApiKey);
        var apiKey = await _dbContext.ApiKeys
            .Include(k => k.Application)
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash, context.HttpContext.RequestAborted);

        if (apiKey == null || !apiKey.IsActive || !apiKey.Application.IsActive)
        {
            context.Result = new UnauthorizedObjectResult(
                ApiResponse<object>.Fail("Invalid, expired, or revoked API key."));
            return;
        }

        // Throttle LastUsedAtUtc database writes to at most once per minute
        if (!apiKey.LastUsedAtUtc.HasValue || (DateTime.UtcNow - apiKey.LastUsedAtUtc.Value).TotalMinutes >= 1)
        {
            apiKey.LastUsedAtUtc = DateTime.UtcNow;
            try
            {
                await _dbContext.SaveChangesAsync(context.HttpContext.RequestAborted);
            }
            catch
            {
                // Non-critical background metadata write failure
            }
        }

        context.HttpContext.Items[ApplicationIdItemKey] = apiKey.ApplicationId;
        context.HttpContext.Items[ApplicationNameItemKey] = apiKey.Application.Name;

        await next();
    }

    private static string? ExtractApiKey(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-API-Key", out var xApiKey) &&
            !string.IsNullOrWhiteSpace(xApiKey))
        {
            return xApiKey.ToString().Trim();
        }

        if (httpContext.Request.Headers.TryGetValue("x-api-key", out var xApiKeyLower) &&
            !string.IsNullOrWhiteSpace(xApiKeyLower))
        {
            return xApiKeyLower.ToString().Trim();
        }

        if (httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
            !string.IsNullOrWhiteSpace(authHeader))
        {
            var headerValue = authHeader.ToString().Trim();
            if (headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return headerValue["Bearer ".Length..].Trim();
            }
            if (headerValue.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
            {
                return headerValue["ApiKey ".Length..].Trim();
            }
        }

        return null;
    }
}
