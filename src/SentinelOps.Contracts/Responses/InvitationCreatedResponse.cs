namespace SentinelOps.Contracts.Responses;

public class InvitationCreatedResponse
{
    public Guid InvitationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string RawToken { get; set; } = string.Empty;
}
