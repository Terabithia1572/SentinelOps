using SentinelOps.Domain.Common;

namespace SentinelOps.Domain.Entities;

public class Organization : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    public ICollection<OrganizationInvitation> Invitations { get; set; } = new List<OrganizationInvitation>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
    public OrganizationSettings? Settings { get; set; }
    public OrganizationUsage? Usage { get; set; }
    public OrganizationSubscription? Subscription { get; set; }
    public BillingAccount? BillingAccount { get; set; }
    public ICollection<SaaSInvoice> Invoices { get; set; } = new List<SaaSInvoice>();
}
