using Microsoft.Extensions.Diagnostics.HealthChecks;
using SentinelOps.Application.Common.Interfaces;

namespace SentinelOps.Infrastructure.Services;

public class TelemetryBufferHealthCheck : IHealthCheck
{
    private readonly ITelemetryBuffer _buffer;

    public TelemetryBufferHealthCheck(ITelemetryBuffer buffer)
    {
        _buffer = buffer;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_buffer.RequireDurableStorage)
        {
            if (!_buffer.IsDurableStorageActive)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Durable Redis stream telemetry buffer is disconnected while production fail-closed policy is active.",
                    data: new Dictionary<string, object>
                    {
                        ["requireDurableStorage"] = true,
                        ["isDurableStorageActive"] = false
                    }));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                "Durable Redis stream telemetry buffer is connected and operational.",
                data: new Dictionary<string, object>
                {
                    ["requireDurableStorage"] = true,
                    ["isDurableStorageActive"] = true
                }));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "Telemetry buffer is active (fallback in-memory mode enabled).",
            data: new Dictionary<string, object>
            {
                ["requireDurableStorage"] = false,
                ["isDurableStorageActive"] = _buffer.IsDurableStorageActive
            }));
    }
}
