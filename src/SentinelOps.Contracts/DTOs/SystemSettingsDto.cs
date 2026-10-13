namespace SentinelOps.Contracts.DTOs;

public class SystemSettingsDto
{
    public string PlatformName { get; set; } = "SentinelOps Enterprise";
    public string EnvironmentName { get; set; } = "Production";
    public bool PublicRegistrationEnabled { get; set; } = true;
    public int AlertEvaluationIntervalSeconds { get; set; } = 30;
    public int MaxNotificationRetries { get; set; } = 5;
    public int WebhookTimeoutSeconds { get; set; } = 10;
    public string SystemNoticeBanner { get; set; } = string.Empty;
    public bool SystemNoticeBannerActive { get; set; } = false;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = "system";
}

public class UpdateSystemSettingsRequest
{
    public string PlatformName { get; set; } = "SentinelOps Enterprise";
    public bool PublicRegistrationEnabled { get; set; } = true;
    public int AlertEvaluationIntervalSeconds { get; set; } = 30;
    public int MaxNotificationRetries { get; set; } = 5;
    public int WebhookTimeoutSeconds { get; set; } = 10;
    public string SystemNoticeBanner { get; set; } = string.Empty;
    public bool SystemNoticeBannerActive { get; set; } = false;
}
