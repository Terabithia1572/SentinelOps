using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using SentinelOps.Infrastructure.Security;

namespace SentinelOps.Infrastructure.Services.Notifications;

public class SlackNotificationProvider : INotificationProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SlackNotificationProvider> _logger;

    public SlackNotificationProvider(
        HttpClient httpClient,
        ILogger<SlackNotificationProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public NotificationChannelType SupportedType => NotificationChannelType.Slack;

    public async Task<NotificationDeliveryResult> SendAsync(
        NotificationOutbox message,
        NotificationChannel channel,
        CancellationToken cancellationToken)
    {
        var (isAllowed, reason) = await SsrfValidator.ValidateDestinationUrlAsync(channel.Destination);
        if (!isAllowed)
        {
            _logger.LogWarning("Slack destination {Destination} blocked by SSRF filter: {Reason}", channel.Destination, reason);
            return new NotificationDeliveryResult(false, $"Blocked by SSRF filter: {reason}", IsTransient: false);
        }

        try
        {
            var slackPayload = new
            {
                text = $"{message.Subject}: {message.Message}",
                blocks = new object[]
                {
                    new
                    {
                        type = "header",
                        text = new
                        {
                            type = "plain_text",
                            text = message.Subject.Length > 150 ? message.Subject[..147] + "..." : message.Subject,
                            emoji = true
                        }
                    },
                    new
                    {
                        type = "section",
                        text = new
                        {
                            type = "mrkdwn",
                            text = message.Message
                        }
                    },
                    new
                    {
                        type = "context",
                        elements = new object[]
                        {
                            new
                            {
                                type = "mrkdwn",
                                text = $"*Channel:* {channel.Name} | *Status:* {message.Status} | *Time:* <!date^{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}^{{date_num}} {{time_secs}}|{DateTime.UtcNow:u}>"
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(slackPayload);
            using var request = new HttpRequestMessage(HttpMethod.Post, channel.Destination)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("User-Agent", "SentinelOps-NotificationEngine/1.0 (Slack)");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _httpClient.SendAsync(request, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                return new NotificationDeliveryResult(true);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cts.Token);
            bool isTransient = (int)response.StatusCode >= 500 || response.StatusCode == System.Net.HttpStatusCode.RequestTimeout;
            return new NotificationDeliveryResult(false, $"Slack HTTP {(int)response.StatusCode}: {responseBody}", isTransient);
        }
        catch (TaskCanceledException ex)
        {
            return new NotificationDeliveryResult(false, $"Timeout delivering to Slack: {ex.Message}", IsTransient: true);
        }
        catch (HttpRequestException ex)
        {
            return new NotificationDeliveryResult(false, $"HTTP request error: {ex.Message}", IsTransient: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error delivering to Slack destination {Destination}", channel.Destination);
            return new NotificationDeliveryResult(false, ex.Message, IsTransient: false);
        }
    }
}
