using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SentinelOps.Application.Common.Interfaces;
using SentinelOps.Domain.Entities;

namespace SentinelOps.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "newpassword", "confirmpassword", "token", "accesstoken",
        "refreshtoken", "secret", "apikey", "key", "authorization", "rawtoken",
        "privatekey", "clientsecret", "connectionstring", "bearer", "cookie", "credentials"
    };

    public AuditService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task RecordAsync(
        string action,
        string targetEntityType,
        string targetEntityId,
        string outcome = "Success",
        string? details = null,
        string? applicationScope = null,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var correlationId = httpContext?.TraceIdentifier;
            var userAgent = httpContext?.Request?.Headers.UserAgent.ToString();
            var ipAddress = _currentUserService.IpAddress;

            string? metadataJson = null;
            if (metadata != null)
            {
                metadataJson = SanitizeMetadata(metadata);
            }

            var auditEvent = new AuditEvent
            {
                UserId = _currentUserService.UserId ?? "system",
                UserName = _currentUserService.UserName ?? "system",
                Action = action,
                EntityType = targetEntityType,
                EntityId = targetEntityId,
                ApplicationScope = applicationScope,
                Outcome = outcome,
                CorrelationId = correlationId,
                IpAddress = ipAddress,
                UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : (userAgent.Length > 500 ? userAgent[..500] : userAgent),
                Details = details ?? $"{action} on {targetEntityType}:{targetEntityId}",
                MetadataJson = metadataJson,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditEvents.Add(auditEvent);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist audit event for action {Action} on {EntityType}:{EntityId}", action, targetEntityType, targetEntityId);
        }
    }

    private static string SanitizeMetadata(object metadata)
    {
        try
        {
            var rawJson = JsonSerializer.Serialize(metadata);
            var node = JsonNode.Parse(rawJson);
            if (node is JsonObject obj)
            {
                SanitizeNode(obj);
                return obj.ToJsonString();
            }
            return rawJson;
        }
        catch
        {
            return "{}";
        }
    }

    private static void SanitizeNode(JsonObject obj)
    {
        var propertyNames = obj.Select(kvp => kvp.Key).ToList();
        foreach (var prop in propertyNames)
        {
            if (SensitiveKeys.Contains(prop))
            {
                obj[prop] = "[REDACTED]";
            }
            else if (obj[prop] is JsonObject childObj)
            {
                SanitizeNode(childObj);
            }
        }
    }
}
