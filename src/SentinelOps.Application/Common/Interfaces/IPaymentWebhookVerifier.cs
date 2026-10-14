namespace SentinelOps.Application.Common.Interfaces;

public interface IPaymentWebhookVerifier
{
    (bool IsValid, string? Error, long Timestamp) VerifySignature(
        string payload,
        string? signatureHeader,
        string secret,
        int toleranceSeconds = 300);

    string ComputeSignatureHeader(string payload, string secret, long? timestamp = null);
}
