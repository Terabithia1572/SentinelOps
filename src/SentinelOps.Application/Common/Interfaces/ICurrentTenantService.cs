namespace SentinelOps.Application.Common.Interfaces;

public interface ICurrentTenantService
{
    Guid? OrganizationId { get; }
    string? OrganizationRole { get; }
    bool HasActiveOrganization { get; }
    bool IsInRole(string role);
}
