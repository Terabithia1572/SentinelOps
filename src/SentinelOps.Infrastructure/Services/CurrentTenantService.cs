using Microsoft.AspNetCore.Http;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Infrastructure.Services;

public class CurrentTenantService : ICurrentTenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public const string OrganizationIdItemKey = "OrganizationId";
    public const string OrganizationRoleItemKey = "OrganizationRole";
    public const string OrganizationHeaderKey = "X-Organization-Id";

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? OrganizationId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            // 1. Check HttpContext.Items
            if (context.Items.TryGetValue(OrganizationIdItemKey, out var itemVal) && itemVal is Guid orgId && orgId != Guid.Empty)
            {
                return orgId;
            }

            // 2. Check X-Organization-Id request header
            if (context.Request.Headers.TryGetValue(OrganizationHeaderKey, out var headerVal) &&
                Guid.TryParse(headerVal, out var parsedHeaderGuid) && parsedHeaderGuid != Guid.Empty)
            {
                return parsedHeaderGuid;
            }

            // 3. Check Claims
            var claimVal = context.User.FindFirst("org_id")?.Value;
            if (Guid.TryParse(claimVal, out var parsedClaimGuid) && parsedClaimGuid != Guid.Empty)
            {
                return parsedClaimGuid;
            }

            return null;
        }
    }

    public string? OrganizationRole
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            if (context.Items.TryGetValue(OrganizationRoleItemKey, out var itemVal) && itemVal is string roleStr)
            {
                return roleStr;
            }

            return context.User.FindFirst("org_role")?.Value;
        }
    }

    public bool HasActiveOrganization => OrganizationId.HasValue && OrganizationId.Value != Guid.Empty;

    public bool IsInRole(string role)
    {
        var currentRole = OrganizationRole;
        if (string.IsNullOrWhiteSpace(currentRole)) return false;

        if (string.Equals(currentRole, role, StringComparison.OrdinalIgnoreCase)) return true;

        if (Enum.TryParse<OrganizationRole>(currentRole, true, out var currentEnum) &&
            Enum.TryParse<OrganizationRole>(role, true, out var requiredEnum))
        {
            return currentEnum >= requiredEnum;
        }

        return false;
    }
}
