namespace SentinelOps.Contracts.Requests;

public class CreateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class InviteMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
}

public class AcceptInvitationRequest
{
    public string Token { get; set; } = string.Empty;
}

public class ChangeMemberRoleRequest
{
    public string Role { get; set; } = "Member";
}

public class UpdateOrganizationSettingsRequest
{
    public bool AllowMemberInvites { get; set; } = true;
    public bool RequireTwoFactor { get; set; }
    public int DefaultRetentionDays { get; set; } = 30;
}
