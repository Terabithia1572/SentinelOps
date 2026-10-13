namespace SentinelOps.Contracts.Requests;

public class GetUsersRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; } // "all", "active", "inactive", "locked"
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}

public class UpdateUserRoleRequest
{
    public List<string> Roles { get; set; } = [];
}

public class UpdateUserStatusRequest
{
    public bool IsActive { get; set; } = true;
    public bool IsLocked { get; set; }
    public int? LockoutDurationMinutes { get; set; }
}

public class AdminResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class AssignApplicationMemberRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = "Viewer"; // Admin, Operator, Viewer
}
