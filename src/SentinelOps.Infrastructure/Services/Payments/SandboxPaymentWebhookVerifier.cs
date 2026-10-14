using System.Security.Cryptography;
using System.Text;
using SentinelOps.Application.Common.Interfaces;

namespace SentinelOps.Infrastructure.Services.Payments;

public class SandboxPaymentWebhookVerifier : IPaymentWebhookVerifier
{
    public (bool IsValid, string? Error, long Timestamp) VerifySignature(
        string payload,
        string? signatureHeader,
        string secret,
        int toleranceSeconds = 300)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return (false, "Missing signature header.", 0);
        }

        if (string.IsNullOrEmpty(secret))
        {
            return (false, "Webhook secret key is not configured.", 0);
        }

        long timestamp = 0;
        string? signatureHash = null;

        var items = signatureHeader.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var item in items)
        {
            var kv = item.Split('=', 2);
            if (kv.Length != 2) continue;

            if (kv[0].Equals("t", StringComparison.OrdinalIgnoreCase) && long.TryParse(kv[1], out var t))
            {
                timestamp = t;
            }
            else if (kv[0].Equals("v1", StringComparison.OrdinalIgnoreCase))
            {
                signatureHash = kv[1];
            }
        }

        if (timestamp <= 0 || string.IsNullOrWhiteSpace(signatureHash))
        {
            return (false, "Malformed signature header. Expected format 't={timestamp},v1={hash}'.", timestamp);
        }

        // Timestamp tolerance check
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now - timestamp > toleranceSeconds)
        {
            return (false, $"Webhook timestamp expired. Event timestamp: {timestamp}, current: {now}, tolerance: {toleranceSeconds}s.", timestamp);
        }

        if (timestamp - now > 60)
        {
            return (false, "Webhook timestamp is in the future.", timestamp);
        }

        // Compute HMAC-SHA256 of "{timestamp}.{payload}"
        var signedPayload = $"{timestamp}.{payload}";
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(signedPayload);

        using var hmac = new HMACSHA256(keyBytes);
        var computedHash = hmac.ComputeHash(payloadBytes);
        var computedHex = Convert.ToHexString(computedHash).ToLowerInvariant();

        var expectedBytes = Encoding.UTF8.GetBytes(computedHex);
        var providedBytes = Encoding.UTF8.GetBytes(signatureHash.ToLowerInvariant());

        if (expectedBytes.Length != providedBytes.Length || !CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes))
        {
            return (false, "Signature verification failed. Forged or mismatched HMAC-SHA256 signature.", timestamp);
        }

        return (true, null, timestamp);
    }

    public string ComputeSignatureHeader(string payload, string secret, long? timestamp = null)
    {
        var ts = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{ts}.{payload}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var hex = Convert.ToHexString(hash).ToLowerInvariant();

        return $"t={ts},v1={hex}";
    }
}
