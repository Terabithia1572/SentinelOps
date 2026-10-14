using FluentAssertions;
using SentinelOps.Infrastructure.Services.Payments;
using Xunit;

namespace SentinelOps.Application.Tests.Payments;

public class SandboxPaymentWebhookVerifierTests
{
    private readonly SandboxPaymentWebhookVerifier _verifier = new();
    private const string Secret = "whsec_test_secret_key_1234567890abcdef";

    [Fact]
    public void VerifySignature_ValidSignatureAndCurrentTimestamp_ReturnsTrue()
    {
        var payload = "{\"id\":\"evt_123\",\"type\":\"checkout.session.completed\"}";
        var header = _verifier.ComputeSignatureHeader(payload, Secret);

        var (isValid, error, timestamp) = _verifier.VerifySignature(payload, header, Secret, toleranceSeconds: 300);

        isValid.Should().BeTrue();
        error.Should().BeNull();
        timestamp.Should().BeGreaterThan(0);
    }

    [Fact]
    public void VerifySignature_ForgedSignature_ReturnsFalse()
    {
        var payload = "{\"id\":\"evt_123\",\"type\":\"checkout.session.completed\"}";
        var header = "t=1700000000,v1=deadbeefcafebabe0123456789abcdef0123456789abcdef0123456789abcdef";

        var (isValid, error, _) = _verifier.VerifySignature(payload, header, Secret, toleranceSeconds: 300);

        isValid.Should().BeFalse();
        error.Should().Contain("expired");
    }

    [Fact]
    public void VerifySignature_TamperedPayload_ReturnsFalse()
    {
        var payloadOriginal = "{\"id\":\"evt_123\",\"amount\":100}";
        var payloadTampered = "{\"id\":\"evt_123\",\"amount\":1000}";
        var header = _verifier.ComputeSignatureHeader(payloadOriginal, Secret);

        var (isValid, error, _) = _verifier.VerifySignature(payloadTampered, header, Secret, toleranceSeconds: 300);

        isValid.Should().BeFalse();
        error.Should().Contain("Signature verification failed");
    }

    [Fact]
    public void VerifySignature_ExpiredTimestamp_ReturnsFalse()
    {
        var payload = "{\"id\":\"evt_expired\"}";
        var oldTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 350; // 350 seconds ago (> 300 tolerance)
        var header = _verifier.ComputeSignatureHeader(payload, Secret, oldTimestamp);

        var (isValid, error, _) = _verifier.VerifySignature(payload, header, Secret, toleranceSeconds: 300);

        isValid.Should().BeFalse();
        error.Should().Contain("expired");
    }

    [Fact]
    public void VerifySignature_FutureTimestampBeyond60Seconds_ReturnsFalse()
    {
        var payload = "{\"id\":\"evt_future\"}";
        var futureTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 120; // 2 minutes in future
        var header = _verifier.ComputeSignatureHeader(payload, Secret, futureTimestamp);

        var (isValid, error, _) = _verifier.VerifySignature(payload, header, Secret, toleranceSeconds: 300);

        isValid.Should().BeFalse();
        error.Should().Contain("future");
    }

    [Fact]
    public void VerifySignature_MissingOrMalformedHeader_ReturnsFalse()
    {
        var payload = "{\"id\":\"evt_bad\"}";

        var (isValid1, error1, _) = _verifier.VerifySignature(payload, null, Secret);
        isValid1.Should().BeFalse();
        error1.Should().Contain("Missing signature");

        var (isValid2, error2, _) = _verifier.VerifySignature(payload, "invalid_header", Secret);
        isValid2.Should().BeFalse();
        error2.Should().Contain("Malformed");
    }
}
