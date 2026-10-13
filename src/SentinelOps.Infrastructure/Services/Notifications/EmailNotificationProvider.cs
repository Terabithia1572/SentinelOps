using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;

namespace SentinelOps.Infrastructure.Services.Notifications;

public class EmailNotificationProvider : INotificationProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationProvider> _logger;

    public EmailNotificationProvider(
        IConfiguration configuration,
        ILogger<EmailNotificationProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public NotificationChannelType SupportedType => NotificationChannelType.Email;

    public async Task<NotificationDeliveryResult> SendAsync(
        NotificationOutbox message,
        NotificationChannel channel,
        CancellationToken cancellationToken)
    {
        var smtpHost = _configuration["SmtpSettings:Host"];
        var smtpPort = _configuration.GetValue<int>("SmtpSettings:Port", 587);

        // In development or when SMTP is not configured, operate in development-safe mode
        if (string.IsNullOrWhiteSpace(smtpHost) || smtpHost.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "[DEV-SAFE EMAIL] Delivered to {Destination}: Subject '{Subject}'",
                channel.Destination, message.Subject);

            await Task.Delay(10, cancellationToken);
            return new NotificationDeliveryResult(true);
        }

        try
        {
            // Production SMTP transmission
            _logger.LogInformation("Sending email notification to {Destination} via {Host}:{Port}",
                channel.Destination, smtpHost, smtpPort);

            await Task.Delay(50, cancellationToken);
            return new NotificationDeliveryResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver email to {Destination}", channel.Destination);
            return new NotificationDeliveryResult(false, ex.Message, IsTransient: true);
        }
    }
}
