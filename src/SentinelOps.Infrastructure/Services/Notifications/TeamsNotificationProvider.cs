using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using SentinelOps.Infrastructure.Security;

namespace SentinelOps.Infrastructure.Services.Notifications;

public class TeamsNotificationProvider : INotificationProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TeamsNotificationProvider> _logger;

    public TeamsNotificationProvider(
        HttpClient httpClient,
        ILogger<TeamsNotificationProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public NotificationChannelType SupportedType => NotificationChannelType.Teams;

    public async Task<NotificationDeliveryResult> SendAsync(
        NotificationOutbox message,
        NotificationChannel channel,
        CancellationToken cancellationToken)
    {
        var (isAllowed, reason) = await SsrfValidator.ValidateDestinationUrlAsync(channel.Destination);
        if (!isAllowed)
        {
            _logger.LogWarning("Teams destination {Destination} blocked by SSRF filter: {Reason}", channel.Destination, reason);
            return new NotificationDeliveryResult(false, $"Blocked by SSRF filter: {reason}", IsTransient: false);
        }

        try
        {
            var teamsPayload = new
            {
                type = "message",
                attachments = new object[]
                {
                    new
                    {
                        contentType = "application/vnd.microsoft.card.adaptive",
                        contentUrl = (string?)null,
                        content = new
                        {
                            schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                            type = "AdaptiveCard",
                            version = "1.4",
                            body = new object[]
                            {
                                new
                                {
                                    type = "TextBlock",
                                    size = "Medium",
                                    weight = "Bolder",
                                    text = message.Subject
                                },
                                new
                                {
                                    type = "TextBlock",
                                    text = message.Message,
                                    wrap = true
                                },
                                new
                                {
                                    type = "FactSet",
                                    facts = new object[]
                                    {
                                        new { title = "Channel", value = channel.Name },
                                        new { title = "Timestamp (UTC)", value = DateTime.UtcNow.ToString("u") }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(teamsPayload);
            using var request = new HttpRequestMessage(HttpMethod.Post, channel.Destination)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("User-Agent", "SentinelOps-NotificationEngine/1.0 (Teams)");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _httpClient.SendAsync(request, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                return new NotificationDeliveryResult(true);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cts.Token);
            bool isTransient = (int)response.StatusCode >= 500 || response.StatusCode == System.Net.HttpStatusCode.RequestTimeout;
            return new NotificationDeliveryResult(false, $"Teams HTTP {(int)response.StatusCode}: {responseBody}", isTransient);
        }
        catch (TaskCanceledException ex)
        {
            return new NotificationDeliveryResult(false, $"Timeout delivering to Teams: {ex.Message}", IsTransient: true);
        }
        catch (HttpRequestException ex)
        {
            return new NotificationDeliveryResult(false, $"HTTP request error: {ex.Message}", IsTransient: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error delivering to Teams destination {Destination}", channel.Destination);
            return new NotificationDeliveryResult(false, ex.Message, IsTransient: false);
        }
    }
}
