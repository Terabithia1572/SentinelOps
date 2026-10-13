using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SentinelOps.Domain.Entities;
using SentinelOps.Domain.Enums;
using SentinelOps.Infrastructure.Security;

namespace SentinelOps.Infrastructure.Services.Notifications;

public class WebhookNotificationProvider : INotificationProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookNotificationProvider> _logger;

    public WebhookNotificationProvider(
        HttpClient httpClient,
        ILogger<WebhookNotificationProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public NotificationChannelType SupportedType => NotificationChannelType.Webhook;

    public async Task<NotificationDeliveryResult> SendAsync(
        NotificationOutbox message,
        NotificationChannel channel,
        CancellationToken cancellationToken)
    {
        var (isAllowed, reason) = await SsrfValidator.ValidateDestinationUrlAsync(channel.Destination);
        if (!isAllowed)
        {
            _logger.LogWarning("Webhook destination {Destination} blocked by SSRF filter: {Reason}", channel.Destination, reason);
            return new NotificationDeliveryResult(false, $"Blocked by SSRF filter: {reason}", IsTransient: false);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, channel.Destination);
            request.Content = new StringContent(message.PayloadJson, Encoding.UTF8, "application/json");

            // Attach HMAC signature if secret configured
            if (!string.IsNullOrWhiteSpace(channel.WebhookSecret))
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(channel.WebhookSecret));
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message.PayloadJson));
                string signature = Convert.ToHexString(hash).ToLowerInvariant();
                request.Headers.Add("X-SentinelOps-Signature", $"sha256={signature}");
            }

            request.Headers.Add("User-Agent", "SentinelOps-NotificationEngine/1.0");

            // Attach optional custom headers
            if (!string.IsNullOrWhiteSpace(channel.CustomHeadersJson))
            {
                try
                {
                    var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(channel.CustomHeadersJson);
                    if (headers != null)
                    {
                        foreach (var (k, v) in headers)
                        {
                            request.Headers.TryAddWithoutValidation(k, v);
                        }
                    }
                }
                catch
                {
                    // Ignore malformed custom headers
                }
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _httpClient.SendAsync(request, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                return new NotificationDeliveryResult(true);
            }

            bool isTransient = (int)response.StatusCode >= 500 || response.StatusCode == System.Net.HttpStatusCode.RequestTimeout;
            return new NotificationDeliveryResult(false, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}", isTransient);
        }
        catch (TaskCanceledException ex)
        {
            return new NotificationDeliveryResult(false, $"Timeout delivering webhook: {ex.Message}", IsTransient: true);
        }
        catch (HttpRequestException ex)
        {
            return new NotificationDeliveryResult(false, $"HTTP request error: {ex.Message}", IsTransient: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error delivering webhook to {Destination}", channel.Destination);
            return new NotificationDeliveryResult(false, ex.Message, IsTransient: false);
        }
    }
}
