using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Domain.Constants;
using SentinelOps.Infrastructure.Services;

namespace SentinelOps.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApplicationDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub");

            if (!string.IsNullOrEmpty(userId))
            {
                var isAdmin = context.User.IsInRole(Roles.Admin);

                // Fetch memberships for this user
                var memberships = await dbContext.OrganizationMembers
                    .AsNoTracking()
                    .Where(m => m.UserId == userId)
                    .Select(m => new { m.OrganizationId, m.Role })
                    .ToListAsync(context.RequestAborted);

                Guid? targetOrgId = null;
                string? targetRole = null;

                // 1. Check if client requested a specific organization via header
                if (context.Request.Headers.TryGetValue(CurrentTenantService.OrganizationHeaderKey, out var headerVal) &&
                    Guid.TryParse(headerVal, out var requestedOrgId) && requestedOrgId != Guid.Empty)
                {
                    var userMembership = memberships.FirstOrDefault(m => m.OrganizationId == requestedOrgId);
                    if (userMembership != null)
                    {
                        targetOrgId = requestedOrgId;
                        targetRole = userMembership.Role.ToString();
                    }
                    else if (isAdmin)
                    {
                        targetOrgId = requestedOrgId;
                        targetRole = "Admin";
                    }
                    else
                    {
                        _logger.LogWarning("Cross-tenant spoofing attempt blocked: User {UserId} requested Organization {OrgId} without membership.", userId, requestedOrgId);
                    }
                }

                // 2. Check if claim contains org_id
                if (!targetOrgId.HasValue)
                {
                    var claimOrg = context.User.FindFirst("org_id")?.Value;
                    if (Guid.TryParse(claimOrg, out var claimOrgGuid) && claimOrgGuid != Guid.Empty)
                    {
                        var claimMembership = memberships.FirstOrDefault(m => m.OrganizationId == claimOrgGuid);
                        if (claimMembership != null || isAdmin)
                        {
                            targetOrgId = claimOrgGuid;
                            targetRole = claimMembership?.Role.ToString() ?? "Admin";
                        }
                    }
                }

                // 3. Fall back to user's first authorized organization
                if (!targetOrgId.HasValue && memberships.Count > 0)
                {
                    targetOrgId = memberships[0].OrganizationId;
                    targetRole = memberships[0].Role.ToString();
                }

                if (targetOrgId.HasValue)
                {
                    context.Items[CurrentTenantService.OrganizationIdItemKey] = targetOrgId.Value;
                    if (!string.IsNullOrEmpty(targetRole))
                    {
                        context.Items[CurrentTenantService.OrganizationRoleItemKey] = targetRole;
                    }
                }
            }
        }

        await _next(context);
    }
}
