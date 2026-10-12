using Microsoft.AspNetCore.Identity;

namespace SentinelOps.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
